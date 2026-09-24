# Agentic exercise — M2 (covering index + removing `EF.Constant`) through an implementer and an adversarial reviewer

Ticket: [`ladder/M2-covering-filtered-indexes-and-sniffing.md`](../ladder/M2-covering-filtered-indexes-and-sniffing.md). Chosen because an agent
will happily "add an index" and "remove EF.Constant for plan reuse" in one PR, and both claims need database evidence to be true.

## Step 1 — You write the spec and design note (30 min, no agent)
`training/agentic/my-spec.md` (create it), max 1 page:
- the index: key columns, INCLUDE columns, online build, the migration name
- whether `EF.Constant` goes, and the evidence that will justify it (M2's four `bench-sniff` runs)
- what must not change: the API response of `usage-summary`, the rollup job's plan
- the acceptance numbers: logical reads for tenant-0001 and tenant-whale, before/after

## Step 2 — Implementer agent
Run in a worktree off `training/lab/sqlserver`. Prompt:
> Implement the spec in `training/agentic/my-spec.md` on this branch. Change only `MeterForgeDbContext.cs`, `UsageReadModel.cs`, one new EF migration,
> and `tools/MeterForge.SqlLab` if a command is missing. Generate the migration with `dotnet dotnet-ef migrations add`. Then run against the container at
> `METERFORGE_SQLSERVER`: `migrate`, `bench-sniff --first tenant-whale`, `bench-sniff --first tenant-0001`, and `sql/01-usage-summary-plan.sql` via sqlcmd.
> Paste the raw outputs. Don't summarize numbers you didn't measure.

## Step 3 — Adversarial reviewer (a different model if you have one)
> You are reviewing a database change for a usage-billing system. Only these categories: correctness, architecture, security, performance,
> tests, failure handling. No style comments. For each claim in the PR description, find the evidence in the pasted output or say "unverified".
> Specifically check: (1) does `dotnet ef migrations script` show `ONLINE = ON`, and what happens on an edition without online index builds;
> (2) is the INCLUDE list exactly what `SummarizeAsync` needs; (3) did the benchmark flush the plan cache between runs; (4) what is the write
> cost of the new index on the ingestion path, and was it measured; (5) is the old `ix_usage_events_tenant_occurred` now redundant, and is
> dropping it in the same migration safe for other queries; (6) does the Down() migration restore the previous state.

## Step 4 — Revision
Feed the reviewer's findings back to the implementer. Only fixes; no new scope.

## Step 5 — Verification (you, not an agent)
- [ ] Run the tests: `dotnet test tests/MeterForge.SqlServer.Tests` with `METERFORGE_SQLSERVER_TESTS` set: 4+ passed, 0 skipped.
- [ ] Read the whole diff. Is anything changed outside the allowed files?
- [ ] Re-run `bench-sniff` yourself. Do your numbers match the agent's within noise?
- [ ] Check claims against code: open the generated migration and the SQL script, not just the C#.
- [ ] Look for fake or placeholder behaviour: a benchmark that doesn't flush, a test that asserts nothing, a `TODO` in `Down()`.
- [ ] Reversible: `dotnet dotnet-ef database update InitialSqlServer` succeeds, then `update` forward again.

## Step 6 — Your explanation, graded by the reviewer
Write 10–15 lines: why the plan differed per tenant before, why it no longer matters after the covering index, what the index costs, and when
you'd put `EF.Constant` (or `OPTION (RECOMPILE)`) back. Give it to the reviewer with:
> Grade this explanation 1–5 on mechanism (not vocabulary). Ask me two follow-up questions where the explanation is vague.

Rubric: [`_answers/agentic-rubric.md`](../_answers/agentic-rubric.md)
