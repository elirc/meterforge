# 20. Proposed proration and calendar segmentation

The current [Subscription](../../src/MeterForge.Domain/Subscriptions/Subscription.cs) can change products, while [SubscriptionAmendmentService](../../src/MeterForge.Application/Subscriptions/SubscriptionService.cs) applies that mutation through an expected-version repository operation. The inspected implementation does not calculate a prorated invoice for the change. This chapter designs a proposed proration module around the existing evidence boundaries. Every proration formula below is a teaching policy, not a claim about an implemented feature.

The prerequisite is comfort with half-open price intervals, Money rounding, and invoice source evidence. Proration combines those concepts with calendar interpretation. A formula that looks simple can still disagree with customer expectations if its day count, effective date, or rounding scope is unspecified.

## Choose a teaching policy explicitly

For this exercise, a billing period is a half-open DateOnly interval from start inclusive to end exclusive. A product change on date D applies to dates from D onward. The charge is divided by the number of calendar days in the full period, and each segment's amount is rounded only under the chosen allocation rule. This is one possible policy, not a universal billing standard.

Take a thirty-day period and a change after ten days. The old product covers ten days and the new product covers twenty. With synthetic full-period prices of thirty and sixty, the unrounded segment charges are ten and forty. The proposed total is fifty before other adjustments. Record segment boundaries and price-version identities so the result is explainable.

Now use a thirty-one-day period. A fixed "monthly price divided by thirty" formula would apply a different denominator from the declared policy. The test should expose that difference. Calendar months vary, and February can vary by year. The module should derive day counts from explicit dates rather than assuming every month has the same duration.

## Boundary changes should be unsurprising

A change exactly at period start gives the old product zero days and the new product the whole period. A change exactly at period end gives the old product the whole period and the new product zero days in that period. A date outside the period should be rejected or assigned to another period under an explicit rule. Do not silently clamp it without documenting that choice.

Two changes on the same date need an ordering or consolidation policy. If only the final accepted product for that date applies, earlier same-date amendments remain audit evidence but produce no positive-duration segment. If intraday billing is required, DateOnly is insufficient and the model needs instants plus a time policy. The existing date-based fields should not be stretched into a precision they do not represent.

The subscription's current ProductId alone cannot reconstruct historical segments. A proposed amendment ledger must preserve effective date, old and new product or price identities, accepted operation identity, and ordering. Overwriting a pointer is appropriate for current state but loses information needed for retrospective billing unless another record retains it.

## Segment before calculating money

Build a pure segmentation function first. Given period boundaries and accepted amendments, return nonoverlapping ordered segments whose union equals the billable interval. Assert that no day is omitted or counted twice. This structural invariant is easier to verify before introducing decimal arithmetic and rounding.

Then attach the appropriate price version to each segment. Price selection has its own effective interval contract from the earlier chapter. An amendment date and a price effective date can create additional boundaries. The final segmentation may need to split at both kinds of change. A single product-change timestamp does not necessarily identify one price for the entire remaining period.

Only after segmentation should the module calculate raw charges and allocate rounded amounts. This order makes it possible to identify whether a discrepancy comes from calendar coverage, price selection, or monetary rounding. A single large formula that performs all three is harder to review and produces less useful failure evidence.

## Rounding residuals need a deterministic owner

Suppose three segments each have a raw charge with fractions of a cent. Rounding each independently can produce a total that differs from rounding the combined raw amount. Choose whether the invoice total is the sum of rounded segment charges or a rounded document amount allocated back to segments. Both are policies; the implementation must not switch accidentally based on iteration order.

For a document-level allocation, calculate the target total, assign initial rounded or truncated segment amounts under the rule, then distribute the residual deterministically. A stable tie breaker such as segment order can make the result reproducible. Preserve raw calculation evidence if the audit contract requires explaining the allocation.

Do not allocate a residual by modifying an arbitrary last line without checking its semantic role. That line might be a tax or discount line governed by a different rule. The allocation algorithm should receive the eligible segment set explicitly. A test with equal fractional remainders should prove the tie-breaking policy.

## Replays and corrections

A prorated invoice operation needs stable identity. If the same amendment is processed twice, it should not create two sets of charges. The identity should include the accepted amendment or billing operation, not a new random identifier generated at each retry. Exact replay returns accepted evidence; conflicting replay requires a deliberate correction path.

If an amendment is entered late with an effective date in a finalized period, the system must decide whether to issue a correction document, defer an adjustment, or reject backdating. The current finalization guard prevents adding lines to a finalized Invoice through its public method. A proposed proration workflow should respect that boundary instead of secretly rewriting historical evidence.

The calculation can be reproducible without being automatically safe to publish. Authorization, subscription ownership, accepted amendment history, invoice uniqueness, and persistence still surround it. A disposable pure model can establish segment arithmetic, while provider-backed and hosted tests would be needed for the larger workflow.

## Exercise MF-20A: prove calendar coverage

Define segmentation for a thirty-day period with changes at start, day ten, and end. Then repeat for a thirty-one-day period. Assert ordered nonoverlap and complete coverage. State how duplicate same-date changes are resolved under your proposed policy.

## Exercise MF-20B: allocate a rounding residual

Construct three synthetic segment charges with fractional cents. Compare per-segment rounding with document-level allocation. Choose one policy and define a deterministic tie breaker. Explain which evidence must be retained to reproduce the accepted lines.

## Exercise MF-20C: handle a late amendment

Work a backdated change arriving after invoice finalization. Choose a correction policy, define operation identity, and show how the original invoice remains distinguishable from the correction. Identify the historical data absent from the current ProductId-only state that your proposed ledger must retain.

## Review checkpoint

Proration is a composition of calendar coverage, price evidence, rounding, and document policy. A strong model makes each choice explicit and testable. It does not claim that changing Subscription.ProductId already implements a billing adjustment or that one arithmetic ratio settles every temporal question.
