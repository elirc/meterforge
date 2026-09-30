# 05. Money values and rounding policy

Usage ingestion stores a decimal quantity, but billing eventually needs monetary values. MeterForge's Money type imposes a particular representation rule: the amount must already be expressible at two decimal places under the chosen rounding operation. This chapter distinguishes validation from rounding, value representation from business permission, and arithmetic precision from financial policy. These distinctions will be used immediately in tiered rating.

Read [Money](../../src/MeterForge.Domain/Common/Money.cs) and [TieredRating](../../src/MeterForge.Domain/Rating/TieredRating.cs). The constructor uses decimal.Round with two places and MidpointRounding.ToEven, then compares the rounded result with the supplied amount. If they differ, it rejects. It does not silently replace the input with the rounded value. The rater, by contrast, explicitly rounds calculated charges before constructing Money.

## A valid representation is not necessarily an allowed price

Money accepts a negative amount if it has supported precision. It also accepts zero. That can be useful for adjustments, but it means a caller that requires a positive unit price needs another business guard. Do not infer positivity from the type's name. A value object can protect one invariant while deliberately leaving another to the consuming domain.

Currency validation checks nonblank input and a trimmed length of three, then trims and uppercases the stored text. The implementation does not verify membership in a real currency registry or require each character to be a letter. The error message describes a three-letter code, but the concrete predicate is a length check. A source-grounded lesson should acknowledge that difference rather than importing an unstated standard.

The operator for addition requires equal stored Currency strings. It rejects unlike currencies instead of applying an exchange rate. That is an important boundary: converting amounts requires a rate, time, source, and rounding policy. None is supplied by the addition operator. Treat a proposed conversion feature as a domain operation with evidence, not as a convenient extension to plus.

## Work the precision guard

An amount of 1.23 passes because rounding it to two places leaves the numeric value unchanged. An amount of 1.234 fails because the rounded value differs. A value written with trailing zeroes, such as 1.230, compares numerically equal to 1.23 and is not a separate three-decimal business amount under this guard. Decimal representation scale and numeric equality are related but not identical concepts.

At a midpoint, ToEven chooses the nearest value whose last retained digit is even. For example, a calculated charge of 0.005 rounds to 0.00, while 0.015 rounds to 0.02. These are calculations in the rater before Money construction. Passing 0.005 directly to Money does not ask it to choose 0.00; it asks it to validate the original amount and therefore fails.

That distinction affects API contracts. A caller sending an invalid precision receives a rejection under the current Money constructor. A calculation producing extra fractional digits can deliberately round before constructing a value. If the API were changed to round incoming prices automatically, clients could observe silently altered prices. Such a change requires an explicit policy and tests rather than a claim that both paths are equivalent.

## Rounding location changes totals

Suppose two separate rated components each calculate 0.005. Rounding each component to two places yields zero plus zero. Summing exact components first yields 0.010, which rounds to 0.01. Both follow ToEven at the point they round, but they produce different totals because rounding is not distributive over addition.

MeterForge's tiered rater rounds each tier's charge, adds those rounded amounts, and rounds the total again. The second rounding does not recover fractions discarded from individual tiers. This is a concrete policy embedded in the algorithm. A learner should be able to explain it with a two-tier fractional-quantity example rather than assuming decimal arithmetic makes rounding location irrelevant.

The correct policy depends on the billing contract. Per-line rounding can make displayed line amounts sum exactly to the invoice total. Aggregate rounding can preserve more fractional value before the final amount is chosen. A product may specify one or need a reconciliation rule. This workbook describes current behavior and asks for explicit alternatives; it does not declare a universal accounting rule.

## Default value construction is another path

Money is a readonly record struct. In C#, default value construction can produce a zero amount and a null Currency without running the custom constructor. That is a language-level path relevant to this type's invariant. Ordinary constructor calls validate currency, but the type cannot guarantee every possible value was constructed that way merely because its explicit constructor contains guards.

Do not immediately redesign the type based on this observation. First identify where default values can enter current code: uninitialized fields, arrays, deserialization behavior, or generic defaults. Then decide whether boundary validation, a different representation, or a documented restriction is needed. A focused test can characterize default(Money) without claiming it is already exploited by an endpoint.

This is a useful general lesson about value objects in languages with defaultable structs. Constructor validation is strong within its invocation path, but type-level guarantees require considering all supported construction paths. The same reasoning applies to mutable collection members or serializers that bypass ordinary constructors, although those mechanisms need their own concrete evidence.

## Overflow is separate from precision

Decimal arithmetic has finite range. Multiplying a very large quantity by a price or adding very large amounts can overflow even if each input has two decimal places. Money's precision check is not a proof that every later arithmetic operation is safe. A failure policy should distinguish invalid input, unsupported precision, currency mismatch, and arithmetic overflow.

The current rater does not wrap every decimal operation in a business-specific overflow translation. Do not infer a stable HTTP error code for an overflow from the existence of BusinessRuleException handling. A direct arithmetic exception and its host-level response are different layers. A proposed quantity or price limit needs a business rationale and a test at the appropriate input boundary.

## Exercise MF-05A: classify construction paths

Predict Money construction for positive, zero, negative, excess-precision, trailing-zero, blank-currency, lowercase-currency, and three-character nonletter-currency inputs. Separate what the code accepts from what a production price policy might permit. Include default(Money) as a different construction path and state its limitation.

## Exercise MF-05B: expose rounding location

Construct two calculated components whose individually rounded total differs from rounding their exact sum. Explain the current rater's sequence and identify a client-visible consequence. Propose an alternative only after stating the desired line-item and total contract. Do not replace the algorithm in the application.

## Exercise MF-05C: design a money boundary review

Write a review checklist for a new pricing endpoint that uses Money. Include currency vocabulary, positivity, precision, rounding ownership, default construction, and overflow. For each item, identify whether Money already enforces it or another boundary must decide it. Avoid claiming that using decimal solves every financial correctness concern.

## Review checkpoint

A strong answer knows exactly when values are rejected and when calculations are rounded. It can produce a small non-distributive rounding example, recognizes the default-struct path, and distinguishes representation from allowed business meaning. That precision is more useful than describing the type as safe money without explaining its actual guarantees.
