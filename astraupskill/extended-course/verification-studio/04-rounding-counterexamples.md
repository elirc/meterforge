# 4. Rounding can break an apparently obvious equality

Money requires values with at most two decimal places. TieredRater multiplies units by unit price, rounds each tier charge to two decimals using midpoint-to-even, then sums those rounded charges. This creates a specific accounting policy. It is not always equivalent to summing unrounded products and rounding once at the end.

## One midpoint

Use unit price 0.10 and quantity 0.05. The raw product is 0.005. Midpoint-to-even at two decimals produces 0.00 because the retained hundredths digit is even. With quantity 0.15, the product is 0.015 and rounds to 0.02. These examples are deliberately small so the rounding rule is decisive.

Calculate several neighbors: 0.004, 0.005 and 0.006; then 0.014, 0.015 and 0.016. State expected two-decimal values before running the harness. Distinguish decimal arithmetic from a binary floating-point approximation. A test using double literals can introduce another representation issue unrelated to the domain's decimal contract.

## Partitioning changes the result

Rate quantity 0.10 in one unlimited tier at 0.10. The raw charge is 0.01 and the total is 0.01. Now split the same quantity into two tiers of 0.05 units each at the same price. Each raw tier charge is 0.005 and rounds to 0.00, so the summed result is 0.00 under per-tier rounding. Equal total units and prices do not guarantee equal totals when the rounding partition changes.

This is not necessarily an implementation bug. It is a policy consequence that must be understood and documented. A product can choose per-tier, per-line or invoice-total rounding, but the choice affects reconciliation and customer explanations. The course reports the current local arithmetic and asks for an explicit proposed policy if changing it.

## Money construction versus rating rounding

Constructing Money directly with 0.005 fails precision validation because the value has more than two decimal places. TieredRater can accept a two-decimal unit price and fractional units, produce an intermediate product with more precision, round it, then construct Money. Those paths are consistent because rounding occurs before the Money constructor in the rater.

Write a test that expects direct construction to reject and rating to return the rounded charge. A weak test that catches any exception could miss an unrelated currency error. Assert the specific code and the expected successful path. The distinction teaches where rounding belongs in the current design.

## Negative amounts and domain meaning

The Money constructor does not reject negative amounts in the inspected code. Credits can reasonably use negative values, while a usage tier with a negative unit price may require a different domain rule. Money is a value representation; it does not automatically enforce every pricing policy. A test should not claim that all negative Money is invalid unless a higher-level contract says so.

Create a characterization case for negative two-decimal Money, then inspect the pricing domain before deciding where a nonnegative rate constraint belongs. A generic value object can be reused for charges and credits, so imposing one policy everywhere may break legitimate flows. Domain-specific validation should express the actual meaning.

## Exercise

Submit midpoint calculations, the partition counterexample, constructor/rater contrast and a proposed rounding policy note. Include a customer-facing explanation for a one-cent discrepancy that is accurate without exposing irrelevant implementation detail. A strong answer understands that arithmetic identities can fail after rounding and chooses tests around those boundaries.

[Studio index](README.md) · [Review guide](reviews/02-money-and-tiers.md)
