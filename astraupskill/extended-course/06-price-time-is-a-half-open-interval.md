# 06. Price time is a half-open interval

PriceVersion represents a unit price that applies over a date interval. The selector demands exactly one applicable version for a product and date. This chapter explains why half-open boundaries avoid one common ambiguity while leaving other catalog questions unresolved. You will distinguish a valid individual interval from a valid set of intervals and trace gaps, overlaps, and product scope.

Read [PriceVersion and PriceSelector](../../src/MeterForge.Domain/Pricing/PriceVersion.cs), [PricingEndpoints](../../src/MeterForge.Api/Pricing/PricingEndpoints.cs), and [the pricing tests](../../tests/MeterForge.Domain.Tests/Pricing/PriceVersionTests.cs). The date type is DateOnly. That choice describes calendar dates without an offset or time of day. Mapping an event's UTC timestamp to a pricing date requires an explicit policy outside this selector.

## Include the start and exclude the end

AppliesOn returns true when date is at or after ValidFrom and either ValidTo is absent or date is before ValidTo. An interval from January first through a July first boundary includes June thirtieth but excludes July first. A new interval beginning July first can therefore be adjacent without overlapping at midnight in the date model.

The constructor rejects an end at or before the start. An absent end represents an open-ended interval. The guard prevents an empty or reversed bounded interval, but it does not inspect other versions. Two individually valid open-ended versions for the same product can overlap. Set consistency is the selector's concern when asked for a date, not something established by constructing either object alone.

The existing adjacent-boundary test creates old and new prices with the same transition date and expects only the new one. This fixture is more discriminating than testing a date deep inside either interval. A boundary rule should be tested at the exact equality point, immediately before it, and where appropriate immediately after it.

## Selection refuses ambiguity

PriceSelector filters by ProductId and AppliesOn, materializes matches, and switches on the number of results. One match succeeds. Zero matches throws price.not_found. More than one throws price.overlap. It does not choose the latest start, lowest price, first enumeration entry, or largest version Id when intervals overlap.

That refusal is a policy choice. Returning a plausible price from an ambiguous catalog could hide an operational defect and produce inconsistent bills depending on enumeration order. By rejecting, the selector makes ambiguity visible. A future product might define precedence rules, but that would be a new contract requiring tests and a clear reason for accepting overlapping versions.

Product filtering matters as much as dates. Two versions for different products can cover the same dates without causing an overlap for one product's selection. A test that omits ProductId distinctions could falsely label a valid catalog ambiguous. Always include scope in the fixture, especially when several kinds of identity appear in one model.

## Work a three-version calendar

For product P, define version A from January first to April first, version B from April first to July first, and version C from July first with no end. Selection on April first chooses B, and selection on July first chooses C. A date before January first has no match. The chain covers its intended future range without a boundary overlap.

Now change B's end to July second while leaving C's start at July first. July first has two matches and is rejected. June thirtieth remains valid. This shows why one successful query does not prove the entire version set is nonoverlapping. The selector validates applicability at one date; a catalog-wide validation tool would need to inspect all intervals or their boundaries.

Change B's start to April second instead. April first becomes a gap. The absence can be intentional if the product was unavailable or an error if uninterrupted pricing was promised. The selector reports no match either way; it does not know the product's commercial requirement. Domain policy must explain whether gaps are legal before a validation tool decides how to report them.

## The endpoint is a calculator boundary

The inspected pricing endpoint receives candidate versions in the request and constructs PriceVersion objects with the request's ProductId. It then selects for AsOf and returns the chosen price. This is not evidence of a persisted authoritative catalog lookup. A client-supplied version list and a server-managed price book have different trust and ownership implications.

The route requires a nonblank tenant string in this handler, but the selector itself filters by product rather than tenant. If a future durable price catalog is introduced, tenant scope and authorization need their own boundary. Do not infer a complete multi-tenant pricing service merely because the path includes tenants.

This calculator shape is useful for learning because inputs are explicit and small. It also makes it easy to test gaps and overlaps without a database. The limitation should remain in the documentation: a correct calculation over supplied versions does not establish that the caller used the authorized or historically effective price book.

## Connect event time to price time carefully

UsageEvent normalizes OccurredAt to UTC, while PriceSelector accepts DateOnly. Suppose usage occurs near midnight UTC and the customer is billed according to a local business timezone. Converting the UTC timestamp directly to a date may choose a different price than converting to the contract's timezone first. Neither choice is universally correct; the billing agreement must define it.

Late-arriving usage adds another dimension. Should a fact be priced using occurrence date, ingestion date, or a locked invoice-period price version? The current selector does not answer that question. A future pipeline should record which date and version were used so a reviewer can reproduce the calculation. Recomputing from today's catalog without version evidence can change historical results.

Keep the initial exercise bounded. Draw two timestamps that represent the same instant with different offsets and show how an explicitly chosen timezone yields one date. Then compare occurrence and ingestion dates for a late event. This is a policy design exercise; it does not require altering current domain classes or pretending a complete billing pipeline already exists.

## Exercise MF-06A: construct the calendar

Write selection results for the three-version calendar at every start and end boundary, in a gap, and in an overlap. Include a version for another product. Explain why individual constructor validity does not establish that the whole catalog is valid for every date.

## Exercise MF-06B: review a precedence patch

A proposed patch orders matches by ValidFrom descending and returns the first instead of throwing on overlap. Give a fixture where the patch changes current behavior. Explain a business requirement that might justify it and the compatibility evidence required. Do not describe the patch as a harmless optimization.

## Exercise MF-06C: define pricing-time evidence

Write a proposed policy for translating usage occurrence time to pricing date and retaining the selected version. Include late usage and a customer timezone. State which current components supply values and which orchestration is not yet implemented. Your output should be a reproducible decision record, not a claim of existing historical billing guarantees.

## Review checkpoint

A strong answer can explain exact date boundaries, distinguish interval validity from catalog coverage, and recognize the endpoint's client-supplied calculator role. It refuses to infer a persisted price authority or time-zone policy from a small selector. That restraint makes later pricing and invoice designs easier to review.
