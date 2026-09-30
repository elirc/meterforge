# Review 1: independent expectations and usage identity

Use this guide after chapters one through three. The examples explain reasoning, not just expected labels. A strong answer should connect raw input, normalized candidate, storage key, payload comparison and returned result without merging those stages.

## Normalization answer

With tenant T, external event text surrounded by spaces, meter text surrounded by spaces and an explicit timestamp offset, the candidate keeps tenant T, trims external ID and meter, and stores occurredAt as the equivalent UTC instant. A retry with the trimmed strings and another offset for the same instant is equivalent if subscription and numerical quantity also match.

A tenant value with surrounding spaces is a different current key because the constructor preserves tenant text. This is a characterization of the local class and in-memory store. It does not establish how every application adapter obtains tenant identity. A reviewer should reject the overbroad statement that all input strings are normalized the same way.

## Payload conflict answer

For a stored event under the same tenant and external ID, a different subscription, meter, quantity or occurredAt produces an idempotency conflict through the service comparison. The stored winner should remain unchanged. Checking only the exception code misses the preservation part of the oracle; query or inspect the stored event through the available boundary as well.

A new external ID selects a different key, even if every payload field matches. That can represent a genuinely new event or an accidental duplicate from the caller. The service's key contract cannot decide that business distinction by comparing payload alone. A correction workflow therefore needs explicit semantics rather than automatically minting a new key after conflict.

## Concurrent wave answer

For equivalent calls sharing one key in the tested in-memory store, a valid outcome has one stored identity across all successful results and one response marked as the original acceptance. Do not require the first task submitted to be the winner. The scheduler determines which candidate reaches GetOrAdd first, while the contract constrains agreement afterward.

For two payload families sharing one key, either family can win. Successful accepted or replayed results should agree with the winning payload; incompatible candidates should conflict. A test that permits some successful responses for each incompatible payload family has an oracle too weak to establish one-key consistency.

## Independent oracle answer

The expected result should be computed from a hand-authored normalization and equivalence table. Reusing the service's comparison logic as the expected-value calculator risks repeating the same defect. Independence does not require a large formal model; a small explicit table with carefully chosen differing fields is often clearer.

Use one changed field at a time initially. Then add combined differences to study error precedence only if that is part of the contract. A case with an invalid empty subscription and changed quantity may fail construction before reaching replay comparison, so it cannot establish the quantity-conflict branch.

## Cancellation answer

The studio checks a token already cancelled before ingestion reaches the store. It observes cancellation without creating the intended event. This does not establish cancellation at every possible point, durable rollback, or an HTTP response contract. An answer should name the tested timing rather than writing cancellation is safe.

## Common review mistakes

Reject claims that a timeout proves no stored event, that dictionary thread safety establishes durable multi-process behavior, or that every repeated payload should be deduplicated regardless of key. Each claim skips a boundary or changes the identity contract. Ask the learner for the smallest counterexample and a more precise replacement statement.

The review is complete when the learner can explain first acceptance, equivalent replay, conflicting replay and a changed tenant key with four concrete examples. They should also name one integration test still needed before claiming persistence across process restart.

[Studio index](../README.md)
