# 30. Amendment intent, resource state, and command replay

Use [SubscriptionAmendmentService](../../src/MeterForge.Application/Subscriptions/SubscriptionService.cs), [SubscriptionEndpoints](../../src/MeterForge.Api/Subscriptions/SubscriptionEndpoints.cs), and [Subscription](../../src/MeterForge.Domain/Subscriptions/Subscription.cs). Earlier chapters explained the expected-version winner. This chapter distinguishes a resource precondition from the identity of a user's intended command. A version check can prevent stale writes while still leaving a caller uncertain after a lost response.

The endpoint reads If-Match, trims quote characters, and tries to parse an integer. The service passes the expected version into repository mutation. The domain ChangeProduct rejects Cancelled and an empty product identifier, then assigns ProductId and increments Version. These mechanisms are concrete, but none of these method signatures includes a separate amendment operation key.

## A successful state change can have a lost response

Suppose a client reads Version one, sends a product change with expected one, and the server accepts it as Version two. The response is lost. The client retries the same request with expected one and receives a version conflict under the repository policy. The conflict does not prove that the first command failed; it may be evidence that it succeeded.

Reloading the resource can show the desired ProductId, but that alone may not prove this client's command caused it. Another actor might have selected the same product. For a simple user interface, observing the desired state may be sufficient to reassure the user. For audit, billing corrections, or side effects, a stable command receipt can provide stronger correspondence.

A proposed amendment operation key binds tenant, subscription, expected version, target product, and any effective-date policy. Exact replay returns the accepted amendment result. A conflicting reuse returns a conflict. This command receipt complements the version precondition; it does not replace stale-write protection for different commands.

## Same-product changes are still mutations

ChangeProduct does not first compare the requested product with the current product. A request selecting the same nonempty ProductId on a noncancelled subscription increments Version. That can be a legitimate record of an accepted command, or it can create surprising no-op versions depending on the product's interpretation.

Work Version five on product P. ChangeProduct(P) yields Version six under the current method. A second caller holding expected five is now stale even though the visible product did not change. If the product wants idempotent desired-state updates, it may choose to treat this as a no-op. If it wants command auditing, it may retain the mutation. The proposal must say which semantics are intended.

Do not silently optimize away the increment without considering callers. Version is part of the response contract and conflict protocol. A client or audit process may rely on accepted commands advancing it. Tests should make the no-op policy explicit rather than using only different product identifiers in every example.

## Current state does not describe an amendment history

The object stores one ProductId and Version. It does not retain old product, effective date, reason, actor, or accepted command identity in this class. A proposed proration workflow therefore needs an amendment ledger, as chapter 20 explained. The ledger records intent and accepted ordering while current state remains a projection or aggregate view.

Consider two changes, P to Q and Q to R, during one billing period. Looking only at current R and Version three cannot reconstruct when Q applied. A later correction needs historical boundaries. An audit log containing only "version changed" is also insufficient unless it preserves the relevant values and cause identity.

The ledger should distinguish requested effective time from accepted processing time. A backdated request can be accepted later under a correction policy. A command's arrival order is not automatically the same as its business effective order. Store both when the product needs them and define how conflicts are resolved.

## Header syntax is part of the public contract

The endpoint's parsing accepts an integer after trimming quote characters. That is a specific implementation, not a complete generic HTTP entity-tag parser. A course test should exercise the accepted syntax, missing input, noninteger input, and quoted integer case. Do not claim support for arbitrary weak tags, lists, or wildcard semantics without inspecting or testing them.

The returned SubscriptionResponse contains Version. The inspected endpoint does not explicitly add a corresponding ETag response header in its body. A client can use the response value under this API's convention, but documentation should not imply a full standards-based conditional-request exchange solely from the If-Match name.

A proposed API can either formalize its integer-version convention or implement a fuller entity-tag contract. Both require clear examples and hosted tests. Avoid a half-documented hybrid where clients assume standard semantics that the parser does not implement.

## Separate amendment side effects

Changing product may eventually trigger entitlement changes, proration, notifications, or provider updates. The current mutation does not perform those effects. A proposed coordinator should record obligations under stable amendment identity and dispatch them with replay-safe handlers. A version increment alone does not prove that every consequence completed.

Work a failure after the subscription mutation commits but before a proration intent is saved. A robust design should either store the obligation in the same acceptance transaction or have a reliable reconciliation mechanism that discovers the missing obligation. Calling the side effect directly after mutation without durable evidence leaves a crash window.

The amendment receipt can expose accepted state and pending consequences separately. A client can learn that the product change was accepted while billing adjustment is still processing. Returning one generic failure after the state already changed can encourage unsafe retries unless the receipt makes the outcome discoverable.

## Exercise MF-30A: analyze a lost response

Trace an accepted product change followed by a retry with the old expected version. Explain why the conflict is compatible with earlier success. Propose a command receipt that resolves the ambiguity without weakening version checks for different commands.

## Exercise MF-30B: choose no-op semantics

Call ChangeProduct with the current product and predict Version. Choose a desired-state or command-recording policy for a proposed API. Define the response, version behavior, and audit evidence for repeated same-product requests.

## Exercise MF-30C: specify amendment consequences

Design an amendment ledger with effective date, accepted order, cause identity, and downstream obligations. Work a crash between accepted state and proration dispatch. State the shared transaction or reconciliation evidence required to avoid losing the consequence.

## Review checkpoint

A resource version answers whether a write is based on current state. A command identity answers whether this intended operation was already accepted. Historical amendment evidence answers what happened over time. Strong designs use each for its own purpose rather than expecting one integer to solve all three problems.
