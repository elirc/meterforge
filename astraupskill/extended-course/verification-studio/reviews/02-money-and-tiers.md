# Review 2: money, allocation and rounding

This guide covers chapters four through six. Work the arithmetic before comparing with the harness. The purpose is to make the oracle inspectable by a reviewer who does not need to execute the implementation to understand the expected values.

## Two-tier calculation

For the first ten units at 0.10 and all later units at 0.05, quantity twelve allocates ten units to the first tier and two to the second. The tier charges are 1.00 and 0.10, so total is 1.10. At quantity zero, no positive units are allocated and the total is zero in the selected currency. At exactly ten, the second tier contributes no positive allocation.

The integer oracle used in the harness is quantity times 0.10 through ten, then 1.00 plus the excess times 0.05. This independent piecewise formula is appropriate for the selected schedule. It is not a universal expected-value calculator for arbitrary tiers and fractional rounding.

## Midpoint answers

Quantity 0.05 at price 0.10 produces 0.005 before rounding and zero at two decimals under midpoint-to-even. Quantity 0.15 produces 0.015 and rounds to 0.02. The even digit rule explains both directions. A claim that every half-cent rounds down is false, as is a claim that every half-cent rounds up.

For quantity 0.10 at price 0.10, one segment charges 0.01. Splitting at quantity 0.05 makes two segments each charging 0.005 before rounding, each becoming zero. The total therefore changes under the current per-tier rounding policy. This counterexample invalidates an unrestricted split-invariance property.

## Coverage answer

With only a finite tier ending at ten and requested quantity twelve, the inspected rater allocates ten units and can return a result whose Quantity is twelve. The missing two units are not evidence of an arithmetic sum error; they reveal absent complete-coverage validation for that schedule. A desired-contract test may require rejection or an unbounded final tier, but that is a proposed stronger rule.

For valid complete schedules, sum of allocated units is a useful invariant. State complete coverage as a precondition. Applying the same property indiscriminately to finite schedules confuses current characterization with a new policy requirement.

## Currency and precision answer

Money normalizes trimmed three-character currency text to uppercase. The inspected constructor does not establish membership in a real currency registry. A numeric three-character value passes its local length check. Negative amounts are representable. More than two-decimal precision that changes under the constructor's rounding comparison is rejected rather than silently rounded into a new Money value.

Addition requires matching normalized currencies. Invoice.AddLine imposes additional nonnegative and matching-currency rules. Explain which layer rejects each input. A representation's acceptance does not imply acceptance in every business operation that uses it.

## Lab interpretation

The executed studio harness reports 621 assertions. Most arise from three checks at each integer quantity from zero through two hundred: independent total, coverage and tier permutation. The remaining checks cover Money validation, midpoint behavior, a partition counterexample, incomplete coverage and in-memory usage identity scenarios.

That count is transparent but not a claim of 621 independent business requirements. Repeated generated assertions explore a bounded family. The lab does not run a database, provider, full API or deployment. A good explanation emphasizes the behaviors and input domains rather than treating the number as a quality score.

## Review completion

Ask the learner to create a new half-cent example that rounds upward, then one that rounds downward, without copying the supplied quantities. Have them explain a case where input permutation should preserve the result and a case where changing partition boundaries need not. They pass when they can identify the rule, the precondition and the counterexample in plain language.

[Studio index](../README.md)
