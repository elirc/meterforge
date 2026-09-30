# Review 02: money, pricing, rating, and ownership

Attempt chapters 05-08 before consulting these answers. The key habit is to separate a constructor's actual predicate from the stronger invariant suggested by a type name. Money does not encode every pricing policy. A valid PriceVersion does not validate an entire catalog. A sorted tier collection is not necessarily complete. A Version property does not control mutations that bypass the repository operation.

## MF-05A: representation and policy

Positive, zero, and negative amounts are accepted when their numeric values have supported precision. Excess fractional precision is rejected rather than silently rounded. Trailing zeroes do not create a numerically different amount, so a value such as 1.230 can pass. Blank currency fails; lowercase currency is uppercased; a trimmed three-character nonletter string satisfies the implemented length predicate even though it is not a meaningful currency code.

default(Money) bypasses the custom constructor because Money is a struct. Its zero amount and default currency illustrate why constructor guards are not a proof about every construction path. The correct response is to investigate where such values can enter supported workflows, not to announce a vulnerability without a reproducing path.

The rubric rewards exact predicates. An answer claiming all amounts are positive or all currency values are validated against a registry is unsupported. A good proposed improvement states which consuming domain needs that stronger rule and how existing callers would be affected.

## MF-05B: rounding is not distributive

Two exact components of 0.005 each round individually to 0.00 under ToEven, while their exact sum 0.010 rounds to 0.01. The current rater rounds tier charges before summing. Therefore the result follows the first policy. The second rounding of the already summed rounded charges does not restore discarded fractional value.

A proposal to round only once at the end changes line-item and total behavior. It must explain whether displayed line charges should add exactly to the displayed total and how any residual is allocated. The exercise does not require one universal financial policy. It requires a reproducible policy with a counterexample showing why placement matters.

## MF-05C: assign each guard to an owner

Money currently checks nonblank trimmed length-three currency and at-most-two-decimal numeric precision, normalizes currency case, and rejects addition of unequal currencies. It does not enforce a real currency catalog, positive prices, arbitrary arithmetic range, or protection against every default construction path.

A pricing boundary can add price-sign and vocabulary rules. A calculation boundary owns rounding. An API boundary may translate invalid input into a documented response. Overflow needs its own analysis because valid individual amounts can still produce an out-of-range product or sum. Using decimal is helpful for decimal quantities but is not a complete billing correctness argument.

## MF-06A: exact boundaries and set validity

The old price excludes its ValidTo date, and the new price includes its ValidFrom date. Adjacent intervals therefore choose only the new version at the shared boundary. A gap produces price.not_found, and an overlap produces price.overlap for dates within the conflicting region. A version for another product does not contribute to matches for the requested product.

The constructor validates one bounded interval's end relative to its start. It does not inspect neighboring versions. Selection checks one product and one date. A catalog can succeed for June thirtieth and still overlap on July first. Global coverage and nonoverlap require a separate catalog-wide policy and validation strategy.

## MF-06B: precedence changes ambiguity handling

Two open-ended versions beginning in January and February both apply in March. Current selection rejects that ambiguity. Ordering by start descending and choosing the first would instead select February. That is a behavioral change, not a performance optimization.

A product might deliberately permit later-start overrides, but it must define tie handling, historical reproducibility, and migration behavior. Tests should distinguish overlapping versions with different prices and equal start dates. A precedence rule that remains ambiguous under equal starts has not finished specifying the contract.

## MF-06C: date policy needs retained evidence

A defensible proposal chooses a billing timezone and derives the pricing date from the usage occurrence instant under that timezone. It records the selected price version so later catalog changes do not silently alter historical explanation. A late event can still use its occurrence date if that is the contract, while invoice-period closure may require an explicit adjustment policy.

Current UsageEvent supplies a UTC-normalized instant, and PriceSelector accepts a DateOnly. The orchestration connecting them with a tenant timezone and durable historical version record is not established by those two types. The answer should name that missing work rather than assuming the conversion is automatic.

