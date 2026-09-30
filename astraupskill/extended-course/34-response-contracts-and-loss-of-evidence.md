# 34. Response contracts and loss of evidence

Read [UsageContracts](../../src/MeterForge.Contracts/V1/Usage/UsageContracts.cs), [InvoiceContracts](../../src/MeterForge.Contracts/V1/Invoicing/InvoiceContracts.cs), [PaymentContracts](../../src/MeterForge.Contracts/V1/Payments/PaymentContracts.cs), and [RatingContracts](../../src/MeterForge.Contracts/V1/Rating/RatingContracts.cs). A response is a projection of internal state for a specific consumer. This chapter examines what each projection preserves, what it omits, and how clients should avoid inventing missing guarantees.

The prerequisite is the distinction between accepted evidence and presentation. A concise response can be appropriate without including every audit field, provided a client knows how to retrieve the authoritative record when needed. Problems arise when a summary is treated as a complete receipt or a preview is stored as proof of durable acceptance.

## Usage replay is response context

UsageEventResponse includes IsReplay. The POST endpoint sets it from the ingestion result, while GET by identifier returns false. The accepted UsageEvent itself does not store a permanent replay flag. Replay describes how this request related to an existing resource, not a changing property of the event's business meaning.

Work a creation response with IsReplay false, a repeated POST with true, and a later GET with false. The same accepted identifier and payload appear with different contextual flags. A client cache that treats any flag difference as resource corruption would misunderstand the contract. Conversely, a test comparing complete response records must account for which operation produced them.

The Location header on creation provides a readback path in the current endpoint. The existing API test uses it to verify the original resource and its preservation after conflict. That relationship is stronger than a client guessing a URL from undocumented conventions. A proposed SDK can preserve the returned location and accepted identity while treating IsReplay as transport metadata.

## Invoice response is a summary

FinalizedInvoiceResponse contains identifier, status, total, currency, finalization instant, and line count. It does not include every line's source reference or price version. The current finalize-preview endpoint returns this summary from an in-memory object. A consumer cannot reconstruct full accepted invoice evidence from that response alone.

This is not necessarily a defect for a preview. The endpoint name and documentation should make the scope clear. A proposed durable issuance API would return a stable receipt and a retrieval link for the immutable document, or include the required evidence directly. The choice depends on response size and consumer needs.

LineCount is not an audit digest. Two different line sets can have the same count and total. A client that stores only the summary loses the ability to distinguish those documents. If a downstream workflow needs exact correspondence, it should retain a document identity tied to durable accepted content rather than comparing summary numbers.

## Payment response and uncertain states

PaymentResponse includes a nonnullable ProviderReference in its declared contract. The normal simulated success path supplies one. The broader PaymentService model can encounter explicit failure or an exception before a valid reference, so a future API supporting those outcomes needs a response contract that represents them honestly.

Do not fill an unknown reference with an arbitrary empty string and call the operation succeeded. A discriminated outcome, nullable evidence under documented states, or separate operation resource can preserve uncertainty. The right shape depends on whether the endpoint returns only confirmed successes or exposes pending and failed collection operations as resources.

Status is serialized from enum ToString in current endpoint mappings. Renaming enum members can therefore change public strings even if numeric values remain unchanged. A contract-evolution review should treat those strings as observable API values and avoid accidental breaking changes during an internal refactor.

## Rating response preserves allocation but not every input

RatingResponse includes quantity, currency, total, and tier results with units, unit amount, and charge. This helps explain allocation and rounding better than a total alone. It does not include every original tier boundary or a price-version identity in this contract. A consumer needing reproducibility should retain the request or a separate accepted calculation record.

The distinction matters when two schedules produce the same rated units and amounts for one quantity but differ outside that quantity. A response for one calculation is not a complete reusable pricing schedule. A client should not reconstruct future pricing rules by extrapolating from one result.

A proposed calculation receipt can include a policy version and input fingerprint. The fingerprint should refer to a retrievable or retained input under the system's evidence contract. A hash without access to the corresponding evidence may detect difference but cannot explain it. Keep verification and explanation as separate needs.

## Evolve contracts deliberately

Adding an optional field can be compatible for tolerant readers, but changing a field's meaning is not harmless merely because its name stays the same. Changing Remaining from hypothetical capacity to committed balance would alter semantics. Changing IsReplay from request context to permanent event state would do the same. Versioning must consider meaning, not only JSON shape.

Create compatibility fixtures containing representative old requests and responses. Verify that new readers interpret them under the old contract and that new writers do not silently emit values old consumers cannot understand. For financial evidence, record which contract version produced an accepted document or integration event when that affects interpretation.

Avoid exposing domain objects directly as a shortcut. Domain members can change for internal reasons, while a public contract needs deliberate stability. The repository already separates Contracts from Domain in these examples; use that seam to discuss mappings and omitted evidence explicitly.

## Exercise MF-34A: trace replay projections

Write the create, replay POST, and GET responses for one accepted usage event. Identify stable resource fields and request-context fields. Propose an SDK representation that avoids treating IsReplay as mutable business state.

## Exercise MF-34B: design a durable invoice receipt

Extend the preview summary into a proposed accepted-document API. Specify identity, retrieval, evidence version, and conflicting replay behavior. Explain why total and line count cannot establish complete document equivalence.

## Exercise MF-34C: review a contract change

Choose one semantic change to payment status, provider reference, rating evidence, or replay metadata. Define old and new consumer behavior and a compatibility fixture. State whether a new version or migration is needed and why field shape alone is insufficient.

## Review checkpoint

A response contract should expose enough evidence for its promised role and make omitted guarantees discoverable. Strong clients preserve accepted identity and understand contextual fields. Strong API reviews treat semantic changes as real changes even when the JSON still parses.
