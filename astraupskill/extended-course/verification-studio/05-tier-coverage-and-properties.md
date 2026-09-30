# 5. State the preconditions of a property

Property-style testing is powerful when a claim holds across a defined input domain. It is misleading when the test silently assumes complete tier coverage, nonnegative rates or unique bounds that the public method does not enforce. This workshop turns those assumptions into explicit preconditions and counterexamples.

## Conservation of units

For a valid increasing tier schedule ending in an unlimited tier and nonnegative quantity, allocated units should sum to quantity under the intended rating contract. The studio harness checks this for an integer range using a two-tier fixture. Now remove the unlimited tier and rate beyond the last bound. The current implementation can report the original quantity while allocating only through the finite limit.

That counterexample does not invalidate the conditional property; it reveals the missing precondition. A desired API may reject incomplete coverage rather than return a partial allocation. If so, write a proposed validation rule and a separate desired-contract test. Do not silently change the fixture so the uncovered case disappears from review.

## Permutation invariance

The rater sorts tiers by upper bound, treating null as the maximum value. Reordering a set of tiers with distinct bounds should preserve the result under the current implementation. The harness checks reversed order for its fixture. Duplicate bounds introduce another question: stable sorting and distinct prices can make zero-width or later segments behave in ways that require characterization.

Define whether duplicate bounds are valid. If they are not, validation should reject them with a useful code. If they are, document how their prices affect allocation. A generated test that only creates strictly increasing bounds cannot claim it covered duplicate-bound semantics.

## Monotonicity

For nonnegative rates, increasing quantity should not reduce total charge under ordinary tier allocation, though rounding can create flat regions. If negative rates are accepted, monotonicity may fail. Money permits negative amounts, so the property needs a pricing-level precondition rather than assuming the value object supplies it.

Create two schedules: ordinary positive rates and a later negative rate. Calculate totals around the boundary. The exercise distinguishes mathematical properties of a chosen domain from universal properties of every object the constructor accepts. A good test report names the constrained generator used for monotonicity.

## Continuity and boundary steps

At a tier boundary, the next unit is charged at the next rate while prior units retain earlier rates under graduated tiering. This differs from volume pricing, where one rate might apply to all units based on total quantity. The current algorithm allocates segments, so a test expecting volume-pricing behavior would encode a different product model.

Use quantities just below, exactly at and just above a bound. Include fractional values that trigger rounding. Calculate units and charges separately. A total-only assertion can miss an incorrect allocation that happens to produce the same amount when adjacent rates are equal.

## Generator design

Generate positive increasing bounds, a final unlimited tier, one currency and two-decimal nonnegative prices for the core conservation and ordering properties. Keep malformed schedules in a separate boundary suite. Record the seed and failing input. The generator is part of the evidence: changing it changes the domain over which the property was explored.

Avoid asserting that a finite random sample proves the property for all possible decimal values. The sample is useful regression evidence, while reasoning about the algorithm and explicit boundary examples provide complementary support. Decimal overflow and extreme values may need separate tests if the API accepts them.

## Deliverable

Submit four properties with preconditions, two counterexamples and a generator sketch. Include conservation, permutation, monotonicity and allocation semantics. A strong answer can explain why a property failed because of a violated assumption rather than immediately labeling the implementation wrong.

[Studio index](README.md) · [Review guide](reviews/02-money-and-tiers.md)
