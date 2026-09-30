# Review 06. Simulation, integration, tax, and catalog

This review covers chapters 17 through 21. The common skill is distinguishing a local representation from the external or shared protocol it might participate in. A simulator, outbox record, tax interface, amendment method, or dictionary can each be useful without implementing every guarantee associated with its name.

## MF-17A: audit simulator claims

The simulator observes an already-canceled token before returning. Otherwise it returns success with a reference derived from InvoiceId. It does not store accepted idempotency keys, compare repeated parameters, model real monetary effects, or persist anything across restart. A precise test name is AlreadyCanceledTokenPreventsSimulatedSuccess. A misleading name is ProviderGuaranteesExactlyOnceCharging.

Repeated calls for one invoice can return the same reference even with different keys or amounts. That is deterministic formatting, not evidence of deduplication. The rubric should require this distinction because a stable-looking reference can easily create false confidence. A good test report states the implementation used and avoids borrowing guarantees from a hypothetical real provider.

## MF-17B: build a replay-aware fake

The proposed fake ledger maps a declared key scope to accepted invoice identity, amount, currency, and reference. First acceptance stores one effect. Identical replay returns the stored reference. Conflicting replay reports a conflict without replacing the accepted tuple. These are assumptions of the educational model until a real provider contract is verified.

For effect-before-exception, record acceptance and then throw. The retry with the same parameters discovers the stored effect and returns it. This distinguishes lost acknowledgment from rejection before effect. If tenant scope is required, include it in a qualified key or provider-account context; the current request record does not carry tenant as a separate field.

Full credit requires making the fake's policy visible in its constructor or documentation. A test double that silently deduplicates while the application never supplies a stable key can hide the very defect the exercise should expose.

## MF-17C: design a provider contract suite

Include explicit success, explicit rejection, identical replay, conflicting replay, effect-before-exception, malformed successful response, and gated cancellation if the chosen scope permits seven cases. Local fakes can exercise application responses to all of these modes. A sandbox is required to establish the actual provider's key scope, retention, and remote response behavior.

The cancellation test should pause after recording the request so the observation point is known. A pre-canceled token test alone cannot model an in-flight external effect. A malformed-success test should show that missing provider evidence does not become an unquestioned local success. The report separates local contract assumptions from sandbox observations and does not claim that either covers all production topology.

## MF-18A: classify current guarantees

OutboxMessage checks required envelope fields and positive schema version, then normalizes time. WebhookDelivery checks identities, tenant, and absolute URI; Retry preserves identities while incrementing Attempt from Failed. Fail and Succeed assign statuses without a full guarded transition graph. The retry-preview endpoint creates and mutates a local object without sending a request.

None of those facts establishes transactional outbox persistence, dispatch claims, receiver idempotency, destination ownership, or a network scheme policy. Absolute URI is a syntax property. Positive schema version is not payload validation. Full credit requires identifying these boundaries without dismissing the local guards as useless.

## MF-18B: draw the acknowledgment gap

The proposed business transaction accepts the invoice and outbox row together. A dispatcher claims the message and sends it. The receiver durably accepts the message identity and its business effect, then responds. If the sender loses the response, it retries the same message identity. The receiver returns the existing outcome without repeating the effect under its inbox protocol.

The sender's delivery identity can distinguish destinations, while message identity represents the business event. Attempt number changes transport history but must not create a new event. A strong answer also handles sender process loss and receiver failure before durable acceptance. Exactly-once receiver effects require receiver cooperation; they cannot be guaranteed by a sender's status enum alone.

## MF-18C: design schema and ordering policy

An aggregate sequence or version can let the receiver recognize stale and future events under a declared policy. Duplicate message identity returns the accepted outcome. Unsupported schema is classified separately from a transient network failure. Adding an optional descriptive field can be compatible for tolerant readers, while changing monetary units without a new contract is incompatible.

A dead-letter packet should contain message and delivery identities, event type, schema version, safe error category, attempts, and relevant ordering evidence. Avoid copying secret payloads into a general report. The reviewer needs enough information to identify the failed contract and plan a safe replay, not unrestricted access to every event body.

## MF-19A: validate quote correspondence

The current boundary provides tenant, country, taxable Money, date, and cancellation as request inputs, and provider reference, tax Money, jurisdiction, and calculation version as output. A proposed workflow additionally binds the quote to a draft or calculation-input identity. It validates required evidence and currency agreement before accepting the result.

