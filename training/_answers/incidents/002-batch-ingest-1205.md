# INC-002 — sealed answer: deadlock between the batch writer and the rollup job

## Root cause
The new `SqlServerBatchUsageIngestor` keeps **one transaction open for the whole batch** (up to 100 events), and inside it interleaves
*inserts* with *dedupe reads*. The rollup job (`UsageRollupJob`) runs one transaction that claims un-rolled rows with
`UPDATE TOP (@batch) ... SET rolled_up = 1 ... WHERE rolled_up = 0 AND occurred_at < @cutoff`.

The cycle (READ COMMITTED with locking):
1. **Writer** inserts new event E1 (SDK flush: `occurred_at` a few minutes old, i.e. before the job's cutoff) and holds an **X** key lock on E1's rows
   (clustered `seq` key and the filtered `ix_usage_events_unrolled` entry) until the batch commits.
2. **Rollup** claims rows in `occurred_at` order through `ix_usage_events_unrolled`: it has already updated (X-locked) event R, and now needs a
   **U** lock on E1 to evaluate it → **waits for the writer**.
3. **Writer** processes the next item, a *retry* of R (SDK retry of an event sent seconds ago). The dedupe lookup seeks
   `ux_usage_events_tenant_external` (not locked by the job, since `rolled_up` isn't in that index), then does a **Key Lookup** into the clustered
   index to fetch the full row (`FirstOrDefault` selects all columns) and needs an **S** lock on R's clustered key, which the job holds X → **waits for the job**.
4. Cycle → SQL Server kills the cheaper transaction (usually the writer, sometimes the job) with **1205**.

Neither piece alone deadlocks: the single-event endpoint holds its insert lock only for one short implicit transaction and never reads after writing;
the job alone is fine. The end-of-hour flush (big batches, many retries of just-sent events, `occurred_at` minutes old) maximizes the overlap.
The reads that matter are replays: that's why the error rate tracks `replays/min`.

## How to read the graph (Part B of `sql/03-blocking-and-deadlocks.sql`)
- `<victim-list>`: the killed process id.
- `<process-list>`: two processes; their `<inputbuf>` shows `-- usage-dedupe-lookup ... SELECT TOP(1)` (writer) and `UPDATE TOP (@batch) billing.usage_events SET rolled_up = 1` (job); `trancount`, `isolationlevel="read committed (2)"`.
- `<resource-list>`: two `keylock`s on `billing.usage_events`: one on `PK_usage_events` (the clustered `seq` key) and one on
  `PK_usage_events` or `ix_usage_events_unrolled`. Each has an `<owner-list>` (mode X) and a `<waiter-list>` (mode S for the writer's lookup,
  U for the job's scan). Draw it: process A owns key 1, waits for key 2; process B owns key 2, waits for key 1.

## Fix options (and the one to pick)
1. **Don't hold a transaction across the batch** (pick this): dedupe + insert per event in short transactions; "all-or-nothing" isn't needed because each event is idempotent by `external_event_id`: a retry of a half-applied batch replays the stored half and inserts the rest. That was the PR's stated goal, and idempotency already gives it.
2. If atomicity is truly required: do all reads first (one set-based lookup `WHERE external_event_id IN (...)`), then all inserts (`SaveChanges` once, EF batches them), so the transaction never reads after it writes, and keep it short.
3. Make the lookup not need the clustered row: project only `id` (a covering read on the unique index, possibly with `INCLUDE`), so it never touches the key the job locked. Helps, but fragile.
4. **RCSI**: the writer's read would read the last committed version of R instead of waiting → breaks this cycle. It doesn't fix writer-vs-writer conflicts, and it changes semantics for every read in the database (plan it, don't flip it during an incident).
5. Retry on 1205 with jitter in both the endpoint and the job: always add, never the only fix.
6. The job: smaller batches (≤ 4,000 rows to avoid lock escalation), and `READPAST` on the claim (`UPDATE TOP ... WITH (READPAST)`) so it skips rows locked by in-flight writers and picks them up next run: removes the job's wait entirely.

Chosen: 1 + 6 + retry on 1205.

## Proof
`SqlLab soak --seconds 60 --writers 6 --batch 20 --replay-percent 20` on the incident branch vs the fixed branch: count of `sql-1205` errors
and at least one captured `xml_deadlock_report` (save them in `training/lab/sqlserver/evidence/`). UNVERIFIED so far: see the note below.

## Lab reproduction
**UNVERIFIED (needs a live SQL Server):** no deadlock was captured while writing this. Reproduce: `SqlLab seed` (optional), then on the incident branch `SqlLab soak --seconds 60 --writers 6 --batch 20 --replay-percent 30`, and the lab branch with `--batch 1` as control; then `sql/03-blocking-and-deadlocks.sql` Part B for the graph. The lock analysis above is derived from the code and SQL Server's documented locking under READ COMMITTED; if your graph shows different resources (e.g. a page lock after escalation, or the filtered index instead of the clustered key), trust the graph and explain the difference.

## Postmortem (model)
- **Impact:** 4.8% of batch requests failed at the top of each hour for 2 days; retries tripled traffic; rollups lagged up to 9 min. No data loss (failed batches were rolled back and retried).
- **Root cause:** a long transaction that writes then reads, meeting a job that holds row locks in a different order.
- **Why not caught:** tests are single-threaded; the soak harness existed but nobody ran it with the new endpoint and the job together.
- **Actions:** per-event transactions; job batch ≤ 4k + `READPAST`; 1205 retry; deadlock count alert from `system_health`; soak run in the PR checklist for anything touching `usage_events` locking.
