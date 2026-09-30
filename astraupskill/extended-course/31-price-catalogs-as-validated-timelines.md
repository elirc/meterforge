# 31. Price catalogs as validated timelines

Read [PriceVersion and PriceSelector](../../src/MeterForge.Domain/Pricing/PriceVersion.cs) alongside [Product](../../src/MeterForge.Domain/Catalog/Product.cs). Earlier lessons established the half-open AppliesOn rule. This chapter treats a collection of price versions as a timeline that must remain coherent during authoring, publication, and correction. Individual valid intervals do not automatically form a complete valid catalog.

PriceVersion validates identifiers and requires a non-null end to follow the start. PriceSelector filters by product and applicable date, then requires exactly one match. Zero matches produce price.not_found; multiple matches produce price.overlap. These are valuable runtime defenses, but they detect problems at selection time rather than preventing every invalid catalog publication.

## Validate the set, not only each member

Two intervals can each satisfy their constructor while overlapping. Two can also leave a gap. A proposed catalog publisher should sort intervals per product and inspect adjacent boundaries, open-ended intervals, and the intended coverage window before accepting a revision. The desired rule may allow gaps for unavailable products or require complete coverage for active subscriptions.

State the coverage window explicitly. Requiring a price for every possible DateOnly value is usually a different requirement from requiring coverage for future billable dates after launch. A validator should receive the relevant interval rather than assuming an infinite past. Historical gaps may be known limitations that need review instead of automatic invented prices.

For an open-ended version, a later price requires closing or superseding its applicability under a declared revision protocol. Simply adding another open-ended version creates overlap for later dates. The current record's properties are read-only, so a proposed publisher may create a new catalog revision rather than mutating the accepted object in place.

## Product price and versioned price are distinct representations

Product contains MonthlyPrice, while PriceVersion contains UnitPrice and effective dates. The source types do not by themselves define which one is authoritative for every billing operation. A workflow must choose whether Product.MonthlyPrice is a display default, an initial price, or a separate pricing concept. Do not assume the two values are always synchronized.

Work a product whose MonthlyPrice is thirty and a selected PriceVersion whose UnitPrice is thirty-five. A calculator using one field and an invoice workflow using the other can produce different results without either type's constructor failing. The contract needs an explicit source of truth and a test that catches unintended mixing.

A proposed catalog can make product metadata separate from published pricing revisions. The UI can display the selected effective price rather than a stale embedded amount. If the embedded field remains, document its role and migration path. Removing ambiguity requires a business decision, not merely copying values between fields whenever one changes.

## Publishing a revision under concurrency

Two administrators can each inspect the same current timeline and propose a new price effective on the same date. Each proposal may be valid against the old revision, yet accepting both creates overlap. A publication operation needs a catalog revision precondition or another shared acceptance rule covering the complete timeline.

Use a stable revision identity and expected prior revision. The acceptance transaction binds the new timeline and its receipt. The loser reloads the accepted revision and either returns exact replay or asks for a revised proposal. A local validation function remains useful, but it is not the authority that serializes competing publication.

The revision should preserve historical selection. If an invoice already references a PriceVersionId, later catalog edits must not silently change what that identifier means. A new corrected version can supersede applicability prospectively or participate in an explicit correction workflow. Reusing an identifier for different monetary evidence undermines reproducibility.

## Backdated price corrections

Suppose an incorrect price was published for a past interval and invoices were accepted using it. Replacing the timeline for future calculations does not automatically correct those invoices. The system must distinguish catalog correction from billing correction. A read-only impact report can identify affected accepted documents before any adjustment is proposed.

The report should include affected price identities, periods, and document references, not automatically rerate and overwrite historical amounts. A new calculation may show a difference, but the business decision to issue credits or debits is separate. The course's correction-ledger capstone provides one way to preserve that distinction.

If the product permits retroactive pricing changes, define which date selects the price: event occurrence, billing period, acceptance date, or another policy. The current PriceSelector accepts a date supplied by its caller and cannot decide the caller's temporal meaning. A test should verify the selection-date policy at the application boundary.

## Test timeline publication economically

A pure validator fixture can cover adjacent intervals, a gap, an overlap, duplicate intervals, an open-ended interval followed by another, and two products whose dates overlap harmlessly because they are separate scopes. These cases establish the set logic without a database. Use exact boundary dates and stable identifiers.

A separate concurrency model covers two revisions validated from one base. A provider-backed test later establishes the actual revision compare-and-accept behavior. A hosted test checks authoring authority and public conflict responses. The evidence ladder keeps the pure interval logic fast while reserving shared-state claims for the correct layer.

Do not add a property claiming every date always selects one price unless complete coverage is part of the generated input preconditions. A valid individual PriceVersion collection can intentionally contain a gap under the current selector contract. Tests should encode the proposed publication policy explicitly instead of declaring the selector wrong for reporting not found.

## Exercise MF-31A: validate a catalog revision

Design a set validator for a declared coverage window. Include adjacent, gapped, overlapping, and open-ended intervals across two products. Separate individual constructor validity from collection validity and explain any intentional gaps.

## Exercise MF-31B: resolve two publishers

Work two administrators proposing different prices from the same base revision. Define expected revision, accepted receipt, exact replay, and conflict response. State the persistence evidence needed to prevent both timelines from being accepted together.

## Exercise MF-31C: report a historical correction

Choose a backdated price correction and design a read-only impact report over accepted invoices. Explain why changing the catalog does not rewrite those invoices. Define the temporal selection policy and the separate correction operation needed for affected customers.

## Review checkpoint

Price selection is a local query over a timeline; catalog publication is a shared acceptance operation. Historical billing evidence depends on stable price identity and an explicit selection-date policy. Strong submissions preserve those distinctions while making invalid revisions detectable before they reach customers.
