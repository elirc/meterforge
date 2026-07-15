# Production-readiness assessment

`v1.0.0` marks curriculum completion, not readiness to handle real money.

| Area | Repository evidence | Real-production blocker |
| --- | --- | --- |
| Business rules | money, pricing, lifecycle, invoice, adjustment, reconciliation tests | validated accounting/tax policy and qualified owners |
| API | versioned routes, Problem Details, idempotency and ETags | identity provider, tenant claims, roles/scopes, quotas, compatibility governance |
| Persistence | Npgsql transaction and migrations | adapter wiring, migrations, full aggregate/outbox atomicity, contract tests, backup/restore |
| Payments | provider port, stable retry identity, reconciliation | compliant provider integration, webhook verification, secrets, ambiguous-outcome operations |
| Security/privacy | threat model and dependency audit | formal review, PCI scope decision, encryption, retention, audit, penetration testing |
| Operations | health, trace header, runbook and drill | telemetry backend, dashboards, alerts, load evidence, SLO ownership, on-call and DR rehearsal |

The repository is ready as a teaching/reference implementation. Real deployment remains blocked by authentication, operated durable storage, complete transaction/outbox integration, provider compliance, validated financial policy, privacy controls, and production observability.
