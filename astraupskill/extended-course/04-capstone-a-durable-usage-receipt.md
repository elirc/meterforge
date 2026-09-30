# 04. First capstone: design a durable usage receipt

This capstone completes the first usable learning loop: understand current identity, locate the atomic winner, explain HTTP uncertainty, and design a stronger receipt boundary. It does not complete the entire extended course or implement durability in MeterForge. Your deliverable is a source-grounded design and disposable prototype that can be reviewed before any application change is proposed.

The current [ingestion service](../../src/MeterForge.Application/Usage/UsageIngestionService.cs) relies on an atomic IUsageEventStore operation. The default [store](../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs) retains its winners only in one process. The capstone's product requirement is that an identical producer retry after a server restart recovers the original event identity, while a changed payload under the same tenant/key still conflicts. State that requirement before choosing a database mechanism.

## Define a receipt that survives uncertainty

A durable receipt needs the scoped producer key, the accepted payload or a rigorously defined comparable representation, the stored internal identity, and enough status to distinguish acceptance from later processing. Do not store only the external key and a boolean. Without payload evidence, the system cannot distinguish a legitimate replay from conflicting reuse. Without a stable stored Id, the response cannot recover the original identity.

Choose exact normalization rules before designing the unique key. The current constructor trims meter and event key, converts time to UTC, and leaves nonblank tenant text unchanged. A database migration that applies a different case or whitespace rule can merge previously distinct keys. That is a compatibility decision requiring evidence and perhaps a migration policy, not an incidental schema detail.

The simplest proposal might use a unique constraint on the normalized tenant/event-key pair and an atomic insert-or-read strategy. The constraint is the durable authority for one winner. A check followed by an unconstrained insert remains racy even if the code is inside an asynchronous service. The losing contender must read the established winner and compare the business payload using the same contract as the current service.

## Trace competing durable candidates

Use two independent application instances with distinct database connections. Both submit the same scoped key. One transaction creates the receipt. The other encounters the unique identity boundary and reads the winner according to the selected database strategy. For identical payloads, both ultimately reference one stored Id. For conflicting payloads, the loser reports the business conflict rather than overwriting the receipt.

The exact transaction and error handling depend on the chosen provider. Do not copy a generic upsert snippet without analyzing whether it updates fields on conflict. An upsert that replaces quantity would violate immutable fact semantics even though it avoids duplicate rows. The desired operation is choose or recover a winner, not silently revise the accepted fact.

A prototype can model unique-key behavior and response classification, but a real integration test must use the actual database provider to establish constraint and isolation behavior. Keep those layers separate in the evidence report. A passing dictionary model cannot prove that a SQL statement behaves the same way under two connections.

## Decide where downstream work belongs

Suppose accepted usage should trigger rating. The receipt and a durable rating intent may need to commit together so a crash does not leave accepted usage permanently unprocessed. A proposed outbox can provide that intent. It does not by itself guarantee exactly-once external delivery; the dispatcher may send successfully and crash before recording acknowledgment.

Define an idempotent downstream identity or a consumer receipt. Distinguish acceptance of usage from completion of rating. A POST response can confirm the durable fact without promising that every derived invoice total is already updated, if that is the chosen product contract. The status and documentation must communicate the distinction clearly.

Avoid expanding the first implementation into every billing feature. The capstone focuses on durable acceptance and one recoverable downstream intent. Pricing versions, late events, corrections, and invoice finalization need separate domain policies. They are planned subjects for deeper installments, not details to hide inside this receipt schema.

## Required incident cases

Model a lost response after receipt commit, a restart before dispatch, a duplicate dispatch after lost acknowledgment, and a changed-payload retry. For every case, record receipt state, downstream intent state, client observation, and recovery action. A useful design can explain what is known even when the caller did not receive success.

Add a cancellation case before the durable write begins and another after commit. The latter must not pretend the fact disappeared because the caller stopped waiting. If the API returns a cancellation-related failure after commit, the producer still needs a way to recover the receipt using its stable key. The recovery contract should be independent of whether the first response was delivered.

## Exercise MF-04A: produce the design packet

Write the receipt schema, uniqueness rule, payload comparison, and transaction boundary. Include a two-contender trace for identical and conflicting payloads. Identify normalization compatibility risks and explain how the current service interface would map to the proposed durable adapter.

## Exercise MF-04B: design the recovery tests

Specify one pure model test, one two-connection database test, and one restart test. State which guarantee each establishes. Include a negative control that would expose check-then-insert without a unique constraint. Do not execute a database or install dependencies as part of this written exercise.

## Exercise MF-04C: defend the scope

Write a short review note explaining which behavior remains the same, which guarantee becomes stronger, and which billing concerns remain outside this capstone. Include a rollout concern and a rollback concern. A reviewer should understand the change without confusing durable acceptance with completed rating or payment.

## Completion rubric

The first learning loop is complete when you can trace the existing implementation, run or accurately scope the disposable lab, and defend this proposed receipt design. The capstone passes when uniqueness, payload equality, recovery, and downstream delivery are explicit and independently testable. It fails if durability is inferred from a concurrent collection or if an upsert silently turns a replay into a correction.
