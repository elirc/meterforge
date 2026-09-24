# Before your first production change to MeterForge

1. **Money has one rule: two decimals, rounded half-even, at the domain edge.** `Domain/Common/Money.cs` throws on a third decimal.
   Any persistence or API path that stores a `decimal` without going through `Money` (see incident 005) can bypass it.
   Know where rounding happens before you touch a price, discount, rating or credit.
2. **Tenant is part of every key.** Every repository and table is keyed by `(tenant_id, …)`. A query without `tenant_id` is a cross-tenant bug, even in a report.
3. **Usage ingestion is idempotent by `(tenant_id, external_event_id)`, and that key is opaque and case-sensitive.** SDKs retry for hours.
   Changing its normalization (trim, case, collation) changes what counts as "the same event", which changes bills. See `UsageIngestionService.cs` and, on the lab branch, the `UseCollation("Latin1_General_100_BIN2")` line.
4. **The running app stores nothing durably** (`Program.cs:27-30`). The Postgres adapter is a class, not a feature. Don't assume a restart is harmless because "there's a database".
5. **Errors are `BusinessRuleException(code, message)` → 422 Problem Details.** Add a stable `code`; clients switch on it. Don't throw other exceptions for rule violations.
6. **Contracts are versioned under `Contracts/V1`.** Adding a field is fine; renaming or changing meaning needs a V2 route.
7. **Optimistic concurrency is by `Version` + `If-Match`** for subscriptions. A new mutation goes through `MutateAsync`, not `GetAsync` + edit.
8. **CI will fail on formatting and warnings** (`dotnet format --verify-no-changes`, `TreatWarningsAsErrors`). Run both locally.
9. **On SQL Server (lab):** read the generated migration SQL (`dotnet ef migrations script`) before any schema PR; check collation, decimal precision, and whether an index build is online.
10. **Where to look first when something is wrong with a bill:** `TieredRater` (rounding per tier), `PriceSelector` (half-open validity `[from, to)`), `Invoice.AddLine` (currency match), then storage.
