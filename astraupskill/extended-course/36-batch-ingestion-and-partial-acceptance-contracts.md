# 36. Batch ingestion and partial-acceptance contracts

The current [usage endpoint](../../src/MeterForge.Api/Usage/UsageEndpoints.cs) accepts one event per request, and [UsageIngestionService](../../src/MeterForge.Application/Usage/UsageIngestionService.cs) resolves one tenant and external-event key at a time. This chapter designs a proposed batch API while preserving the meaning of individual accepted events. It does not add a batch endpoint or claim that a loop over the current service is automatically atomic.

The prerequisite is exact replay and conflicting replay. A batch contains multiple operation identities and may also have its own request identity. These layers answer different questions. A batch retry should not create duplicate events, but it also needs a clear way to report which items were accepted before an interruption.

## Choose atomic or partial acceptance deliberately

An all-or-nothing batch promises that either every eligible item is accepted together or none is. That requires one shared transaction or an equivalent acceptance protocol across the batch's state. The current per-event in-memory GetOrAdd calls do not provide rollback of earlier accepted items if a later item conflicts.

A partial-acceptance batch can accept independent items and return a result per item. This often aligns naturally with stable external event keys, but it must define ordering, duplicate items within the batch, and retry behavior. A generic500 after accepting several items is a poor contract unless the client can discover the accepted subset by identity.

For the teaching design, choose partial acceptance with explicit item results. Each result names the input position or client item identifier, accepted resource identifier when available, replay classification, and error code. The batch-level response summarizes transport processing without pretending all items share one business outcome.

## Work a mixed batch

Use four items: a new valid event A, an exact replay B, a conflicting replay C, and an invalid positive-quantity rule violation D. Under the proposed partial contract, A is created, B returns its stored resource, C reports conflict without replacing the winner, and D is rejected before store acceptance. The response preserves all four correspondences.

Now place two items with the same external key inside one batch. If their normalized semantic payloads are equal, the second can be an exact replay of the first accepted item. If they differ, one winner and one conflict is possible under sequential processing. A parallel implementation must preserve the same declared winner or explicitly state that acceptance order is nondeterministic.

Do not use array position as the durable event identity. A client can reorder a batch on retry. External event keys should remain stable, while positions only help map the response to this request. A separate client item identifier can improve correlation without replacing the event's accepted identity.

## Batch identity and replay scope

A batch operation key can identify an exact submitted envelope, allowing the service to return a previous batch receipt. That is useful when clients need a stable aggregate response. It does not remove the need for individual event identities, especially when the same event appears in different batches.

Define whether a repeated batch key with reordered identical items is equivalent. If order determines winner semantics for duplicate conflicting keys, reordering can change meaning and should conflict. If the contract treats the batch as a set, canonicalization must define duplicates and stable ordering before acceptance. There is no safe universal answer without a declared policy.

A batch receipt should preserve accepted item outcomes, not rerun all validation under a newer policy and return a different historical response for the same accepted envelope. If validation policy changes, distinguish a new submission from replay of an accepted batch. This is another example of versioned acceptance evidence.

## Interruption and bounded work

Suppose the service accepts A and B, then the request is canceled before C and D are processed. A partial contract can expose a durable batch progress record, or the client can safely resubmit all items under stable event keys and reconcile results. The choice should be explicit. Without either mechanism, the client cannot tell whether silence means unprocessed or accepted-with-lost-response.

Bound batch size and total payload size under a documented policy. The current single-event endpoint does not establish an appropriate batch limit. A proposed limit protects memory, response size, and time spent holding resources. Test the exact maximum and one beyond rather than choosing an arbitrary enormous test payload.

Concurrency should also be bounded. Launching one task per item can create uncontrolled load on a future database or provider. A limited worker pool or sequential acceptance can be easier to reason about. The throughput design must preserve per-key winner semantics and result correlation even when items finish out of order.

## Error envelopes and client algorithms

Item errors should retain machine-readable codes and distinguish validation, conflicting replay, transient infrastructure failure, and unprocessed state. A client retries transient or unprocessed items according to policy, while a conflicting replay requires correcting identity or payload intent. Retrying every item with new external keys defeats idempotency and can duplicate usage.

The batch response should not leak another tenant's accepted payload when reporting a conflict. Tenant scope remains part of the key and authority boundary. A proposed secured host must establish tenant membership before processing the batch; per-item identity validation is not authentication.

Write a client algorithm in prose: retain original external keys, submit bounded batches, persist accepted receipts, retry only under the declared policy, and surface conflicting meaning for review. The algorithm should remain safe if the entire response is lost. That is the strongest test of whether the batch protocol has enough identity.

## Exercise MF-36A: specify mixed outcomes

Define the response for the four-item batch and for duplicate keys inside one batch. Choose whether ordering is meaningful. Include accepted resource identity, replay context, validation code, and unprocessed state where relevant.

## Exercise MF-36B: compare atomic and partial modes

Describe the persistence mechanism required for all-or-nothing acceptance and contrast it with a loop over current IngestAsync. For partial acceptance, work an interruption after two items and show the client's safe recovery using stable identities.

## Exercise MF-36C: design a bounded client

Specify batch size, concurrency, response correlation, and retry rules. Include a lost entire response and a reordered retry. Explain when a batch key adds value and why it does not replace individual external event keys.

## Review checkpoint

Batching changes the public outcome contract, not just performance. A strong design makes partial acceptance visible, preserves per-item identity, and defines recovery before increasing concurrency. It does not call a sequence of independent commits atomic merely because they share one HTTP request.
