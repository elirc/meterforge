# J3 — Pin the adapter's behaviour with tests that hit real SQL Server

**Context.** `tests/MeterForge.SqlServer.Tests/UsagePersistenceTests.cs` (lab branch) has four tests that run the real migration
into a throwaway database (`SqlServerFixture`) and skip themselves when `METERFORGE_SQLSERVER_TESTS` is unset.
The EF Core **InMemory** or **SQLite** providers would be faster, but they don't have collations, `datetimeoffset`, `decimal(19,2)`
rounding or unique-index violations the way SQL Server does.

**Ticket.**
> Add two tests the product actually depends on:
> 1. `Ingest_ExternalIdsThatDifferOnlyByCase_AreTwoEvents` — producers send opaque, case-sensitive ids (`evt_aB3` and `evt_Ab3` are different events).
> 2. `Ingest_OccurredAtWithOffset_IsStoredAsTheSameInstant` — an event sent as `2026-09-01T02:00:00+02:00` must be found by a summary for `[2026-09-01T00:00Z, 2026-09-01T01:00Z)`.

**Constraints.** Real SQL Server only (the lab container). No mocks of `DbContext`. Each test must fail if you break the thing it protects.

**Definition of done.**
- [ ] `dotnet test tests/MeterForge.SqlServer.Tests` runs 6 tests green with the env var set, and reports them **skipped** (not passed) without it.
- [ ] You broke each protected behaviour once on purpose (e.g. remove `.UseCollation(...)` and add a migration; or convert to local time) and saw the test fail. Write down how.
- [ ] One paragraph: why these two tests would pass on EF InMemory even when production is broken.

## Explain before touching
1. Which line of `MeterForgeDbContext.cs` makes test 1 pass today? What does the server default collation `SQL_Latin1_General_CP1_CI_AS` do to `'evt_aB3' = 'evt_Ab3'`?
2. How does SQL Server compare two `datetimeoffset` values with different offsets? What does Postgres `timestamptz` store instead?
3. The fixture creates one database per test class. What happens to test isolation if two tests in the class use the same `external_event_id`?
4. Why is a skipped test better than a test that silently passes when the database is missing?

<details><summary>Hints</summary>

- `SqlServerFactAttribute` sets `Skip` in its constructor.
- `datetimeoffset` comparisons are done on the UTC instant.
</details>

Answer: [`_answers/J3.md`](../_answers/J3.md)
