# 38. Mutation callbacks and exception atomicity

Read [ISubscriptionRepository](../../src/MeterForge.Application/Subscriptions/SubscriptionService.cs) and [InMemorySubscriptionRepository](../../src/MeterForge.Infrastructure/Subscriptions/InMemorySubscriptionRepository.cs). The repository accepts an Action of Subscription and invokes it inside a lock after checking the expected version. This chapter explores a subtle distinction: mutual exclusion prevents competing code from interleaving through that gate, but it does not automatically roll back mutations when a callback throws.

The prerequisite is chapter 08's reference-ownership analysis. The dictionary stores a mutable Subscription reference. The callback receives that same object. The current amendment service supplies one ChangeProduct call, whose own guards run before its mutation. The broader repository interface nevertheless permits callbacks containing several mutations or additional code.

## Work a callback with a late failure

Start with an Active subscription at Version one. A synthetic callback calls Pause, which succeeds and advances the object to Paused at Version two. It then attempts Activate, which rejects because Activate requires Trialing. The callback throws, but the earlier Pause already changed the object. Exiting the lock releases mutual exclusion; it does not reconstruct Version one.

This schedule demonstrates partial mutation within one callback. It does not claim the current ChangeProduct service executes that sequence. The lesson concerns the interface's general contract and future callers. A repository method named MutateAsync can be interpreted as transactional by readers unless its exception semantics are documented.

The caller may receive an exception while a later GetAsync observes Paused and Version two. If the caller assumes exception means no change, it can issue an incorrect retry or report misleading state. A test should assert both the thrown error and the retained object state. Observing only the exception misses the important effect.

## Locking and rollback solve different problems

The lock makes the expected-version check and callback invocation mutually exclusive relative to other calls using the same gate. It helps ensure one writer based on a particular version wins. It does not isolate readers that obtain shared references and mutate them outside the gate, nor does it create a transaction log for undoing callback effects.

A proposed copy-on-write repository can clone the current aggregate, invoke the callback on the private candidate, and replace the stored object only after success. That changes reference semantics: callers holding the old object no longer observe in-place updates. The application contract and tests must adapt deliberately rather than assuming the change is invisible.

Another proposal restricts repository mutation to a narrow command object or validated state transition rather than an arbitrary Action. The repository can then construct the new state from explicit inputs. This reduces the range of possible partial side effects, but it also changes extensibility and domain ownership. Choose a design based on the needed contract, not merely a preference for delegates or records.

## External effects must stay outside callbacks

An arbitrary callback could send a notification, write a file, or call another service before throwing. Even a copy-on-write aggregate cannot roll back those external effects. A repository contract should prohibit such effects inside its mutation callback or explicitly model them as durable obligations accepted with the state change.

For a proposed amendment workflow, the callback can change domain state and return a description of consequences. The acceptance layer then stores state and outbox intent together in a durable implementation. A dispatcher performs external work later. In the current in-memory model, this remains a design proposal rather than an implemented transaction.

Do not perform slow network calls while holding a global mutation lock. That would serialize unrelated subscriptions and make latency depend on an external system. The current Action signature is synchronous, which discourages ordinary awaited I/O but does not prevent blocking calls. Documentation and interface design should make the intended pure mutation boundary explicit.

## Exception-safe domain methods help but do not compose automatically

The shown Subscription methods validate their state and required arguments before changing fields. A single rejected method therefore leaves the earlier state unchanged through that method's code. This is useful local exception safety. Combining two successful or failing methods in one callback creates a larger sequence whose atomicity must be considered separately.

The same distinction appears in invoice building. Adding several valid lines and then rejecting a later invalid line leaves the earlier lines in the in-memory draft. That may be acceptable if the draft is discarded after validation failure. It is not an all-or-nothing collection operation unless a surrounding builder or transaction provides that behavior.

A test portfolio should include individual method rejection and composite callback failure. The first protects local guards; the second documents repository semantics. Avoid declaring the repository transactional solely because every current domain method happens to validate before mutation.

## Design a stronger mutation contract

Specify what happens on callback success, business-rule exception, unexpected exception, and cancellation. State whether the callback can run more than once, whether returned references are live, and whether mutation is all-or-nothing. A durable optimistic implementation may retry a computation, so callbacks with hidden side effects become especially dangerous.

If callbacks can be retried, they should be deterministic over the supplied state and explicit inputs, or the system must preserve generated identities and timestamps from outside the retry loop. Generating a new external operation key inside a retried callback can create multiple intended effects. The retry contract is part of correctness, not an implementation detail.

For a teaching prototype, implement two disposable repositories: current in-place semantics and proposed copy-on-write semantics. Run the same late-failure schedule and compare results. Do not replace the application repository. The contrast makes the tradeoff tangible and gives the learner evidence to choose a future design.

## Exercise MF-38A: trace partial mutation

Use Pause followed by invalid Activate on an initially Active subscription. Predict state and Version after the callback throws. Explain what the lock guarantees and what it does not undo. Distinguish the general interface scenario from the current one-call amendment service.

## Exercise MF-38B: specify callback purity

Define allowed callback operations and forbidden external effects. Include generated identifiers, timestamps, and retry behavior. Explain why a synchronous Action can still violate the intended boundary through blocking I/O or shared-state mutation.

## Exercise MF-38C: compare mutation strategies

Design an in-place and copy-on-write disposable model for the same command. Compare exception aftermath, reference identity, reader observations, and allocation cost. State which behavior a durable adapter must document rather than assuming interface signatures settle it.

## Review checkpoint

Mutual exclusion, exception safety, and transactionality are different guarantees. A strong answer can point to the code that provides each guarantee and identify what remains absent. It does not let a lock or an Async suffix imply rollback that the implementation never performs.
