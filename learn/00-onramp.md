# MeterForge on-ramp: 7 warm-ups before `training/`

Do these in order. Write each answer down **before** you check `learn/_answers/00-onramp.md`.
Read committed code with `git show HEAD:<path>`, because your working tree has local edits in the Usage area.
Exercises 1–5 need nothing but the repo. Exercises 6–7 read the lab branch. None of them needs SQL Server.

---

## 1. Predict: what does `Money` accept? (10 min)
File: `src/MeterForge.Domain/Common/Money.cs`. For each line, predict: does it succeed, and if so what are `Amount` and `Currency`? If it throws, which error code?
```csharp
new Money(10.50m, " usd ");
new Money(10.005m, "USD");
new Money(3m, "US");
new Money(1.10m, "EUR") + new Money(2m, "USD");
```
Then answer: `10.50m` and `10.5m` are both allowed. Why does the precision check `decimal.Round(amount, 2, ToEven) != amount` not care about the trailing zero?

## 2. Predict: tiered rating (15 min)
File: `src/MeterForge.Domain/Rating/TieredRating.cs`. Tiers: `UpTo = 1000` at `0.02 USD`, then `UpTo = null` at `0.01 USD`.
- a) `TieredRater.Rate(1500, tiers)`: list every `RatedTier` (units, charge) and the total.
- b) `TieredRater.Rate(0, tiers)`: what comes back?
- c) A product manager asks for a unit price of **0.005 USD** per API call. Where exactly does that fail, and with which error code? (It isn't in `TieredRater`.)
  In one sentence, why does that matter for a *metering* product?

## 3. Trace one request end to end (15 min)
`POST /api/v1/tenants/tenant-a/usage-events` with body `{"subscriptionId":"…","meter":"api_calls","quantity":10,"occurredAt":"…","externalEventId":"evt_1"}`.
Write the chain as `file → method` for each hop, down to where the event is stored. Include the line in `Program.cs` that decides which store is used,
and say whether that store is a singleton or scoped. What HTTP status does the *first* call return, and the *second* identical call?

## 4. Predict: the leading space (15 min)
Using the same files as exercise 3, predict the status code, `isReplay`, and whether the returned `id` is actually stored, for each call. Run the two scenarios separately, each on a fresh store.
- Scenario A: first `externalEventId = " evt_1"` (leading space), then `"evt_1"`.
- Scenario B: first `"evt_1"`, then `" evt_1"`.

Point to the two lines whose disagreement causes Scenario B's result. (Hint: one of them is in the `UsageEvent` constructor.)

## 5. Write one characterization test (30 min)
Pin Scenario B from exercise 4 exactly as it behaves **today**. Don't fix anything. Work in a detached worktree so your local edits stay untouched:
```powershell
git -C C:\Users\Owner\Desktop\meterforge worktree add --detach ..\mf-onramp HEAD
cd ..\mf-onramp
```
Add the test next to the existing ones in `tests/MeterForge.Application.Tests/Usage/UsageIngestionTests.cs`, using the same style
(`new UsageIngestionService(new InMemoryUsageEventStore())`). Name it after the behaviour, e.g. `Ingest_PaddedDuplicate_ReturnsCreatedWithIdThatIsNotStored`.
Assert at least: the second result is **not** a replay, its `Event.Id` differs from the first, and a lookup of `"evt_1"` still returns the *first* event.
Run `dotnet test tests/MeterForge.Application.Tests`. It must **pass**. Add a `// BUG:` comment with the one-line production story.
Remove the worktree afterwards (`git worktree remove ..\mf-onramp`).

## 6. Explain the schema (20 min)
Read `git show training/lab/sqlserver:src/MeterForge.Infrastructure.SqlServer/MeterForgeDbContext.cs`, the `UsageEventRow` block. Answer in 1–2 lines each:
- a) Why is the clustered key `seq` (an identity column) and not the `Guid` `id`?
- b) What is each of these for: `ux_usage_events_id`, `ux_usage_events_tenant_external`, `ix_usage_events_tenant_occurred`, `ix_usage_events_unrolled` (note its `HasFilter`)?
- c) `external_event_id` uses `UseCollation("Latin1_General_100_BIN2")`. What would change if it used the database default (case-insensitive) collation instead?
  Give one concrete pair of ids that would behave differently.
- d) `tenant_id` has `IsUnicode(false)`. What SQL type does that produce, and what kind of *parameter* would have to be sent for SQL Server to seek on it efficiently?

(Parts c and d are exactly where INC-004 and INC-003 happen. You're learning the vocabulary before the incident.)

## 7. Generate the DDL yourself, no database needed (20 min)
In a worktree of `training/lab/sqlserver`, run `dotnet tool restore`, then
`dotnet dotnet-ef migrations script --project src/MeterForge.Infrastructure.SqlServer -o ..\my-initial.sql`.
(`migrations script` only reads the model, so it needs no SQL Server. It builds the context through `DesignTimeDbContextFactory` in
`SqlServerRegistration.cs:28`. Open that class and say why it exists.) Find in your output:
- the `CREATE TABLE [billing].[usage_events]` column types for `quantity`, `occurred_at` and `external_event_id`
- the `WHERE` clause of `ix_usage_events_unrolled`

Diff your file against `training/lab/sqlserver/evidence/initial-sqlserver.sql`. They should match apart from whitespace and the byte-order mark. If yours differs, explain why before you move on.
(If the command fails, read the error before you ask anyone. Understanding it is part of the exercise.)

---
Done? Check `learn/_answers/00-onramp.md`, then go to `learn/README.md` session 4 (blind review).
