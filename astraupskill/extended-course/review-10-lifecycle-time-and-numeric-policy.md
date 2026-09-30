# Review 10. Lifecycle, time, and numeric policy

This review covers chapters 29 through 33. These exercises distinguish values stored on an object from the policy that interprets them. A trial date does not run a scheduler, a version does not identify a command, an occurrence instant does not define a billing watermark, and a decimal type does not choose a supported business range. Grade the relationship between policy and mechanism, not merely the vocabulary used.

## MF-29A: separate permission and eligibility

An expired trial still constructs as Trialing when TrialEndsOn is non-null. A future-start subscription without a trial constructs as Active. Activate can run before trial end because it checks only Trialing state. The constructor and transition methods do not consult a current date. A correct trace states these observations even if the learner would choose a different product policy.

A proposed eligibility function receives business date, start, trial end, payment-readiness policy, and current accepted state. It decides whether to propose activation. Activate then enforces local state permission. Full credit requires keeping those responsibilities separate. Putting DateTime.Now inside every domain method would make tests harder and still would not define the tenant's business calendar or durable scheduling behavior.

The review should also distinguish unsupported policy from invalid object state. A Trialing object past its trial date can be a valid representation of delayed orchestration rather than a constructor defect. The operational question is how the scheduler discovers and resolves that state under the chosen policy.

## MF-29B: design a recoverable scheduler

Discovery reads eligible persisted subscriptions, but acceptance uses an expected version and stable lifecycle operation identity. Two schedulers reading Version one can both propose activation; only one should accept the transition under the shared authority. The loser reloads and classifies the outcome as already achieved, conflicting manual action, or still eligible under a new version.

If cancellation wins first, activation must not overwrite it from stale state. If activation wins first, a later valid cancellation can still be accepted according to policy. Record accepted order and downstream obligations. The activation receipt and billing intent should share an acceptance boundary or a reliable reconciliation mechanism so process loss cannot erase the consequence.

A timer event alone is not recovery evidence. After restart, a sweep should discover eligible unprocessed state or persisted work. A process-local test proves the model's ordering, while independent-process tests establish the durable adapter's conditional acceptance. Deduct credit if the answer assumes one scheduler instance will exist forever.

## MF-29C: test a calendar boundary

Choose a fixed instant and a declared business calendar that maps it to a date. Assert the pure eligibility result for that date, then separately assert the domain transition from the required state. A neighboring instant or alternate calendar can demonstrate why the conversion policy matters without changing the operating-system clock.

DateOnly has no timezone, so it cannot identify the conversion by itself. A current wall-clock call introduces environmental behavior unless the clock is supplied. If calendar configuration can change, preserve the relevant policy version for accepted lifecycle decisions. Full credit requires reproducibility and a clear statement of which layer selects the date.

The test need not simulate daylight-saving rules comprehensively unless the chosen product policy requires it. Start with the smallest boundary that distinguishes two plausible interpretations, then add targeted cases. More calendar examples do not compensate for an undefined policy.

## MF-30A: analyze a lost response

The first amendment can commit Version two while its response is lost. A retry with expected Version one then conflicts. That conflict is compatible with earlier success and must not be interpreted as proof that no mutation occurred. Reloading may show the desired product, but another actor could have produced the same state.

A proposed command receipt binds the stable amendment key to tenant, subscription, expected version, target product, and accepted result. Exact replay returns the receipt. A different command still uses a fresh expected-version check, so command replay does not weaken stale-write protection. The two mechanisms answer different questions.

The best answer includes a case where another actor changes the product again before readback. Current state alone then cannot explain the first command's outcome. Historical command evidence is what resolves that ambiguity. A client should preserve unknown outcome rather than guessing from the latest product pointer.

## MF-30B: choose no-op semantics

Current ChangeProduct with the existing nonempty product increments Version on a noncancelled subscription. A desired-state API may instead choose a no-op response without version advancement, while a command-recording API may retain an accepted command and version change. Either proposal must state its effect on concurrent clients and audit history.

