# SEALED: MeterForge agent-drill rubrics

Root causes are in `training/_answers/incidents/00N-*.md`. They aren't repeated here. These rubrics grade **your spec and your review**, not the agent.
Score each drill out of 10. Passing is 7. Anything that needs a live SQL Server is UNVERIFIED until you run it (see `training/lab/sqlserver/evidence/README.md`).

## A. INC-005 (decimal truncation)
**Spec (4 pts)**
- 1: names the layer. EF's default `decimal(18,2)` on `ProrationCreditRow.Amount` + **SqlClient truncating** a parameter to scale 2 (not rounding). Cites `evidence/decimal-mapping.txt`.
- 1: the model fix is explicit precision (e.g. `HasPrecision(19, 6)`), or rounding half-even *before* persisting into `decimal(19,2)`, with the inputs stored as provenance.
- 1: the class guard: `ConfigureConventions` → `Properties<decimal>().HavePrecision(...)` **and/or** a model test that fails on any decimal without precision, **and/or**
  `ConfigureWarnings(w => w.Throw(SqlServerEventId.DecimalTypeDefaultWarning))`.
- 1: data repair is in scope. 1,318 invoices get credit/debit notes, and finalized invoices are never edited.

**Review: traps an agent typically falls into (6 pts, 1 each you caught or proved absent)**
- Changes the *read* rounding (e.g. `AwayFromZero`) so the example gets 27.77 while storage still truncates → wrong for other values, and a mismatch with the provider's half-even rule.
- Adds `Math.Round` in the ledger but leaves the column `decimal(18,2)` **without** a guard → the next decimal property repeats the bug.
- Edits the existing migration instead of adding a new one (breaks any database already migrated).
- A test that asserts the domain rounding (`decimal.Round(27.7666m,…)`) and never goes through EF/SQL → it proves nothing about storage. The incident's own unit test already did that and passed.
- Migration `Down()` missing or a `TODO`.
- Claims "tests pass" while `MeterForge.SqlServer.Tests` were **skipped** (no server).

## B. INC-003 (implicit conversion)
**Spec (4)**: nvarchar parameter vs varchar column → `CONVERT_IMPLICIT` → no seek (1). Fix: revert to LINQ (EF types from the mapping) or `SqlDbType.VarChar` with sizes 64/128 (1).
Drop `WITH (UPDLOCK)`: it's released at the end of its own implicit transaction and protects nothing. The unique index + 2601/2627 is the real guard (1).
Guard: CI grep/analyzer for `new SqlParameter(` without `SqlDbType` in `Infrastructure.SqlServer`, or a Query Store alert on the `usage-dedupe-lookup` tag (1).
**Review (6)**: the agent keeps `UPDLOCK` "for safety" (1). It types the parameter as `NVarChar` or leaves the size off (1). It adds `HOLDLOCK` and creates the blocking INC-002 teaches (1).
It shows C# but not the SQL/parameter list (1). No proof of a seek: needs a live server, so accept "UNVERIFIED" plus the exact `SET STATISTICS IO` command (1).
It removes the `TagWith("usage-dedupe-lookup")`, which is your Query Store handle (1).

## C. INC-002 (deadlock)
**Spec (4)**: a correct two-process, two-key cycle: the writer holds X on a new row and waits for S on a replayed row's clustered key, while the job holds X on that key and waits for U on the writer's row (1).
Primary fix: no transaction spanning the batch. Each event is idempotent by `external_event_id`, so atomicity isn't needed (1).
Supplements: `READPAST` + smaller batches on the job's claim, and retry on 1205 with jitter (1). The acceptance number comes from `SqlLab soak`, against a control run (1).
**Review (6)**: retry as the only change (1). RCSI flipped on during an incident without discussing what it does to every read (1). No soak output → "unverified" (1).
The job batch left large enough to escalate locks (> ~5,000 rows) (1). The fix touches the single-event endpoint, which never deadlocked: out of scope (1).
The batch's "all-or-nothing" promise changed without saying so in the PR text (1).
UNVERIFIED: B07 derived the cycle from code and documented locking. If your graph shows other resources, the graph wins.

## D. INC-004 (collation)
**Spec (4)**: restore `UseCollation("Latin1_General_100_BIN2")` in a **new** migration (1). Fix the reporting join with `COLLATE` on the join or on the temp-table column,
because tempdb's collation is the real conflict (1). Recovery: lost events were never stored. Rebuild them from the customer's send log + our logs of
`isReplay=true` responses whose request id differs in case from the returned one, re-ingest, then issue adjustments for closed periods (1). A real-SQL-Server regression test with
two ids that differ only by case (1).
**Review (6)**: you read the generated SQL, not only the C# (1). The index rebuild's `ONLINE` behaviour and edition limits are noted (1). The agent "recovers" by querying
`usage_events` (impossible: the rows were never written) (1). The agent drops `ux_usage_events_tenant_external` to make the migration pass (1). No `Down()` (1).
The test uses EF InMemory, which compares ordinally and can't reproduce a collation bug (1).

## What "good" looks like overall
Your review found at least one real problem, or listed the evidence that ruled each trap out. "The agent did well" with no evidence scores 0 for review.
