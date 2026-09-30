# 18. Outbox records and webhook delivery protocols

Read [IntegrationModels](../../src/MeterForge.Domain/Integrations/IntegrationModels.cs), the [retry preview endpoint](../../src/MeterForge.Api/Integrations/IntegrationEndpoints.cs), and the [integration model tests](../../tests/MeterForge.Domain.Tests/Integrations/IntegrationModelTests.cs). The source provides an OutboxMessage record and a WebhookDelivery object. Their names suggest a larger integration architecture, but the inspected types implement local data and transitions, not a complete durable dispatcher.

The prerequisite is the payment chapter's separation between intent and external effect. A webhook has the same fundamental ambiguity: the receiver can act before the sender receives an acknowledgment. A durable delivery protocol must preserve identity and support replay without assuming that every timeout means no receiver action.

## The message constructor preserves evidence

OutboxMessage requires a nonempty identifier, tenant, event type, subject, and payload, plus a positive schema version. It normalizes OccurredAt to UTC. These guards ensure that important envelope fields are present. They do not parse the payload, validate it against the schema version, persist it, or ensure it was created in the same transaction as a business change.

The existing test checks UTC normalization. That is a meaningful local assertion: an offset-bearing input instant is represented with a zero offset. It does not prove event ordering across machines, nor does it make the timestamp a unique identity. Two events can share a timestamp, and clocks can disagree. Use the message identifier and a declared ordering policy rather than relying on timestamp uniqueness.

The payload is a string. A nonblank payload can still be malformed for its intended schema. If the event type is invoice.finalized and schema version one, some boundary must validate or construct the corresponding content. A proposed serializer can own that contract, but the current constructor only checks presence. Keep envelope validation and payload validation distinct in tests.

## Delivery identity survives a retry

WebhookDelivery requires nonempty delivery and message identifiers, a nonblank tenant, and an absolute endpoint URI. It starts Pending with Attempt one. Fail sets Failed, Succeed sets Succeeded, and Retry accepts only Failed before incrementing the attempt and returning to Pending. The existing test demonstrates that Retry preserves delivery and message identities.

This preservation is valuable because a receiver can use stable event identity to recognize repeated delivery. Attempt number should describe transport effort, not redefine the business event. A new event identifier for every retry would make duplicate receiver effects harder to prevent. A delivery identifier can distinguish destinations while a message identifier identifies the business event itself.

The current Fail and Succeed methods do not enforce a complete terminal transition graph. They assign their statuses directly. A proposed dispatcher must decide whether a completed delivery can ever fail later, whether retries have a limit, and how concurrent workers claim it. Do not attribute those policies to the object merely because it has a status enum.

## Absolute URI is not an outbound policy

The constructor checks IsAbsoluteUri. That does not establish an allowed scheme, destination ownership, or an outbound-network safety policy. A production webhook registration workflow would need to decide which destinations are permitted and how they are verified. This chapter does not send requests to arbitrary endpoints or diagnose a deployed vulnerability; it identifies the boundary that a future sender must own.

Even a valid HTTPS URL is not by itself proof that the registering tenant controls the destination. A registration challenge, administrative approval, or another product-specific mechanism may be appropriate. The lesson is to separate syntactic URI validity from destination authorization. A disposable model can represent those states without making network calls.

The retry-preview endpoint creates a new delivery, marks it failed, retries it, and returns its state. It does not transmit a webhook in the inspected body. Calling this endpoint therefore demonstrates local transition behavior, not remote delivery or receiver idempotency. A response with Attempt two means the preview object advanced, not that two network requests occurred.

## Work a durable outbox proposal

Suppose invoice finalization and event creation belong to one accepted business operation. A proposed transaction stores the finalized document and the outbox row together. If it commits, both exist; if it rolls back, neither exists under the database contract. A dispatcher later claims the row and attempts delivery. This is a proposed composition, not functionality supplied by the OutboxMessage record alone.

Now pause after the receiver accepts the event but before the sender saves success. On restart, the sender may retry. The receiver needs an inbox or equivalent stable-identity check tied to its business effect. The sender cannot guarantee exactly one receiver action solely by recording its own successful HTTP responses. Both sides' protocols matter.

If the receiver returns success but then loses its own uncommitted work, the meaning of that response is also important. A reliable receiver should acknowledge according to a declared acceptance boundary, often after durable acceptance. The sender's tests should model that contract explicitly instead of assuming every successful status proves the receiver's final business outcome.

## Ordering and schema versions

Two events for one subscription can arrive out of order. A receiver that applies a cancellation before an earlier activation needs a policy: event versions, aggregate sequence, state replacement, or reconciliation. OccurredAt alone may not resolve the order because clocks and delivery schedules differ. A proposed envelope can include an aggregate version while preserving the existing message identity.

Schema version is similarly operational. A positive integer does not prove a consumer understands it. A receiver should distinguish supported, unsupported, and malformed payloads. A dispatcher should not blindly retry an permanently unsupported schema as though the network were temporarily unavailable. Classifying failure affects retry, dead-letter review, and compatibility rollout.

Backward-compatible evolution needs examples. Adding an optional descriptive field may be acceptable to tolerant readers. Changing amount units or renaming a required identity can alter meaning. A course submission should include one compatible and one incompatible change and explain how producer and consumer versions coexist during deployment.

## Exercise MF-18A: classify current guarantees

List the constructor guards, timestamp normalization, retry identity preservation, and permissive terminal mutations. Distinguish each from persistence, sending, destination authorization, and receiver acceptance. Explain what the retry-preview response actually demonstrates.

## Exercise MF-18B: draw the acknowledgment gap

Work invoice acceptance, outbox persistence, dispatcher claim, receiver acceptance, and lost sender acknowledgment. Define stable identities on both sides and show how replay avoids repeating the receiver's business effect under your proposed contract.

## Exercise MF-18C: design schema and ordering policy

Choose an aggregate ordering field and two schema changes. Specify receiver behavior for older, newer, duplicate, and unsupported messages. Include a read-only dead-letter review packet that avoids payload secrets and preserves enough identity for investigation.

## Review checkpoint

An outbox type is useful vocabulary, but durability comes from the transaction and dispatch protocol around it. A webhook status is useful state, but receiver effects require their own acceptance evidence. Strong answers connect both sides without hiding the acknowledgment gap.
