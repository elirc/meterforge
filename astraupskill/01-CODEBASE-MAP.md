# 01 — Follow one business fact through the codebase

[Course home](README.md) · [Next: concepts](02-CONCEPTS.md)

The business fact for this course is “subscription S consumed quantity Q of meter M at time T.” An HTTP request reports that fact, but a request and a fact are different things: a network retry can produce a second request for the same fact. Keeping that distinction visible explains the design.

## Read these files in order

| File | Responsibility | Question to answer |
|---|---|---|
| [UsageContracts.cs](../src/MeterForge.Contracts/V1/Usage/UsageContracts.cs) | Public request and response records | Which fields identify the fact, and which describe the response? |
| [UsageEndpoints.cs](../src/MeterForge.Api/Usage/UsageEndpoints.cs) | HTTP binding and status codes | Why can the same POST return 201, 200 or 409? |
| [UsageEvent.cs](../src/MeterForge.Domain/Usage/UsageEvent.cs) | Valid, immutable business data | What is rejected or normalized before persistence? |
| [UsageIngestionService.cs](../src/MeterForge.Application/Usage/UsageIngestionService.cs) | Orchestration and replay policy | Who decides whether an existing key is a valid replay? |
| [InMemoryUsageEventStore.cs](../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs) | Atomic storage operation and reads | Where exactly is the winner chosen? |
| [BusinessRuleExceptionHandler.cs](../src/MeterForge.Api/Errors/BusinessRuleExceptionHandler.cs) | Domain-error-to-HTTP translation | Why should the domain not construct HTTP responses? |
| [Program.cs](../src/MeterForge.Api/Program.cs) | Dependency registration and middleware | Why is the store singleton while the service is scoped? |
| [UsageIngestionTests.cs](../tests/MeterForge.Application.Tests/Usage/UsageIngestionTests.cs) | Application behavior and concurrency checks | What evidence is stronger than “nothing threw”? |
| [UsageApiTests.cs](../tests/MeterForge.Api.Tests/Usage/UsageApiTests.cs) | Real routing, serialization and problem responses | Which bugs would a service-only test miss? |

Read a file once for its shape, then again with a specific question. Do not attempt to memorize every `using` statement. Follow the value called `candidate`, then `stored`, then `response`. Those names describe different stages of one operation.

## Request path

```text
HTTP POST + route tenant
    -> IngestUsageRequest
    -> UsageIngestionService.IngestAsync
    -> UsageEvent constructor: validate and normalize
    -> IUsageEventStore.GetOrAddAsync
    -> ConcurrentDictionary.GetOrAdd: choose stored winner
    -> compare business payload with winner
    -> UsageIngestionResult
    -> HTTP 201 (created) or 200 (replay)

different business payload for an existing key
    -> BusinessRuleException("usage.idempotency_conflict", ...)
    -> BusinessRuleExceptionHandler
    -> HTTP 409 Problem Details + trace ID
```

The domain does not know about `HttpContext`, JSON or ASP.NET. The store does not decide whether a changed quantity counts as a replay. The service does not know how a concurrent dictionary implements synchronization. These boundaries let you change one concern without making every other concern understand it.

## Read path

GET binds `{tenantId}` and `{id:guid}`, asks the store for that identity within that tenant, and returns either the response record or 404. The memory implementation scans stored values, so this is O(n) in the number of events. That is acceptable for this small in-memory lab, but it is an explicit performance limit. A database implementation should support the lookup with suitable keys/indexes and preserve the tenant predicate.

The GUID route constraint also rejects a malformed GUID before the endpoint body runs. A syntactically valid but unknown GUID reaches the store and produces 404. These two paths can share a status without being the same execution path; trace them separately while debugging.

## Dependency injection is object construction with a policy

### What `IUsageEventStore` deliberately does not offer

The contract has exactly two methods:

```csharp
public interface IUsageEventStore
{
    Task<UsageEvent?> FindByIdAsync(string tenantId, Guid id, CancellationToken cancellationToken);
    // Atomically return the stored winner for the tenant/external-event key.
    Task<UsageEvent> GetOrAddAsync(UsageEvent candidate, CancellationToken cancellationToken);
}
```

There is no `FindByExternalIdAsync`, and its absence is the design. A method that answers "is there already an event with this external id?" makes the broken sequence in [02 §3](02-CONCEPTS.md) — look it up, decide, then insert — the natural thing to write, and every caller who writes it reintroduces the race. `GetOrAddAsync` is the only way in, so the decision and the insert cannot be separated by anyone. An interface that makes the wrong thing unsayable is worth more than a comment asking people not to say it.

(That method did exist immediately after the repair, unused by production code and called only by two test assertions. It has since been removed and those assertions rewritten to go through the public behaviour: the race test now proves the winner survived by ingesting the same key again and asserting a replay with the same id, and the cancellation test proves nothing was stored by ingesting that key afterwards and asserting a *creation*. Both are stronger than the original reads, because they assert through the contract the application actually uses.)

`Program.cs` registers `IUsageEventStore` as a singleton. Requests in one host share its data. `UsageIngestionService` is scoped, so separate requests can have separate service objects but still compete on that shared store. A lock on a service instance would not coordinate those requests. This is why the invariant belongs in the storage operation.

Replacing the singleton with a scoped in-memory store would make every request see a different collection. POST could return 201 and a subsequent GET could return 404, even though both methods were individually correct. This is a useful example of a wiring bug: no syntax error is required for the application to violate its contract.

## Connect this slice to ordinary CRUD

In a customer-management app, contracts might be `CreateCustomerRequest` and `CustomerResponse`; the same route/service/repository separation still applies. Usage records differ because they represent historical facts. Editing or deleting them casually can invalidate invoices. The absence of an update/delete endpoint here is a domain choice, not a reason to add generic CRUD automatically. A correction workflow can record a compensating event with its own identity and provenance.

Sketch that difference: a contact's display name can be edited in place, while a metered event used in billing may require an auditable correction. A mid-level engineer asks what the entity means before choosing HTTP verbs.

## Checkpoint

Without opening a file, explain where you would change each policy: acceptable quantity; public error status; storage durability; what makes two events equivalent; caller permissions. Then verify your answers. If you put all five in the endpoint, redraw the map with the smallest appropriate owner for each policy.