Under no-op semantics, define whether an outdated expected version is still rejected before recognizing the same target. Otherwise the endpoint might accept a stale command that happens to match current state and conceal intervening history. Under command semantics, explain why the additional version represents an accepted action despite unchanged visible product.

Tests should include same-product requests, repeated command identity, and a different command based on the old version. Full credit requires a consistent relationship among response, version, and receipt. Merely skipping assignment because values are equal does not settle the public contract.

## MF-30C: specify amendment consequences

The ledger preserves cause identity, old and new product or price references, requested effective date, accepted processing order, actor evidence under a future authority model, and downstream obligations. Current ProductId and Version alone cannot reconstruct a billing timeline. Each obligation references the accepted amendment rather than a transient request trace.

If state commits before proration intent exists, a crash can lose the consequence. Store state and intent together where the persistence mechanism permits, or define a reconciliation query that reliably identifies missing obligations from durable amendment evidence. A direct network call after mutation without a receipt does not close the gap.

The review should require a replay policy for the downstream handler too. Durable intent can be delivered more than once. A proration operation must recognize the same amendment cause and preserve accepted document identity rather than issuing another adjustment on every delivery.

## MF-31A: validate a catalog revision

Group versions by product and inspect sorted boundaries within the declared coverage window. Adjacent half-open intervals meet without overlap. A gap is either rejected or explicitly allowed under publication policy. An open-ended interval followed by another applicable interval requires revision or produces overlap. Identical dates on different products are not a conflict by themselves.

Individual constructors validate each interval's identifiers and end-after-start relation. They do not establish collection completeness. PriceSelector remains a useful defensive query that reports zero or multiple matches. A publication validator should not remove those runtime checks merely because it expects clean input.

Full credit includes the intended coverage scope. A requirement to cover active billing periods differs from a requirement to cover every possible historical date. The answer should not invent prices for unknown history to satisfy an overbroad validator.

## MF-31B: resolve two publishers

Both administrators validate against base revision R. Acceptance conditionally replaces or advances R with one coherent new revision and receipt. The second publisher sees a stale base and must compare with the accepted revision. Identical proposed content can be classified as replay under stable operation identity; different content requires conflict or a new reviewed revision.

The persistence authority must bind the complete timeline, not insert individual versions independently while hoping the selector later catches overlap. A two-context integration test coordinates both validations before either acceptance and verifies one accepted revision. The pure interval validator cannot prove that concurrency property.

Historical PriceVersion identifiers retain their meaning. Reusing an identifier for a new amount would undermine invoices that reference it. A correction creates new evidence and an impact report rather than rewriting accepted price identity invisibly.

## MF-31C: report a historical correction

The report identifies accepted invoices referencing affected price versions or intervals under the declared selection-date policy. It shows original accepted amounts, proposed recalculation evidence, and the difference without editing documents. The product then decides whether to issue a credit, debit, or no correction under its policy.

Changing a catalog affects future selections; it does not automatically authorize changes to finalized invoices. Occurrence date, billing-period date, and acceptance date can select different prices, so the application must state which is used. The report should expose that assumption rather than burying it in a query.

Full credit includes an uncertainty category for missing historical evidence. If the original selection inputs are absent, the tool cannot certify a reconstructed amount merely because today's catalog returns one price. Preserve the limitation and define the next review step.

## MF-32A: classify event time

The current event records OccurredAt normalized to UTC. It does not record a separate accepted sequence or ingestion time in that type. A June occurrence accepted in July therefore requires additional acceptance evidence if billing needs to distinguish when it became known. Billing cutoff and correction time are separate policy events.

A timeline should show the producer occurrence, server acceptance, captured billing selection, invoice acceptance, and later correction. These can have different dates without contradiction. Do not use the random internal Guid as an ordering surrogate or assume the producer's clock is the server's acceptance clock.

