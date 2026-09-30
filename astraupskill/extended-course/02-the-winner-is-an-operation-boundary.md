# 02. The winner is an operation boundary

Concurrency reasoning becomes easier when you identify the exact operation that chooses the stored result. In this implementation, that operation is ConcurrentDictionary.GetOrAdd on the tenant/external-key tuple. The surrounding service constructs a candidate and checks the returned winner. This chapter studies what that boundary guarantees, what it does not, and how to design tests that distinguish those claims.

Start with [the store](../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs) and [the application tests](../../tests/MeterForge.Application.Tests/Usage/UsageIngestionTests.cs). The tests create many service instances sharing one store. That arrangement matters: sharing the service type is not enough; the competing calls must share the authority that chooses the winner. The API registers one singleton store and scoped ingestion services, so requests in one process can reach that shared authority.

## Trace two candidates without inventing a schedule

Let candidates C1 and C2 have the same scoped key and identical business payloads. Each has a different generated Id. Both call GetOrAdd. One candidate becomes stored; the other call receives that stored candidate. The exact scheduling order is not part of the public contract. Tests should accept either identity as the winner while requiring all results to agree on it.

After the calls return, payload comparison succeeds for both. One attempt has candidate Id equal to stored Id and returns creation; the other has a different candidate Id and returns replay. With many identical contenders, the intended invariant is one creation classification and one stored identity across all results. A test that expects a particular task number to win would depend on scheduling rather than the operation contract.

Now change C2's quantity. Whichever candidate wins establishes the stored payload. The losing request receives that winner and detects a mismatch, so it conflicts. There is no rule that the smaller quantity wins or that the earlier task in a collection wins. If the business requires a predetermined winner, that requirement needs a different ordering or admission protocol.

## Atomic selection is narrower than a transaction

GetOrAdd chooses one dictionary value atomically for one key. It does not validate that the subscription exists in another repository, debit a budget, publish an event, or commit a database transaction. Those concerns are absent from this use case. Adding them before or after GetOrAdd would introduce new failure windows even if the dictionary operation remains correct.

Consider a proposed notification sent immediately after insertion. A failure after insertion but before notification could leave a stored event with no notification. Retrying the same usage would return replay. If the service only notifies on creation, the notification might never be sent. If it notifies on every replay, duplicate notifications become possible. This is a proposed extension problem, not a hidden feature of current source.

The solution cannot be summarized as use a thread-safe collection. The new requirement spans storage and another effect. A durable outbox or a replayable consumer protocol might be appropriate, but each needs explicit state and tests. The lesson is to identify the boundary that owns the whole invariant rather than borrowing the word atomic from one internal operation.

## Cancellation before and after responsibility

The service checks the token before constructing the candidate. The store checks it before GetOrAdd. A token already cancelled at either observation point prevents insertion. The existing cancellation test cancels before the call and then proves a later noncancelled request is a creation. That test is stronger than checking only that an exception occurred because it inspects the consequence for stored state.

There is no cancellation check after the dictionary operation in this store. If cancellation occurs after insertion has taken place, it does not remove the event. This is a reasonable local boundary to understand: a caller losing interest is not an instruction to roll back a fact that the store has already accepted. Do not infer a full HTTP disconnect policy from these two checks; the endpoint and host add other behavior.

The interval between the store's token check and GetOrAdd is also relevant. Cooperative cancellation is observed at checks, not continuously enforced by the runtime. A proposed test that cancels in that interval needs a controlled hook or wrapper and must be labeled as a designed schedule. Arbitrary sleeps do not prove where the signal landed.

## Object immutability supports the stored-winner rule

UsageEvent exposes get-only properties set by its constructor. The winning object is returned to callers, but the public API does not offer setters for quantity, meter, or key. That makes the stored winner easier to reason about than a mutable entity returned by reference. A caller cannot ordinarily change the usage payload through a public property assignment after insertion.

This observation does not establish every possible reflection or serialization behavior, and the course does not need those mechanisms to explain the ordinary boundary. Focus on the supported public API. If a future revision adds mutable properties or update methods, replay comparison and dictionary-key consistency need review. A key stored outside an object can become inconsistent with a mutable object's fields if ownership is unclear.

The record declaration also does not automatically make every future member deeply immutable. A get-only collection property could still expose mutable contents. The current payload uses scalar values and strings, which makes this particular invariant straightforward. State that concrete reason instead of treating the keyword record as a universal immutability guarantee.

## Test the invariant rather than the implementation spelling

The existing race fixture releases sixty-four tasks from a shared completion source and asserts one creation and one distinct identity. It also performs a later replay and compares the retained identity. This tests a useful operation-level result. It does not establish cross-process durability, because every attempt uses one in-memory store in one test process.

A proposed negative control can replace GetOrAdd in a disposable copy with an unsafe check-then-store sequence and introduce an explicit gate between the check and write. The gate makes both actors observe absence before either writes. Without that coordination, the unsafe version may appear to pass simply because the schedule never exposes the race. A test needs a discriminating schedule, not just many iterations.

Do not change the application to add a race for the sake of teaching. Use a signature-compatible copied store or a small educational model with its limitations stated. The goal is to show why ownership of the compound operation matters while preserving the user's source and existing working behavior.

## Exercise MF-02A: define the linearization claim

Describe the single operation that chooses the winner and the state visible before and after it. Explain why two service instances can still coordinate when they share one store, and why two independent stores cannot establish the same guarantee. Include identical and conflicting payload contenders.

## Exercise MF-02B: design a deterministic broken-store lab

Specify an unsafe check-then-write implementation in a disposable copy and a two-actor gate schedule that makes it fail. Write assertions for stored identity, creation count, and later replay. Explain why an ungated loop that happens to pass is insufficient evidence for the unsafe version.

## Exercise MF-02C: add one effect on paper

Propose sending an ingestion notification. Draw the failure windows around storage and sending. Explain why notifying only on creation can lose a notification after a crash and why notifying every retry can duplicate it. Identify a durable design candidate without claiming it is already implemented.

## Review checkpoint

A complete answer locates the operation boundary and keeps its scope narrow. It can explain a winner without predicting task scheduling, cancellation without promising rollback, and thread safety without claiming a multi-resource transaction. Its test design has a reason to reject the wrong implementation rather than merely producing a busy workload.
