# 1. Build an oracle before building a test

This studio extends the main MeterForge course with experiments, counterexamples and review exercises. Its focus is the quality of evidence. Read the main lessons first, then use these workshops to distinguish a test that executes code from a test that establishes the intended contract. Application source remains unchanged.

## Choose one claim

Begin with [UsageIngestionService](../../../src/MeterForge.Application/Usage/UsageIngestionService.cs). The service creates a normalized candidate, asks the store for the winner under a tenant/external-event key, compares payload fields and reports whether the stored ID differs from the candidate. A useful claim is: equivalent repeated input for the same key returns the stored event identity, while changed payload under that key raises the specific conflict.

That claim is stronger than “IngestAsync returns an object” and narrower than “billing is exactly once.” It names identity, equivalence and conflict. The test oracle should list the expected event ID, replay flag and unchanged stored payload after a conflicting call. It should not calculate expected behavior by invoking the same comparison helper a second time.

## Separate construction from storage

UsageEvent trims meter and externalEventId, converts occurredAt to UTC and requires positive quantity. It preserves the supplied tenant string after checking that it is not whitespace. The in-memory store keys by tenant and normalized external ID. These details determine equivalence before the service comparison occurs.

Create a table with raw input and normalized candidate. Include padded meter, padded external ID, two offsets representing the same instant, a different quantity and a tenant with leading whitespace. Predict which pairs share a key and which payloads compare equal. Do not apply an imagined tenant normalization that the constructor does not perform.

## Independent arithmetic

For [TieredRater](../../../src/MeterForge.Domain/Rating/TieredRating.cs), write expected allocations and charges by hand. If the first ten units cost 0.10 and later units cost 0.05, quantity twelve allocates ten and two, charging 1.00 and 0.10. The total is 1.10 under the current per-tier rounding. A test that calls Rate to compute expectedTotal before calling Rate again proves almost nothing.

Use a second representation for the oracle where possible. A small integer-unit table can independently calculate charges for quantities zero through twenty. Fractional quantities need explicit midpoint rounding expectations. Do not silently use binary floating-point arithmetic to predict decimal behavior; the representation choice is part of the contract.

## Oracles can expose desired-versus-current differences

The rater sorts tiers and allocates only through supplied upper bounds. A finite final tier may leave quantity unallocated without throwing in the inspected implementation. A desired contract might require complete coverage, but the current characterization should record the actual result. Keep a failing desired-contract test separate from a passing characterization test.

Likewise, Money checks two-decimal precision and a trimmed three-character currency length, then uppercases it. It does not consult a currency registry or require alphabetic characters in the inspected constructor. A test expecting every three-character numeric string to be rejected would express a proposed stronger policy, not the current implementation.

## Evidence hierarchy

Source inspection establishes what branches and predicates are present. A pure-function harness establishes behavior for supplied inputs. A concurrent in-memory experiment establishes the observed winner contract for that implementation and run. A relational test establishes configured storage behavior. A provider integration test establishes another boundary entirely. Keep these layers in separate report columns.

Do not rank evidence only by test count. One controlled database rollback test can establish a property that ten thousand pure arithmetic cases cannot. Conversely, a database smoke test may not explore rounding boundaries. The best test suite combines narrow evidence appropriate to each claim.

## Exercise

Write three independent oracles: usage replay/conflict, tier allocation and subscription version transition. Include one current behavior you would propose changing and explain why it remains in the characterization table. The reviewer should be able to calculate expected results without reading the implementation line by line. That independence is the foundation of the studio.

[Studio index](README.md) · [Review guide](reviews/01-oracles-and-usage.md)
