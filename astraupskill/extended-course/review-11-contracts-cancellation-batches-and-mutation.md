# Review 11. Contracts, cancellation, batches, and mutation

This review covers chapters 34 through 38. The exercises ask what a caller can infer from a response, an exception, a cancellation, or a completed task. Correct answers preserve the distinction between transport evidence and accepted business state. They also recognize that an optimization or convenience interface can create new obligations without changing the apparent method signature.

## MF-34A: trace replay projections

The first POST returns the accepted usage fields with IsReplay false and creation semantics. An exact repeated POST returns the same accepted identity and semantic fields with IsReplay true. GET returns the resource with IsReplay false because that flag describes the request path, not a permanently replayed event. The accepted UsageEvent itself has no mutable replay flag.

An SDK can represent stable resource data separately from response metadata such as replay classification, status, trace, and Location. This prevents a cache from treating a contextual flag change as corruption of the resource. The SDK should preserve the server's accepted identifier rather than substituting the client external key as though they were interchangeable.

Full credit requires recognizing that a response record can mix resource and operation context. Comparing every field across different endpoint types without understanding that distinction can produce a misleading test failure. Conversely, ignoring stable fields during replay would miss a real accepted-identity defect.

## MF-34B: design a durable invoice receipt

A proposed issuance response includes stable operation and invoice identity, accepted status, document retrieval location, evidence or schema version, and enough summary information for the immediate client. The retrieved document preserves complete accepted lines and provenance. Exact replay returns the accepted receipt; conflicting replay does not silently produce a new preview.

Total and line count cannot establish equivalence. Two line sets can share both while referring to different usage, price versions, or tax evidence. A canonical document fingerprint can detect differences, but the system also needs retained evidence to explain them. A hash alone is not an audit narrative.

The current finalize-preview response remains a useful summary of an in-memory calculation. The answer must not claim it already invokes durable storage or supports retrieval of a persisted document. The extension should identify exactly where that acceptance protocol would be added.

## MF-34C: review a contract change

Changing a payment response from confirmed-success-only to an operation resource with Pending and Unknown states requires clients to handle absent provider evidence under those states. Keeping a nonnullable string and filling it with an empty placeholder can hide the semantic change. A new version or explicit compatible extension should preserve old consumer expectations.

Similarly, renaming enum members changes ToString-based public status values. A compatibility fixture should contain representative old payloads and verify interpretation by the new reader. Test meaning, not just successful JSON deserialization. A field can parse while being interpreted under the wrong policy.

Full credit includes a rollout plan for old and new consumers and a statement of accepted historical evidence. Do not recalculate or relabel old receipts under a new meaning without preserving which contract produced them.

## MF-35A: map observation points

UsageIngestionService checks before candidate construction, and the in-memory store checks before GetOrAdd. The subscription repository checks before entering its lock. PaymentService forwards the token to the provider after local attempt construction or retry mutation; the simulated provider checks immediately. These are discrete points, not continuous rollback protection.

Cancellation after the final local check can still allow the synchronous dictionary or callback work to occur. A pre-canceled test proves the early guard, not every later interleaving. The Async suffix and Task return do not imply that work yields or remains interruptible between every statement.

A strong answer identifies the adapter used. A database or network adapter can have different cancellation aftermath, including unknown acceptance. Stable operation identity remains necessary to resolve that outcome after the caller stops waiting.

## MF-35B: test a canceled lock waiter

Use a first repository mutation callback that signals entry and waits on a test-controlled gate. Start a second mutation with a live token, allow it to reach the wait, cancel it, then release the first. The current lock does not observe the token while waiting, and there is no inside-lock recheck before mutation. The fixture must coordinate phases rather than rely on a sleep.

A proposed recheck inside the lock can reject the second caller before its callback runs. It does not undo the first accepted mutation or any effect already performed. The test should assert callback invocation, state, version, and exception separately. If the fixture cannot prove the second caller passed the initial check, it cannot distinguish the intended schedule reliably.

The review should also note the danger of using private-field reflection to force a test state that public behavior cannot produce. Controlled callbacks on fresh disposable instances provide a more faithful path for this experiment.

## MF-35C: separate business cancellation

A proposed cancel command identifies the accepted operation, authorized tenant, expected state or generation, and stable cancellation cause. It conditionally changes an eligible operation into a canceled or cancellation-requested state and returns a receipt. If external execution already crossed its point of no return, the response must expose the remaining obligation rather than pretending rollback occurred.

Request-token cancellation only stops or interrupts the transport-associated work where observed. It does not carry the same authority or durable intent as a business command. Closing a client connection should not silently erase accepted billing evidence. A production policy can choose to continue accepted work under a worker-owned token and recoverable state.

Full credit includes the race between cancellation and completion. One accepted ordering must determine the outcome, and the loser reads the receipt. A command that merely sets a Boolean without comparing current state leaves the race unresolved.

## MF-36A: specify mixed outcomes

The proposed partial batch reports A as created, B as exact replay, C as conflicting replay, and D as validation rejection. Each result correlates to the submitted item and includes accepted identity only where one exists. The batch summary does not claim one universal business outcome. Unprocessed items after interruption need a distinct representation or a documented resubmission strategy.

Duplicate external keys within the batch require a declared order policy. Sequential first-winner behavior can be deterministic; parallel acceptance may select either candidate unless the protocol defines a stronger ordering. Equal normalized payloads can replay safely, while different payloads cannot both become accepted meaning for the same key.

The input position is response correlation, not durable event identity. Reordering a retry should not create new usage. Full credit separates batch identity, item correlation, and external event identity instead of overloading one field for all three roles.

## MF-36B: compare atomic and partial modes

