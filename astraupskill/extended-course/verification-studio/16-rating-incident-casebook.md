# 16. Explain billing differences with arithmetic evidence

These scenarios use [Money](../../../src/MeterForge.Domain/Common/Money.cs) and [TieredRater](../../../src/MeterForge.Domain/Rating/TieredRating.cs). Calculate the expected allocation and rounded charge before discussing remediation. An incident explanation should distinguish a calculation defect from an unexpected but documented rounding or coverage rule.

## Case A: a one-cent difference

A schedule charges 0.10 per unit. Quantity 0.10 rated as one segment produces 0.01. A revised schedule splits that same quantity into two segments of 0.05 at the same price. Each segment produces an unrounded charge of 0.005, which rounds to zero with midpoint-to-even. The revised total is zero.

The difference is not caused by binary floating-point approximation in this decimal calculation. It follows from rounding each tier separately. Record the raw product, rounded amount and rounding point for each segment. A spreadsheet using a different midpoint rule may produce another result; that spreadsheet is not automatically the authoritative oracle.

The policy question is whether to round per tier, per line, per invoice or at another defined boundary. Changing that boundary can affect existing bills. A proposal needs comparison fixtures and an explicit effective date or version, not just a new rounding helper.

## Case B: quantity exceeds the last bound

An invoice explanation says twelve units were rated, but a schedule contains only a finite tier ending at ten. The result reports the requested quantity while its tier allocations sum to ten. Under the inspected implementation, the remaining quantity need not be rejected or allocated because the loop has no final coverage check.

Calculate the missing two units and identify whether a catalog author intended an unbounded final tier. The next discriminating evidence is the exact schedule supplied to the rater, including nullable bounds and ordering. A screenshot showing only a price label does not establish the tier structure.

Separate a proposed validation fix from historical correction. A future rule requiring complete coverage may prevent new omissions, but it does not identify every past invoice affected. A reconciliation query would need stored quantity, schedule version and allocated units or enough evidence to reconstruct them.

## Case C: a negative amount exists but cannot be invoiced

A developer constructs Money with a negative amount successfully, then receives invoice.line_negative when adding it to an Invoice. They conclude that the two classes disagree.

The classes enforce different contracts. Money checks representation precision and currency shape. Invoice.AddLine imposes a nonnegative line rule. A negative monetary value can be useful elsewhere, such as a calculation or a future credit representation, without being valid for this particular invoice method. The failure does not prove that Money should universally reject negatives.

Design a credit-note proposal separately if the product needs one. Explain identity, relation to the original invoice, allowed amounts and audit evidence. Do not bypass the invoice rule by changing signs or labels in a lesson. The appropriate representation is a domain decision.

## Case D: a strange currency code is accepted

Money accepts a trimmed three-character currency string and uppercases it. A three-character numeric string therefore passes the inspected length-based validation. An operator calls this evidence that the application supports a real currency with that code.

Acceptance by this constructor proves only that the value passed its local rule. It does not establish a recognized currency registry, provider support, tax treatment or settlement capability. The next step is to inspect higher-level validation and integration boundaries before describing end-to-end support.

A stronger currency policy might validate against an explicit supported set, version that set and keep historical invoices readable even if support changes. Tests should distinguish unsupported-new-input rejection from the ability to display an old immutable document. Rejecting every old code during deserialization can create an operational problem of its own.

## Case E: order changes but price does not

A client rearranges tiers in its input payload and expects a changed total. The rater sorts by upper bound, treating an unbounded tier as last through its ordering expression. For distinct valid bounds, input permutation should preserve allocation and total. This is why the studio harness includes a permutation check.

Duplicate bounds and multiple unbounded tiers need separate treatment. Do not extend the distinct-bound property to malformed or ambiguous schedules without examining behavior. A robust catalog validator can reject such schedules, while the rater's characterization documents what it currently accepts.

## Exercise

Write one calculation sheet for each scenario. Include inputs, exact decimal products, allocated units, rounded charges and the observed or predicted outcome. Then write a remediation note separating validation, calculation-policy changes and historical reconciliation. The note should make clear which actions are proposals and which source behavior has actually been executed by the studio harness.

[Studio index](README.md) · [Review](reviews/06-incident-casebooks.md)
