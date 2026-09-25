# INC-005 — sealed answer: EF's default `decimal(18,2)` + SqlClient truncation ate the third decimal

## Root cause
`ProrationCreditRow.Amount` has **no `HasPrecision`**, so EF Core's SQL Server provider maps it to its default `decimal(18,2)` (the migration
`AddProrationCredits` shows `type: "decimal(18,2)"`; EF also logs warning `Microsoft.EntityFrameworkCore.Model.Validation[30000]` "No store type was specified for the decimal property 'Amount'…" at startup, which nobody reads).
`RecordAsync` stores the exact credit `49.00 × 17 / 30 = 27.7666…`. EF sends it as a parameter typed `decimal(18,2)`; **SqlClient truncates**
a decimal value whose scale exceeds the parameter's scale (it doesn't round), so `27.76` is stored. The read side's
`decimal.Round(row.Amount, 2, ToEven)` rounds a value that is already `27.76` → `27.76`. The provider computes `27.77`.
Only credits whose third decimal is ≥ 5 differ (roughly half of them), always by exactly 0.01 on the credit: the report's pattern.

Note the contrast: T-SQL `CAST(27.7666 AS decimal(18,2))` and inserting a literal **round** to `27.77`. The loss happens in the client
parameter, which is why a quick check in SSMS "can't reproduce" it. (Evidence file shows both.)

The design ("store exact, round once on read") was fine in principle; it failed because the storage wasn't exact.

## Diagnostic path
1. All mismatches: +0.01, only prorated invoices, ~half of them → rounding at the third decimal.
2. Recompute: 27.7666 → half-even 27.77, truncation 27.76. Truncation matches ours.
3. `SELECT amount FROM billing.proration_credits WHERE ...` → 27.76 already stored: the loss is on the way in.
4. `sp_help 'billing.proration_credits'` / the migration → `decimal(18,2)`. Model has no precision. Startup log has the EF warning.
5. Prove which layer: SQL Profiler / XEvent `rpc_completed` shows `@p2 decimal(18,2)` with value `27.76`, or run the harness with `LogTo` and `EnableSensitiveDataLogging`.

## Fix
- Model: `credit.Property(c => c.Amount).HasColumnName("amount").HasPrecision(19, 6)` (exact enough for proration ratios), migration `AlterColumn` (widening a decimal is safe and online-cheap for small tables).
- Better domain design: round **before** persisting with the domain's rule and store `decimal(19,2)`, plus store the inputs (`unused_days`, `days_in_period`, old price) as provenance, which the row already does. Then storage precision can't change money.
- Guard for the whole model: a convention in `MeterForgeDbContext.ConfigureConventions`: `configurationBuilder.Properties<decimal>().HavePrecision(19, 4);` so no decimal ever falls back to 18,2, plus a unit test that iterates `db.Model.GetEntityTypes()` and fails on any decimal property without explicit precision.
- Treat EF model-validation warnings as errors in tests: `ConfigureWarnings(w => w.Throw(SqlServerEventId.DecimalTypeDefaultWarning))`.

## Regression test (real SQL Server)
```csharp
[SqlServerFact]
public async Task ProrationCredit_RoundsHalfEven_NotTruncated()
{
    await using var db = fixture.CreateContext();
    var ledger = new ProrationLedger(db, TimeProvider.System);
    var id = await ledger.RecordAsync("tenant-p", Guid.NewGuid(), new Money(49m, "USD"), 17, 30, CancellationToken.None);
    Assert.Equal(27.77m, (await ledger.GetCreditAsync("tenant-p", id, CancellationToken.None))!.Value.Amount);
}
```

## Other places in MeterForge where the same thing could happen
Any new `decimal` without precision on the lab model; quantities (`decimal(19,4)`) receiving values with more than 4 decimals (GB-hours
computed from seconds: `seconds / 3600m` has 28 digits of scale → truncated on insert); `TieredRater` per-tier amounts if ever persisted before
rounding; any raw `SqlParameter` with `Precision/Scale` set smaller than the value.

## Correcting the 1,318 invoices
Finalized invoices are immutable: issue a +0.01 **credit note** per affected invoice (the domain has `CreditNote`) referencing the proration credit id,
recomputed from the stored inputs (`unused_days`, `days_in_period`, old price), not from the truncated amount. Mark the reconciliation mismatches as explained.

## Lab reproduction
**Offline-verified:** `evidence/inc-005-migration.sql` (`amount decimal(18,2)`) and `evidence/decimal-mapping.txt`: EF logs `DecimalTypeDefaultWarning` ("values … silently truncated"), types the parameter `decimal(18,2)`, and `SqlDecimal.AdjustScale(27.7666…, fRound: false)` = `27.76` vs rounded `27.77`.
**UNVERIFIED (needs a live SQL Server):** the stored value. On the incident branch: `SqlLab migrate`, `SqlLab prorate tenant-0001 --monthly 49 --unused 17 --days 30` → expected `credit …: 27.76 USD`; then `SELECT CAST(27.7666 AS decimal(18,2))` → expected `27.77`.
