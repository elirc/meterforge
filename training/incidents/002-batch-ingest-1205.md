# INC-002 — Batch ingestion failing with error 1205 at the top of every hour

**Branch:** `training/incidents/002-batch-ingest-1205` (based on `training/lab/sqlserver`)

**Alert (Thursday 10:01 UTC)**
```
[P1] usage-ingest error rate 4.8% (threshold 1%)   POST /api/v1/tenants/{tenantId}/usage-events:batch   5xx 212/min
```

**What on-call saw**
- Errors cluster in the first 5 minutes of each hour (SDKs flush their hourly buffers) and fade after.
- The rollup job's dashboard shows occasional failed runs too, and `usage_daily_rollups` lagging up to 9 minutes behind.
- SDK retries turn each failed batch into 2–3 more requests; request volume at :00–:05 is 3× normal.
- Nothing changed in the database. The only deploy this week is the PR below (Tuesday). The single-event endpoint (`POST .../usage-events`) has no errors.

**Application log**
```
10:00:42 ERR usage-events:batch tenant=tenant-0012 items=100 failed after 1843ms
  Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
   ---> Microsoft.Data.SqlClient.SqlException (0x80131904): Transaction (Process ID 71) was deadlocked on lock resources
        with another process and has been chosen as the deadlock victim. Rerun the transaction.
10:00:43 ERR usage-events:batch tenant=tenant-0007 items=100 failed after 2210ms
   ---> Microsoft.Data.SqlClient.SqlException (0x80131904): Transaction (Process ID 64) was deadlocked on lock resources ... Error Number:1205
10:00:44 WRN rollup-job run failed: SqlException 1205 (claimed 0, will retry in 60s)
```

**Metrics excerpt**
```
time    batch_req/min  batch_5xx/min  replays/min  rollup_runs_failed  lock_wait_ms/s
09:58          310            0          1.1k            0                  40
10:00        1,820          212          9.4k            2               2,900
10:03        1,410          118          7.7k            1               1,700
10:10          330            2          1.3k            0                  60
```

**Suspect PR (merged Tuesday)**: `feat(usage): atomic batch ingestion endpoint for SDK flushes`
```diff
+/// SDKs flush usage in batches of up to 100 events. A batch is all-or-nothing so a client retry
+/// never leaves half a batch behind.
+public sealed class SqlServerBatchUsageIngestor(MeterForgeDbContext db, TimeProvider time)
+{
+    public async Task<IReadOnlyList<UsageIngestionResult>> IngestBatchAsync(...)
+    {
+        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
+        foreach (var item in items)
+        {
+            var existing = await db.UsageEvents.AsNoTracking()
+                .Where(e => e.TenantId == tenantId && e.ExternalEventId == item.ExternalEventId)
+                .FirstOrDefaultAsync(cancellationToken);
+            if (existing is not null) { results.Add(/* replay */); continue; }
+            db.UsageEvents.Add(new UsageEventRow { ... });
+            await db.SaveChangesAsync(cancellationToken);
+            results.Add(/* created */);
+        }
+        await transaction.CommitAsync(cancellationToken);
```
(full diff: `git diff training/lab/sqlserver training/incidents/002-batch-ingest-1205`)

**Your job.** Get the deadlock graph out of SQL Server, name both processes, what each held and what each wanted, fix it, and prove the fix
with the soak harness (`SqlLab soak`, read its options). Write the postmortem.

Sealed: [`_answers/incidents/002-batch-ingest-1205.md`](../_answers/incidents/002-batch-ingest-1205.md)
