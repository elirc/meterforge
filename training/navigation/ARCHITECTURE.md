# MeterForge architecture, reconstructed from the code

Commit `43ee18d` on `main`, plus the lab branch `training/lab/sqlserver` where noted. Every claim has a path.

> **Note added 2026-10-06.** This page is a record of `43ee18d`. On today's `main`, commit `4e3f344` has replaced the usage-ingestion steps below (find-then-`TryAdd`) with an atomic `GetOrAddAsync` that returns the stored winner and a 409 on a changed payload, and the `Program.cs` registrations are at lines 31–39. The rest of the system context still matches today's `main`. The lab branch is not on GitHub.

## System context
- One ASP.NET Core 10 minimal-API process (`src/MeterForge.Api/Program.cs`). No auth, no background workers, no message broker.
- Storage at runtime: **in-memory only** (`Program.cs:27-30` registers four `InMemory*`/`Simulated*` singletons).
  A Postgres adapter exists (`src/MeterForge.Infrastructure/Persistence/PostgresInvoiceSnapshotStore.cs`) but is never registered.
  `docker-compose.yml` starts Postgres 18 for that adapter only.
- External systems are ports with fakes: payments (`IPaymentProvider` → `SimulatedPaymentProvider`), tax (`ITaxQuoteProvider`, no implementation).
- On the lab branch: SQL Server 2022 via EF Core 10, opt-in with `ConnectionStrings:MeterForgeSqlServer`.

## Components and dependency direction
| Project | Holds | Depends on | Evidence |
|---|---|---|---|
| `MeterForge.Domain` | Money, Product, Subscription (versioned state machine), PriceVersion, TieredRater, Invoice, UsageEvent, Entitlements, Reconciliation, Outbox/Webhook models | nothing | `MeterForge.Domain.csproj` has no references; enforced by `tests/MeterForge.Architecture.Tests/DependencyDirectionTests.cs` |
| `MeterForge.Application` | Use cases + ports: `ProductService`, `SubscriptionService`, `SubscriptionAmendmentService`, `PaymentService`, `UsageIngestionService`, `IInvoiceSnapshotStore`, `ITaxQuoteProvider` | Domain | `MeterForge.Application.csproj` |
| `MeterForge.Contracts` | Versioned HTTP DTOs under `V1/*` | nothing | `src/MeterForge.Contracts/V1/**` |
| `MeterForge.Infrastructure` | In-memory repositories, simulated provider, Npgsql adapter + SQL migrations | Application, Domain, Npgsql 10 | `MeterForge.Infrastructure.csproj` |
| `MeterForge.Api` | Endpoint groups per feature folder, Problem Details mapping, trace header middleware | all of the above | `Program.cs` |
| *(lab)* `MeterForge.Infrastructure.SqlServer` | DbContext, migrations, EF stores, read model, rollup job | Application, Domain, EF Core SqlServer 10.0.12 | lab branch |

Note: the "modular monolith" is organized **by layer**, with feature folders inside each layer, not by module with its own data. There is no module boundary to enforce between, say, Payments and Usage.

## Three request flows
**A. Ingest a usage event** — `POST /api/v1/tenants/{tenantId}/usage-events`
1. `Api/Usage/UsageEndpoints.cs` binds `IngestUsageRequest` (`Contracts/V1/Usage/UsageContracts.cs`).
2. `Application/Usage/UsageIngestionService.IngestAsync`: `FindByExternalIdAsync(tenant, externalId)` → if found, replay (200); else `new UsageEvent(...)` (validates, trims, converts to UTC; `Domain/Usage/UsageEvent.cs`) → `AddAsync` → 201.
3. `Infrastructure/Usage/InMemoryUsageEventStore.cs`: `ConcurrentDictionary<(tenant, externalId), UsageEvent>`, `TryAdd` result ignored.
   Lab: `SqlServerUsageEventStore` → `billing.usage_events` with unique index `(tenant_id, external_event_id)`, BIN2 collation.
4. Failures: `BusinessRuleException` → `Api/Errors/BusinessRuleExceptionHandler.cs` → 422 Problem Details with `code` and `traceId`.

**B. Change a subscription's product with optimistic concurrency** — `PUT .../subscriptions/{id}/product` + `If-Match: "3"`
1. `Api/Subscriptions/SubscriptionEndpoints.cs:26` parses `If-Match` as an int version, else 400.
2. `SubscriptionAmendmentService.ChangeProductAsync` → `ISubscriptionRepository.MutateAsync(tenant, id, expectedVersion, s => s.ChangeProduct(p))`.
3. `InMemorySubscriptionRepository.MutateAsync` takes a lock, compares `Version`, mutates in place (`Domain/Subscriptions/Subscription.cs` increments `Version`). Mismatch → `subscription.version_conflict` → 422.

**C. Collect a payment** — `POST .../payments` + `Idempotency-Key`
1. `Api/Payments/PaymentEndpoints.cs:14` requires the header (400 without it).
2. `PaymentService.CollectAsync` builds a new `PaymentAttempt` and calls `IPaymentProvider.ChargeAsync` with the key as the provider idempotency key.
3. `SimulatedPaymentProvider` always succeeds with `sim_{invoiceId}`. Nothing is stored.

## Data model
- Runtime: dictionaries keyed by `(tenantId, id)` or `(tenantId, externalEventId)`: tenant is part of every key.
- Postgres (`Persistence/Migrations/001_invoices.sql`, `002_outbox.sql`): `invoices (tenant_id, id)` PK, `invoice_lines` FK to it, `numeric(19,2)` money, `outbox_messages` with `jsonb` payload and a partial index on unpublished rows.
- SQL Server (lab `MeterForgeDbContext.cs`): the same invoice/outbox tables in schema `billing`, plus `usage_events` (clustered `seq bigint identity`, unique `id`, unique `(tenant_id, external_event_id)`, `ix (tenant_id, occurred_at)`, filtered `ix (occurred_at) WHERE rolled_up = 0`) and `usage_daily_rollups`.
- Money: domain `Money` allows at most 2 decimals and normalizes currency (`Domain/Common/Money.cs`). Quantities are unconstrained `decimal` in the domain and `decimal(19,4)` on SQL Server.

## Test strategy (what exists)
- `tests/MeterForge.Domain.Tests`: one file per domain area (rating, pricing, invoice, subscription, …): the bulk of the value.
- `tests/MeterForge.Application.Tests`: services against in-memory adapters; `InvoiceSnapshotTests` checks the mapping only.
- `tests/MeterForge.Api.Tests`: `WebApplicationFactory` tests for products, rating and subscriptions.
- `tests/MeterForge.Architecture.Tests`: one dependency-direction test.
- Nothing runs against Postgres. Lab: `tests/MeterForge.SqlServer.Tests` runs against a real SQL Server when an env var is set.

## Build and deploy
- `.github/workflows/ci.yml`: locked restore → `dotnet format --verify-no-changes` → Release build (`TreatWarningsAsErrors` from `Directory.Build.props`) → tests → vulnerable-package listing (non-failing).
- `global.json` pins SDK 10.0.302. No Dockerfile, no deploy job, no migration runner.
