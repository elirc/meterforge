# STAR stories from MeterForge (complete them after doing the work)

> Contains incident spoilers. Use this file only after you have worked incidents 001-005.

Each needs your real numbers. If you didn't measure it, you can't tell it.

## 1. Debugging — "The usage page was fast for us and slow for every small customer" (incident 001)
- **S:** multi-tenant usage-billing API on SQL Server, one tenant 2,000× bigger than the median. After a "plan cache cleanup" PR, small tenants' usage summary went from ___ ms to ___ ms.
- **T:** find why only small tenants, with nothing else deployed.
- **A:** plan cache query (`sql/02`), `ParameterCompiledValue` in the plan XML = the whale; reproduced with `bench-sniff` in both orders; ___
- **R:** ___ (fix + the regression guard you added). **Lesson:** ___

## 2. Trade-off — "Covering index vs per-tenant plans vs RECOMPILE" (M2)
- **S/T:** the DBA wanted plan reuse; the product needed predictable latency for all tenant sizes.
- **A:** measured three options: ___ / ___ / ___ (logical reads, CPU, write cost).
- **R:** chose ___ because ___. What I gave up: ___.

## 3. Incident — "Deadlocks after we shipped batch ingestion" (incident 002)
- **S:** SDKs moved to a batch endpoint; 1205 deadlock errors started at ___/min at the end of each hour.
- **A:** read the deadlock graph from `system_health` (victim, owned vs requested keylocks, on which index) ___
- **R:** ___. **Lesson about lock order and transaction scope:** ___

## 4. Migration — "Porting a billing schema from Postgres to SQL Server" (M1 + S1)
- **S/T:** enterprise customer required Azure SQL.
- **A:** column-by-column port review with proofs: collation, `datetimeoffset`, money scale, identity. Found ___ risks that tests on EF InMemory could not catch.
- **R:** ___ (what you'd put in the cut-over plan).

## 5. Agent-caught / agent-introduced bug — "The PR that made two events one" (incident 004)
- **S:** an innocent "use the database default collation" PR, reviewed and approved.
- **A:** how the undercount showed up (replay rate, customer counts), how you proved it (`'evt_aB3' = 'evt_Ab3'`), the J3 test that would have caught it.
- **R:** ___. **What you now check in every migration diff:** ___
