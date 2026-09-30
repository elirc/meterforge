# Review 05. Decisions, adjustments, and snapshots

Use this review after chapters 12 through 16. The grading principle is correspondence: does the claimed business guarantee correspond to the mechanism actually executed? A correct arithmetic result is not automatically a reservation, refund, immutable document, or durable receipt. Each answer should identify both the local guarantee and the surrounding workflow still required.

## MF-12A: explain remaining quantity

With allowance 100, consumed 80, and requested 20, the decision allows the request and reports zero remaining after the hypothetical request. Requested 21 is denied and reports the unchanged available 20. Unlimited returns Allowed true with Remaining null. A missing feature returns a denial with zero and a distinct reason. These values are calculations from supplied inputs, not committed balances.

A client should render null as unlimited under this contract rather than replacing it with zero. It should not claim the displayed remainder is reserved. A good response description says whether the number is hypothetical after-request capacity or current available capacity. Full credit requires checking Reason as well as Allowed and Remaining, because identical numeric values can have different explanations.

## MF-12B: expose duplicate feature ambiguity

Two inputs such as a padded mixed-case feature and its canonical lower-case form normalize to the same Feature. SingleOrDefault encounters multiple matches and throws rather than selecting the first, summing allowances, or returning a normal denial. The item constructor cannot establish uniqueness of the collection that later contains it.

A proposed policy can reject duplicate canonical features at collection construction. If merging is intentional, define the algebra: whether finite amounts add, whether unlimited dominates, and whether duplicated data can accidentally inflate capacity. The same rule must apply during imports and API construction. Deduct credit for calling duplicates harmless because normalization succeeded; normalization is what makes the ambiguity visible.

## MF-12C: design enforceable admission

Both preflights see four available and allow three. That is expected for the pure evaluator. A proposed reservation service must atomically accept at most one three-unit reservation when only four remain. Use a stable operation identity and a shared conditional transition or versioned write, then return the accepted receipt on exact replay.

If the response is lost after reservation commit, a retry must not reserve another three. It reads or competes under the same identity. A process-local lock can demonstrate ordering within one process but does not establish independent-process enforcement. The evaluator remains useful for explaining the arithmetic and reason, while the reservation authority supplies state and concurrency.

## MF-13A: label every amount

Apply returns 2.00 for a 19.99 base at 10 percent. The endpoint's net is 17.99. Treating 2.00 as the final price confuses reduction with payable amount. For a midpoint example, a 0.05 base at 10 percent produces 0.005 before rounding, which midpoint-to-even rounds to 0.00 at two decimal places. The original Money remains unchanged.

The answer should show the raw percentage result, rounded reduction, and final subtraction separately. That makes the role of rounding visible. A test that checks only the final number can conceal an incorrect intermediate interpretation that happens to work for one example. Full credit also preserves currency and normalized discount code evidence.

## MF-13B: choose discount scope

Three 0.05 lines at 10 percent each yield zero rounded reduction per line, while the combined 0.15 base yields 0.015 and rounds to 0.02 under midpoint-to-even. This demonstrates a real policy difference. A document-level policy must allocate the two-cent reduction if line-level evidence is required; a per-line policy intentionally retains a zero total reduction for this example.

Neither answer is automatically wrong, but the policy must be consistent across preview, finalization, refunds, and tax input selection. A deterministic allocation might assign residual cents by fractional remainder and a stable line order. Store the policy version and accepted allocation. Deduct credit for silently switching scope to make a preferred test total pass.

## MF-13C: design a credit workflow

The workflow authorizes the tenant, loads the referenced invoice, verifies currency and eligible credit amount, then accepts a stable correction operation under a cumulative limit. Two concurrent80-unit credits against 100 cannot both succeed under a no-over-credit policy. A shared conditional ledger update or equivalent transaction must choose the accepted total.

The CreditNote constructor still protects nonempty identities, positive amount, tenant, and reason, but it does not know previous credits. Exact replay of an accepted correction returns that credit rather than constructing another random identity. A conflicting key with a different amount or invoice requires a conflict response. Free-form reason text remains explanation, not the only replay key.

## MF-14A: preserve sign and identity

Internal 100 and provider 99 produce Difference minus1. Internal 99 and provider 100 produce plus1. The orientation is provider minus internal. Two local 50 payments and two provider 50 records remain ambiguous without transaction correspondence, even though every amount comparison returns matched.

A coordinator should establish provider account and transaction identity, local operation identity, tenant, and currency before comparing amounts under its policy. Missing and ambiguous records are separate categories. Full credit requires refusing to certify correspondence from amount equality alone and explaining that the endpoint compares supplied values rather than independently retrieving a statement.

