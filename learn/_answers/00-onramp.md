# SEALED: MeterForge on-ramp answers

All answers come from reading committed code (`main` @ `43ee18d` plus the training commits, and lab branch `d7a4fd6`). None of them was executed in the B08 pass,
which was docs-only. Where an answer depends on runtime behaviour, your own run is the authority. If it disagrees with this file, trust your run and note it.

> **Note added 2026-10-06.** Answers 3–5 describe `43ee18d`, and the line numbers were re-checked against `git show 43ee18d:<path>`. On today's `main` (after `4e3f344`), the trace in answer 3 is `UsageIngestionService.cs:23-30` → `InMemoryUsageEventStore.GetOrAddAsync` (`ConcurrentDictionary.GetOrAdd`). `FindByExternalIdAsync` and `AddAsync` no longer exist, so the reference test in answer 5 only compiles at `43ee18d`. The registrations are now `Program.cs:34` and `:39`. Answers 6–7 cite lab commit `d7a4fd6`, which is not on GitHub.

## 1. Money
| Call | Result |
|---|---|
| `new Money(10.50m, " usd ")` | OK. `Amount = 10.50`, `Currency = "USD"` (trimmed and upper-cased at `Money.cs:18`) |
| `new Money(10.005m, "USD")` | throws `money.precision_invalid`: rounding to 2 places gives 10.00 (ToEven: the digit before the 5 is 0, which is even), and 10.00 != 10.005 |
| `new Money(3m, "US")` | throws `money.currency_invalid` (the length after trimming isn't 3) |
| `EUR + USD` | throws `money.currency_mismatch` (`operator +`, `Money.cs:27-30`) |

Trailing zero: `decimal` equality compares **values**, not scale. `10.50m == 10.5m` is true, so the check only rejects values with a third significant decimal place.
(The scale is still kept inside the value, `10.50m.ToString()` prints `10.50`. That's why the database column scale matters later, in INC-005.)

## 2. TieredRater
- a) Two tiers: `(1000 units, 0.02, 20.00)` and `(500 units, 0.01, 5.00)`. Total `25.00 USD`. On the second tier `upper = UpTo ?? quantity = 1500`, so `units = 1500 - 1000`.
- b) `Rate(0, …)`: `0 < 0` is false, so no exception. In the first loop iteration `quantity <= previousLimit` (0 <= 0) → `break`. Result: `Quantity 0`, empty `Tiers`, `Total 0.00 USD`.
- c) It fails when you build the tier's `UnitPrice`: `new Money(0.005m, "USD")` → `money.precision_invalid` in `Money.cs:12-15`, before `TieredRater` ever runs.
  Why it matters: metering products price per unit in fractions of a cent all the time (per API call, per GB-second). Using the *invoice* money type for a
  *unit price* means the model can't express common prices. The usual design is a separate unit-price type with more decimal places, rounded only when a charge is produced.
  That's a good design discussion to bring to the blind review.

## 3. Trace
1. `src/MeterForge.Api/Usage/UsageEndpoints.cs:10-18`: `MapPost(".../usage-events")` binds `IngestUsageRequest` (`src/MeterForge.Contracts/V1/Usage/UsageContracts.cs`).
2. `src/MeterForge.Application/Usage/UsageIngestionService.cs:15-25`: `IngestAsync` → `store.FindByExternalIdAsync`, then on a miss `new UsageEvent(...)` + `store.AddAsync`.
3. `src/MeterForge.Domain/Usage/UsageEvent.cs:7-21`: validation, and `ExternalEventId.Trim()` at line 20.
4. `src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs`: a `ConcurrentDictionary` keyed by `(TenantId, ExternalEventId)`, with `TryAdd` at line 21.
- Store selection: `Program.cs:30` `AddSingleton<IUsageEventStore, InMemoryUsageEventStore>()`, a **singleton** (it has to be, because it *is* the data). The service is scoped (`Program.cs:35`).
- First call → **201 Created** with a `Location` header. Second identical call → **200 OK** with `isReplay: true` and the same `id` (`UsageEndpoints.cs:17`).

## 4. The leading space
- **Scenario A** (`" evt_1"` then `"evt_1"`): the first lookup uses the raw `" evt_1"` and misses. The event is stored under the **trimmed** key `"evt_1"`, and you get 201 with an id that really is stored.
  The second call looks up `"evt_1"` and finds it: 200, `isReplay: true`, same id. Behaves fine, by luck.
