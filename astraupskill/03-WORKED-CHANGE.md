# 03 — Repair an operation, then repair its public contract

[Course home](README.md) · [Previous: concepts](02-CONCEPTS.md) · [Next: tests](04-TESTING-AND-DEBUGGING.md)

## Start with the observable problem

The original ingestion service first searched for an external event ID and then called `AddAsync`. The adapter used `ConcurrentDictionary.TryAdd`, but discarded the boolean result. If two callers observed an absent key, the losing caller still returned its own newly generated event as a creation. Storage contained one event, while responses could claim two different events existed.

There was also a deterministic error without concurrency: replaying an existing key with a changed quantity returned the original event as though the changed request had succeeded. A billing client could think quantity 11 had been accepted when storage still held quantity 10. Avoid describing that as harmless deduplication; it hides a mismatch in business intent.

## Step 1: give the repository an operation that can keep the invariant

Read [the interface and service](../src/MeterForge.Application/Usage/UsageIngestionService.cs) alongside [the adapter](../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs). The new method returns the stored event, whether it inserted the candidate or found an existing winner:

```csharp
var stored = _events.GetOrAdd(
    (candidate.TenantId, candidate.ExternalEventId), candidate);
return Task.FromResult(stored);
```

The method uses the value overload. Constructing a candidate GUID is cheap and has no external side effect. Do not put a payment call, email send or outbox write inside a dictionary value factory and assume it runs exactly once. The store's return value, not merely the fact that code attempted an insertion, determines the outcome.

For this adapter, the dictionary operation chooses the winner atomically. There is no `await` between a separate lookup and insertion because there is no separate lookup/insert protocol in the service any more. A future database adapter must implement the same semantic contract using database concurrency controls; copying the in-memory implementation's class name is not enough.

## Step 2: normalize before choosing the key

The service constructs a `UsageEvent` first. Its constructor validates the IDs and quantity, trims the meter and external event ID, and converts the time to UTC. Only that normalized candidate reaches `GetOrAddAsync`.

```csharp
var candidate = new UsageEvent(Guid.NewGuid(), tenantId,
    subscriptionId, meter, quantity, occurredAt, externalEventId);
var stored = await store.GetOrAddAsync(candidate, cancellationToken)
    .ConfigureAwait(false);
```

Trace the string `" event-1 "` through these lines. There is now one representation of the idempotency key throughout the storage operation. Normalization is deliberately not duplicated in the endpoint and adapter; duplication would allow those rules to drift.

## Step 3: verify that a replay means the same business fact

The service compares the stored and candidate subscription, meter, quantity and occurrence time. If any differ, it throws a stable `usage.idempotency_conflict` business-rule exception. The existing event is immutable, so rejecting the second payload does not change it. If they match, the service returns the stored event and determines replay from its identity relative to the candidate.

The API's exception handler maps this specific code to 409. Other business validation errors retain 422. A dedicated error code lets a client show a useful recovery action without parsing an English message. Changing wording should not break the client.

## Step 4: make the creation link useful

The POST endpoint already emitted a resource `Location`, but there was no corresponding usage-event GET route. The new GET endpoint reads by both tenant and event ID and returns 404 if that pair is absent. The integration test follows the exact `Location` returned by POST, rather than constructing a separate URL and accidentally overlooking a malformed link.

This is a general CRUD review habit: when an endpoint returns an identifier, pagination cursor or related URL, test what a consumer can actually do with it. A response body can look correct while its navigation contract is unusable.

## Step 5: let a regression reveal a second bug

The new conflict test expected `X-Trace-Id`, but the first run failed. The old middleware set the header before the error handler produced its response. Error handling cleared it. Service tests could not see this because they do not run HTTP middleware.

The fix registers the header with `Response.OnStarting` and places that middleware around exception handling. The callback adds the trace ID as the actual response starts, including handled errors. The test checks both that the header exists and that it equals the Problem Details `traceId`. This connects what a caller reports to the same request identifier used in the error body.

The framework's default Problem Details writer can supply its own trace identifier. Explicit `CustomizeProblemDetails` configuration now makes the body use the same `HttpContext.TraceIdentifier` as the header. Checking equality exposed that second inconsistency; checking only that a header existed would have missed it.

## Review the change as a colleague would

Explain the user-visible behavior before listing files. Then identify compatibility: the successful request/response shape is unchanged; changed-payload retries now return a conflict; a missing read route now exists; the internal store interface changed and its sole adapter was updated. No dependency versions or database migrations were needed.

Finally, state limits. A tenant-scoped read is not an authorization system. Memory is not durable. Lookup by internal ID currently scans the in-memory collection. These statements make the review more useful because they tell the next engineer what must remain explicit when extending the feature.
