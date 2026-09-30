# 01. Three identities in one usage request

This first route begins with a deceptively small question: what makes a usage event the same event? MeterForge has several answers because database identity, producer identity, and business meaning serve different purposes. You will derive those answers from the current code, trace competing requests, and design a reviewable identity policy. Later installments will extend the route into pricing, subscription changes, payment outcomes, and reconciliation. This chapter establishes the vocabulary needed to discuss those topics without treating every identifier as interchangeable.

Read [UsageEvent](../../src/MeterForge.Domain/Usage/UsageEvent.cs), [UsageIngestionService](../../src/MeterForge.Application/Usage/UsageIngestionService.cs), and [InMemoryUsageEventStore](../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs). You need basic C# records, constructor guards, tuples, and asynchronous method signatures. If those are unfamiliar, draw the values passed into the constructor before following the control flow. No API server or database is required to understand this boundary.

## Identity has an owner

The application creates a new Guid for every candidate event. That Id identifies the stored entity if the candidate wins. The caller supplies ExternalEventId, which identifies the producer's fact for retry purposes. The store combines TenantId and ExternalEventId into its dictionary key. SubscriptionId identifies the subscription whose usage the fact describes, but it is not part of that dictionary key.

Those choices create a precise scope. The same external event string in two tenants can identify two independent facts. The same external event string within one tenant competes for one stored winner, even if a caller changes SubscriptionId. Changing subscription therefore produces an idempotency conflict rather than a new event under that key. This is observable policy, not merely a storage optimization.

The service compares subscription, meter, quantity, and occurred-at time after the store returns its winner. These fields describe the business payload that must agree for a replay. It does not compare candidate Id because a fresh Id is generated for each attempt. If generated identity were included in payload equality, every ordinary retry would conflict. Keeping identity roles separate is what makes retries meaningful.

## Work the first create and replay

Use tenant alpha, subscription S, meter requests, quantity three, occurrence time noon UTC, and producer key event-seven. The first request builds candidate C1 with a new internal Id. The store has no entry for the tenant/key pair, so C1 becomes the stored winner. Every compared payload field agrees with itself. The service returns C1 with IsReplay false.

The second request carries the same business fact but builds candidate C2 with another internal Id. The dictionary already contains C1 and returns it. The payload comparison succeeds, and the different internal IDs cause IsReplay true. The service returns the stored C1, not the discarded C2. A client can therefore recover the stable stored identity after losing the first response, as long as it retries the same scoped key and payload against the same surviving store.

That final qualification matters. The default store is in memory. A process restart creates a new dictionary, so the same request can become a new creation with a different internal Id. The local retry behavior is real, but it is not a cross-restart guarantee. The [API composition](../../src/MeterForge.Api/Program.cs) explicitly reports an in-memory default and durableDefault false.

## Normalization is selective

The event constructor trims Meter and ExternalEventId and converts OccurredAt to UTC. It checks that TenantId is not blank but stores it without trimming. It does not lowercase meter, event key, or tenant. A careful identity worksheet therefore includes whitespace and case as separate dimensions rather than saying inputs are normalized in general.

For example, requests and a surrounding-whitespace version of requests become the same meter. Requests with a capital first letter remains a different string under the current comparison. An external key with surrounding spaces is trimmed before dictionary insertion, but a nonblank tenant with surrounding spaces remains distinct. Whether that tenant policy is desirable is a separate product and authorization question; the source observation comes first.

Time normalization provides another useful distinction. Noon UTC and two in the afternoon at offset plus two denote the same instant. The constructor normalizes both, so they can replay as the same payload. Noon UTC and noon at offset plus two denote different instants and therefore conflict under the same scoped event key. A timestamp's displayed clock face is not enough to decide equality.

## A replay is a classification of an attempt

IsReplay describes how this ingestion attempt relates to the store's existing winner. It is not an immutable property of the business fact. The first accepted call receives false; subsequent identical calls receive true. The GET endpoint constructs its response with IsReplay false because reading an event is not an ingestion replay. Do not build a data model that treats this flag as permanent event history without reviewing that distinction.

A response lost after successful insertion creates an ambiguous client experience: the client does not know whether creation occurred. Retrying the same key resolves that ambiguity locally by returning the stored identity. Retrying with a new external key instead creates another logical fact. A producer's retry policy is therefore part of system correctness, even though it lives outside this service.

A changed payload under the same key is rejected. It is not an update operation. If the business needs corrections to usage, it needs an explicit correction policy, perhaps a separate adjustment fact or versioned replacement command. Reusing the original key with a different quantity is not currently that mechanism. Later financial lessons will distinguish immutable facts, corrections, and derived totals.

## A source-grounded prediction exercise

Create a table with columns for tenant, external key, payload, stored identity, and attempt result. Begin with the first request above. Add an identical replay, a changed quantity, the same key in another tenant, a trimmed meter, a case-changed meter, and an equivalent time offset. Predict whether each attempt creates, replays, or conflicts. Write the expected stored identity after every row, including rejected rows.

Do not run the whole application simply to obtain the table. First derive it from the constructor, store key, and service comparison. The disposable lab will execute selected predictions using copied current source. That combination is stronger than observing output without understanding why it occurred, and narrower than claiming a full API or database contract.

## Exercise MF-01A: identify the authority

For internal Id, ExternalEventId, TenantId, SubscriptionId, and OccurredAt, state who supplies the value, whether it participates in the store key, and whether it participates in replay payload comparison. Explain why a new internal Id does not make an identical request conflict. Include one consequence of excluding subscription from the store key.

## Exercise MF-01B: challenge normalization

Construct six pairs of requests that differ only by meter whitespace, meter case, external-key whitespace, external-key case, tenant whitespace, or equivalent timestamp offset. Predict each pair's outcome under current source. Then propose one normalization change and explain which existing identities it might merge. Do not implement the change in the application.

## Exercise MF-01C: design a producer retry note

Write a short integration note for a producer that times out after sending usage. Explain which values must remain stable during retry, what a replay response means, and why generating a new external key is unsafe for the same fact. Include the current in-memory lifetime limitation and an explicit distinction between retry and correction.

## Review checkpoint

A strong answer can point to the exact source of every equality rule. It does not equate tenant scope with authenticated authority, a completed task with durable storage, or a replay flag with permanent event metadata. It leaves a trace another learner can evaluate without having to trust a broad claim that the operation is idempotent.
