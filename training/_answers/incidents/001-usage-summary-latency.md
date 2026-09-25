# INC-001 — sealed answer: parameter-sniffing regression in the usage summary

## Root cause
`UsageReadModel.SummarizeAsync` filters `tenant_id = X AND occurred_at in [from, to)` and needs `subscription_id`, `meter`, `quantity`,
which `ix_usage_events_tenant_occurred (tenant_id, occurred_at)` doesn't contain. The best plan depends on how many rows the tenant has:
- small tenant (~50 rows in the week): **Index Seek** on `ix_usage_events_tenant_occurred` + **Key Lookup** per row (cheap);
- `tenant-whale` (~115k rows in the week): **Clustered Index Scan** of the whole table (a lookup per row would cost more).

Before the PR, `EF.Constant(tenantId)` put the tenant in the SQL text as a literal, so each tenant had its own plan (hence 38k plans, the DBA's complaint).
The PR made the tenant a parameter: **one** cached plan for all tenants, compiled with whatever tenant ran first after the plan was (re)compiled.
Monday 16:30 → Tuesday 02:10 the plan happened to be compiled for a small tenant (whale slower but tolerable, nobody noticed).
The 02:10 failover emptied the plan cache; the first call was the whale's overnight job → a scan plan was cached → every small tenant now scans ~1M rows (reads and CPU ×100+).
The "deploy was fine for 10 hours" and "failover" details are the classic signature: the regression depends on **which value compiles first**.

## Diagnostic path a senior takes
1. Latency up for small tenants only, CPU up, no blocking → a plan problem, not locking.
2. `sql/02-plan-cache.sql`: one row for `-- usage-summary`, huge `execution_count`, `avg_logical_reads` far above what a 50-row tenant needs.
3. Open the cached plan XML → `ParameterCompiledValue` for the tenant parameter = `'tenant-whale'`; the operator is a Clustered Index Scan.
4. Confirm by forcing a recompile for one execution (`EXEC sp_recompile 'billing.usage_events'` or `DBCC FREEPROCCACHE (plan_handle)`) and letting a small tenant run first: small tenants recover, the whale gets slower. That's sniffing.
5. Read the PR again: it turned a literal into a parameter. That's the trigger; the missing covering index is the cause.

## Fix (in order of preference)
1. **Covering index** so one plan is right for every tenant (M2):
   ```csharp
   usage.HasIndex(e => new { e.TenantId, e.OccurredAt })
       .IncludeProperties(e => new { e.SubscriptionId, e.Meter, e.Quantity })
       .HasDatabaseName("ix_usage_events_tenant_occurred_cover")
       .IsCreatedOnline();
   ```
   Seek + range scan: small tenant reads a handful of pages, the whale reads only its week. Then keeping the tenant as a parameter is safe, and the DBA gets plan reuse.
2. If an index isn't possible yet: restore `EF.Constant` (per-tenant plans), or add `OPTION (RECOMPILE)` via an interceptor/`FromSql` (CPU per call ~1–5 ms for compilation), or `OPTIMIZE FOR (@tenantId UNKNOWN)` (average-density plan: stable but not optimal).
3. Emergency mitigation during the incident: evict the bad plan (`DBCC FREEPROCCACHE (plan_handle)`) or force the good plan with Query Store (`sp_query_store_force_plan`). Both are band-aids.

Note: at compatibility level 160 (SQL Server 2022 / Azure SQL default for new databases), **Parameter Sensitive Plan optimization** can keep several plan variants for one query based on the tenant's cardinality bucket, which would mask this. The lab database runs at 150, like many migrated production databases. Don't rely on PSPO as the fix: it only covers equality predicates on skewed columns with a qualifying histogram.

## Regression guard
- A test on the lab branch (real SQL Server, J3 pattern) that seeds 1 whale + small tenants, runs the whale first, then asserts a small tenant's `SET STATISTICS IO` logical reads stay below a bound (e.g. < 200), via `SqlConnection.StatisticsEnabled`/`RetrieveStatistics()` or a `sys.dm_exec_query_stats` read.
- Query Store "regressed queries" alert on `usage-summary` (tag makes it findable).

## Lab reproduction
**Offline-verified:** `training/lab/sqlserver/evidence/usage-summary-sql.txt` shows the only change on the wire: `'tenant-0001'` literal (lab) became `@_8__locals1_tenant varchar(64)` (incident branch).
**UNVERIFIED (needs a live SQL Server):** the plan flip and timings. Run `SqlLab seed`, then `SqlLab bench-sniff --first tenant-whale` and `SqlLab bench-sniff --first tenant-0001` on the incident branch, and the same on the lab branch as control. Expected: whale-first makes small-tenant p50 jump by one to two orders of magnitude on the incident branch only. Server: see `evidence/README.md`.

## Postmortem (model)
- **Impact:** 11 h of 1.4–1.9 s usage pages for all small tenants; DB CPU 70%; no data impact.
- **Trigger:** plan-cache cleanup PR changed a literal to a parameter; the 02:10 failover made the whale compile first.
- **Root cause:** a query whose optimal plan depends on the parameter value, without an index that makes one plan good for all values.
- **Why not caught:** tests run on tiny data; no test runs the query at realistic skew; the PR had no plan evidence.
- **Actions:** covering index (online); performance test with skewed data in CI for tagged hot queries; Query Store regressed-query alert; PR template asks for before/after plans on query changes.
