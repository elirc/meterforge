# MeterForge

MeterForge is a complete fifteen-sprint C#/.NET teaching repository for a fictional multi-tenant usage-based subscription billing platform. It is designed to help a junior software engineer grow toward mid-level ownership through business rules, API design, persistence, concurrency, integrations, testing, and operations.

It is an original educational product—not a Stripe clone, payment processor, accounting system, or production financial service. All tenants, providers, money flows, and policies are synthetic fixtures.

## What the course teaches

- model money, subscriptions, effective prices, usage, invoices, credits, refunds, and entitlements without framework coupling;
- make tenant scope, idempotency, optimistic concurrency, and immutable financial provenance explicit;
- integrate PostgreSQL and external providers through narrow application ports;
- design versioned APIs with Problem Details, `Idempotency-Key`, `If-Match`, and trace correlation;
- reason about outbox/webhook delivery, retries, ambiguous failures, and reconciliation;
- choose tests by risk and recognize when architecture or delivery ceremony becomes over-engineered.

## Architecture

```text
HTTP / OpenAPI contracts
          |
          v
Application use cases ------> persistence, provider, time, and event ports
          |
          v
Domain rules and financial records
          ^
          |
In-memory, PostgreSQL, and simulated provider adapters
```

The deployable shape is a modular monolith. MeterForge deliberately avoids MediatR, AutoMapper, generic repositories, microservices, and an internal framework.

## Course map

| Sprint | Outcome |
| --- | --- |
| 1 | Product and fixed-price vertical slice |
| 2 | Subscriptions, trials, and lifecycle |
| 3 | Effective-dated price versions |
| 4 | Idempotent usage ingestion |
| 5 | Tiered rating and `v0.5.0` |
| 6 | Invoice calculation and finalization |
| 7 | Transactional PostgreSQL adapter |
| 8 | Concurrent subscription amendments |
| 9 | Discounts, tax boundary, and credits |
| 10 | Payment provider integration |
| 11 | Outbox and webhooks |
| 12 | Entitlements and usage limits |
| 13 | Reconciliation, refunds, and disputes |
| 14 | Security and five-flaw harvest |
| 15 | Operations, retrospective, and `v1.0.0` |

Each outcome has a milestone, four stakeholder issues, ordered commits, a reviewed pull request, CI evidence, and [sprint notes](docs/sprints).

## Run

The repository pins .NET 10 in `global.json`.

```powershell
dotnet restore MeterForge.slnx --locked-mode
dotnet run --project src/MeterForge.Api
dotnet test MeterForge.slnx --configuration Release
```

OpenAPI is available at `/openapi/v1.json` in Development. Check `/health/live` and `/health/ready`; readiness explicitly reports that the default runtime uses non-durable in-memory adapters. `docker compose up postgres` starts the local database used by the PostgreSQL adapter exercise.

Start with the [learner guide](LEARNER-GUIDE.md), [testing guide](TESTREADME.md), [over-engineering review](OEReadMe.md), and [production-readiness assessment](docs/quality/PRODUCTION-READINESS.md).

## Limits

MeterForge stores no card or bank data and has no production authentication, tax logic, provider credentials, telemetry backend, migration runner, or operated restore process. `v1.0.0` means the curriculum is complete—not that the software should handle real money.
