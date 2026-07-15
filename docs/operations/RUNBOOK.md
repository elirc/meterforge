# MeterForge teaching-service runbook

## Start and verify

```powershell
dotnet restore MeterForge.slnx --locked-mode
dotnet run --project src/MeterForge.Api
```

Verify `GET /health/live`, `GET /health/ready`, and Development OpenAPI at `/openapi/v1.json`. Record `X-Trace-Id`, UTC time, tenant, route, status, deployment SHA, and idempotency identity during triage. Never paste provider secrets or complete financial payloads into tickets.

Readiness reports `durableDefault: false`. A default-process restart loses in-memory state. The presence of PostgreSQL adapter code does not mean the running API is durable.

## Incident sequence

1. Determine affected tenants, invoices/payments, time range, and whether callers retried.
2. Separate business rejection (`422` plus stable code), concurrency conflict, provider ambiguity, tenant miss, and unexpected failure.
3. Pause the affected workflow before attempting manual repair of money or duplicate side effects.
4. Reconcile internal identity/amount with provider facts. Preserve the original `Idempotency-Key`.
5. Use credits/refunds/compensating records; never edit finalized financial history.
6. Resume only after invariant checks, queued-work reconciliation, and a regression test or explicit risk acceptance.

## Candidate objectives

These are design candidates, not measured claims: 99.9% monthly API availability, 99% synchronous latency below 500 ms, outbox publication within 60 seconds, recovery time below 60 minutes, and recovery point below 15 minutes. A real owner must validate user impact, load, dependencies, and cost.

## Recovery

The teaching API has no supported backup or restore. A real deployment requires migration ownership, encrypted backups, point-in-time recovery, restore rehearsal, compatible rollback, outbox-lag validation, provider reconciliation, and documented RTO/RPO evidence.
