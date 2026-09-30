# 08. Subscription version and reference ownership

Subscription amendments introduce a different state model from immutable usage facts. The domain object is mutable, transitions advance Version, and the repository exposes a mutation callback guarded by one lock. This chapter studies the useful guarantee of that callback boundary and the limits created by returning live object references. The goal is to understand ownership rather than assuming a version number protects every mutation automatically.

Read [Subscription](../../src/MeterForge.Domain/Subscriptions/Subscription.cs), [SubscriptionService and SubscriptionAmendmentService](../../src/MeterForge.Application/Subscriptions/SubscriptionService.cs), and [InMemorySubscriptionRepository](../../src/MeterForge.Infrastructure/Subscriptions/InMemorySubscriptionRepository.cs). If the application service file name differs in a future revision, follow the interface declaration rather than a remembered path. The present source defines AddAsync, GetAsync, and MutateAsync with an expected version and Action callback.

## Derive the lifecycle table

A subscription without a trial end starts Active; one with a trial end starts Trialing. The constructor rejects a trial end before the start date but permits equality. Activate accepts only Trialing. Pause accepts only Active. Resume accepts only Paused. Cancel accepts any state except already Cancelled. Every successful transition increments Version from its initial value of one.

ChangeProduct rejects Cancelled and an empty product Id, then assigns ProductId and increments Version. It does not check whether the new product equals the existing product. A same-product amendment therefore still advances Version under current behavior. It also does not verify that the target product exists in a product repository. These observations help distinguish domain transition rules from cross-aggregate validation.

The domain methods do not accept expectedVersion. That comparison belongs to repository MutateAsync for callers using that operation. Calling ChangeProduct directly on a Subscription object bypasses the repository's stale-version check. A learner should be able to point to both boundaries rather than saying the entity is optimistic-concurrency-safe without qualification.

## Check and mutate under one gate

MutateAsync checks cancellation, acquires the repository's mutation gate, finds the tenant/id entry, compares Version with expectedVersion, invokes the callback, and returns the subscription. The comparison and callback occur within the same lock. Two repository-mediated writers carrying version one cannot both pass if the first callback increments Version before the second checks it.

The existing application test releases two concurrent amendment calls and expects one success, one subscription.version_conflict, and final Version two. That is a useful local guarantee for this operation. The exact winning product is scheduling-dependent. A test should not assign priority to the first task in an array unless the contract provides such ordering.

The gate is shared across the repository rather than keyed per subscription. This simplifies mutual exclusion but serializes mutations for unrelated subscriptions as well. That is a performance design tradeoff, not automatically a defect for this teaching implementation. A per-key lock proposal would need lifecycle, cleanup, and atomicity reasoning rather than merely replacing one lock with a dictionary of locks.

## Live references weaken exclusive ownership

AddAsync stores the supplied Subscription reference. GetAsync returns the stored reference. MutateAsync also returns it. Therefore callers can retain a live object and invoke public domain methods outside the repository gate. The lock protects the repository-mediated operation, not every possible mutation of that object throughout the process.

Consider a caller that fetches version one and directly calls Pause while another caller uses MutateAsync. The direct method does not acquire the gate or compare an expected version. This is a designed counterexample to an overly broad ownership claim, not a claim that the ordinary endpoint currently uses that sequence. The repository interface allows an escape path that a stronger design might close with snapshots or restricted mutation authority.

The immutable UsageEvent store from earlier chapters has a different exposure profile because its public payload properties are get-only. Returning a reference is not automatically wrong; its consequences depend on the object's mutability and supported methods. Compare the two cases concretely rather than applying a blanket rule that every repository must clone every object.

## Callback failure is not rollback

MutateAsync invokes an arbitrary Action on the stored object. If that callback performs a valid mutation and then throws, the lock is released by normal language semantics, but the earlier object mutation remains. There is no copy-and-commit candidate or rollback mechanism in this repository. A lock establishes mutual exclusion, not transactional reversal of arbitrary callback work.

The provided amendment service passes a single ChangeProduct call whose guards precede assignment, which keeps its ordinary failure behavior simple. That does not prove every future callback is safe. A proposed compound amendment should either validate completely before effects, mutate an isolated candidate and publish it, or use a persistence transaction with appropriate semantics. The choice depends on the required invariant.

This distinction makes a good incident exercise. A callback changes product, then attempts a second action that throws. The caller observes failure, but ProductId and Version may already have changed. A test should inspect state after the exception, not assume that exception equals no change. The report must label this as behavior of the general callback boundary rather than an executed HTTP incident.

## Cancellation and durability remain scoped

The repository checks the token before entering the lock. It does not check again after waiting for the gate. Cancellation while blocked may therefore not prevent mutation once the gate becomes available. A proposed stronger cancellation contract needs a check at the actual responsibility boundary and tests with controlled contention. The current precheck is still useful for already-cancelled calls.

The dictionary and lock live in one process. Version conflict handling here does not establish database durability or multi-process coordination. A durable adapter could use an atomic conditional update where stored Version equals expectedVersion, but its behavior must be verified against the real provider. The domain Version is evidence to compare; it is not a distributed lock by itself.

## Exercise MF-08A: write the lifecycle matrix

List every state and action combination, the successful next state, and the Version change. Include equal start/trial dates and a same-product amendment. Explain which cross-aggregate facts are not validated by the domain object.

## Exercise MF-08B: distinguish guarded and escaped mutation

Draw one two-writer trace through MutateAsync and one trace where a caller mutates a live GetAsync result directly. State which guarantee the repository gate establishes and which ownership assumption the second trace violates. Propose a snapshot or restricted-command design without changing application source.

## Exercise MF-08C: design a partial-callback incident

Specify a callback that changes state and then throws. Predict the retained object and Version after failure. Compare validation-before-effect, isolated-candidate publication, and a durable conditional update as possible design responses. Include the cancellation-before-lock limitation in the review note.

## Review checkpoint

A strong answer distinguishes version data, atomic compare-and-mutate, object ownership, and rollback. It can praise the actual guarded operation without claiming it controls escaped references or survives a process restart. It also recognizes when a small single-action callback is safer than an unconstrained compound mutation.