The proposed accepted-sequence watermark belongs to a shared persistence contract. It must identify the scope in which order is meaningful and how a selection query sees all earlier accepted events. A sequence number without that relation is insufficient.

## MF-32B: freeze selection across replay

Operation B retains watermark 100 and its selection policy. Event 101 arriving later does not change B's exact replay output even if its occurrence date belongs to the same period. A separate correction operation can include 101 and reference the original invoice. Repeated ingestion of101 resolves to one accepted event and must not create repeated corrections.

The query must observe every accepted eligible event through the watermark under the chosen transaction or materialized batch protocol. If acceptance sequence assignment and event visibility are not coordinated, the watermark can lie about completeness. A provider-backed test should pause acceptance and selection at that boundary.

Full credit distinguishes output replay from recalculation. Re-running today's query and obtaining a different event set is a new proposal, not exact replay of the original billing operation. Preserve the accepted input identity or explicit selection evidence.

## MF-32C: design accepted-usage correction

A separate correction record references the original accepted event and declares delta, replacement, or cancellation semantics. The original UsageEvent remains unchanged. Because its constructor requires positive quantity, negative adjustment semantics need a new explicit representation rather than an undocumented special meter value.

The correction has stable cause identity, tenant and subscription scope, meter policy, and accepted amount or replacement evidence. Exact replay returns the accepted correction. A changed correction payload under the same identity conflicts. Billing references that accepted correction once, regardless of transport deliveries.

The answer should state how case-sensitive meter behavior is preserved or deliberately migrated. Aggregating case-insensitively after ingestion can merge meanings that the current service compares distinctly. Canonicalization must be consistent across acceptance, correction, and rating.

## MF-33A: distinguish precision and magnitude

A value such as0.005 is rejected by Money because rounding to two places changes it. A sufficiently large integral decimal can pass Money's precision check. Adding two accepted large values or multiplying a large amount by a percentage before division can overflow. Precision acceptance is not proof that every later operation fits.

Currency mismatch and precision rejection use BusinessRuleException, while decimal overflow follows arithmetic exception behavior unless another layer translates it. The API's custom handler does not map every exception type as a business rule. A proposed magnitude envelope can produce stable business errors before arithmetic reaches unsupported range.

Do not globally prohibit negative Money to solve one payment constraint. Reconciliation differences use signs. The supported sign and magnitude belong to the semantic operation, with tests that separate those dimensions.

## MF-33B: define a supported envelope

Choose finite synthetic maxima for quantity, unit amount, line count, and document total, then demonstrate that supported products and sums fit both the in-memory decimal range and the proposed storage precision. The actual business requirements should determine real limits; the course's numbers are test policy, not universal recommendations.

Reject one-above-limit inputs with a stable category before acceptance. Test a small extra-precision input separately from a large exact-cent input. A request can satisfy one rule while violating the other. Avoid saturation unless the product explicitly wants altered amounts and can explain them to callers.

The rubric requires an aggregate bound, not only per-line bounds. Many individually valid lines can exceed a document limit. It also requires identifying storage precision, because a database column can be narrower than decimal.

## MF-33C: review a proposed arithmetic rewrite

Multiply-then-divide and divide-then-multiply are algebraically equivalent over ideal numbers but can differ under finite precision, overflow, and rounding. Define the exact rounding stage and compare candidate implementations against a bounded high-precision reference calculation. Include midpoint and magnitude cases that isolate the differences.

Removing one overflow does not prove that all accepted values produce the same monetary result. A rewrite that changes a cent at a boundary changes billing evidence. The review should state whether the new policy intentionally changes results or must preserve them, and should version accepted calculations if their meaning changes.

An excellent answer keeps the reference oracle independent enough to detect the same mistake. Copying the candidate expression into the expected-value calculation produces a tautological test. Use hand-worked cases and a separately specified arithmetic policy to anchor the comparison.
