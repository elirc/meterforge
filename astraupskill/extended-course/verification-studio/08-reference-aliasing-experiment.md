# 8. Observe what a repository reference really owns

Inspect [InMemorySubscriptionRepository](../../../src/MeterForge.Infrastructure/Subscriptions/InMemorySubscriptionRepository.cs) and [the repository interface](../../../src/MeterForge.Application/Subscriptions/SubscriptionService.cs). GetAsync returns the stored Subscription instance. It does not build a detached snapshot. The concurrent dictionary protects its own operations; it does not turn every method on the referenced aggregate into a repository-mediated mutation.

## Predict before running

Construct an Active subscription at version one, add it to a fresh repository and retrieve it twice. Name the returned references first and second. Before running anything, predict ReferenceEquals(first, second), then call Pause on first. Inspect second.Status and retrieve the aggregate a third time. The source predicts that all references point to the same object, so both views observe Paused and version two.

This is an aliasing characterization, not proof of a concurrent exploit. It demonstrates that a caller holding a returned aggregate can invoke public methods without the repository's expectedVersion check. The production reachability of that caller requires separate inspection of application entry points. Avoid turning a local object behavior into an unsupported claim about an HTTP endpoint.

## Why expectedVersion still matters

MutateAsync acquires one repository-wide lock, looks up the aggregate, checks Version against the caller's expected value and invokes the supplied callback. When all writers use this method and callbacks obey its intended discipline, competing calls with the same expected version can have one accepted mutation followed by a version conflict. That is a meaningful property, but its assumptions belong beside it.

A public aggregate reference creates another possible writing path. The repository lock cannot protect code that does not acquire it. A test suite that exercises only MutateAsync may establish its narrow behavior while missing this ownership boundary. The lesson is to enumerate writers before claiming that every mutation participates in optimistic concurrency.

## A callback is not a transaction

Use a fresh Active aggregate. Invoke MutateAsync with expected version one and a callback that calls Pause, then throws a deliberate exception. Catch that exact injected exception. Retrieve the aggregate and predict Paused at version two. The mutation happened on the live object before the callback failed, and the repository does not clone or restore it.

This example should not be implemented by adding a fault to application source. A caller-supplied callback is sufficient in the isolated harness. Make the deliberate exception type recognizable so an unrelated constructor or version error cannot be mistaken for the intended injection. Record state after failure as well as exception type.

Now compare a callback that calls Activate on an already Active aggregate. That method rejects before mutation, so the aggregate remains Active at version one. Both callbacks throw, but only one changes state. A blanket rule that every exception means rollback would incorrectly group them.

## Add is a separate writer

AddAsync assigns the dictionary entry using the tenant and subscription ID. A second aggregate with the same key replaces the stored reference in this implementation. Construct two instances with the same identity but different products, add both sequentially, and inspect which object GetAsync returns. Treat overwrite behavior as another characterization rather than assuming create-only uniqueness from the method name.

An old reference can remain alive after replacement. Mutating the old object then need not alter the new dictionary value. This produces a useful ownership diagram: the dictionary points to one aggregate, while a caller may still hold another aggregate with the same logical identity. Equal identifiers do not prove shared object identity, and shared object identity does not prove durable consistency.

## Candidate designs

Propose three alternatives without editing application code. One returns immutable read snapshots and keeps all mutable aggregates private. Another performs copy-validate-swap under the repository boundary. A third moves concurrency to durable conditional updates and reloads a detached result. For each, identify which guarantees improve and which new costs appear.

Copying requires a complete aggregate copy, including version and all future fields. A shallow copy can preserve mutable aliases. A database transaction supplies different rollback behavior but does not automatically solve stale client intent or external effects. An immutable snapshot reduces accidental writes but may still become stale immediately. There is no single keyword that settles all of these issues.

## Review artifact

Submit a four-panel experiment record: shared reads, mutation after callback failure, duplicate Add replacement and stale reference after replacement. Each panel needs initial state, action, predicted observation, actual observation and scope. If you have only inspected source, leave actual observation marked unexecuted. A precise prediction is valuable without being presented as a completed test.

Finish with an application question: which callers can obtain the mutable aggregate, and which paths expose only mapped DTOs? The answer requires tracing actual callers. The local repository experiment creates the question; it does not answer the entire application authority model.

[Studio index](README.md) · [Review](reviews/03-subscriptions-and-snapshots.md)
