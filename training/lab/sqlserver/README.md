# MeterForge on SQL Server: the lab track

MeterForge ships with in-memory adapters and one Npgsql adapter that nothing wires up. This lab adds a real
**SQL Server 2022 + EF Core 10** persistence layer for usage metering. It lives only on the branch **`training/lab/sqlserver`**
and is never merged to `main`.

> **Status (2026-09-24):** the branches build and the non-database tests pass, but Docker broke on the authoring machine before the container
> started, so `start-sqlserver.ps1`, `SqlLab migrate/seed/soak/bench-sniff` and the SQL Server tests have **not yet been run against a live server**.
> `evidence/README.md` lists every UNVERIFIED check with its exact command and expected result. Running them is your first job (J1).

## What the lab branch adds (read these files; they are what the exercises talk about)

| Path on `training/lab/sqlserver` | What it is |
|---|---|
| `src/MeterForge.Infrastructure.SqlServer/MeterForgeDbContext.cs` | EF Core model: `billing.usage_events` (bigint identity clustered key, GUID `id` unique), `usage_daily_rollups`, `invoices`/`invoice_lines`, `outbox_messages` (JSON check constraint, filtered index) |
| `.../Migrations/*_InitialSqlServer.cs` | The generated migration. Read it next to `src/MeterForge.Infrastructure/Persistence/Migrations/001_invoices.sql` and `002_outbox.sql` (Postgres) |
| `.../SqlServerUsageEventStore.cs` | `IUsageEventStore` on EF Core: dedupe lookup + insert, unique-violation (2601/2627) handling |
| `.../UsageReadModel.cs` | `SummarizeAsync`: the per-tenant usage summary query that the plan exercises use |
| `.../UsageRollupJob.cs` | Raw T-SQL rollup: claim rows (`UPDATE ... OUTPUT INTO #claimed`), then `MERGE` into `usage_daily_rollups` |
| `.../SqlServerInvoiceSnapshotStore.cs` | `IInvoiceSnapshotStore` on EF Core (the SQL Server twin of `PostgresInvoiceSnapshotStore`) |
| `src/MeterForge.Api/Program.cs`, `Usage/UsageSummaryEndpoints.cs` | Opt-in wiring: set `ConnectionStrings__MeterForgeSqlServer` and the API stores usage in SQL Server and exposes `GET /api/v1/tenants/{tenantId}/usage-summary` |
| `tools/MeterForge.SqlLab/Program.cs` | Harness: `migrate`, `seed`, `summary`, `bench-sniff`, `rollup`, `ingest`, `soak` |
| `tests/MeterForge.SqlServer.Tests/` | Integration tests against a real SQL Server; skipped unless `METERFORGE_SQLSERVER_TESTS` is set |
| `dotnet-tools.json` | Local (not global) `dotnet-ef` 10.0.12 |

## Setup (about 10 minutes, most of it the first image pull)

```powershell
# 1. Container (port 14337, throwaway SA password, compat level 150). Sets env vars for this PowerShell session.
. .\training\lab\sqlserver\start-sqlserver.ps1

# 2. Check the lab branch out in its own worktree. Never switch main's working tree.
git worktree add ..\meterforge-sqllab training/lab/sqlserver
cd ..\meterforge-sqllab
dotnet tool restore

# 3. Schema, then data: ~1M usage events (one 900k-row "whale" tenant, 300 small tenants of 400 rows each).
dotnet run --project tools/MeterForge.SqlLab -- migrate
dotnet run --project tools/MeterForge.SqlLab -- seed

# 4. Run the API on SQL Server.
$env:ConnectionStrings__MeterForgeSqlServer = $env:METERFORGE_SQLSERVER
dotnet run --project src/MeterForge.Api
```

Interactive SQL: `docker exec -it meterforge-training-mssql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'MeterForge#Lab2026' -d MeterForge`.
Azure Data Studio and SSMS also work against `localhost,14337`, and they draw the graphical plan from the XML that the scripts below capture.

## Scripts in this folder

| Script | Used by |
|---|---|
| `sql/01-usage-summary-plan.sql` | J2, M2: `SET STATISTICS IO, TIME` + actual plan XML for the usage summary, small tenant vs whale |
| `sql/02-plan-cache.sql` | M2, INC-001: what is in the plan cache for the usage summary, and with which sniffed values |
| `sql/03-blocking-and-deadlocks.sql` | M3, INC-002: live blocking chain + deadlock graphs from the `system_health` session |
| `sql/04-index-usage.sql` | M2, S2: which indexes are read, their size, and statistics age |

`evidence/` holds reference output generated offline (migration DDL, the SQL EF sends, parameter types) and the list of checks that
still need a live server, with expected results (`evidence/README.md`). Try the exercise first, then compare.

## Tear down

```powershell
.\training\lab\sqlserver\stop-sqlserver.ps1     # removes the container and all lab data
git worktree remove ..\meterforge-sqllab
```

Concepts: [10 — plans and indexes](../../../../opusorganize/apprenticeship/curriculum/10-query-plans-and-indexes.md) ·
[07 — zero-downtime migrations](../../../../opusorganize/apprenticeship/curriculum/07-zero-downtime-migrations.md) ·
[03 — concurrency](../../../../opusorganize/apprenticeship/curriculum/03-concurrency-races.md).
(The curriculum lives in `Desktop\opusorganize\apprenticeship\curriculum\`.)
