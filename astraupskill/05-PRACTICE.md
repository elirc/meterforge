# 05 — Progress from a safe change to independent ownership

[Course home](README.md) · [Previous: tests](04-TESTING-AND-DEBUGGING.md) · [Solutions and review](06-SOLUTIONS-AND-REVIEW.md)

These are learner assignments. They are not additional delivered features. Work on a practice branch or copy, keep synthetic data, and record what you predicted before implementation. Read hints only after making a specific attempt.

## Exercise 1: explain a response without changing code

Run the [trace lab](07-TRACE-LAB.md). Save a compact transcript of create, replay, conflict and read outcomes. Draw the path for the 409 response, including the file that constructs the domain error and the middleware that writes the trace header.

Acceptance: your transcript includes the original stored ID, the replay ID, unchanged stored quantity after conflict, and matching error-body/header trace IDs. Explain why the final GET has `IsReplay = false`: it is a read response, not a retried ingestion result. Do not describe that field as a permanent attribute of the usage fact.

## Exercise 2: add a bounded external-event identifier

Story: an API maintainer wants a documented maximum external-event ID length so the service does not retain arbitrarily large keys.

Before coding, choose and justify a limit. Decide whether it applies before or after trimming, and whether you are counting UTF-16 code units or another representation. Put the business rule in the existing constructor and expose it through the existing exception mapping. Do not create a second conflicting validation rule in the endpoint.

Acceptance: tests cover exactly the limit, one over, whitespace-only input, and a padded valid identifier. Invalid input leaves storage unchanged. Existing valid replay tests still pass. Update the contract explanation so a client can generate compliant IDs.

Hint: C# `string.Length` is not a count of user-perceived characters. For a technical identifier, an explicit ASCII policy can sometimes be simpler, but changing accepted characters is a compatibility decision.

## Exercise 3: a conflict-aware client

Create a small client function or UI that submits a synthetic usage event. It must keep the same external ID for a network retry of the same action, retain the draft when the server reports 409/422, and display a useful status without parsing an English error message.

Acceptance: a simulated lost response followed by a retry creates one stored event; a changed quantity under the same key shows a conflict; a new deliberate event gets a new key; a pending request cannot be duplicated by repeated clicks. Tests should distinguish uncertain network failure from a known validation response.

Hint: “retry” and “submit a new action” need separate state transitions. Blindly generating a new UUID in a catch block loses the server's deduplication guarantee.

## Exercise 4: add a tenant-scoped list endpoint

Story: an operator wants to inspect recent usage without knowing an internal GUID. Design a GET collection endpoint with bounded page size, deterministic ordering and a clear filtering contract.

Write a design note before editing. Decide which timestamp determines ordering and how equal timestamps are broken. Define behavior for zero, negative and excessive page size. Explain whether your cursor remains meaningful as new events arrive. Do not claim a stable snapshot if your implementation only reads the current collection.

Acceptance: records from another tenant never appear; equal timestamps produce deterministic order; pagination does not depend on dictionary iteration order; invalid parameters get a documented client error. Add tests using several events with exactly equal times. Keep authorization as a separate unresolved boundary unless you actually implement it.

## Exercise 5: validate the parent relationship

Story: usage should only be accepted for a subscription belonging to the route tenant. The current use case does not check that relationship. Inspect [SubscriptionService.cs](../src/MeterForge.Application/Subscriptions/SubscriptionService.cs) and its repository contract before designing the change.

Acceptance: a valid owned subscription is accepted; a missing subscription and another tenant's subscription are rejected without inserting an event; identical replay behavior remains defined. Decide whether a replay is still readable/acceptable if the subscription later changes status. That is a product policy, not a fact you can infer from an interface name.

Hint: passing a tenant ID into a repository method is useful only if the implementation actually applies it. Tests should create two tenants and a real mismatched relationship rather than merely assert that a mock received an argument.

## Exercise 6: design a durable adapter

Write a short ADR and a schema proposal for PostgreSQL usage ingestion. Include the unique idempotency key, resource lookup, atomic insert-or-return behavior, payload mismatch detection and cancellation/uncertain-commit handling. This repository's existing PostgreSQL invoice adapter is a useful reference, but it does not already implement this feature.

