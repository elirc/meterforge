# MeterForge product and curriculum specification

## Product statement

MeterForge lets fictional SaaS businesses define products and prices, subscribe customers, ingest metered usage, calculate charges, issue invoices and credits, collect payments through replaceable providers, enforce entitlements, deliver webhooks, and reconcile financial records.

## Invariants

- Money carries an ISO currency and unlike currencies never combine implicitly.
- Every resource belongs to a tenant at every application and repository boundary.
- Issued invoices are immutable; corrections use credits or replacement documents.
- Usage ingestion, commands, payment attempts, and webhook delivery have stable idempotency identities.
- Effective-dated price versions never overlap.
- Aggregate writes use an expected version where concurrent decisions can conflict.
- Business state, audit evidence, and outbox messages commit atomically in a durable adapter.
- Public contracts do not expose domain or persistence types.

## Fifteen sprints

| Sprint | Outcome | Teaching focus |
| --- | --- | --- |
| 01 | Product and fixed-price vertical slice | dependency direction, contracts, Problem Details |
| 02 | Customer subscriptions and trials | aggregates and explicit transitions |
| 03 | Effective-dated price versions | temporal modeling and immutable history |
| 04 | Idempotent usage ingestion | deduplication and tenant scope |
| 05 | Tiered usage rating; `v0.5.0` | algorithms, money, boundary tests |
| 06 | Invoice calculation and finalization | immutable financial provenance |
| 07 | PostgreSQL-shaped durable adapter | transactions, migrations, adapter contracts |
| 08 | Subscription amendments and concurrency | compare-and-swap and conflict responses |
| 09 | Discounts, tax boundary, and credits | policy composition and corrections |
| 10 | Payment-provider adapter | timeouts, retries, and idempotency |
| 11 | Transactional outbox and webhooks | eventual consistency and delivery identity |
| 12 | Entitlements and usage limits | commercial policy versus authorization |
| 13 | Reconciliation, refunds, and disputes | ledgers and compensating records |
| 14 | Security/performance and five-flaw harvest | counterexamples and hardening |
| 15 | Operations and `v1.0.0` | observability, restore, incidents, judgment |

## Out of scope

Real card data, payment processing, tax advice, production accounting, PCI scope, microservices, Kubernetes, and copying any proprietary billing implementation or interface.
