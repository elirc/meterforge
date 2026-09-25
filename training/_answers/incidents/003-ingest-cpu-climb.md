# INC-003 — sealed answer: implicit conversion (nvarchar parameter vs varchar column) kills the dedupe seek

## Root cause
`new SqlParameter("@tenant", tenantId)` with a `string` value infers **`nvarchar`** (SqlDbType.NVarChar, size = string length).
`tenant_id` is `varchar(64)` with the database collation `SQL_Latin1_General_CP1_CI_AS`, and `external_event_id` is `varchar(128) COLLATE Latin1_General_100_BIN2`.
`nvarchar` has higher data-type precedence, so SQL Server converts the **column**: `CONVERT_IMPLICIT(nvarchar(64), [tenant_id], 0) = @tenant`.
With a SQL collation, converted varchar values don't sort like the nvarchar values, so the optimizer **can't seek** on
`ux_usage_events_tenant_external`: it scans an index (or the table) and converts every row. Cost grows with table size, which is why p95
rose a little every day. The plan also shows the warning *"Type conversion in expression (CONVERT_IMPLICIT...) may affect CardinalityEstimate / SeekPlan"*.

(With a Windows collation such as `Latin1_General_100_CI_AS` SQL Server can often still do a range seek via `GetRangeThroughConvert`; the SQL collation is the worst case. That's why "it was fine on my machine" happens.)

The EF LINQ version was fine because EF types parameters from the column mapping (`IsUnicode(false)`, `HasMaxLength(64)`) → `varchar(64)`.

## Bonus finding: the PR didn't achieve its goal
`WITH (UPDLOCK)` in a standalone `SELECT` is released when the statement's implicit transaction commits, i.e. immediately. The lookup and
the later insert are separate transactions, so two deliveries can still both miss. (It's also not a range lock: without `HOLDLOCK` there's
no key-range lock on a key that doesn't exist yet.) The unique index `ux_usage_events_tenant_external` is what actually prevents duplicates;
`AddAsync` already maps 2601/2627. So the PR added cost and no protection. Old-AI smell: a locking hint as a comment-driven "fix".

## Diagnostic path
1. CPU-bound (RUNNABLE, `SOS_SCHEDULER_YIELD`), not lock waits; one statement dominates in Query Store; its logical reads per execution (~42k) ≈ an index scan.
2. Actual plan of the dedupe lookup: Index Scan (or Clustered Index Scan) with a `CONVERT_IMPLICIT` predicate and a yellow warning on the SELECT operator.
3. Look at parameter types in the plan's `ParameterList` / the `sp_executesql` call: `@tenant nvarchar(11)`.
4. Compare with the pre-PR plan: Index Seek on `ux_usage_events_tenant_external`, 3–6 logical reads.

## Fix
Revert to the LINQ query (preferred), or type the parameters:
```csharp
new SqlParameter("@tenant", SqlDbType.VarChar, 64) { Value = tenantId },
new SqlParameter("@external", SqlDbType.VarChar, 128) { Value = externalEventId }
```
and drop the misleading `UPDLOCK` (rely on the unique index + 2601/2627 handling; if you truly need serialization, do lookup + insert in one
transaction with `UPDLOCK, HOLDLOCK`, and accept the blocking cost).

## Regression guard
- Real-SQL-Server test: run the lookup against the seeded table and assert logical reads < 20 (`SqlConnection.StatisticsEnabled = true`, `RetrieveStatistics()["SelectRows"/"BytesReceived"]` isn't reads; use `SET STATISTICS IO` output via `InfoMessage`, or query `sys.dm_exec_query_stats` for the tag).
- A repo check (grep/analyzer in CI) for `new SqlParameter(` without an explicit `SqlDbType` in `Infrastructure.SqlServer`.
- Query Store alert on the `usage-dedupe-lookup` tag's avg logical reads.

## Lab reproduction
**Offline-verified:** `evidence/dedupe-lookup-sql.txt`: EF LINQ sends `@tenantId varchar(64)`, `@externalEventId varchar(128)`; the PR's `FromSqlRaw` sends `@tenant nvarchar(11)`, `@external nvarchar(12)` (`SqlDbType: NVarChar`).
**UNVERIFIED (needs a live SQL Server):** the scan, the `CONVERT_IMPLICIT` warning and the read counts. After `SqlLab seed`, run both queries from that file with `SET STATISTICS IO ON` and `SET STATISTICS XML ON`.

## Postmortem (model)
- **Impact:** 4 days of steadily degrading ingestion, p95 up to 2.4 s at 96% DB CPU; SDK retries added load; no data loss.
- **Root cause:** a raw SQL rewrite passed strings as `nvarchar`, forcing conversion of `varchar` columns and an index scan per lookup.
- **Why not caught:** the query returned correct results; tests ran on empty tables; no plan review for a hot-path query.
- **Actions:** revert; typed-parameter rule; plan evidence for hot-path SQL changes; performance test on seeded data.
