# 39. Capstone: a recoverable webhook dispatcher

Build a disposable protocol model around [OutboxMessage and WebhookDelivery](../../src/MeterForge.Domain/Integrations/IntegrationModels.cs). Chapter 18 introduced the acknowledgment gap. This capstone turns that analysis into a concrete proposed dispatcher with claims, bounded retries, receiver receipts, and reviewable dead-letter evidence. It does not add a network sender to the current retry-preview endpoint.

The deliverable is a deterministic model with a fake receiver. No real endpoint is contacted. The model should make process loss and duplicate delivery explicit through controlled steps rather than using random failures. Its purpose is to establish the protocol before selecting a queue, database, or hosting technology.

## Define the work record

A proposed delivery work record binds delivery identity, message identity, tenant, approved destination identity, payload schema version, state, attempt count, next eligible time, and claim generation. Keep the original message immutable under its acceptance contract. A retry changes delivery history, not the event's business identity or payload meaning.

Distinguish Pending, Claimed, Succeeded, RetryScheduled, and DeadLettered in the model. The current WebhookDelivery enum is smaller, so these are proposed states. Every transition should have a predicate and evidence. A worker cannot mark success merely because it started, and a stale claim cannot overwrite a newer accepted result.

The destination should reference an approved registration rather than arbitrary unvalidated input in a production design. The current constructor checks absolute URI only. The capstone can use a synthetic destination identifier and a fake receiver, leaving network policy and ownership verification to the integration plan.

## Claims and stale workers

Two workers can discover the same eligible delivery. A conditional claim assigns one generation to one owner. If the owner disappears, a later claim can supersede it after a declared expiry rule. Finalization must compare generation so a late response from the old worker cannot change the newer work state.

Work worker A at generation one, paused after sending. Its claim expires; worker B takes generation two and receives an accepted receiver receipt. A later returns with an error. The dispatcher must not replace Succeeded with Failed based on A's stale authority. The current permissive Fail method alone cannot enforce this protocol.

Claim expiry does not mean the original receiver call had no effect. The receiver can see duplicates from A and B. Stable message identity and receiver-side acceptance are therefore required even with correct sender claims. Fencing local finalization protects sender state but does not recall an already transmitted request.

## Receiver inbox model

The fake receiver stores message identity and accepted semantic payload under a declared scope. First acceptance applies one synthetic business effect and records a receipt. Exact replay returns that receipt. Conflicting payload under the same identity is rejected and preserves the original effect. This mirrors the meaning-preserving receipt pattern used elsewhere in the course.

Add a mode that accepts and then drops the acknowledgment. The sender schedules a retry, and the receiver returns the existing receipt on the next attempt. Assert one business effect despite multiple delivery attempts. Then add a mode that rejects before acceptance, showing why that case is different from an unknown acknowledgment.

Do not model receiver idempotency with an unbounded process-local dictionary and call it durable. The dictionary is the educational state machine. The integration plan must identify receiver persistence and its transaction with the business effect. The model's output should state this limitation explicitly.

## Retry policy and time

Use a manual clock and a bounded backoff schedule. The policy should distinguish transient transport failure, explicit permanent rejection, unsupported schema, and conflicting identity. A permanently malformed payload should not consume endless retries as though waiting longer would repair it. A rate-limited response may require a separate eligibility rule under the real receiver contract.

Attempt count should increment when an actual dispatch attempt starts under a valid claim, not whenever a duplicate notification is observed. Maximum attempts and maximum age are separate controls. A delivery can remain young but have many rapid failures, or be old with few attempts because the service was unavailable.

Jitter can reduce synchronized retries in a real deployment, but deterministic tests should inject the delay policy and verify bounds rather than asserting one random value. The capstone can use a fixed schedule for its core proof, then explain how a production randomization strategy preserves the eligibility envelope.

## Dead-letter review is an operation

A dead-letter record should preserve message and delivery identities, schema version, destination registration, attempt history, last bounded error category, and the accepted payload fingerprint. Avoid storing credentials or unrestricted payload text in a general review report. The operator needs enough evidence to diagnose and choose a safe next action.

Replaying a dead-lettered message should retain its business identity if it is the same event. If the payload is corrected, that is a new accepted event or an explicitly versioned correction, not an invisible rewrite under the old identity. A replay command itself needs authorization and audit evidence in a future production system.

Do not make the review tool send automatically as a side effect of displaying a record. Read-only diagnosis and consequential replay should be separate operations. In this course, both remain synthetic model actions with no network access.

## Exercise MF-39A: implement the claim schedule

Model two workers, claim expiry, and stale finalization. Assert that generation one cannot overwrite generation two's accepted success. Include a duplicate receiver call to show that local fencing does not eliminate the need for receiver receipts.

## Exercise MF-39B: prove receiver effect identity

Implement first acceptance, exact replay, conflicting replay, and accepted-with-lost-acknowledgment in the fake receiver. Count synthetic effects and return stable receipts. State the persistence transaction required before translating the model into a real receiver.

## Exercise MF-39C: submit the dispatcher capstone

Provide the state graph, manual-clock schedules, bounded retry policy, dead-letter packet, and integration plan. Include unsupported schema and operator replay. Label the current source records and the proposed dispatcher states separately.

## Review checkpoint

The dispatcher capstone succeeds when retries preserve event meaning, stale workers lose authority, and receiver effects remain accountable after lost acknowledgments. A queue or HTTP client is not the central proof. The proof is the acceptance protocol on both sides and the evidence needed to recover it.