Acceptance: the design explains two concurrent API processes, one created response, stable replay identity and restart behavior. If you implement it, use a disposable database and run the same application behavior contract against both adapters. Record any difference deliberately; do not call an in-memory-only test a database integration test.

## Exercise 7: make `FindByIdAsync` O(1) without breaking the winner

This one is small, concrete and follows directly from a limit the verification record already discloses. Do it before the capstone.

`InMemoryUsageEventStore` keys its dictionary by the *idempotency* identity, `(TenantId, ExternalEventId)`, because that is what `GetOrAddAsync` needs to be atomic. But the GET route added by this delivery looks a resource up by its *entity* identity, and the store answers it with:

```csharp
public Task<UsageEvent?> FindByIdAsync(string tenantId, Guid id, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(_events.Values.FirstOrDefault(item => item.TenantId == tenantId && item.Id == id));
}
```

That is a linear scan of every event in the process, on every GET. With ten events it is invisible; with a hundred thousand it is the endpoint's whole cost. The database version of this problem is "you have a unique constraint on the natural key but no index on the primary key you actually query by", which would be an odd mistake in SQL and is an easy one to make in a dictionary.

**Your change.** Add a second `ConcurrentDictionary<(string TenantId, Guid Id), UsageEvent>` and maintain both indexes.

**The part that is actually interesting.** Two dictionaries are two writes, and `GetOrAdd` on the first one is where the race is settled. Think carefully about ordering before you write anything:

- Write the secondary index **first** and the primary one second, and two concurrent ingests of the same key both publish to the secondary index — you now have two entries for one logical event, and the loser's entry is unreachable garbage that a GET can still find.
- Write the primary one first and the secondary one second, and there is a window in which `GetOrAddAsync` has returned a winner that `FindByIdAsync` cannot yet see. A client that follows the `Location` header immediately can get a 404 for a resource the server just created.

Neither is free. Decide which one you want, say why, and then make the *loser* path correct: on a replay, `GetOrAdd` returns somebody else's object, and you must not index your own candidate under its id. This is the one line that most attempts get wrong.

**Acceptance.**
- `Ingest_CompetingServices_ReturnExactlyOneCreationAndOneStoredIdentity` still passes unchanged — 64 tasks, one creation, one distinct id.
- A new test proves both indexes agree after the race: the id returned by the single winner is findable via `FindByIdAsync`, and **no other id from any of the 64 results is findable** (they are all the same id, so assert on the count of distinct findable ids being exactly one).
- A new test proves the tenant scoping still holds on the secondary index: `FindByIdAsync("other-tenant", winnerId, default)` returns null. (`Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal` already asserts this for the current implementation; make sure your change does not quietly drop it.)
- One paragraph naming the window you chose to leave open and what a client would observe if it hit that window.

Run it narrow while you work:

```powershell
dotnet test tests/MeterForge.Application.Tests --no-restore --filter "FullyQualifiedName~UsageIngestionTests"
```
```bash
dotnet test tests/MeterForge.Application.Tests --no-restore --filter 'FullyQualifiedName~UsageIngestionTests'
```

**Then answer the question that makes this exercise worth doing:** which of the two windows above still exists once the store is PostgreSQL with the schema from [02 §7](02-CONCEPTS.md)? (One of them disappears entirely, because a row insert publishes both "indexes" at once. The other changes shape rather than going away, because commit visibility is still a boundary.) Getting this right is the difference between "I added a dictionary" and "I understand what an index is for".

## Capstone: auditable usage corrections

A customer disputes a usage fact that may already have contributed to an invoice. Design a correction workflow that preserves the original record and explains the relationship between the original and correction. Define who can act, what can be corrected, how repeated correction requests behave, and what happens to downstream billing.

Deliverables: a one-page design, a small end-to-end implementation, meaningful failure tests, API examples and a review note describing durability/authorization limits. Include at least one rejected alternative, such as deleting history or silently overwriting quantity, with a business consequence. Keep payment and financial examples synthetic.

## Stop and review

You are ready to call the capstone complete when another engineer can run your checks, follow the request through real files, and challenge your consistency story without discovering an unexplained gap. Counting files changed or hours spent is not a substitute for those outcomes.
