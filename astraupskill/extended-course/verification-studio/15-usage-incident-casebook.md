# 15. Work four usage-ingestion incidents

Use [UsageIngestionService](../../../src/MeterForge.Application/Usage/UsageIngestionService.cs), [UsageEvent](../../../src/MeterForge.Domain/Usage/UsageEvent.cs) and [InMemoryUsageEventStore](../../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs) as your evidence. Each case below provides observations and asks for a discriminating next step. The cases are educational scenarios, not claims that these incidents occurred in the user's application.

## Case A: a replay unexpectedly succeeds

A client sends meter text with surrounding spaces and external event text with surrounding spaces. Its second request removes those spaces, uses a different numeric decimal scale for the same quantity and expresses the same instant using another offset. The response says replay and returns the original event ID. An operator suspects the service ignored changed input.

Construct normalized candidates before judging the result. The domain constructor trims meter and external ID and converts occurredAt to UTC. Decimal equality treats equal numerical values as equal despite textual scale. Under those transformations the second request can be equivalent. The replay response is consistent with the current contract.

The investigation should still confirm subscription identity and tenant string. Do not compare only the fields highlighted in the report. An apparently harmless retry under a changed subscription should conflict when it uses the same key, because subscription is part of the payload comparison.

Deliver a raw-versus-normalized table and a short explanation for the client team. Avoid saying the system ignores whitespace everywhere: tenant text is treated differently in this implementation.

## Case B: two winners appear in a dashboard

Two records show the same external event ID and nearly identical tenant labels. One label has a trailing space. Both responses report an original event rather than replay. The dashboard trims text for display, making the records appear identical.

The in-memory key includes the tenant string, and UsageEvent preserves that string after checking it is nonblank. Therefore the source permits separate keys for differently padded tenant values. The display normalization can hide an identity difference that storage preserves. This is a hypothesis supported by the inspected normalization rules, not proof that the real requests contained those exact bytes.

Ask for a safe structured representation of the tenant values, their lengths and request provenance. Do not dump unrelated credentials or entire production payloads. The minimum evidence is enough to distinguish the two identity strings and trace which boundary supplied them.

A proposed repair could normalize tenant identity at an authoritative boundary, but migration needs care. Existing distinct keys might collide. Define a collision review and replay-equivalence policy before merging records. A string trim is not a complete data migration plan.

## Case C: a conflict after response loss

The first request times out from the client's perspective. A retry with the same key but a corrected quantity receives usage.idempotency_conflict. The client argues that the first request failed and therefore should not reserve the key.

Separate the client response from the store outcome. A lost response does not establish that no event was stored. The service returns the stored winner and compares it with the candidate. If the original payload won, a changed quantity under the same key conflicts by design. Querying the stored event by a known ID is different from guessing that a timeout erased the first attempt.

The next step is to establish the original event identity and payload through an authorized lookup or receipt mechanism available in the actual application. This studio does not invent an HTTP recovery endpoint. A design proposal may add one, with tenant authorization and careful disclosure of stored values.

Explain the correction workflow separately from retry. Correcting a business event may require a compensating event or an explicit amendment policy. Reusing an idempotency key for different content hides that distinction.

## Case D: equivalent concurrent calls

Sixty-four calls use the same tenant, external ID and payload. The actual-source studio harness observes one distinct returned event ID and one response marked non-replay. The in-memory store's GetOrAdd supplies a stored winner for the key, and each service call compares its candidate with that winner.

This observation supports the winner behavior for the tested in-memory implementation. It does not establish durable recovery after process restart, a relational uniqueness constraint, multi-process coordination or provider billing. The useful report includes both the successful invariant and those scope boundaries.

Now alter half the calls to a different quantity. Predict that one payload family can win and equivalent calls within that family replay, while the other family conflicts. The scheduler need not choose a predetermined family. An oracle should accept either valid winner while rejecting mixed accepted payloads under one stored key.

## Exercise

For each case write observed facts, plausible explanation, one alternative explanation, next discriminating observation and proposed change if justified. Include a sentence suitable for a nontechnical operator. The operator summary must preserve uncertainty and identify an action; it should not simply repeat exception names.

[Studio index](README.md) · [Review](reviews/06-incident-casebooks.md)