## MF-07A: normal allocation

With a one-hundred-unit first tier at one dollar and an open-ended second tier at fifty cents, zero yields no rated tiers and zero total; fifty yields fifty dollars; one hundred yields one hundred dollars; one hundred fifty yields one hundred plus twenty-five. The final case allocates fifty units to the second tier, not one hundred fifty again.

Rating zero can be meaningful for a calculator even though ingestion rejects an event with zero quantity. Validation belongs to the operation's purpose. The sum of emitted line charges should equal Total under normal nonoverflowing execution, but that assertion is separate from checking that all requested units were allocated.

## MF-07B: identify missing schedule invariants

A single finite tier up to one hundred with quantity one hundred fifty rates only one hundred units and still reports requested Quantity one hundred fifty. A negative threshold can move previousLimit below zero and cause a later positive tier to overallocate units. Duplicate thresholds can create zero-width entries, and multiple open-ended tiers can leave later entries unused.

These are predictions of the inspected loop, not claims that a schedule validator already rejects them. A proposed conservation rule compares the sum of RatedTier.Units with requested quantity for accepted complete schedules. A shape validator should reject or explicitly define malformed boundaries before rating begins.

The best fixture is small enough to calculate manually. A negative threshold of minus five followed by an open-ended tier for quantity ten makes the extra-five-unit problem visible. The exact charge then follows the selected unit price and rounding rule. A large random fixture would obscure why the result is wrong under the proposed policy.

## MF-07C: define the proposed schedule language

One possible validator requires positive strictly increasing finite thresholds followed by exactly one open-ended tier, one currency, and nonnegative unit prices. Another permits a finite final threshold but rejects requests above it. Either can be coherent; they describe different accepted input languages.

State stable error categories for shape, coverage, currency, and price policy. Preserve the existing rounding contract unless changing it deliberately. A migration plan should identify callers currently sending duplicate or incomplete schedules and provide a clear transition rather than silently normalizing away their mistakes.

## MF-08A: lifecycle is not a clock-driven scheduler

Trialing can activate; Active can pause; Paused can resume; every noncancelled state can cancel. Repeating cancel is rejected. ChangeProduct is allowed except when Cancelled and rejects an empty product Id. Every successful method increments Version, including a same-product amendment. Equal start and trial-end dates are accepted by the constructor's strict less-than guard.

The presence of trial dates does not mean an automatic scheduler activates the subscription. The methods shown require explicit calls. Product existence and authorization are also outside this entity's checks. A lifecycle table should not add transitions merely because they sound commercially reasonable.

## MF-08B: the gate has an ownership assumption

Two writers using MutateAsync with expected version one are serialized around comparison and callback. After the first increments Version, the second sees a conflict. A caller holding a live GetAsync result can invoke a domain method outside that gate, so the repository does not own every possible mutation of the object.

A proposed snapshot read plus restricted command operation can reduce that escape path. An isolated candidate followed by atomic publication can also make mutation authority clearer. These designs have costs and must preserve identity and version semantics. The lesson is not clone everything; it is define who may change authoritative state.

## MF-08C: mutual exclusion is not rollback

A callback that calls ChangeProduct and then throws leaves the product and Version changed in the stored reference. The lock prevents overlapping guarded callbacks while held, but it does not reverse completed object mutations. The ordinary amendment service's single guarded domain call is simpler than an arbitrary compound callback.

Validation before effects can prevent predictable partial failures. Candidate-copy publication can keep failed changes private. A durable conditional update can establish a database version boundary. None should be claimed merely because the repository uses a lock. Cancellation is checked before acquiring the gate, so cancellation while waiting is another separate contract question.

## Review rubric

Strong answers preserve the distinction between current behavior and proposed invariants. They use boundary fixtures, explain ownership, and identify the execution layer required for stronger claims. They do not call every accepted input valid business data, every version check a transaction, or every calculated total complete billing evidence.
