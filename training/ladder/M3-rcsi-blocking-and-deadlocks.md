# M3 — Readers, writers and the rollup job: blocking, RCSI, and reading a deadlock graph

**Context.** Three workloads share `billing.usage_events` on the lab branch:
- **Writer:** `SqlServerUsageEventStore` (dedupe lookup, then insert), about 400 inserts/s at peak.
- **Rollup job:** `UsageRollupJob.RunOnceAsync`: one transaction that claims up to 50k rows (`UPDATE TOP (@batch) ... OUTPUT INTO #claimed`) and `MERGE`s them into `usage_daily_rollups` `WITH (HOLDLOCK)`.
- **Reader:** `UsageReadModel.SummarizeAsync` (the customer usage page).

The lab database runs **READ COMMITTED with locking** (boxed SQL Server default). Azure SQL Database defaults to **READ COMMITTED SNAPSHOT (RCSI)**.

**Ticket.**
> Finance wants the rollup job to run every minute with 50k-row batches. Before we agree: show me what the job does to the
> usage page and to ingestion while it runs, under both isolation settings, and teach the team to read a deadlock graph.

**Constraints.** Measure with `SqlLab soak --seconds 60` (writers + rollup loop) while running `SqlLab summary tenant-0001 --repeat 50`
in a second terminal. Use `sql/03-blocking-and-deadlocks.sql`. Flip RCSI with the `ALTER DATABASE` line in that script.

**Definition of done.**
- [ ] Blocking chain captured (Part A) with lock-wait types, **with RCSI OFF**: who blocks whom, on which resource.
- [ ] Summary latency p50/max during the soak, RCSI OFF vs ON.
- [ ] Explanation of what RCSI changes for the **reader**, and what it does **not** change for writer vs rollup (both write).
- [ ] Deadlock drill: in two `sqlcmd` sessions, reproduce a deadlock by hand between "insert event then update rollup row" and "update rollup row then update event", read the graph from Part B, and label: victim, each process's owned lock and requested lock, resource (keylock on which index), and lock modes.
- [ ] A recommendation: batch size, lock order rule, and whether the job needs `HOLDLOCK`.

## Explain before touching
1. Under locking READ COMMITTED, which locks does `SummarizeAsync` take and for how long? Which rows can the rollup's `UPDATE` block it on?
2. RCSI reads the last committed version from the version store in `tempdb`. What does that cost, and what new anomaly can a reader see?
3. Why is `HOLDLOCK` (serializable) on the `MERGE` there? What race does it prevent in "match, else insert"?
4. Why does `UPDATE TOP (50000)` risk lock escalation, and what does escalation to a table lock do to the writers?

<details><summary>Hints</summary>

- Lock escalation is attempted at about 5,000 locks on one object in one statement.
- A deadlock needs a cycle: A holds X, wants Y; B holds Y, wants X. Collect "holds" and "wants" from the `<resource-list>`.
</details>

Answer: [`_answers/M3.md`](../_answers/M3.md)
