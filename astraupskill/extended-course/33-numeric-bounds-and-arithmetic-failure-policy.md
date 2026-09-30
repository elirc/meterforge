# 33. Numeric bounds and arithmetic failure policy

Use [Money](../../src/MeterForge.Domain/Common/Money.cs), [TieredRater](../../src/MeterForge.Domain/Rating/TieredRating.cs), [PercentageDiscount](../../src/MeterForge.Domain/Adjustments/Adjustments.cs), and [EntitlementEvaluator](../../src/MeterForge.Domain/Entitlements/Entitlements.cs). Earlier lessons explained decimal precision and rounding location. This chapter focuses on range, intermediate arithmetic, and the difference between a business-rule rejection and an arithmetic exception.

Decimal is appropriate for many exact decimal calculations, but it has finite range. Choosing decimal does not make multiplication, addition, or subtraction unbounded. A valid Money value can participate in an operation whose result is outside the representable range. The domain should have a declared policy for supported business magnitudes rather than relying on extreme runtime failure as normal validation.

## Precision validation is not range planning

Money checks that rounding the supplied amount to two places would not change its value. It stores the original accepted value, not a rounded replacement. It does not impose a product-specific maximum invoice or unit price. A large integral decimal can satisfy its precision rule while being impractical or unsafe for a later multiplication.

The addition operator checks currency agreement and then adds amounts. If the decimal sum overflows, that failure is not the same BusinessRuleException as currency mismatch. A caller that catches only business-rule codes may observe a different error path. Tests should distinguish valid-domain rejection from runtime arithmetic failure instead of assuming every invalid operation yields one code.

The invoice Total folds Money additions over lines. Each line can be nonnegative and individually valid while the sum exceeds decimal range. A proposed invoice policy can bound each line and aggregate total before acceptance. The current local guards do not by themselves prove that any arbitrarily large set of valid lines can be summed safely.

## Intermediate multiplication can fail before division

PercentageDiscount computes amount times Percent and then divides by one hundred. Even if the mathematically simplified final result fits, the intermediate multiplication can exceed decimal range. Reordering arithmetic may change overflow and rounding behavior, so a repair needs an explicit numeric strategy and tests rather than a casual algebraic rewrite.

For ordinary business amounts this may never arise, but the boundary is still part of input policy. A proposed API can reject magnitudes above a supported limit before calculation. That creates a stable business error and avoids relying on implementation-specific arithmetic exceptions as customer-facing validation.

Tiered rating similarly multiplies units by unit price before rounding. Quantity is validated as nonnegative, but there is no product-specific maximum quantity in the shown method. Large quantity and large unit price can overflow. A test should choose values that isolate multiplication range from invalid tier coverage or currency mismatch so the observed failure has one clear cause.

## Subtraction and clamping have preconditions

EntitlementEvaluator calculates included quantity minus consumed, then clamps at zero. Both finite inputs are nonnegative under its guards, which narrows the subtraction's behavior compared with arbitrary signed values. Understanding those preconditions is better than adding defensive code without a threat model.

Reconciliation subtracts internal amount from provider amount. Money permits signed values, so extreme opposite-signed inputs can exceed range. Whether negative payment amounts should reach reconciliation depends on the caller's semantic contract. The calculator does not validate that both inputs represent positive settled charges. A proposed coordinator can enforce that role before invoking arithmetic.

Avoid a blanket rule that every decimal input must be positive. Quantities, prices, differences, credits, and balances have different sign policies. The right boundary is operation-specific. The course's earlier separation of monetary roles becomes useful here because supported range and sign can be defined per role.

## Design a numeric envelope

Choose a maximum supported quantity, unit amount, line count, and document total for the proposed billing workflow. Derive why their combinations stay within the intended range. The envelope should reflect product requirements and storage precision, not merely decimal.MaxValue. A database numeric column can have a narrower precision and scale than the in-memory type.

Record whether the API rejects excessive magnitude before rating or whether the calculator returns a domain error. Keep the error stable and distinguish it from precision rejection. A request can have exactly two decimal places and still exceed the supported magnitude. Another can be small but have invalid precision. Tests should cover both independently.

Do not silently saturate or clamp financial amounts to a maximum unless that is an explicit business policy. Saturation can make a successful response contain a different charge from the requested calculation. Rejection with clear evidence is often easier to reason about than silent alteration, especially before document acceptance.

## Boundary tests without brute force

Use a few carefully selected values: zero, smallest accepted positive amount, exact configured maximum, one unit beyond it, and a pair whose intermediate product exceeds the envelope. Add a high line-count aggregate case if the document policy includes such a bound. These tests are more informative than thousands of random ordinary values.

For a proposed arithmetic rewrite, compare against a high-precision reference model within a bounded synthetic domain. State the reference model's rounding policy and avoid assuming algebraic equivalence after rounding. A calculation that divides first may lose precision differently from one that multiplies first. The acceptance criterion is the declared monetary policy, not only absence of overflow.

The current course does not alter Money or the rater. Characterization tests can document present exceptions, while proposed envelope tests describe a future application boundary. Keep them separate so an implementation change can deliberately replace a runtime failure with a stable business rejection.

## Exercise MF-33A: distinguish precision and magnitude

Create one small value rejected for extra precision, one large value accepted by Money, and an operation on accepted values that can overflow. State the expected exception category from source and the additional policy a production boundary might choose.

## Exercise MF-33B: define a supported envelope

Choose synthetic limits for quantity, unit amount, line count, and total. Show that supported combinations fit the chosen arithmetic and storage representation. Define stable errors for violations without globally banning signed Money used for differences.

## Exercise MF-33C: review a proposed arithmetic rewrite

Compare multiply-then-divide with divide-then-multiply for percentage calculation under finite decimal precision. Specify a reference oracle, boundary examples, and rounding stage. Explain why removing one overflow case is not enough to prove identical accepted monetary results.

## Review checkpoint

Numeric correctness includes representation, range, sign, and operation order. A strong review gives each a policy and a discriminating test. It does not treat decimal as unlimited arithmetic or confuse a runtime overflow with a deliberate customer-facing validation contract.
