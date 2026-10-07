# Learn MeterForge: a study guide for a junior developer (B08)

> **Status note (2026-10-06).** This guide was written on 2026-09-26 against `main` @ `43ee18d`. Two things have changed since then:
> - **Usage ingestion is fixed.** Commit `4e3f344` replaced find-then-`TryAdd` with an atomic `GetOrAddAsync`, normalized the key first, and returns 409 on a changed payload. The "phantom id" bug that sessions 1–2 and on-ramp 4–5 study now exists only at `43ee18d`, so do those exercises in a worktree of that commit (on-ramp 5 shows the command). On today's `main`, `Ingest_NormalizedReplay_ReturnsOriginalIdentity` is the test that "flipped".
> - **The lab and incident branches are not on GitHub.** `git ls-remote origin` lists only `main`. From a fresh clone, on-ramp 6–7 and sessions 3, 5–8 can be read but not run. See the status note in [`training/README.md`](../training/README.md).

## What this app is and why it's in the portfolio
MeterForge is a .NET 10 usage-metering and billing API: tenants send usage events, and those events are rated in tiers and turned into invoices.
On `main` everything lives in memory (`src/MeterForge.Api/Program.cs:31-39` on today's `main`; `27-35` at `43ee18d`, `src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs`).
It's in the portfolio as the **SQL Server lab**. The branch `training/lab/sqlserver` adds a real EF Core SQL Server adapter
(`src/MeterForge.Infrastructure.SqlServer/`), a load harness (`tools/MeterForge.SqlLab`) and five SQL Server incidents, as described in `training/README.md`.
Billing is where a lost event or a wrong cent is a customer complaint, so what you practise here is getting correctness right, not just getting the code to run.

> **Runtime status: UNVERIFIED.** Nobody has run the lab against a live SQL Server yet. Docker broke on this laptop (its WSL distro is gone), and LocalDB
> isn't installed. The code builds and the generated SQL is real tool output, but every plan, timing and deadlock is still expected behaviour,
> not something anyone has seen (`training/lab/sqlserver/evidence/README.md`).

### When Docker is fixed: the exact path
1. Open Docker Desktop and choose **Troubleshoot → Restart** (or **Reset to factory defaults**; everything inside Docker was already wiped).
   Check with `wsl -l -v` (a `docker-desktop` distro must be listed) and `docker ps` (it must answer without hanging).
2. Start SQL Server 2022 on port 14337 with the throwaway lab password:
   ```powershell
   docker run -d --name meterforge-training-mssql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=MeterForge#Lab2026" -e MSSQL_PID=Developer -e MSSQL_MEMORY_LIMIT_MB=2048 -p 14337:1433 mcr.microsoft.com/mssql/server:2022-latest
   ```
   You can also run `. .\training\lab\sqlserver\start-sqlserver.ps1` instead. It runs the same container, and it also creates the `MeterForge` database at **compatibility level 150** and sets
   `METERFORGE_SQLSERVER` / `METERFORGE_SQLSERVER_TESTS` for your session. INC-001 depends on level 150.
3. Work through the checklist table in `training/lab/sqlserver/evidence/README.md` from top to bottom. Save your outputs in `evidence/` and replace each "UNVERIFIED" with your numbers.
   **Once that table is done, the lab is verified instead of just built.** Do it before session 4 below if you can.

## Prerequisites (fundamentals first)
| # | You need | Where to learn it |
|---|---|---|
| 1 | C# records, `decimal` vs `double`, `async/await` basics | Read `src/MeterForge.Domain/Common/Money.cs` and `src/MeterForge.Domain/Rating/TieredRating.cs`, then do onramp 1 and 2 |
| 2 | ASP.NET Core minimal APIs and dependency injection (singleton vs scoped) | `src/MeterForge.Api/Program.cs:22-39`, `src/MeterForge.Api/Usage/UsageEndpoints.cs`. Then INC-001 in the sibling `globehire` repo is the deep dive on lifetimes |
| 3 | SQL basics: primary key, unique index, what an index seek is | curriculum 10: plans and indexes (apprenticeship page `10-query-plans-and-indexes`, not in this repo) |
| 4 | How SQL Server differs from Postgres (collation, `varchar`/`nvarchar`, `decimal(p,s)`) | curriculum 22: SQL Server for Postgres developers (apprenticeship page `22-sqlserver-for-postgres-devs`, not in this repo) |
| 5 | Idempotency: why the same event may arrive twice | curriculum 02: idempotency (apprenticeship page `02-idempotency`, not in this repo) |
| 6 | Checking code you didn't write | curriculum 05: verify before trust (apprenticeship page `05-verify-before-trust`, not in this repo) |

Money precision (INC-005) also uses curriculum 20: money and time (apprenticeship page `20-money-and-time`, not in this repo).

**Rules for every session:** `main`'s working tree holds your own uncommitted work in the Usage area. Never switch it to another branch.
For branches, use `git worktree add ..\mf-<name> <branch>`. For a clean copy of `main`, use `git worktree add --detach ..\mf-main HEAD`.

## Study route (10 sessions, about 12 hours, plus the rest of `training/` afterwards)
Legend: **[HAND]** means you do it yourself. **[AGENT]** means you may hand it to a coding agent, and then you verify what it produced.

| # | Session (time) | What you do | Exit criterion (you can check it) |
|---|---|---|---|
| 1 | **Read and trace** (60 min) | [HAND] `learn/00-onramp.md` exercises 1–4: Money, TieredRater, then trace `POST /usage-events` end to end. [AGENT] You may ask an agent "list every file a usage event passes through", then check its list against your own trace. | You can draw the request path (endpoint → service → store) from memory, and you predicted onramp 4 correctly *before* checking. |
| 2 | **First test** (60 min) | [HAND] Onramp 5: write the characterization test that pins the phantom-id behaviour, in a detached worktree. [AGENT] You may let an agent write the xUnit boilerplate, but you write the assertion. | The test **passes on `43ee18d`** (it pins that commit's behaviour; on today's `main` the bug is fixed and the test would fail), and you can explain in one sentence why that behaviour is a bug. |
| 3 | **Schema on the lab branch** (60 min) | [HAND] Onramp 6–7: read `MeterForgeDbContext.cs` on `training/lab/sqlserver` and generate the migration script offline. Then read `training/navigation/ARCHITECTURE.md` and `FIRST_CHANGE.md`. | You can explain the clustered key and the four indexes on `usage_events` and why `external_event_id` has a BIN2 collation, without notes. |
| 4 | **Blind review** (90 min) | [HAND] `training/review/EXERCISE.md` (60 min, no agent), then `COMPARE.md`, then `_answers/review.md`. | At least 8 findings with `path:line`. `COMPARE.md` is filled in *before* you open the answer. |
| 5 | **Run it on SQL Server: J1** (60–90 min) | Needs Docker (see above). [HAND] The "Explain before touching" answers in `training/ladder/J1-run-meterforge-on-sqlserver.md`. [AGENT] Container start-up and HTTP calls can be scripted by an agent. You read the output. | Two POSTs give 201, then 200 with `isReplay: true`. One row in `billing.usage_events`. **No Docker yet?** Do J1's explain-first questions and the J1 answer's reading instead, and mark the session "UNVERIFIED". |
| 6 | **Read a real plan: J2** (60–90 min) | [HAND] `ladder/J2-read-an-actual-execution-plan.md`. Read the plan XML yourself. An agent may *explain operators you don't know*, but it doesn't interpret the plan for you. | You can name the operator for the small tenant (seek + key lookup) and for the whale (scan), with logical reads for each. |
| 7 | **Money and collation incidents: INC-005, INC-004** (90 min) | [HAND] Diagnosis and the postmortem (`training/incidents/005-*.md`, `004-*.md`). [AGENT] Once *you* have named the layer that changed the value, the fix and the regression test can go to an agent under the checklist in `learn/01-agentic-practice.md`. | For INC-005 you can say which layer truncated 27.7666 to 27.76 and prove it (offline evidence: `evidence/decimal-mapping.txt`). Your regression test fails on the incident branch. |
| 8 | **Implicit conversion and sniffing: INC-003, INC-001** (90 min) | [HAND] Read `ladder/M2-*.md` first, then diagnose both incidents from the briefs alone. | You can explain `CONVERT_IMPLICIT` and "one plan compiled for the whale, reused for everyone" in 3 lines each. |
| 9 | **Agent drill** (90 min) | `learn/01-agentic-practice.md`, drill A or B. [HAND] Spec + adversarial review. [AGENT] Implementation. | Your review finds at least one problem in the agent's PR, or you can show evidence that there was none (test output, not a feeling). |
| 10 | **Defend it** (60 min) | [HAND] `training/interview/TECHNICAL_QUESTIONS.md` questions 5–13 aloud, then `MOCK_DEFENSE.md`. | You can answer "why would a 1M-row table suddenly get slow for small tenants?" in under 2 minutes, without notes. |

After session 10, continue with the rest of `training/README.md` in its suggested order: M1, M3 + INC-002, M4, S1, S2, and the tradeoff drills.

## Do vs. delegate: the rule for this repo
- **Always by hand:** reading plans and deadlock graphs, deciding *which layer* changed a value, writing the spec, reviewing the diff, and writing the postmortem.
  An agent will always come up with a plausible cause. What you're learning here is noticing when the evidence doesn't support it.
- **Fine to delegate:** xUnit boilerplate, EF migrations (`dotnet dotnet-ef migrations add`), SqlLab command plumbing, and turning your notes into a tidy postmortem.
- **Never accept without re-running:** any number (logical reads, p95, row counts) an agent reports. Re-run `bench-sniff` or `sql/01..04` yourself.
  `training/agentic/WORKFLOW.md` step 5 says the same.

## How you know you're done here
- All 7 onramp exercises and 3 agent drills done, each checked against `learn/_answers/`.
- The `evidence/README.md` checklist run on a live SQL Server with your own numbers. Until Docker is fixed, this stays open, and that's fine.
- You can explain all 5 incidents, each in one sentence of mechanism (not vocabulary).

**Next repo:** `atlas` (github.com/elirc/atlas: the other SQL Server practice in the competency graph: its M1 query work plus an Azure deploy design).
Its release and migration material builds on S1 here. If you want more .NET DI and time-handling fundamentals first, do the sibling `globehire` repo.