- **Scenario B** (`"evt_1"` then `" evt_1"`): the first call is a normal 201. The second call looks up the raw `" evt_1"` and **misses**. It builds a new `UsageEvent` with a new Guid,
  whose key is trimmed to `"evt_1"`. `TryAdd` returns `false` because the key exists, and **the return value is ignored**. The response is **201 Created, `isReplay: false`, with an id that was never stored.**
- The two lines that disagree: `UsageEvent.cs:20` (trims) vs `UsageIngestionService.cs:19` (looks up the untrimmed value). The third culprit is `InMemoryUsageEventStore.cs:21`, which ignores the result of `TryAdd`.
- This is review finding material (`training/_answers/review.md`). batch7report lists it under "Real bugs". The same "ignored add result" is behind the concurrent-duplicate phantom id.

## 5. Characterization test (reference)
```csharp
[Fact]
public async Task Ingest_PaddedDuplicate_ReturnsCreatedWithIdThatIsNotStored()
{
    // BUG: an SDK that pads ids gets a 201 and an id we never stored; a support lookup by that id finds nothing.
    var store = new InMemoryUsageEventStore();
    var service = new UsageIngestionService(store);
    var subscriptionId = Guid.NewGuid();
    var at = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    var first = await service.IngestAsync("tenant-a", subscriptionId, "api_calls", 10, at, "evt_1", CancellationToken.None);
    var padded = await service.IngestAsync("tenant-a", subscriptionId, "api_calls", 10, at, " evt_1", CancellationToken.None);

    Assert.False(padded.IsReplay);                       // today: treated as new
    Assert.NotEqual(first.Event.Id, padded.Event.Id);    // today: a fresh Guid is returned
    var stored = await store.FindByExternalIdAsync("tenant-a", "evt_1", CancellationToken.None);
    Assert.Equal(first.Event.Id, stored!.Id);            // ...but the store still holds only the first event
}
```
Grading: the test must **pass on `main`**. If you "fixed" the bug so the test asserts a replay, you wrote a spec test, not a characterization test (see
curriculum 04 (apprenticeship page `04-characterization-tests`, not in this repo)). When someone fixes the trim mismatch, this test is the one that should flip, on purpose.
Not executed in B08: the `dotnet test` result is yours to produce.

## 6. Schema
- a) An append-only identity key means new rows always go on the last page. A random `Guid` clustered key causes page splits and fragmentation on a table that grows by millions of rows a week.
  `id` is still unique (`ux_usage_events_id`), because the API hands it out.
- b) `ux_usage_events_id`: lookups by public id, and enforcing uniqueness. `ux_usage_events_tenant_external`: **the real idempotency guarantee**. The database refuses a duplicate
  `(tenant, external id)` even when two requests race (the SQL Server store maps errors 2601/2627 to "replay"). `ix_usage_events_tenant_occurred`: the per-tenant usage
  summary by time range (J2, INC-001). `ix_usage_events_unrolled`: a **filtered** index (`WHERE [rolled_up] = 0`) that holds only rows the rollup job hasn't
  processed yet, so it stays small however big the table gets (INC-002 is about the locks on it).
- c) With a case-insensitive collation, `evt_7hQ2mZx1` and `evt_7Hq2mZX1` compare **equal**. The unique index and the dedupe lookup treat the second as a replay of the first,
  so a real, different event is dropped with a 200 response. SDKs that generate base62 ids hit this all the time. That is INC-004.
- d) `varchar(64)`. The parameter must be `varchar` too, which EF does automatically from the mapping. An `nvarchar` parameter has higher type precedence, so SQL Server converts
  the **column** (`CONVERT_IMPLICIT`), and with a SQL collation it can't seek any more. That is INC-003.

## 7. DDL
Expected (from `evidence/initial-sqlserver.sql`, produced offline in B07): `[quantity] decimal(19,4)`, `[occurred_at] datetimeoffset`,
`[external_event_id] varchar(128) COLLATE Latin1_General_100_BIN2`, and `CREATE INDEX [ix_usage_events_unrolled] ... ([occurred_at]) WHERE [rolled_up] = 0`.
`DesignTimeDbContextFactory` exists because the `dotnet ef` tools need a `DbContext` without running the API. The API only registers SQL Server when
`ConnectionStrings:MeterForgeSqlServer` is set. The factory reads `METERFORGE_SQLSERVER` or falls back to the lab container string. It doesn't *connect* for `script`.
Differences you might see: a BOM, line endings, or a different `ProductVersion` if your `dotnet-ef` isn't the pinned 10.0.12 (`dotnet-tools.json`).
