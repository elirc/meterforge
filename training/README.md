# MeterForge training pack (B07 apprenticeship, LAB tier) — focus: real SQL Server + EF Core

MeterForge is a .NET 10 usage-billing teaching repo whose running app stores everything in memory. This pack adds a **runnable SQL Server
track**: a SQL Server 2022 container, a branch with an EF Core SQL Server adapter and migrations, and five SQL Server incidents (code on branches; live reproduction pending, see below).

**Verification status:** everything that needs a live SQL Server is marked UNVERIFIED, with the exact command to run it (`lab/sqlserver/evidence/README.md`). Docker failed on the authoring machine before the container could start; the code builds and the generated SQL/DDL is real tool output.

> **Status note (2026-10-06): read this before starting.**
> 1. **The branches below are not on GitHub.** `git ls-remote origin` lists only `main` (plus closed-PR refs). The `training/lab/sqlserver` and `training/incidents/*` branches, and the lab commit `d7a4fd6` cited in `_answers/`, exist only in the author's local clone. From a fresh clone, every step that says `git worktree add … training/…` or `git show training/lab/sqlserver:…` will fail. In a clone you can still do the reading, review, navigation, design (S1/S2), interview and explain-first parts. Treat lab and incident steps as reading exercises until those branches are pushed.
> 2. **This pack describes `main` @ `43ee18d`, not today's `main`.** It was written on 2026-09-24/26. On 2026-09-30, commit `4e3f344` repaired usage ingestion: atomic `GetOrAddAsync` instead of find-then-`TryAdd`, normalization before the key is used, `409` on a changed payload, a tenant-scoped GET, and a trace-header fix. It also moved lines in `Program.cs`: registrations went from 27–35 to 31–39, and readiness from 47–53 to 56–62. Where this pack says "`TryAdd` result ignored", "phantom id" or cites `UsageIngestionService.cs:19`, it describes `43ee18d`. Read those files with `git show 43ee18d:<path>`. [`_answers/review.md`](_answers/review.md) records which findings that commit closed.

Nothing here changes `main`'s source. SQL Server code lives only on branches:

| Branch | What |
|---|---|
| `training/lab/sqlserver` | The lab: `MeterForge.Infrastructure.SqlServer` (EF Core 10 + migrations), opt-in API wiring, `tools/MeterForge.SqlLab` harness, SQL Server integration tests |
| `training/incidents/001-usage-summary-latency` | INC-001 (on top of the lab branch) |
| `training/incidents/002-batch-ingest-1205` | INC-002 |
| `training/incidents/003-ingest-cpu-climb` | INC-003 |
| `training/incidents/004-usage-undercount` | INC-004 |
| `training/incidents/005-proration-reconciliation-drift` | INC-005 |

Check a branch out with `git worktree add ..\mf-<name> <branch>`; never switch `main`'s working tree (it has your uncommitted work).

## Contents
- `lab/sqlserver/`: setup (`README.md`, `start-sqlserver.ps1`, `stop-sqlserver.ps1`), T-SQL scripts (`sql/01..04`), captured reference output (`evidence/`: open only after the exercise).
- `ladder/`: J1 run on SQL Server · J2 read an actual plan · J3 real-database tests · M1 Postgres→SQL Server port review · M2 covering/filtered indexes + parameter sniffing · M3 RCSI, blocking, deadlock graphs · M4 EF Core translation · S1 Azure SQL migration plan · S2 100M rows maintenance.
- `incidents/`: five on-call briefs (symptoms only).
- `review/`: 60-minute blind review of `main` + `COMPARE.md`.
- `navigation/`: architecture with evidence, first-change onboarding, five "Why?" trade-off drills, a change request (usage caps).
- `agentic/`: M2 run through an implementer agent and an adversarial reviewer.
- `interview/`: stories, 20 questions, system design, mock defense, 25 flashcards (spoilers: after the incidents).
- `_answers/`: sealed. One file per exercise; incidents in `_answers/incidents/`.

## Suggested order (≈ 30–36 hours)
| # | Item | Time |
|---|---|---|
| 1 | `navigation/ARCHITECTURE.md`, `FIRST_CHANGE.md` | 1 h |
| 2 | `review/EXERCISE.md` (+ COMPARE) | 1.5 h |
| 3 | `lab/sqlserver/README.md` setup, then **J1** | 1.5 h |
| 4 | **J2** (read the plans yourself before opening `evidence/`) | 1.5 h |
| 5 | **M1** port review | 2.5 h |
| 6 | **INC-004**, **INC-005** (collation, money) | 3 h |
| 7 | **J3** tests (now you know why they matter) | 1.5 h |
| 8 | **M2** + **INC-001** + **INC-003** (plans, sniffing, implicit conversion) | 5 h |
| 9 | **M3** + **INC-002** (locking, deadlocks) | 4 h |
| 10 | **M4** EF translation | 2 h |
| 11 | `agentic/WORKFLOW.md` (M2 via agents) | 2 h |
| 12 | `navigation/TRADEOFFS.md`, `CHANGE_REQUEST.md` | 2 h |
| 13 | **S1**, **S2** design docs | 4 h |
| 14 | Interview kit + mock defense | 3 h |

Shared concepts (don't re-learn them here). These are pages in the author's separate, unpublished apprenticeship workspace:
10 plans and indexes (apprenticeship page `10-query-plans-and-indexes`, not in this repo),
07 zero-downtime migrations (apprenticeship page `07-zero-downtime-migrations`, not in this repo),
03 concurrency (apprenticeship page `03-concurrency-races`, not in this repo),
05 verify before trust (apprenticeship page `05-verify-before-trust`, not in this repo).

Existing learning material in this repo: `astraupskill/` (committed to `main` in `4e3f344`), `docs/sprints`, `docs/curriculum`, `LEARNER-GUIDE.md`, `TESTREADME.md`.
This pack doesn't duplicate them: they teach the domain and the in-memory design; this pack teaches the database underneath.
