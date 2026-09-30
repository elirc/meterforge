# 18. Capstone: defend a usage replay contract

Your task is to prepare a reviewable improvement proposal for usage ingestion. The existing service already distinguishes a stored winner from a candidate and compares selected payload fields. The capstone builds on that behavior rather than replacing it with a vague claim of exactly-once billing. No production code changes are required for the course deliverable.

## Scenario

A client occasionally loses responses and retries. Its data pipeline can also correct an event quantity after discovering a source error. Tenant labels arrive through multiple adapters, and some adapters pad text. The team wants clients to recover an original result reliably while preserving a clear distinction between retry and correction.

Start by documenting the current key and normalization rules from [UsageEvent](../../../src/MeterForge.Domain/Usage/UsageEvent.cs), [UsageIngestionService](../../../src/MeterForge.Application/Usage/UsageIngestionService.cs) and [the in-memory store](../../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs). Include the fact that tenant text is not trimmed by the constructor. This current-state account is the basis for your proposal.

## Required contract

Specify equivalent replay, conflicting replay and correction as separate operations or outcomes. Define which fields are immutable under one event key. Explain how a caller can recover an event after losing a response, using an authorized lookup or receipt design. If the current API does not expose the required lookup, label it as proposed and define its request and response shape.

Choose where tenant identity becomes authoritative. A caller-controlled display label should not casually become a normalized storage identity. If you propose a canonical tenant identifier, explain how adapters obtain it and how existing padded keys are handled. Include a collision case where two current strings would normalize to the same value but carry different payloads.

## Correction policy

Pick a correction design and defend it. A compensating event preserves the original fact and adds a separately identified adjustment. An explicit amendment can work with versioning and audit evidence. A destructive overwrite would need a much stronger justification because it changes the meaning of a previously accepted event. State how downstream rating knows which events or adjustments to include.

Do not use a fresh random key merely to bypass a conflict without explaining the business effect. That can turn a correction into an additional charge. The proposal must connect identity decisions to aggregation and invoice evidence.

## Failure table

Describe at least six moments: before validation, before winner selection, after winner selection but before response, during equivalent replay, during conflicting replay and during correction publication. For each, identify the last confirmed effect and the client's safe next action. Unknown outcomes should remain explicit until a recovery observation resolves them.

If you propose durable storage, include uniqueness and transaction requirements plus a relational test plan. The current in-memory experiment does not establish process-restart survival. State which new evidence is needed to promote the proposal from design to an implemented durable guarantee.

## Test portfolio

Reuse the actual-source normalization and concurrent-wave checks as a starting point. Add a mixed-payload concurrent wave, tenant-collision fixtures, response-loss recovery and correction accounting. Give each test an independent oracle. The mixed wave may have either payload family win, but accepted results must agree with the stored winner.

Include negative cases for unauthorized cross-tenant lookup and a receipt belonging to a different tenant if those interfaces are proposed. Specify what must remain undisclosed on rejection. These are contract tests for the proposed boundary, not evidence that a current endpoint has those behaviors.

## Review packet

Submit a current-state diagram, proposed contract, normalization table, six-row failure table, test ledger and migration note. Keep the user-facing response examples short and actionable. An operator should know whether to replay, correct, query status or investigate an unresolved outcome.

The capstone passes when a reviewer can trace one event through first acceptance, response loss, equivalent retry, conflicting correction and final invoice evidence without changing the meaning of its identity halfway through the story. It fails if the design treats all retries as harmless or all timeouts as absence of effect.

[Studio index](README.md) · [Review](reviews/07-capstone-rubric.md)
