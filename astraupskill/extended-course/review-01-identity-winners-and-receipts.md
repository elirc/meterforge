# Review 01: identity, winners, and receipts

Try the twelve exercises before reading the answers. For the identity questions, begin with three separate columns: dictionary key, payload comparison, and attempt metadata. For concurrency, draw the store operation rather than guessing thread order. For HTTP recovery, distinguish an observed rejection from an outcome the client never received. These hints apply to this source path; they are not a substitute for inspecting the concrete methods.

## MF-01A: three roles, not one request identifier

The service generates the internal event Id. The caller supplies external key, tenant, subscription, meter, quantity, and time. The store key contains tenant and external key. The replay comparison contains subscription, meter, quantity, and occurrence time. The candidate Id determines whether the returned winner is this attempt's candidate, so it contributes to IsReplay rather than business-payload equality.

Subscription is excluded from the store key but included in payload comparison. Reusing one tenant/event key with another subscription therefore conflicts. A design that adds subscription to the unique key changes the meaning of producer identity and could accept facts previously rejected. That is not a harmless index optimization.

Full credit requires explaining why a new candidate Id on every retry is expected. An answer that compares generated IDs as business data would reject ordinary replays. An answer that ignores the tenant component would merge independent producers across tenants.

## MF-01B: normalization has boundaries

Meter whitespace is trimmed, so the pair can replay if every other field agrees. Meter case is not normalized, so changing it conflicts under the same key. External-key whitespace is trimmed before key selection, while case remains distinct and can create a separate event. Tenant whitespace is not trimmed by this constructor, so two nonblank differently spaced tenant strings form different keys. Equivalent timestamp offsets normalize to the same UTC instant.

A proposal to lowercase keys or trim tenants must investigate collisions among existing values. It must also align route resolution, authorization context, persistence, and client expectations. Normalizing only one layer can make an event creatable under one spelling and unreadable under another. The source lesson is precise observation before improvement.

## MF-01C: retry the fact, not the function call

The producer should generate and retain an external key for the business fact before sending it, then preserve the immutable payload during retries. A timeout leaves acceptance uncertain. An identical retry can recover the stored winner while the current in-memory store survives. A new external key describes a new fact and can duplicate accounting intent.

A correction needs a separately defined operation or adjustment policy. It should not masquerade as an identical retry with changed quantity. The note must also disclose that server restart loses the default dictionary; the current API does not provide a durable receipt across that boundary.

## MF-02A: shared authority selects the winner

GetOrAdd on the tenant/event tuple is the relevant operation. Multiple service instances coordinate when they use the same dictionary-backed store. Two independent stores each permit their own winner, so matching source code alone does not create cross-process coordination. The API's singleton registration supports sharing within one process, not across replicas.

Identical contenders return one stored identity with one creation classification. Conflicting contenders can establish either payload depending on which wins; the loser conflicts. Tests should assert invariant outcomes rather than a particular task number winning.

## MF-02B: make the bad schedule observable

An unsafe store first checks absence and later writes. A deterministic fixture places a gate after both actors have observed absence, then releases both writes. Each can believe it created a value, and the final dictionary may retain only the last assignment. The test should detect multiple creation classifications or disagreement with the later retained identity.

Without the gate, a sequential-looking schedule can hide the defect. Repeating the test many times increases opportunity but does not explain the failure mechanism. The disposable negative control should preserve signatures and never replace the user's application store.

## MF-02C: notification adds a second resource

Insertion followed by notification creates a gap where accepted usage exists but no notification was sent. Notifying only on creation means a replay may never repair that gap. Notifying every replay can duplicate delivery. A durable intent stored with the accepted fact and an idempotent consumer is one design candidate.

The proposal still needs dispatch acknowledgment and retry semantics. An outbox does not make a remote receiver participate in the local transaction. Full credit requires naming both the acceptance identity and downstream processing identity rather than asserting exactly once without a mechanism.

## MF-03A: resolve uncertainty with stable identity

Created confirms acceptance and supplies a stored Id and Location. Replay Ok confirms recovery of an existing equal fact under the scoped key. A payload conflict is a deterministic disagreement that should be investigated, not retried unchanged indefinitely. A timeout is uncertain and can be resolved by an identical retry under the current store lifetime.

The producer key remains stable across attempts; trace identifiers may differ; the stored Id comes from the winner. A server restart breaks the current default's receipt continuity. A client journal can preserve intent but cannot make the server dictionary durable.

## MF-03B: scope and authority need different tests

A repository test stores under tenant A and verifies a lookup under tenant B returns no event. A proposed hosted authorization test supplies a principal allowed only tenant A and attempts a tenant B route. The first exercises key filtering; the second exercises whether the principal may choose that route context.

The current route and Program inspection do not establish production tenant authorization. A direct service method receiving a string cannot prove a policy was enforced. The answer should describe the proposed policy and the host configuration needed to test it.

## MF-03C: a producer journal preserves intent

A minimal journal stores the scoped external key and immutable business payload before transmission. Attempts record uncertain or resolved outcomes without changing the fact's key. A replay resolves to the same stored identity. A conflict moves the entry into a review state instead of silently generating a replacement key.

The journal needs its own durability and concurrency design if implemented. The exercise is a proposal, not an existing MeterForge component. It does not solve server-side restart duplication until the server receipt boundary also becomes durable.

## MF-04A: durable uniqueness owns the decision

A defensible schema includes normalized tenant/event key, internal Id, subscription, meter, quantity, occurrence time, and acceptance status or metadata needed by the chosen protocol. A unique constraint protects the scoped producer identity. The losing contender reads the winner and compares payload instead of updating it on conflict.

Normalization must match the published contract. If a new adapter changes case or whitespace handling, the migration needs a collision policy. The service's GetOrAdd abstraction can remain useful if the adapter genuinely implements atomic winner selection and returns the durable winner, not a locally constructed losing candidate.

## MF-04B: three layers answer different questions

A pure model tests response classification and failure-state transitions. A two-connection database test establishes the chosen constraint and transaction behavior under competing candidates. A restart test establishes that the accepted receipt survives a process boundary and is recovered with the same identity.

The negative control removes the unique authority and synchronizes two absence checks before insertion. A test that never exposes the bad interleaving cannot establish why the durable design is needed. Report actual execution separately from this written specification.

## MF-04C: preserve the fact while strengthening its lifetime

The proposed adapter preserves scoped key, payload comparison, first accepted fact, and replay versus conflict semantics. It strengthens survival across process restarts and replicas using a real shared authority. It does not automatically add subscription validation, pricing correctness, invoice finalization, or payment completion.

Rollout must consider existing in-memory facts and normalization differences. Rollback must consider how clients behave if a durable receipt is no longer consulted. A review note that ignores those transitions describes only steady-state code, not an operationally coherent change.

## Rubric for the first loop

Assess source accuracy, identity precision, concurrency reasoning, and evidence scope independently. A learner can understand the code but overclaim durability; another can design a good receipt but miss current normalization behavior. Feedback should identify the exact missing dimension. The strongest submission uses small counterexamples and explains why each test would reject a plausible wrong implementation.
