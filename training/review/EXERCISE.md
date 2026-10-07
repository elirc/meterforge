# Blind review — MeterForge `main` (60 minutes)

You're the new senior on the billing team. MeterForge's README calls it "complete" at `v1.0.0`, and `docs/quality/PRODUCTION-READINESS.md`
lists what's missing. Assume a teammate proposes putting the usage-ingestion path in front of a real customer next month, on SQL Server.

**Scope.** Committed code at `43ee18d`, the commit the answer key grades against. Use `git show 43ee18d:<path>`, or a detached worktree of that commit. (Note added 2026-10-06: today's `main` already fixes the usage-ingestion race and the trim mismatch, so reviewing `HEAD` would hide findings 1–3.)
`src/**`, `tests/**`, `.github/workflows/ci.yml`, `docker-compose.yml`, and the two SQL migrations.
Second pass, 15 minutes: the lab branch `training/lab/sqlserver` (`src/MeterForge.Infrastructure.SqlServer/**`).
Don't read `docs/quality/FLAW-HARVEST.md` or `docs/sprints/sprint-14.md` first; they are spoilers.

**Output.** A list of findings, each with:
- severity: **S1** (money wrong / data lost or corrupted / security), **S2** (production incident likely), **S3** (maintainability or operability debt)
- evidence: `path:line`
- why it matters in production, in one sentence, **for a metering/billing product**
- the smallest fix, in one sentence

Aim for at least 12. Mark every finding that looks like a typical **old-AI-generated smell** (fake integrations, tests without real assertions,
hardcoded fallbacks, missing transaction boundaries, endpoints that look like features but aren't, docs or health checks claiming things the code doesn't do).

**Prompts if you get stuck** (don't read until 30 minutes in):
- Follow one usage event from HTTP to storage. What happens to `" evt_1"` vs `"evt_1"`? What happens when two identical events arrive at the same time?
- For each `Map*` endpoint in `src/MeterForge.Api`: where does its data come from, and where does it go?
- What does `/health/ready` actually check?
- Which CI step can never fail?

When done: fill in [`COMPARE.md`](COMPARE.md), then open [`../_answers/review.md`](../_answers/review.md).
