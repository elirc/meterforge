# S2 — `usage_events` at 100M rows: indexes, statistics, partitions, and the maintenance window you don't have

**Scenario.** The lab schema, in production on SQL Server, a year later. `billing.usage_events`: 100M rows, growing 8M per month,
clustered on `seq` (identity), plus `ux_usage_events_id`, `ux_usage_events_tenant_external`, `ix_usage_events_tenant_occurred` (with your M2 INCLUDEs),
and the filtered `ix_usage_events_unrolled`. Symptoms this month:
- The usage page is fast at 09:00 and slow by 17:00 for tenants who signed up this week.
- A nightly `ALTER INDEX ALL ... REBUILD` job now runs 3h40m and blocks ingestion (Standard edition, so no online rebuild).
- The rollup job's plan flips between a seek and a scan on `ix_usage_events_unrolled`.
- Retention policy: raw events are kept 13 months, then deleted; the delete job causes log growth of 60 GB.

**Write an operating plan (max 2 pages)** covering:
1. **Statistics.** Why ascending keys (`occurred_at`, new `tenant_id` values) make estimates wrong between updates; the automatic update threshold on a 100M-row table (compat 130+); what you schedule instead (`UPDATE STATISTICS ... WITH SAMPLE`, incremental statistics on partitions, `AUTO_UPDATE_STATISTICS_ASYNC`).
2. **Index maintenance.** When fragmentation matters for this workload (append-only clustered key vs random `ux_usage_events_id` GUID inserts), why "rebuild everything nightly" is wrong, and a threshold-based plan. Resumable online index rebuild if the edition allows it.
3. **Partitioning.** Partition function on `occurred_at` by month, aligned indexes, what happens to the unique indexes that don't contain `occurred_at`, and the sliding window (`SWITCH OUT` + `TRUNCATE ... WITH (PARTITIONS)`) that replaces the 60 GB delete.
4. **Rollups at scale.** Would a clustered columnstore on an archive table, or on `usage_daily_rollups`, help the finance reports? What does it cost the writers?
5. **Monitoring.** Query Store regressed-queries view, `sys.dm_db_stats_properties.modification_counter` alerts, index usage stats to find unused indexes, log reuse wait.
6. **Rollout.** How you introduce partitioning on a live 100M-row table without a long outage (new partitioned table + batched copy + swap, or rebuilding the clustered index onto the partition scheme online), and how you roll back.

**Constraints.** Ingestion SLO is p99 < 300 ms at all times. The maintenance window is 30 minutes on Sunday. Standard edition for the first half of the year, then Azure SQL (Business Critical).

## Explain before touching
- A unique index must contain the partitioning column to be aligned. What does that do to `ux_usage_events_tenant_external`, and so to dedupe?
- What does a GUID key do to page splits, and which of the lab's indexes has one?

Answer (reference plan + rubric): [`_answers/S2.md`](../_answers/S2.md)
