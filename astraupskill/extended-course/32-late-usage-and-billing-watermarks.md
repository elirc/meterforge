# 32. Late usage and billing watermarks

Read [UsageEvent](../../src/MeterForge.Domain/Usage/UsageEvent.cs), [UsageIngestionService](../../src/MeterForge.Application/Usage/UsageIngestionService.cs), and [IUsageEventStore](../../src/MeterForge.Application/Usage/UsageIngestionService.cs). The accepted event contains an occurrence instant but no separate ingestion timestamp or accepted sequence in this model. This chapter designs a proposed billing-selection policy for late-arriving events without changing ingestion's current replay contract.

The prerequisite is the distinction between external event identity and internal accepted resource identity. Idempotency prevents one external event key from silently identifying different usage. It does not decide which invoice should include the event, whether it arrived late, or whether a billing run has already consumed it.

## Occurrence and acceptance answer different questions

OccurredAt is normalized to UTC. Two representations of the same instant can therefore compare as equivalent in replay after construction. That describes when the producer says the usage occurred. It is not necessarily when MeterForge received, validated, or accepted the event.

Imagine an event occurring on June thirtieth but arriving on July second, after the June invoice was accepted. A billing query based only on OccurredAt would place it in June's business interval. A workflow based on the set known at June finalization would not have seen it. Both facts need representation if the product supports late-arrival corrections.

The current store interface provides lookup by identifier and atomic get-or-add; it does not expose a billing-range query or an ingestion watermark. A proposed billing module must add an explicit selection boundary or consume a separately recorded accepted-event stream. Do not assume an unimplemented query exists because the event has a timestamp.

## Choose a late-arrival policy

One policy includes only events accepted before a billing watermark and carries late events into a later adjustment. Another delays finalization for a grace interval, then still requires a policy for events arriving afterward. A third allows reopening drafts but never silently rewrites finalized documents. These choices have customer and operational consequences, so document them as product rules.

For this exercise, choose an accepted-sequence watermark. Each durable accepted event receives a monotonic sequence within a declared scope. A billing operation captures a watermark and selects eligible occurrence dates among events accepted up to that sequence. Later accepted events are not silently added during replay of the same billing operation.

This sequence is proposed infrastructure, not a property of Guid identifiers or the current dictionary. A random identifier cannot be treated as an acceptance order. A timestamp may also have ties and clock uncertainty. The shared persistence mechanism must define how sequences are assigned and compared if the design relies on them.

## Work the replay boundary

Billing operation B captures watermark 100 and calculates a document from eligible events through 100. Event 101 then arrives with an occurrence date inside the billed period. A retry of B must use the same accepted selection evidence, not capture101 and produce a different total under the same operation identity. Exact replay depends on stable inputs as well as stable output identity.

A later correction operation C can reference event 101 and the original invoice. It has its own identity and policy. If event 101 is replayed at ingestion, the accepted event remains one resource and should not create another correction cause. The correction ledger should reference accepted event identity, not each transport delivery.

Now suppose event99 was accepted before the watermark but its metadata becomes visible to a reader later because of a poorly chosen consistency boundary. A watermark alone is not enough unless the selection query observes all accepted events up to it under the persistence contract. The integration test must establish that relation. Avoid using a sequence number as a magical consistency guarantee without defining its transaction.

## Corrections are not mutations of accepted usage

The current ingestion service rejects a replay key with different subscription, meter, quantity, or occurrence instant. To correct an accepted event, a producer cannot simply resend the same external key with a changed quantity and expect replacement. The conflict protects the accepted meaning.

A proposed correction model can create a separate adjustment event referencing the original. It should define whether the adjustment is a delta, a replacement instruction, or a cancellation plus new event. The current UsageEvent requires positive quantity, so a negative correction cannot be represented by that constructor without changing the model. Keep such extensions explicit.

Do not encode cancellation through a positive event with a special meter string unless the contract deliberately defines and validates that scheme. Hidden conventions make rating and entitlement calculations fragile. A typed correction record or explicit event category is easier to audit and test.

## Meter identity and temporal grouping

UsageEvent trims Meter but does not lower-case it. Two meter strings differing only by case can therefore remain distinct under current equality. A billing aggregator must use the same canonical meter policy as ingestion or declare a migration. Grouping case-insensitively later can merge accepted identities that ingestion treated as different semantic values.

Similarly, tenant is stored as supplied after nonblank validation, unlike Product's trimmed TenantId. A proposed durable event stream must define canonical identity consistently across modules. This chapter does not change that policy; it asks the learner to identify the mismatch before building a billing query that assumes all strings are already normalized alike.

## Design the selection evidence

At minimum, record billing operation identity, tenant and subscription scope, occurrence interval, accepted watermark or explicit selected identifiers, meter policy version, and late-arrival policy. The document can reference this evidence rather than copying every raw event into each line. The relationship must still permit an auditor or test to explain which events were included.

An explicit selected-identifier set is easy to reason about for a small prototype but can be large. A watermark plus deterministic query can be compact but depends on immutable event records and stable query semantics. A materialized billing batch can combine these approaches. The exercise should compare the tradeoffs rather than assuming one representation is universally best.

## Exercise MF-32A: classify event time

Draw a June event accepted in July and distinguish occurrence, acceptance, billing cutoff, and correction time. State which timestamps or sequences exist in the current model and which your proposed billing policy adds.

## Exercise MF-32B: freeze selection across replay

Work billing operation B at watermark 100 and a later event 101 belonging to the same occurrence period. Specify replay behavior and a separate correction operation. Explain the persistence relation needed to ensure the watermark query includes every earlier accepted event.

## Exercise MF-32C: design accepted-usage correction

Choose delta, replacement, or cancellation semantics for a correction record. Preserve the original accepted UsageEvent and its replay contract. Include meter canonicalization, positive-quantity constraints, and a test preventing repeated transport delivery from creating repeated corrections.

## Review checkpoint

Late usage is a temporal and identity problem, not merely a query filter. A strong design preserves what was known at acceptance, keeps retries stable, and gives later evidence a deliberate correction path. It does not mutate accepted events or infer ordering from random identifiers.
