# Evidence: what was verified, and what still needs a live SQL Server

**Honest status (2026-09-24).** The lab and incident branches build, the existing 42 tests pass on the lab branch, and the four SQL Server
integration tests are *skipped* (no server). Docker on the authoring machine broke before the SQL Server container could start, and no other
SQL Server was available. So everything below is either **offline-verified** (produced by EF Core / SqlClient without a database) or
**UNVERIFIED**: expected results from SQL Server's documented behaviour, which you must confirm by running them.

## Offline-verified files (real tool output)
| File | What it proves |
|---|---|
| `initial-sqlserver.sql` | `dotnet ef migrations script` for `InitialSqlServer`: the real DDL (identity clustered `seq`, BIN2 collation on `external_event_id`, filtered indexes, `datetimeoffset`, `decimal(19,4)`/`(19,2)`) |
| `usage-summary-sql.txt` | The SQL EF sends for `SummarizeAsync`: lab branch inlines `'tenant-0001'` (`EF.Constant`); INC-001 branch sends `@_8__locals1_tenant varchar(64)` |
| `dedupe-lookup-sql.txt` | EF LINQ types the dedupe parameters `varchar(64)`/`varchar(128)`; INC-003's `FromSqlRaw` + untyped `SqlParameter` sends `nvarchar(11)`/`nvarchar(12)` (`SqlDbType: NVarChar`) |
| `inc-004-migration.sql` | INC-004's migration drops `ux_usage_events_tenant_external`, `ALTER COLUMN external_event_id varchar(128)` **without COLLATE** (→ database default), recreates the unique index; offline, not `ONLINE` |
| `inc-005-migration.sql` | INC-005 creates `amount decimal(18,2)` |
| `decimal-mapping.txt` | EF's own `DecimalTypeDefaultWarning` ("values will be silently truncated"), the `decimal(18,2)` parameter type, and `27.7666…` truncated (`AdjustScale`, no round) vs rounded (`27.77`) |
| `probe-*.cs.txt` | The .NET 10 file-based probes that produced the two files above |

## UNVERIFIED: run these against a real SQL Server 2022
Start a server (exact command; port 14337, throwaway password):
```powershell
docker run -d --name meterforge-training-mssql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=MeterForge#Lab2026" -e MSSQL_PID=Developer -e MSSQL_MEMORY_LIMIT_MB=2048 -p 14337:1433 mcr.microsoft.com/mssql/server:2022-latest
# or simply: . .\training\lab\sqlserver\start-sqlserver.ps1   (also sets compat level 150 and the env vars)
```
Then, in a worktree of the named branch (`dotnet tool restore` once):

| Check | Branch | Command | Expected |
|---|---|---|---|
| Integration tests | `training/lab/sqlserver` | `dotnet test tests/MeterForge.SqlServer.Tests` (env `METERFORGE_SQLSERVER_TESTS` set) | 4 passed, 0 skipped |
| J1 end to end | lab | `SqlLab migrate`, `SqlLab seed`, API + 2 POSTs | 201 then 200 `isReplay: true` |
| J2 plans | lab | `sql/01-usage-summary-plan.sql` | small tenant: Index Seek `ix_usage_events_tenant_occurred` + Key Lookup, ~150–300 reads; whale: Clustered Index Scan, ~10k+ reads |
| INC-001 | `training/incidents/001-usage-summary-latency` | `SqlLab bench-sniff --first tenant-whale` then `--first tenant-0001` | whale-first: small tenants p50 ≫ tenant-0001-first (scan plan reused). If not, check compat level is 150 (PSPO at 160) |
| INC-002 | `training/incidents/002-batch-ingest-1205` | `SqlLab soak --seconds 60 --writers 6 --batch 20 --replay-percent 30`; lab branch with `--batch 1` as control | `sql-1205=N>0` on the incident branch, none on the control; graph via `sql/03` Part B |
| INC-003 | `training/incidents/003-ingest-cpu-climb` | after `seed`: `SET STATISTICS IO ON` + the `nvarchar` query from `dedupe-lookup-sql.txt` vs the `varchar` one | nvarchar: Index Scan with `CONVERT_IMPLICIT`, thousands of reads; varchar: Index Seek on `ux_usage_events_tenant_external`, < 10 reads |
| INC-004 | `training/incidents/004-usage-undercount` | `SqlLab migrate`; `SqlLab ingest tenant-0042 evt_7hQ2mZx1`; `SqlLab ingest tenant-0042 evt_7Hq2mZX1` | second prints `REPLAY` (lab branch: `CREATED`) |
| INC-005 | `training/incidents/005-proration-reconciliation-drift` | `SqlLab migrate`; `SqlLab prorate tenant-0001 --monthly 49 --unused 17 --days 30`; `SELECT amount FROM billing.proration_credits` | prints `27.76`; `SELECT CAST(27.7666 AS decimal(18,2))` returns `27.77` |
| M1 proofs | any | the snippets in `ladder/M1` hints | as in `_answers/M1.md` |

When you run them, save the outputs here (`inc-00N-*.txt`, `.sqlplan`, `inc-002-deadlock.xml`) and replace "UNVERIFIED" in the answer files with your numbers.
That is part of the exercise: your numbers, not the reference's.
