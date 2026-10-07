# Reference review — MeterForge `main` @ 43ee18d (+ lab branch pass)

Ranked by production severity for a **usage-billing** product. Line numbers are from `git show HEAD:<path>`.
[AI-smell] = typical old-AI-generated smell.

> **Note added 2026-10-06 (this record is frozen at `43ee18d`; grade your review against that commit).** Commit `4e3f344` (2026-09-30) changed `main` after this answer key was written:
> - **Findings 1–3 are fixed on today's `main`.** `InMemoryUsageEventStore.GetOrAddAsync` uses `ConcurrentDictionary.GetOrAdd` and returns the winner. `UsageIngestionService.cs:23-30` normalizes before the key is used and throws `usage.idempotency_conflict` on a changed payload. `BusinessRuleExceptionHandler.cs:13-14` maps that code to **409**. Tests: `UsageIngestionTests` (race, normalized replay, 4 changed-payload cases) and `UsageApiTests.CreateReadReplayConflict_PreservesTheOriginalResource`.
> - **Finding 10 is narrowed, not closed.** Only `usage.idempotency_conflict` became 409. `subscription.version_conflict` is still 422.
> - **Findings 4–9 and 11–20 still hold on today's `main`.** The line numbers moved: `Program.cs:27-30` is now `31-34`, and `47-53` is now `56-62`. `PaymentEndpoints.cs:14-17`, `PaymentService.cs:18`, `SubscriptionEndpoints.cs:26` and `TieredRating.cs:33` are unchanged.
> - **Findings 21–25 cite the `training/lab/sqlserver` branch.** It is not on GitHub (see the status note in [`../README.md`](../README.md)).

## S1 — money wrong, data lost, or security

1. **The ingestion path can report "created" for an event it never stored.** `UsageIngestionService.cs:19-24` does find-then-add; `InMemoryUsageEventStore.cs:21` calls `TryAdd` and **ignores the `false` result**. Two concurrent deliveries of the same event both miss the lookup, both get `IsReplay=false`, and both receive different `id`s, but only one is stored. The client stores an id that doesn't exist. *Why it matters:* SDKs log/reconcile by the returned id; support can't find the event; if the payload differed, the billed quantity is whichever write won. *Fix:* make the store's add atomic and return the stored winner (`GetOrAdd`, or a unique index + read-back on SQL). [AI-smell] (check-then-act with a discarded return value).
2. **Whitespace makes a replay look new.** The domain trims `ExternalEventId` (`UsageEvent.cs:20`), but the lookup uses the raw string (`UsageIngestionService.cs:19`). `" evt_1"` misses the stored `"evt_1"`, a new event is built, `TryAdd` silently fails (finding 1), and the caller gets `201 Created` with a phantom id. *Fix:* normalize once at the boundary, and use the same normalized key for lookup and storage.
3. **A replay with a different payload is accepted silently.** Same `external_event_id`, different `quantity`/`meter` → returns the old event with `200`. For billing, that's either a client bug or tampering, and it should be `409 Conflict` with both payloads logged. *Fix:* compare a payload fingerprint on replay.
4. **No durable storage in the running app, but the readiness probe says "ready".** `Program.cs:27-30` registers only in-memory singletons. `Program.cs:47-53` always returns `ready` with `postgresAdapterAvailable = true`, while the Npgsql adapter is never registered. A restart loses all usage = lost revenue, and the orchestrator will happily route traffic. [AI-smell] (health check that checks nothing; docs/flags claiming an integration).
5. **No authentication or tenant binding.** Every route takes `{tenantId}` from the URL (`UsageEndpoints.cs`, `PaymentEndpoints.cs`), and nothing checks who is calling. Anyone can post usage onto another tenant's bill or collect payments against any invoice id. (Admitted in `PRODUCTION-READINESS.md`; it still blocks any real use.)
6. **Payments endpoint: `Idempotency-Key` required but not honoured by the API.** `PaymentEndpoints.cs:14-17` → `PaymentService.cs:18` creates a **new** `PaymentAttempt` with a new id on every call; API-level dedupe is delegated entirely to the provider. And the amount is not checked against the invoice (no invoice lookup at all), so any amount can be collected for any invoice id. [AI-smell] (header ceremony without the behaviour).

## S2 — production incident likely