The interface does not itself prove the response corresponds to the request. A provider adapter or application service must preserve that relationship. Full credit distinguishes fields currently present from proposed additions and avoids inventing real tax rules. Synthetic examples demonstrate engineering behavior only.

## MF-19B: resolve a stale quote

The quote request captures draft version three. A concurrent line change creates version four. When the quote returns, conditional acceptance compares the captured identity with the current draft. A mismatch leads to re-quotation or a conflict response under the chosen policy; it does not attach stale evidence merely because the tax amount happens to be unchanged.

The retry identity should distinguish a retry of the same quote operation from a new quote for version four. Preserve both histories if needed for diagnosis. A long-running external call should not require holding a database transaction open indefinitely. The proposal can capture inputs, call externally, and finalize conditionally in a short transaction.

## MF-19C: design a tax-boundary fake

Valid evidence is accepted only when it corresponds to the expected draft and currency. Wrong currency and missing calculation version are rejected by the proposed boundary policy. A delayed result after a draft change is classified as stale and not silently used. The fake records the input date and taxable amount so the test can verify forwarding.

The model does not certify tax compliance or a real provider's behavior. Its value is exposing malformed and stale evidence before invoice acceptance. The rubric should require explicit fictional rates and a limitations statement, not a confident claim that one synthetic percentage covers real taxation.

## MF-20A: prove calendar coverage

Under the chapter's half-open policy, a change at start gives the new product the full period; a change at end contributes no new-product days to that period. A day-ten change partitions the interval without overlap. A thirty-one-day period uses thirty-one as its denominator rather than a fixed thirty. Same-date changes follow the declared consolidation or ordering rule.

Assert that adjacent segment boundaries meet, every segment has nonnegative duration, and the union equals the intended billing interval. These structural assertions should run before monetary calculation. A current ProductId cannot reconstruct the sequence, so the proposed amendment ledger must supply accepted historical changes.

## MF-20B: allocate a rounding residual

Per-segment rounding and rounded-document allocation can differ when several raw charges contain fractions of a cent. Choose one policy and retain its version. For document allocation, derive the target total, allocate initial amounts, then distribute the residual using a stable rule such as fractional remainder followed by segment order.

The allocation must apply only to eligible segment charges. Moving a residual into an unrelated tax line can change meaning. Preserve raw inputs, segment boundaries, price identities, and accepted rounded amounts when reproducibility requires them. Full credit requires a concrete example and deterministic tie behavior rather than an instruction to "round correctly."

## MF-20C: handle a late amendment

A backdated change after finalization should follow a declared correction policy. One coherent choice issues a linked adjustment document in a later accepted operation while retaining the original invoice unchanged. The correction operation has stable identity and includes the historical segment evidence it changes.

The current subscription pointer lacks the full amendment timeline required for this calculation. A proposed ledger must preserve effective date, accepted ordering, old and new product or price identities, and cause identity. Do not rewrite the original invoice's lines to conceal the correction or infer missing history from today's product alone.

## MF-21A: trace canonical keys

Product trims TenantId during construction, so the repository stores the normalized tenant string in its tuple key. GetAsync uses the supplied lookup string directly. A raw whitespace-bearing lookup can miss while the normalized lookup succeeds. This is a source-level path inconsistency for that input, not evidence that every real route supplies such a value.

A coherent proposal can reject noncanonical tenant identifiers at the boundary or canonicalize them once into a trusted internal value. Apply the same rule to creation, lookup, replay identity, and migration. Case changes require a separate decision; product-code uppercase normalization does not automatically define tenant identity semantics.

## MF-21B: distinguish code and identity

Two products with one normalized code and different identifiers occupy different dictionary keys. Two products with the same tenant and identifier use one key, and the later indexer assignment replaces the stored reference. The normal service's fresh Guid reduces accidental key reuse but does not change the repository method's semantics.

A unique-code proposal needs an atomic acceptance boundary, not a scan followed by an unguarded add. A duplicate-code policy instead requires consumers to use identifiers unambiguously. Full credit describes the chosen behavior for concurrent create requests and a conflict or replay response that preserves accepted identity.

## MF-21C: design a catalog creation receipt

Bind a stable operation key to canonical tenant, normalized code, name policy, price, and currency. Exact replay returns the accepted Product identifier. Conflicting replay returns a conflict without replacing the original. The receipt and product acceptance must be stored together under the chosen durable authority.

The in-memory repository can support a local teaching model but does not preserve receipts across process loss. A migration must handle normalization collisions rather than merging them silently. The review rewards a design that makes identity explicit before adding storage, because persistence alone can make inconsistent keys more durable without making them correct.