All-or-nothing acceptance requires one shared transaction or a staged acceptance mechanism covering the complete batch. A loop over current IngestAsync can leave earlier events accepted when a later item conflicts. It therefore implements neither automatic rollback nor an atomic batch merely because it runs within one request.

Under partial acceptance, a lost response after two items can be recovered by resubmitting stable item identities and obtaining accepted winners, or by reading a durable batch progress receipt. The response contract must explain which path exists. New external keys on retry would defeat the safety property.

A good answer also addresses validation ordering. Prevalidating all items can catch local errors before acceptance but still does not prevent later concurrent conflicts or infrastructure failures. It improves diagnostics without magically supplying transactionality.

## MF-36C: design a bounded client

The client retains original event keys, limits batch size and concurrent requests, correlates per-item results, and persists accepted receipts. It retries transient or unprocessed states under the declared policy while surfacing conflicting meaning for review. A lost entire response is safe only if the original identities survive locally.

A batch key adds value when the server preserves one accepted envelope and its result set. It does not replace event keys because events can appear in multiple batches. Reordered batches are equivalent only if the contract defines set semantics and canonicalizes duplicates; otherwise order can affect which conflicting item wins.

The rubric should require a clear policy for unknown item outcomes and a maximum work envelope. Launching unlimited tasks is not a complete throughput strategy, and interpreting every item error as a reason to generate a fresh identity is unsafe.

## MF-37A: characterize query work

Usage GetOrAdd uses the tenant and external-event tuple key, while FindById scans values for tenant and internal identifier. Product GetAsync uses its tuple key directly. PriceSelector filters supplied versions and materializes matches to distinguish none, one, and overlap. These are structural observations, not measured latency numbers.

A synthetic workload should separate new acceptance, exact replay, conflicting replay, early successful readback, late or absent readback, and different tenant distributions. Record dataset size and adapter mode. A benchmark that looks up only the easiest item cannot justify broad scalability claims.

Full credit combines cost analysis with correctness assertions. Before comparing time, verify that the implementation returns the accepted winner and preserves tenant scope. A faster incorrect result is not an optimization, and a precise stopwatch number without workload description is weak evidence.

## MF-37B: add a secondary index safely

Only the accepted winner should populate a proposed tenant-and-internal-id index. Two candidates with one external key can have different generated Id values; exposing both would create a readable losing resource. The acceptance boundary must coordinate the winner map and identifier map so a returned Location resolves coherently.

A process-local model can use one gate around the composite update. A durable adapter can use a transaction and indexes over accepted rows. The test coordinates competing candidates, asserts one accepted identifier, reads that identifier, and verifies the losing candidate identifier is absent. It should also test interruption or failure between conceptual index updates in the proposed model.

The review rewards recognizing that a performance index is additional state with consistency obligations. It is not merely a cache that can return arbitrary stale data when the API promises immediate readback of an accepted resource.

## MF-37C: review an optimization proposal

FirstOrDefault would choose one applicable price and hide overlap, changing the selector's business contract. Preserve zero, one, and multiple-match detection even if the optimized query retrieves at most enough rows to establish ambiguity. If uniqueness is guaranteed by publication, keep that guarantee explicit and still consider defensive selection behavior.

Event eviction can forget replay identity. A later old key can be accepted as a new resource unless a receipt or tombstone survives under a declared window. Archive payload and replay evidence separately if needed. The retention policy must say what clients can expect after the window rather than silently treating all old retries as new events.

Full credit discusses cache keys and catalog revision for price selection. Caching only by product can ignore date and revision. A performance change must preserve historical evidence, not merely reduce allocations in one happy-path benchmark.

## MF-38A: trace partial mutation

Pause changes an Active subscription to Paused and increments Version. A following Activate rejects because Paused is not Trialing. The repository callback throws, but the earlier mutation remains on the stored reference. The lock prevents interleaving through that gate; it does not undo completed assignments.

The current amendment service supplies one ChangeProduct call, so this composite schedule characterizes the broader Action interface rather than alleging that service executes it. A test should assert the retained Paused state and advanced version after the exception. Observing only the error misses exception atomicity.

Full credit distinguishes three guarantees: individual domain methods validate before their own mutation, the repository gate serializes participating writers, and a larger callback sequence lacks automatic rollback. Combining the first two does not imply the third.

## MF-38B: specify callback purity

Allow deterministic domain mutation based on explicit inputs and prohibit external network, file, or shared-service effects inside the callback. If a future adapter retries the callback, generated identifiers and timestamps should be captured outside or otherwise remain stable. A synchronous Action can still block on I/O, so the type alone does not enforce purity.

Describe consequences as data to be accepted with state, then dispatch them through a separate protocol. Copy-on-write can protect aggregate rollback but cannot undo a webhook already sent inside the callback. The review should require both object-state and external-effect analysis.

A strong contract states whether callbacks may run more than once and what happens on each exception category. Without that statement, application callers can accidentally depend on one invocation and introduce duplicate effects when an adapter adds optimistic retries.

## MF-38C: compare mutation strategies

In-place mutation preserves reference identity and exposes successful field changes immediately to holders of that reference. A late exception can leave partial state. Copy-on-write mutates a private candidate and publishes only after success, but old references remain old and allocation increases. Readers and tests must understand the changed ownership model.

A durable adapter may reconstruct objects per context and use conditional persistence, producing yet another reference behavior. Its contract should specify accepted state and conflict semantics without relying on one implementation's aliasing. The comparison should include callback retry and external effects, not just whether a clone method exists.

Full credit chooses a strategy for a declared scope and names its cost. It does not claim that copying automatically provides a distributed transaction or that locking automatically provides exception rollback. Those guarantees require their own mechanisms and evidence.
