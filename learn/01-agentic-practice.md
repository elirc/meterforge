# MeterForge: agent-direction drills

These drills reuse the existing incidents. What's different: **you** write the spec, **an agent** writes the code, and **you** review it as an adversary
before you open the sealed answer. The skill is *directing and verifying*. Diagnosis is still yours. Finish the incident brief's
"explain before touching" questions first, or you'll have nothing to review the agent's work against.

Setup for every drill: a worktree of the incident branch, never `main`:
`git -C <your-meterforge-clone> worktree add ..\mf-drill-<n> training/incidents/<branch>`.
(As of 2026-10-06 the `training/incidents/*` branches are not on GitHub; see the status note in `training/README.md`. From a fresh clone, write the spec and review the agent's output against the brief, but you cannot run the drill code.)
Give the agent **only** the worktree path, your spec and the allowed files. Don't point it at `training/_answers/` or `learn/_answers/`.

---

## Drill A: INC-005, the missing cent (no SQL Server needed for most of it) · 75 min
Brief: `training/incidents/005-proration-reconciliation-drift.md`.
1. **[HAND] Spec (20 min).** Write `..\mf-drill-5\my-spec.md` (outside `training/`), one page at most:
   the layer you've proved truncates the value (cite `training/lab/sqlserver/evidence/decimal-mapping.txt`), the model change,
   a guard that stops *any* future `decimal` without precision, the migration, and what must not change (the half-even rule, the API shape).
2. **[AGENT] Implement.** Prompt skeleton:
   > In this worktree, implement `my-spec.md`. Allowed files: `src/MeterForge.Infrastructure.SqlServer/**`, one new EF migration generated with
   > `dotnet dotnet-ef migrations add`, and tests under `tests/MeterForge.SqlServer.Tests/` or a new model-only test. Show me the output of
   > `dotnet build MeterForge.slnx`, `dotnet test` and `dotnet dotnet-ef migrations script <previous> <new>`. Don't summarize output you didn't run.
3. **[HAND] Adversarial review (25 min).** Use the checklist at the bottom. Also ask: did it fix *storage*, or did it only move the rounding around?
4. Grade with `learn/_answers/01-agentic-practice.md` §A.

## Drill B: INC-003, the "locking fix" that burned CPU · 60 min
Brief: `training/incidents/003-ingest-cpu-climb.md`.
1. **[HAND] Spec.** State the mechanism in one line, the fix you chose (and the one you rejected), what happens to `WITH (UPDLOCK)` and why,
   and a guard that stops untyped `SqlParameter`s coming back.
2. **[AGENT] Implement.** Allowed: `src/MeterForge.Infrastructure.SqlServer/SqlServerUsageEventStore.cs`, one CI/test guard.
   Ask for the SQL that EF now sends for the dedupe lookup (the same probe style as `evidence/probe-dedupe.cs.txt`), so parameter types are *shown*, not claimed.
3. **[HAND] Review.** Before you accept, check the parameter type in the output (`varchar(64)` / `varchar(128)`). The C# can look right while the output says `nvarchar`.
4. Grade with §B.

## Drill C: INC-002, the deadlock (needs SQL Server; otherwise do the spec and review only) · 90 min
Brief: `training/incidents/002-batch-ingest-1205.md`. Agents almost always answer "add retry on 1205". Your spec must forbid retry as the *only* change.
1. **[HAND] Spec.** Draw the lock cycle (two processes, two keys) *from the brief and the code*: `SqlServerBatchUsageIngestor` on the branch and
   `UsageRollupJob.cs`. Choose a primary fix. State the acceptance number: `SqlLab soak --seconds 60 --writers 6 --batch 20 --replay-percent 30` shows `sql-1205=0`.
2. **[AGENT] Implement** the primary fix plus retry-with-jitter as a supplement. Have it paste the raw `soak` output for both the branch and the fix.
3. **[HAND] Review.** No soak output means the change is *unverified*, not "probably fine". If Docker is still down, your review verdict is
   "cannot accept: evidence missing", and that's the right answer.
4. Grade with §C.

## Drill D: INC-004, the collation cleanup (review a migration, not C#) · 60 min
Brief: `training/incidents/004-usage-undercount.md`.
1. **[HAND] Spec.** Restore case-sensitive dedupe, fix the reporting join the right way, and plan recovery of the lost usage. Recovery goes in the spec
   even though no code does it.
2. **[AGENT] Implement** the model and migration, plus the reporting-join fix as a SQL snippet in the PR description.
3. **[HAND] Review the generated migration SQL** (`migrations script`), not the C#. Is the index rebuild online? What happens if two rows that collide case-insensitively
   were stored *before* the fix? (They weren't: the unique index refused them. Explain why that matters.)
4. Grade with §D.

---

## Verification checklist: before you trust agent output in MeterForge
Tick each one with evidence (command output, or `path:line`), not with "looks fine".

**Scope and diff**
- [ ] `git diff --stat` touches only the files you allowed. Nothing under `src/MeterForge.Domain/` unless your spec said so.
- [ ] No edits in `main`'s working tree (it holds your own uncommitted Usage changes). `git -C <your-meterforge-clone> status` looks the same as before.

**Tests that actually test**
- [ ] `dotnet build MeterForge.slnx` is clean. `Directory.Build.props` treats warnings as errors, so a suppressed warning is a red flag.
- [ ] The new test **fails on the incident branch without the fix** and passes with it. Ask for both runs.
- [ ] SQL Server tests (`tests/MeterForge.SqlServer.Tests`) show **passed, not skipped**. They're env-gated, and "4 skipped" looks green in a summary.
- [ ] No test that only asserts `NotNull` or `> 0` on money. Amounts are asserted exactly (`27.77m`).

**Database invariants in this repo**
- [ ] `ux_usage_events_tenant_external` still exists and still covers `(tenant_id, external_event_id)`: the idempotency guarantee.
- [ ] `external_event_id` stays `COLLATE Latin1_General_100_BIN2` (`MeterForgeDbContext.cs`). Every `decimal` has explicit precision.
- [ ] Parameters that hit `varchar` columns are `varchar`. Check the SQL EF sends, not the C#.
- [ ] Every new migration has a real `Down()`, and its generated SQL (`dotnet dotnet-ef migrations script`) was read, including index drop/create and `ONLINE`.
- [ ] Money: rounding is half-even (`MidpointRounding.ToEven`), in the domain, once. Storage must not change the value (INC-005).

**Claims vs evidence**
- [ ] Every number in the PR description (reads, p95, 1205 count) matches output you can see. Re-run at least one yourself.
- [ ] Locking hints (`UPDLOCK`, `HOLDLOCK`, `READPAST`) come with a stated transaction scope. A hint in a single-statement implicit transaction protects nothing (INC-003 bonus).
- [ ] `/health/ready` claims (`src/MeterForge.Api/Program.cs:56-62` on today's `main`; `47-53` at `43ee18d`) aren't extended with things the code doesn't check.
- [ ] No placeholders: `TODO` in `Down()`, benchmarks that don't flush the plan cache, `catch { }` around SQL errors other than 2601/2627.

The full agent workflow for ladder M2 is in `training/agentic/WORKFLOW.md`, and its rubric is in `training/_answers/agentic-rubric.md`. Do it after these drills.