## MF-14B: enforce cumulative refund limits

Each70-unit Refund can pass the local constructor against an original 100. A shared refund ledger must prevent the accepted sum from exceeding the remaining refundable amount. Both callers need stable refund-operation identities. One can accept70 while the other is rejected or recalculates against 30 remaining according to the API contract.

If the accepted refund reaches the provider but its response is lost, recovery queries or replays under the same provider refund identity. It must not assume the refund is absent or reuse a new key merely because the local call threw. The original charge reference supplies correspondence; the refund operation supplies its own replay identity.

## MF-14C: classify a statement import

The six categories are exact matched pair, amount mismatch, currency mismatch, missing provider record, duplicate provider record, and ambiguous correspondence. Each report row should include safe identifiers, observed values, the matching rule, and a next check. Compare is invoked only after a valid same-currency pair is established.

The first tool remains read-only. It should not refund an apparent duplicate automatically or rewrite an internal amount to match a statement. PaymentDispute's record shape does not add a dispute lifecycle to this import. A strong submission proposes separate review and authority for consequential actions while preserving the evidence that motivated them.

## MF-15A: audit the conversion map

From copies header identity, tenant, subscription, currency, period boundaries, and finalization instant. It projects each line's identity, description, amount, currency, price-version identity, and source reference. The existing test checks a small subset plus draft rejection. Additional assertions should target fields whose loss changes meaning, such as source reference, optional price identity, and normalized timestamp.

Use two lines with deliberately different evidence so swapped or omitted fields fail visibly. A test containing identical lines can miss ordering or mapping errors. Assert the same instant after UTC normalization rather than only checking that a timestamp is non-null. The goal is a discriminating map test, not many assertions that all restate the invoice identifier.

## MF-15B: prove collection ownership

The factory projects a new array, so replacing an array element in a disposable snapshot does not replace the original invoice's private line. However, the snapshot's IReadOnlyCollection can still refer to that mutable array. A caller with the concrete reference can replace an element, changing the snapshot's observed collection.

A proposed immutable collection or defensive copy at acceptance can prevent later mutation from changing persisted evidence. Document whether consumers receive a live view or a stable value. The cost can include allocation and serialization changes. Full credit distinguishes separation from the aggregate from deep immutability of the snapshot; these are not the same guarantee.

## MF-15C: choose a snapshot purpose

For issued-document evidence, preserve exactly the accepted lines and metadata displayed or used at issuance. For reproducible calculation evidence, also retain input selection, algorithm and policy versions, and external quote correspondence, possibly through linked records. For storage transfer, keep the DTO narrower but document where authoritative evidence lives.

A later price change must not silently overwrite the historical amount or PriceVersionId. A new calculation can be a preview or correction proposal, clearly distinguished from the accepted original. The rubric rewards a coherent purpose and explicit links between evidence records rather than one ever-growing object that vaguely promises to preserve everything.

## MF-16A: classify replay outcomes

Identical header and line keys can encounter do-nothing conflicts and avoid duplicate rows. Reusing keys with changed values does not compare those values; existing data can remain while the call completes. Reusing the header identity with new line identifiers attempts new line inserts under that header. Schema constraints may affect the exact result and need integration evidence.

Do nothing is not an equality assertion. It suppresses the conflicting insertion. A strong answer identifies the accepted-document ambiguity even when all rows have valid primary keys. It does not claim that the source alone proves a particular deployed mixed document exists, and it does not assume absent schema checks without inspection.

## MF-16B: design immutable acceptance

Bind a stable document operation key to a canonical evidence payload and accepted result. The canonical representation includes line identities or semantic causes, amounts, currencies, periods, source references, price identities, and other required evidence. It defines ordering and serialization so equivalent payloads compare consistently.

The transaction accepts one coherent header and line set plus its receipt. A competing caller reads the accepted evidence: identical means replay; different means conflict. A total-only hash is insufficient because different evidence can sum to the same total. A preflight hash comparison outside the acceptance transaction is also insufficient under concurrent first submissions.

## MF-16C: write the integration plan

Use a disposable PostgreSQL instance with the intended schema. Test first save, exact repeat, changed header under the same key, changed line values under the same keys, new line keys under the same header, partial command failure, and two competing acceptances. Observe rows, transaction outcome, and caller response together.

A pure model can explain the proposed winner protocol but cannot prove PostgreSQL constraints or isolation. The report must say which experiments ran and which remain planned. No existing customer database or external payment provider is needed for this bounded design exercise. Full credit goes to a plan that tests coherent document meaning rather than only row counts.
