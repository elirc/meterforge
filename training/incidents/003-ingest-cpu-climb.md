# INC-003 — Ingestion latency climbing day by day, database CPU pinned

**Branch:** `training/incidents/003-ingest-cpu-climb` (based on `training/lab/sqlserver`)

**Alert (Friday 08:40 UTC)**
```
[P2] POST /api/v1/tenants/{tenantId}/usage-events p95 2,380 ms (baseline 18 ms)   SQL CPU 96% for 20 min
```

**What on-call saw**
- The p95 has risen every day since Monday's release, not all at once. It tracks the size of `billing.usage_events` (+8M rows/week).
- Replays and new events are both slow. The usage page (`usage-summary`) is slow only when ingestion is busy.
- `sys.dm_exec_requests` during the spike: dozens of sessions running the same statement, few waiting on locks, most `RUNNABLE`/`SOS_SCHEDULER_YIELD`.
- No schema change this week. The release contained the PR below.

**Top statement by CPU (from Query Store, last 24 h)**
```
executions  avg_cpu_ms  avg_logical_reads  query_text (first 120 chars)
   2.9M        61.4          41,880         -- usage-dedupe-lookup
                                             SELECT TOP(1) [e].[seq], [e].[external_event_id], ... FROM (SELECT * FROM billing.usage_events WITH (UPD
   0.3M         0.6              14          INSERT INTO [billing].[usage_events] ...
```
Previous week, same query tag: avg_cpu_ms 0.2, avg_logical_reads 6.

**Suspect PR (merged Monday)**: `fix(usage): lock the dedupe lookup to close the double-ingest window`
```diff
-        var row = await db.UsageEvents.AsNoTracking()
-            .TagWith("usage-dedupe-lookup")
-            .Where(e => e.TenantId == tenantId && e.ExternalEventId == externalEventId)
-            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
+        // UPDLOCK so two deliveries of the same event serialize on the key instead of both missing it.
+        var row = await db.UsageEvents
+            .FromSqlRaw(
+                "SELECT * FROM billing.usage_events WITH (UPDLOCK) WHERE tenant_id = @tenant AND external_event_id = @external",
+                new SqlParameter("@tenant", tenantId),
+                new SqlParameter("@external", externalEventId))
+            .AsNoTracking()
+            .TagWith("usage-dedupe-lookup")
+            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
```

**Your job.** Show the actual plan for the dedupe lookup before and after the PR (the lab container with `SqlLab seed` has ~1M rows; use
`SqlLab ingest <tenant> <id>` and `sql/02-plan-cache.sql`), explain every number in the Query Store row above, fix it, and decide whether the
PR's original goal was even achieved. Write the postmortem.

Sealed: [`_answers/incidents/003-ingest-cpu-climb.md`](../_answers/incidents/003-ingest-cpu-climb.md)
