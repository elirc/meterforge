# J1 — Run MeterForge's usage ingestion on SQL Server

**Context.** On `main`, `POST /api/v1/tenants/{tenantId}/usage-events` stores events in `InMemoryUsageEventStore`
(`src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs`): a restart loses every metered event, which for a billing
product means lost revenue. The branch `training/lab/sqlserver` has an EF Core SQL Server adapter. Nobody has run it end to end.

**Ticket (from the platform lead).**
> Prove the SQL Server adapter works before anyone reviews the migration. Bring up the lab container, apply the migration,
> ingest the same event twice through the real HTTP API, and show me the row in `billing.usage_events` and the second call's
> `isReplay: true`. Then restart the API and show the replay still works.

**Constraints.** Work in a worktree of `training/lab/sqlserver` (see `training/lab/sqlserver/README.md`). Don't edit `main`.
Use the lab container on port 14337, not a local SQL Server install.

**Definition of done.**
- [ ] `dotnet run --project tools/MeterForge.SqlLab -- migrate` prints `InitialSqlServer`.
- [ ] Two identical `POST .../usage-events` calls: first returns **201**, second **200** with `"isReplay": true` and the same `id`.
- [ ] `GET /health/ready` reports `"storage": "sqlserver-usage-events"`.
- [ ] After an API restart, the replay still returns the original `id`.
- [ ] `SELECT seq, id, tenant_id, external_event_id, rolled_up FROM billing.usage_events WHERE tenant_id = 'tenant-j1'` shows exactly one row.
- [ ] A 5-line note: what `seq` is for, and why `id` is not the clustered key.

## Explain before touching (write your answers first)
1. Which DI registration decides between the in-memory store and SQL Server? Name the file and line on the lab branch.
2. The HTTP request crosses API → Application → Infrastructure → SQL Server. Name the class at each hop.
3. `SqlServerUsageEventStore.AddAsync` catches SQL errors 2601/2627. What produces them, and in which real-world situation will you see one?
4. Which columns in `InitialSqlServer.cs` are `varchar` rather than `nvarchar`, and why might that matter later?
5. The API keeps `IUsageEventStore` as a **singleton** on main, but the SQL Server store is **scoped**. Why must it be scoped?

<details><summary>Hints</summary>

- `Program.cs` on the lab branch: search for `AddMeterForgeSqlServer`.
- `DbContext` is not thread-safe, and each request needs its own.
- The request body shape is `IngestUsageRequest` in `src/MeterForge.Contracts/V1/Usage/UsageContracts.cs`.
</details>

Answer: [`_answers/J1.md`](../_answers/J1.md)
