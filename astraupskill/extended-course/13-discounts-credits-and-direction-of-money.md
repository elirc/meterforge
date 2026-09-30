# 13. Discounts, credits, and the direction of money

Read [PercentageDiscount and CreditNote](../../src/MeterForge.Domain/Adjustments/Adjustments.cs), the [discount preview endpoint](../../src/MeterForge.Api/Adjustments/AdjustmentEndpoints.cs), and the [adjustment tests](../../tests/MeterForge.Domain.Tests/Adjustments/AdjustmentTests.cs). The central trap is interpreting a positive monetary value without naming its role. A discount reduction, a final payable amount, and a credit amount can all be positive while changing a balance in different directions.

The prerequisite is the Money chapter. The calculations here use decimal values and explicit rounding, but arithmetic alone does not determine business meaning. Every worked trace should label the original amount, reduction, net amount, and any separate credit. Otherwise a test can assert the right number in the wrong field.

## Apply returns the reduction

PercentageDiscount requires a nonblank code and a percentage greater than zero and at most one hundred. It trims and upper-cases the code. Apply computes the percentage of the supplied amount, rounds to two decimal places with midpoint-to-even, and returns that reduction as Money in the same currency. It does not return the amount remaining after discount.

The existing test uses an original amount of 19.99 and ten percent. The reduction is 2.00, while the original Money remains 19.99. The endpoint then subtracts the reduction from the original to produce its net field. A caller that treats Apply's result as the discounted price would charge 2.00 instead of 17.99 in this example.

Naming can prevent that mistake. A local variable called reduction communicates more than result. A contract could expose Original, Reduction, and Net together with the discount evidence. The endpoint already demonstrates this separation at the response level. A learner should preserve it when designing an invoicing integration.

## Rounding is a policy with observable boundaries

Take a small synthetic amount where the unrounded percentage lands exactly halfway between two cents. Midpoint-to-even selects the neighboring cent with an even final digit. This differs from always rounding away from zero. The exercise should calculate the unrounded decimal value, identify the two candidate cents, and explain which one is selected.

Applying the discount to each line and summing reductions can differ from applying it once to the invoice total. Neither strategy is automatically supplied by PercentageDiscount, because the method receives one Money value and does not know whether it represents a line or a document. The invoicing workflow must choose a scope and preserve enough evidence to explain it.

For example, several very small lines can each produce a zero rounded reduction while their combined amount produces a nonzero reduction. A test should demonstrate that case with synthetic values. If the product chooses document-level discounts, it also needs an allocation rule for distributing the reduction across lines when taxes, refunds, or reporting require line-level evidence.

## Negative input is a separate contract question

PercentageDiscount validates its percentage but does not reject a negative Money input in Apply. Money itself supports values used elsewhere as differences, so nonnegativity cannot be assumed from the type. Applying a positive percentage to a negative amount produces a negative reduction under the arithmetic. Whether that is meaningful depends on the caller's business role.

The preview endpoint constructs Money from the requested amount and does not add a separate nonnegative guard in the inspected body. Characterize this behavior without inventing a financial policy. A proposed customer-facing discount preview may reject negative bases, while an internal correction calculation might intentionally allow signed values. The guard belongs where the semantic role is known.

Avoid solving the issue by globally forbidding negative Money. Reconciliation differences need a sign, and other adjustment representations may as well. A stronger design can distinguish amount roles through methods, value types, or explicit validation at boundaries. The lesson is to constrain the operation, not indiscriminately weaken a reusable value type.

## A credit is a separate document

CreditNote requires nonempty credit and invoice identifiers, a positive amount, tenant, and reason. It trims the reason and normalizes the issued timestamp to UTC. It does not load the invoice, compare currencies against that invoice, or check how much credit has already been issued. Those facts require an application workflow with access to authoritative records.

A positive CreditNote.Amount expresses the magnitude of a credit. It should not be confused with a positive invoice line, which increases an invoice's total. The current Invoice rejects negative lines, so a workflow cannot simply append a negative credit line to a finalized invoice. A separate correction document can preserve the original invoice's evidence while recording the new event.

Work an invoice of one hundred and a proposed credit of twenty. The credit document can be valid by its local constructor guards, but the account balance changes only when some application process applies or records that document under a declared policy. Constructing the record is not equivalent to persisting it, refunding money, or notifying a provider.

## Repeated credits need identity and limits

Two credit notes of eighty each can each satisfy the constructor's positive-amount guard against the same invoice identifier. The constructor does not know the invoice total or earlier credits. If the product forbids cumulative credits beyond the eligible amount, that invariant requires a shared ledger and concurrency rule.

A replay also needs a stable cause identity. A new Guid per retry can create two valid credit records for one customer-service decision. An application can use a correction operation key and compare the accepted invoice, amount, currency, and reason policy before returning the original receipt. A repeated key with conflicting parameters should not silently create a different correction.

Reasons are evidence, but free-form text is not a reliable replay identity. A user may correct a typo without changing the intended credit, and two legitimate credits may share a reason. Separate machine identity from explanatory text. Preserve the explanation as part of the accepted document rather than using it as the only deduplication key.

## Exercise MF-13A: label every amount

Trace the 19.99 ten-percent example through Apply and the preview response. Then choose a midpoint example and show the rounding decision. Identify the field that would be wrong if a caller interpreted the reduction as the final price.

## Exercise MF-13B: choose discount scope

Construct a small multi-line example where per-line and document-level rounding differ. Choose a policy, define an allocation rule if needed, and specify evidence stored with the final invoice. Explain why the current Apply method alone does not choose that policy.

## Exercise MF-13C: design a credit workflow

Define authorization, invoice lookup, currency matching, cumulative-credit limits, operation identity, and persistence. Work two concurrent eighty-unit credits against a one-hundred-unit eligible amount. State which constructor guards still help and which new shared invariant prevents over-crediting.

## Review checkpoint

A correct adjustment calculation has a direction, scope, and cause. A correct workflow also has authority, cumulative limits, and replay evidence. Keep the small domain records honest: their local guards are valuable, but they do not establish a complete accounting transaction by themselves.
