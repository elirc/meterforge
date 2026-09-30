# 07. Tiered rating and unallocated units

TieredRater allocates quantity across sorted thresholds, rounds each tier charge, and sums the results. The normal example looks familiar, but useful engineering lessons appear when tiers do not cover the quantity or contain malformed boundaries. This chapter derives actual behavior before proposing stronger validation. It does not assume that a type named UsageTier already enforces a complete pricing schedule.

Read [TieredRating](../../src/MeterForge.Domain/Rating/TieredRating.cs), [RatingEndpoints](../../src/MeterForge.Api/Rating/RatingEndpoints.cs), and [TieredRaterTests](../../tests/MeterForge.Domain.Tests/Rating/TieredRaterTests.cs). UsageTier is a positional record with an optional UpTo threshold and Money unit price. It contains no constructor guards of its own. The rater rejects negative quantity, an empty tier collection, and mixed currencies, then proceeds through the supplied boundaries.

## Thresholds describe cumulative limits

For a first tier up to one hundred units at one dollar and an open-ended second tier at fifty cents, a quantity of one hundred fifty allocates one hundred to the first and fifty to the second. The second threshold is not an additional quantity cap; finite thresholds describe cumulative upper positions. The algorithm tracks previousLimit to compute each segment width.

The source sorts tiers by UpTo, using decimal.MaxValue as the sort value for an absent threshold. This usually places an open-ended tier last. It does not validate that there is exactly one open-ended tier or that every finite threshold is positive and unique. A correct explanation must separate sorting from validating a schedule.

Within the loop, upper is the finite threshold or the requested quantity for an open-ended tier. Units are the smaller of quantity and upper minus previousLimit. Positive units produce a rounded charge and a RatedTier. previousLimit then becomes upper. The loop stops when quantity is at or below previousLimit.

## Work the ordinary boundary

At quantity fifty, only the first tier contributes fifty units. At one hundred, the first contributes one hundred and the next iteration stops because the quantity has reached previousLimit. At one hundred fifty, the second contributes fifty. These predictions match the existing ordinary tests and show why equality matters at the tier transition.

Quantity zero is accepted if tiers exist and use one currency. The loop produces no rated tiers and a zero Money total. This is different from UsageEvent, which rejects nonpositive quantities. A rating calculator can legitimately answer a zero-usage question while ingestion requires an event to represent positive usage. The two boundaries need not share identical validation merely because both use a variable named quantity.

A negative unit price is not rejected by Money's representation guard. Therefore the current rater can calculate negative charges from valid-precision Money values. Whether negative pricing is permitted is a policy not enforced here. Do not write a test that expects rejection without labeling it as a proposed contract change.

## An incomplete schedule can underallocate

Supply one finite tier ending at one hundred and request one hundred fifty. The algorithm charges the first one hundred, finishes the collection, and returns a RatingResult whose Quantity is still one hundred fifty. It does not reject the uncovered fifty units or add an implicit final tier. This is a consequential current behavior that a caller must understand.

An independent conservation check can compare requested quantity with the sum of RatedTier.Units. For a well-formed fully covering schedule, they should agree. The current algorithm does not enforce that invariant for every supplied schedule. A proposed validation layer can require coverage, require an open-ended final tier, or reject quantities beyond the final finite boundary. Each option is a different API policy.

The response's Quantity field should not be treated as proof that all units were priced. It carries the requested quantity. A client displaying total divided by quantity as an effective rate could conceal underallocation by producing a plausible low value. A diagnostic report should show allocated units and coverage status if the product chooses to support partial schedules.

## Malformed thresholds reveal hidden assumptions

A negative finite threshold can make previousLimit negative even when no charge is emitted for that tier. A later tier then computes units from that negative boundary, potentially allocating more units than requested. This follows the arithmetic directly and demonstrates why sorting cannot replace validation of nonnegative, increasing limits.

Duplicate finite thresholds produce a zero-width segment for one tier. Its price may never affect the result. Multiple open-ended tiers can also leave later entries unused after quantity is covered. The algorithm can return a result without telling the caller that part of its supplied schedule was ineffective. A review should ask whether such schedules should be rejected, normalized, or explicitly supported.

Do not turn these observations into an unrequested production patch. First build a tiny fixture and state actual behavior. Then write a separate proposed schedule validator with clear error codes and compatibility expectations. Existing callers may rely on permissive inputs, so tightening validation requires a deliberate transition.

## Rounding remains per tier

For fractional units, each tier's units times unit amount can have more than two decimal places. The rater rounds each charge to two places with ToEven before summing. Two small tier components can therefore total differently from an aggregate-rate calculation rounded once. Use the prior money chapter's midpoint example to make that difference visible.

The sum of emitted line charges should equal the returned total under ordinary nonoverflowing execution because the total is built from those rounded amounts. That is a useful assertion distinct from unit conservation. One protects monetary presentation consistency; the other protects quantity coverage. A test suite should not confuse them or assume one proves the other.

Large decimal quantities and prices can overflow multiplication or addition. The rater's business guards do not establish arbitrary-range safety. A proposed input maximum or overflow translation should be tested separately from tier-shape validation. Financial algorithms need both domain policy and numerical boundary analysis.

## Exercise MF-07A: build a rating ledger

For quantities zero, fifty, one hundred, and one hundred fifty under the two-tier example, list previous limit, upper limit, allocated units, rounded charge, and total. Explain why zero rating is allowed while a zero UsageEvent is not. Include an assertion that compares line-charge sum with result total.

## Exercise MF-07B: expose schedule assumptions

Construct fixtures for a finite schedule that does not cover quantity, a negative threshold, duplicate thresholds, and two open-ended tiers. Predict current behavior from the loop. Identify which fixtures violate a proposed conservation or schedule-validity rule without falsely claiming that rule already exists.

## Exercise MF-07C: propose a validator contract

Define accepted threshold shapes, coverage requirements, price-sign policy, and rounding ownership. Choose stable rejection categories and describe a migration strategy for existing permissive callers. Keep the calculator's actual behavior and your proposed validator in separate columns throughout the design.

## Review checkpoint

A strong answer can calculate the normal schedule and challenge it with a small malformed one. It distinguishes requested quantity from allocated units, sorting from validation, and per-tier rounding from aggregate rounding. It leaves a precise improvement proposal rather than silently attributing stronger invariants to the existing code.