7. **Failed payments return `200` with `providerReference: null`**: `PaymentEndpoints.cs:17` uses `payment.ProviderReference!`. The null-forgiving operator hides a contract lie; clients treat 200 as paid. And `SimulatedPaymentProvider` always succeeds, so no test exercises the failure branch. [AI-smell]
8. **In-memory subscription mutation isn't atomic for readers.** `InMemorySubscriptionRepository.cs:30-36` locks for writers, but `GetAsync` (`:22`) returns the **same live object** without the lock, so readers can see a half-applied mutation, and a mutation that throws midway leaves partial state with the version bumped or not. *Fix:* copy-on-write (clone, mutate, swap).
9. **`ChangeProduct` accepts any product id**: no existence or same-tenant check (`SubscriptionService.cs`, amendment path). A subscription can point at another tenant's product or at nothing, and invoicing then prices it from whatever resolves.
10. **Every rule violation is 422**: `BusinessRuleExceptionHandler.cs:13`. `subscription.version_conflict` should be `409`/`412` (clients retry-on-conflict logic can't distinguish it from bad input), `price.not_found` is not "unprocessable". API design debt that becomes a client bug.
11. **"Features" that are calculators.** `entitlements/check` evaluates entitlements **and consumption supplied by the caller** (`EntitlementEndpoints.cs`); `reconciliation/payments` compares two amounts the caller sends; `invoices/finalize-preview`, `webhooks/retry-preview` persist nothing. They read like shipped capabilities in the course map. [AI-smell] (reskinned endpoints presented as features).
12. **Outbox table without a writer or publisher.** `002_outbox.sql` and `OutboxMessage` exist; nothing writes an outbox row in the same transaction as an invoice, and nothing publishes. Sprint 11 "Outbox and webhooks" is a schema plus a model. [AI-smell]
13. **`PostgresInvoiceSnapshotStore` is untested against Postgres and blind on conflict.** `on conflict ... do nothing` on invoices **and** lines (`:19`, `:38`): re-saving the same invoice id with different lines silently merges line sets, so "immutable snapshot" isn't verified. No test runs the adapter (only `InvoiceSnapshot.From` is tested). [AI-smell] (integration with no integration test).
14. **Events with any `occurred_at` are accepted.** `UsageEvent.cs` has no bounds: an event dated last quarter lands in an invoiced period, one dated next year sits in the future. For metering, you need a lateness window and a policy for late events (credit/debit adjustment).

## S3 — operability, tests, maintainability

15. **The CI vulnerability step can never fail.** `ci.yml:23` runs `dotnet list package --vulnerable`, which exits 0 even when it finds vulnerabilities. [AI-smell] (a gate that isn't one).
16. **Architecture test checks only the domain.** `DependencyDirectionTests.cs` asserts `Domain` has no outward references; nothing stops `Application` from referencing `Infrastructure` or ASP.NET.
17. **Weak ETag handling.** `SubscriptionEndpoints.cs:26` `Trim('"')` rejects `W/"3"`; `GET` never emits an `ETag` header, so clients must scrape `version` from the body.
18. **Per-tier rounding.** `TieredRating.cs:33` rounds each tier, then sums; with many tiers, total ≠ round(sum). It may be the intended policy, but it isn't written down or tested as such.
19. **`DateTimeOffset.UtcNow` in the finalize endpoint** (`InvoiceEndpoints.cs`) instead of an injected `TimeProvider`: untestable time, and a pattern that spreads.
20. **Product codes aren't unique per tenant** (`InMemoryProductRepository` indexes by id only).

## Lab branch pass (`training/lab/sqlserver`)

21. **`SqlServerInvoiceSnapshotStore.SaveFinalizedAsync` is check-then-insert.** Two concurrent saves both see "not exists"; the second fails with a PK violation (2627) as an unhandled 500 instead of the Npgsql adapter's silent no-op. Different contract from its Postgres twin.
22. **`SqlServerUsageEventStore` turns the dedupe race into a 422** (`usage.concurrent_duplicate`) rather than returning the stored winner. Safe for money (one row), unfriendly for SDKs.
23. **`UsageRollupJob` 50k-row `UPDATE TOP` in one transaction** will escalate to a table lock (≥ 5,000 locks) and block ingestion. See M3.
24. **`EF.Constant(tenantId)` in `UsageReadModel`** trades plan-cache bloat for plan stability, and nobody wrote down why. See M2 and incident 001.
25. **`ux_usage_events_id` is a unique index on a random GUID**: every insert lands on a random page of that index (page splits, fragmentation) even though the clustered key is sequential. See S2.

**Scoring.** 12+ findings with 4 of the S1 items = strong. Missing findings 1 and 2 means you didn't trace one request end to end.
