# 14. Generate useful cases and shrink failures

The studio's actual-source harness iterates integer quantities from zero through two hundred for a two-tier schedule. That is a bounded systematic check, not an exhaustive proof over all decimal values and tier structures. This workshop extends the experimental design while preserving clear preconditions and independent expectations.

## Generate inside a declared domain

Define a valid tier family with strictly increasing positive finite bounds, a final unbounded tier and nonnegative prices represented to two decimal places. Generate quantities in a bounded range. These preconditions make unit conservation meaningful: allocated units should sum to requested quantity. If the final tier is finite, conservation may not hold in the current implementation, so that input belongs in a separate characterization family.

Keep invalid-input generation separate. Negative quantities, empty schedules, mixed currencies and incomplete coverage do not all have the same expected response. Some are explicitly rejected by the inspected code; others are accepted with surprising results. A generator that labels all unusual inputs invalid can conceal gaps between desired policy and current behavior.

## Bias toward boundaries

Uniform random quantities may rarely land exactly on a tier boundary or a rounding midpoint. Include each finite bound, a small amount below it and a small amount above it. Include zero, the maximum selected quantity and fractional products ending in half a cent. Explain the chosen decimal step; it defines what near a boundary means in this experiment.

Generate price and quantity pairs deliberately. A price of 0.10 with quantity 0.05 produces 0.005 before rounding, while price 0.10 with quantity 0.15 produces 0.015. These distinguish the even-digit rule. Randomly choosing only whole quantities and whole-cent prices can miss every fractional-cent case.

## Metamorphic relationships

For valid schedules with distinct bounds, permuting the input tiers should not alter the result because the implementation sorts them. Increasing quantity under nonnegative prices should not reduce the total, although rounding may produce plateaus. Adding a zero-price unbounded final tier can improve coverage without increasing the charge for units beyond the prior final bound.

Some attractive relationships are false. Splitting a tier with the same price can change the total because each tier charge is rounded separately. Combining two independent usage quantities before rating can differ from rating them separately because both tier thresholds and rounding are nonlinear. State these counterexamples explicitly rather than encoding an invalid property and calling the implementation defective.

## Reproducible seeds

Record the seed, generator version, number of generated cases and all preconditions. On failure, persist the complete failing input, not just the seed. A future generator change can make the same seed produce different cases. A persisted concrete counterexample remains useful after the random strategy evolves.

Report coverage categories as well as case count. For example, how many cases crossed multiple tiers, used fractional quantity, hit a midpoint or contained an unsorted schedule? Ten thousand cases concentrated in one easy region may be less informative than twenty carefully chosen boundaries.

## Shrinking without breaking the premise

When a property fails, reduce the input while preserving both the failure and the property's preconditions. Remove irrelevant tiers, lower quantity, simplify prices and reduce identifiers where appropriate. A shrinker that removes the unbounded final tier may create incomplete coverage and therefore change the reason for failure.

Keep a short shrink trace. It should show which simplification was attempted, whether the input remained valid, and whether the same property still failed. The final counterexample should be understandable by hand. If shrinking changes the exception or failed assertion, investigate whether you have found a different defect rather than continuing under the original label.

## Example failure explanation

Suppose a property claimed that splitting any tier at the same price preserves total. A large generated schedule fails. Shrinking can reduce it to quantity 0.10, price 0.10 and a split at 0.05. The unsplit charge is 0.01. Each split segment charges 0.005 before rounding and rounds to zero, so the total becomes zero. The failure reveals a false proposed property under the current per-tier rounding rule.

The correct response is to revise the specification or consciously propose another rounding policy, not to hide the counterexample. Property-based testing is useful partly because it can expose mistakes in the test author's assumptions.

## Exercise

Design two generators and one shrinker on paper. One generator covers valid complete tier schedules; the other targets accepted but incomplete schedules. Give three true properties with explicit preconditions and two tempting false properties with counterexamples. Explain how your result file would let another learner reproduce a failure without depending on your current random generator implementation.

[Studio index](README.md) · [Review](reviews/05-experimental-design.md)
