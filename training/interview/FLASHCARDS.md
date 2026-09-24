# Flashcards — MeterForge on SQL Server (25)

> Contains incident spoilers. Use this file only after you have worked incidents 001-005.

| # | Front | Back |
|---|---|---|
| 1 | Dedupe key for usage events | `(tenant_id, external_event_id)`, unique index `ux_usage_events_tenant_external` (lab `MeterForgeDbContext.cs`) |
| 2 | Error numbers for a unique violation | 2601 (unique index), 2627 (unique/PK constraint); caught in `SqlServerUsageEventStore.AddAsync` |
| 3 | Why `seq bigint identity` clusters `usage_events` | Append-only inserts, narrow 8-byte key in every nonclustered index; GUID `id` stays unique but nonclustered |
| 4 | `SQL_Latin1_General_CP1_CI_AS`: `'evt_aB3' = 'evt_Ab3'`? | True. CI = case-insensitive. Hence `UseCollation("Latin1_General_100_BIN2")` on `external_event_id` (incident 004) |
| 5 | `varchar` column vs `nvarchar` parameter | Column side gets `CONVERT_IMPLICIT`; with a SQL collation the index seek becomes a scan (incident 003) |
| 6 | What `new SqlParameter("@x", "abc")` sends | `nvarchar(3)`/`nvarchar(4000)`; set `SqlDbType.VarChar` + `Size` to match the column |
| 7 | EF default store type for an unconfigured `decimal` on SQL Server | `decimal(18,2)`, with a warning in the logs at startup (incident 005) |
| 8 | SqlClient decimal parameter with more scale than the parameter | Truncated, not rounded (verify in `training/lab/sqlserver/evidence/`) |
| 9 | T-SQL `CAST(27.765 AS decimal(19,2))` | Rounds → 27.77 |
| 10 | `datetimeoffset` comparison | By UTC instant; `02:00+02:00 = 00:00+00:00` |
| 11 | `CAST(occurred_at AS date)` on a `datetimeoffset` | Uses the stored **local** date; safe in MeterForge only because `UsageEvent` converts to UTC |
| 12 | Parameter sniffing | Plan compiled with the first parameter values seen, reused for all later values of that query text |
| 13 | `EF.Constant(x)` | Inlines `x` as a literal → one plan per value (plan cache growth) |
| 14 | Covering index for the usage summary | Key `(tenant_id, occurred_at)` + INCLUDE `(subscription_id, meter, quantity)`; seek + range scan for any tenant size |
| 15 | Filtered index gotcha | `WHERE rolled_up = 0` index can't serve `rolled_up = @p` (the plan must be valid for any value) |
| 16 | Lock escalation threshold | ~5,000 locks on one object in one statement → table lock; `UPDATE TOP (50000)` in the rollup job |
| 17 | RCSI in one line | Readers read the last committed row version from tempdb instead of waiting for writers' locks; writers still block writers |
| 18 | Where deadlock graphs live | `system_health` XE session, event `xml_deadlock_report` (`sql/03-blocking-and-deadlocks.sql`) |
| 19 | Deadlock recipe | Two transactions take the same two resources in opposite order; incident 002: batch insert + dedupe read vs rollup claim |
| 20 | Why `HOLDLOCK` on the rollup `MERGE` | Serializable range lock so "not matched → insert" can't race another insert of the same key |
| 21 | `Include` vs `AsSplitQuery` | Single query repeats parent columns per child; split = N round trips, consistent only in a transaction; matters with 2+ collections |
| 22 | `AsNoTracking` on the dedupe lookup | Skips change-tracker snapshots; lookups never modify |
| 23 | Plan cache query for a tagged EF query | `sys.dm_exec_query_stats` + `dm_exec_sql_text` filtered on the `TagWith` comment (`sql/02-plan-cache.sql`) |
| 24 | PSP optimization | Compat level 160: multiple plan variants for one parameterized query by predicate cardinality; can mask sniffing |
| 25 | Azure SQL vs boxed SQL Server defaults that matter here | RCSI on by default, Query Store on, latest compat level for new DBs |
