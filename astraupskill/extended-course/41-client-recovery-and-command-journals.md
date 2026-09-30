# 41. Client recovery and command journals

Use [UsageEndpoints](../../src/MeterForge.Api/Usage/UsageEndpoints.cs), [SubscriptionEndpoints](../../src/MeterForge.Api/Subscriptions/SubscriptionEndpoints.cs), and [PaymentEndpoints](../../src/MeterForge.Api/Payments/PaymentEndpoints.cs). A server can expose useful replay and version semantics while a client still defeats them by generating new identities on every retry. This chapter designs a proposed client-side command journal that preserves intent through lost responses, restarts, and user interaction.

The journal is a teaching model, not an installed SDK. It stores synthetic request metadata in a disposable workspace if implemented. Do not write real provider keys, credentials, or customer payloads into course reports. The model should explain what a client must retain and which server guarantees it can actually rely on.

## One user action can produce several requests

A user submits one usage event. The client sends a POST, loses the response, and retries. Under the current usage contract, retaining the same external event identifier and semantic payload allows the server to return the accepted winner. Generating a new external identifier for the retry instead asks for a new event and can duplicate usage.

The journal records the intended operation before sending: tenant scope, external event identity, normalized or original payload according to the contract, and local state. Transport attempts reference that entry. A request trace can change per attempt while the intended operation remains stable. This mirrors the server's distinction between request context and accepted resource identity.

The client must not normalize payloads differently on replay if that changes semantic meaning. For example, changing meter case between attempts can conflict under the current UsageEvent comparison because trimming does not lower-case Meter. Store the accepted request intent or apply one stable canonicalization policy before the first send.

## Reconcile response classes

For usage creation, store the accepted identifier and Location. For exact replay, store the same accepted identifier and mark the local command resolved. For conflicting replay, stop automatic resubmission and surface the mismatch between retained intent and accepted key meaning. A conflict is not a transient network error to solve by inventing another key without user intent.

For a network timeout, keep outcome unknown and retry under the same identity or perform documented readback. The current usage API supports readback by internal identifier, but the client may not know that identifier if the first response was lost. Reposting with the stable external key is therefore the practical current recovery path. Do not invent an external-key lookup endpoint that is not present.

For a subscription amendment, the old expected version can conflict after a successful lost response. Reload current state and follow the application's chosen reconciliation policy. The current API lacks a separate amendment command receipt, so the client cannot always prove its exact command caused the observed state. The proposed journal should preserve that limitation rather than reporting false certainty.

## Payment requests need a different caution

The payment endpoint requires an Idempotency-Key header and forwards it to PaymentService, but the current default provider is a stateless simulator. A client should retain the key for retries, yet the server's current implementation does not provide the same local durable receipt guarantees as usage ingestion. Do not generalize one endpoint's contract to another based on the header name.

A production client would need a documented payment operation resource or provider-backed reconciliation path for unknown outcomes. Until that exists, the teaching model can classify the state as unresolved and avoid creating a new logical collection automatically. A user-facing message should distinguish accepted invoice from unconfirmed collection rather than declaring the entire billing operation absent.

The journal should store only the minimum safe evidence required. Provider operation identifiers can be sensitive operational data even when they are not credentials. Use access controls and retention appropriate to a real implementation. In the course, use fixed synthetic identifiers and no live network calls.

## User edits after an uncertain submission

Suppose a user changes the quantity after a timeout while keeping the old external event key. Under the current usage contract, that becomes conflicting replay if the first event was accepted. The client should distinguish editing an unsent draft from creating a new correction or new event after submission. The interface can lock accepted intent while offering an explicit correction workflow.

Do not silently mutate a journal entry's payload after its first send. Preserve the original intent and create a linked new command when the user deliberately changes meaning. This makes recovery and support explanations possible. It also prevents an automatic retry from accidentally sending a different payload under the original identity.

For subscriptions, a user may choose a different target product after seeing a conflict. That is a new command based on newly observed Version, not a retry of the old command. The journal should record the relationship while keeping separate expected versions and target products. This is how a client remains honest about intent under concurrent edits.

## Local crash and journal durability

A browser or desktop client can crash after sending but before storing the response. A durable journal can retain the stable operation key, allowing recovery on restart. If the journal itself is only in memory, its limitations mirror the server's in-memory stores. Do not call it durable simply because it is represented by a class named Journal.

A teaching implementation can serialize synthetic entries to a new temporary file and reload them. That proves the model contains enough information for a restart demonstration, not production crash consistency or secure storage. A real client needs atomic file or database updates, user-scoped access, and careful handling of sensitive data.

Journal cleanup should preserve unresolved operations. Deleting an unknown entry after an arbitrary timeout can lose the only stable identity needed for safe replay. A retention policy should distinguish resolved, abandoned-before-send, and unresolved-after-send entries. The last category may need reconciliation before removal.

## Exercise MF-41A: implement usage recovery

Define journal states for unsent, sent-unknown, accepted, replay-resolved, and conflict. Work a lost creation response and a later exact retry. Preserve the external event key and show how accepted identity is stored when finally observed.

## Exercise MF-41B: separate edit from retry

Model a quantity edit after a timeout and a product change after a version conflict. Specify which actions are retries and which are new commands. Explain why mutating a sent journal payload can violate the server's accepted identity contract.

## Exercise MF-41C: compare endpoint guarantees

Create a client capability table for usage, subscription amendment, and payment collection. Include current readback, replay, version, and unknown-outcome evidence. Do not assign usage's durable-looking semantics to the simulated payment path or invent missing endpoints.

## Review checkpoint

A reliable client preserves intent across transport uncertainty and understands each endpoint's actual contract. It does not solve every failure by changing an idempotency key or resetting local state. A good journal makes uncertainty recoverable without claiming guarantees the server does not provide.
