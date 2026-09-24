# Senior trade-offs in MeterForge ("Why?" drills)

For each decision, fill in the missing parts yourself before reading the sealed notes in `_answers/tradeoffs.md`.
Format: Problem → naive approach → where it fails → options → trade-offs → choice → failure modes → how it's tested → how it's monitored → when to revisit.

## T1 — Dedupe usage by a producer-supplied `external_event_id` (not by our own id)
- **Problem:** SDKs retry after timeouts; the same usage must never be billed twice.
- **Naive approach:** generate our own id per request, dedupe nothing.
- **Where it fails:** every retry is a second charge.
- **Options:** A) producer key + unique index; B) content hash of (meter, quantity, occurred_at); C) API-level `Idempotency-Key` header with a stored response.
- **Your job:** fill in trade-offs → choice → failure modes → tests → monitoring → when to revisit. Evidence: `UsageIngestionService.cs`, lab `ux_usage_events_tenant_external`.

## T2 — Clustered key `seq bigint identity` with a separate unique GUID `id` (lab branch)
- **Problem:** 400–3,000 inserts/s into one table that is also read by tenant and time range.
- **Naive approach:** make the domain's `Guid Id` the clustered primary key (EF's default).
- **Where it fails:** random insert positions → page splits, fragmentation, larger log. And the key most reads need is `(tenant, time)` anyway.
- **Options:** A) GUID clustered; B) identity clustered + GUID unique; C) cluster on `(tenant_id, occurred_at, seq)`; D) sequential GUIDs (`NEWSEQUENTIALID`, UUIDv7).
- **Your job:** complete the drill. Evidence: `MeterForgeDbContext.cs` (lab), incident 002's hot spots.

## T3 — Money is a domain type with exactly two decimals, rounded half-even
- **Problem:** rating, discounts, credits and invoices must agree to the cent with the payment provider.
- **Naive approach:** `decimal` everywhere, round when displaying.
- **Where it fails:** sums of unrounded values drift from the invoice lines; reconciliation mismatches.
- **Options:** A) `Money` value object enforcing scale (current); B) integer minor units (`long cents`); C) high-precision internal amounts rounded only at invoice finalization.
- **Your job:** complete the drill, including what happens at persistence boundaries (`decimal(19,2)`, EF defaults). Evidence: `Money.cs`, `TieredRating.cs:33`, incident 005.

## T4 — Ports + in-memory adapters by default, real adapters optional
- **Problem:** teach and test business rules fast without infrastructure.
- **Naive approach:** EF Core DbContext in every service from day one.
- **Where it fails (the other way):** the default runtime is in-memory, so nothing in CI proves the real adapters. The Postgres adapter drifted into being never wired.
- **Options:** A) ports + fakes, real adapters behind contract tests run against real databases; B) EF InMemory provider in tests; C) Testcontainers in CI for every adapter.
- **Your job:** complete the drill. Evidence: `Program.cs:27-30`, `PostgresInvoiceSnapshotStore.cs`, lab `tests/MeterForge.SqlServer.Tests`.

## T5 — Rollups computed by a claim-then-merge batch job (lab) rather than on ingest
- **Problem:** invoicing and the usage page need totals per subscription/meter/day over 100M+ events.
- **Naive approach:** `SUM` over raw events on every page view and invoice run.
- **Options:** A) batch job with claim flag (current); B) increment a rollup row inside the ingest transaction; C) indexed view (`WITH SCHEMABINDING`, `COUNT_BIG`); D) stream processing outside the database.
- **Your job:** complete the drill; include locking (M3, incident 002) and late events.
