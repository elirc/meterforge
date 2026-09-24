# Technical questions — MeterForge (20)

> Contains incident spoilers. Use this file only after you have worked incidents 001-005.

Answer out loud in 2 minutes each, then check against the code and the linked exercises. No answer key: the evidence is in the repo and `_answers/`.

## API design
1. `POST /usage-events` returns 201 for new and 200 for replay (`UsageEndpoints.cs`). What should it return when the replay has a different `quantity`? Defend the status code to an SDK author.
2. Every `BusinessRuleException` becomes 422 (`BusinessRuleExceptionHandler.cs:13`). Which MeterForge codes deserve 409, 412 or 404, and how do you change that without breaking V1 clients?
3. `PUT .../subscriptions/{id}/product` requires `If-Match` as a bare integer. What breaks with a proxy that adds `W/`, and what would you emit on `GET`?
4. The payments endpoint requires `Idempotency-Key` but creates a new attempt every call (`PaymentService.cs:18`). Design the storage for real API-level idempotency.

## Database (SQL Server + EF Core)
5. Why is `billing.usage_events` clustered on `seq` and not on the GUID `id`? What does that cost the `ux_usage_events_id` index?
6. `ix_usage_events_unrolled` is filtered on `rolled_up = 0`. Why does the rollup SQL use a literal `0`, and what happens if EF generates `rolled_up = @p0`?
7. Explain parameter sniffing using `UsageReadModel.SummarizeAsync` and the whale tenant. Give three fixes and what each costs.
8. EF sends the dedupe lookup parameters as `varchar(64)`/`varchar(128)`. Why does that matter? What does `new SqlParameter("@x", "abc")` send instead, and what does the plan show?
9. The server collation is `SQL_Latin1_General_CP1_CI_AS`. Which MeterForge columns must not use it, and why?
10. `SqlServerInvoiceSnapshotStore` checks `AnyAsync` then inserts. What happens with two concurrent saves of the same invoice? Rewrite it so a second save is a no-op without a race.
11. Walk through `UsageRollupJob`'s SQL: why claim with `UPDATE ... OUTPUT INTO #claimed` before `MERGE`, why `HOLDLOCK` on the `MERGE`, and what `UPDATE TOP (50000)` does to lock escalation.
12. What does READ COMMITTED SNAPSHOT change for the usage page while the rollup job runs? What does it not change for the writer vs the rollup job?
13. Postgres `numeric(19,2)` vs SQL Server `decimal(19,2)` vs EF's default `decimal(18,2)`: where can a MeterForge amount lose a cent, and does it round or truncate?

## Testing
14. Why do `tests/MeterForge.SqlServer.Tests` run against a real SQL Server instead of EF InMemory? Name two MeterForge behaviours InMemory would get wrong.
15. How would you write a test that proves two concurrent deliveries of the same usage event produce exactly one row and two consistent responses?

## Security
16. There is no authentication, and the tenant comes from the URL. List the three most damaging requests an attacker could make against MeterForge today.
17. `FromSqlRaw` appears in a PR (incident 003). When is it injection-safe, and what else can go wrong with it even when it is?

## Operations
18. `/health/ready` always says ready (`Program.cs:47`). What should readiness check for the SQL Server build, and what should it *not* check?
19. You get a page: "1205 errors on batch ingestion". Which three queries do you run first, and in what order?
20. The monthly invoicing run reads `usage_daily_rollups`. How do you know the rollups are complete for the period before you invoice?
