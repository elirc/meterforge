# 17. Simulators, test doubles, and provider contracts

Read [SimulatedPaymentProvider](../../src/MeterForge.Infrastructure/Payments/SimulatedPaymentProvider.cs) alongside [PaymentService](../../src/MeterForge.Application/Payments/PaymentService.cs). The simulator checks cancellation and returns success with a reference derived from the invoice identifier. Its simplicity is useful for a demonstration, but it must not be mistaken for a durable provider ledger or a comprehensive failure model.

This chapter teaches how to design test doubles that expose uncertainty rather than hiding it. The prerequisite is chapter 11's distinction between local attempts and external effects. A fake should make a specific scenario controllable; it should not silently add guarantees that the production integration has never established.

## Describe the simulator exactly

ChargeAsync first calls ThrowIfCancellationRequested. If the token is already canceled, it throws before constructing the successful result. Otherwise it returns a completed task with Succeeded true and a synthetic reference based on InvoiceId. There is no visible dictionary of accepted keys, stored charge amount, remote network call, or durable record.

Two requests for the same invoice produce the same reference even if they have different keys or amounts. That deterministic reference is not proof that only one monetary effect occurred, because the simulator does not model a monetary ledger. Conversely, different invoices produce different references without proving a provider's actual idempotency scope. The reference is a demonstration string, not a receipt from a financial institution.

Cancellation after the initial check may not interrupt additional work because there is little additional asynchronous work in this implementation. A test that cancels a real network request halfway through cannot be replaced by an already-canceled-token test against this simulator. Both are useful, but they exercise different observation points.

## Separate stub, spy, and stateful model roles

A fixed-result stub answers a simple question: how does PaymentService respond to a given ProviderChargeResult? A recording spy answers what parameters were sent and in what order. A stateful model can represent key acceptance, repeated requests, conflicting parameters, and lost acknowledgments. These roles may be combined in a small fixture, but their assumptions should remain visible.

For explicit success, a stub with a valid reference is enough to test the local Succeed transition. For key preservation on retry, record every request. For effect-before-exception, maintain a synthetic ledger and throw only after recording acceptance. If the test merely throws before doing anything, it cannot establish the ambiguous-outcome behavior that the exercise claims to cover.

Avoid making a fake deduplicate by every field just because that is convenient. Real providers often define a particular key scope and parameter-comparison rule. Until an actual integration contract is selected, label the fake's rule as a proposed contract. The application can then be tested against that contract without pretending it has been verified remotely.

## Work a stateful fake ledger

Define a synthetic key space and an accepted record containing invoice identity, amount, currency, and provider reference. On the first request, the model accepts the tuple. On an identical repeat, it returns the same reference without adding another effect. On a repeated key with different accepted parameters, it reports a conflict under the proposed policy.

Add a switch that throws after first acceptance. The first caller sees an exception, but the model ledger contains one effect. A retry with the same key and parameters returns the stored reference. This schedule teaches why a timeout cannot be treated as an empty ledger. It also shows the value of returning the accepted result rather than generating a new reference on every replay.

Then introduce two tenants using the same textual key. The current ProviderChargeRequest does not carry tenant as a separate field. The proposed model must decide whether the key is globally scoped, account-scoped, or already tenant-qualified by the caller. Do not silently choose a scope and later claim that the application enforces tenant isolation. Make the ambiguity a test-design question.

## Fault modes should have names

Use distinct modes for rejection before acceptance, transient failure before acceptance, acceptance followed by lost acknowledgment, malformed successful response, and delayed response canceled by the caller. Each has a different recovery implication. A single Boolean fail switch loses this information and can encourage one unsafe retry strategy for every case.

Malformed success is especially useful here because Succeed requires a nonblank provider reference. The service's null-forgiving operator does not change runtime data. A test should verify that an invalid result does not become a locally succeeded attempt without evidence. It should also preserve the possibility that the external effect happened despite the malformed acknowledgment.

Delayed responses need deterministic gates. Let the test wait until the request is recorded, then cancel or release the provider response. Avoid fixed sleeps, which can make the interesting interleaving depend on machine speed. The test should report the named phase it reached before cancellation, not merely that it eventually threw.

## Contract tests and integration tests

A reusable provider contract suite can run against the stateful model and later against an explicitly provisioned sandbox adapter. It should check key scope, identical replay, conflicting replay, reference stability, cancellation categories, and parameter preservation. Some properties may be untestable without provider-specific capabilities; record those gaps rather than fabricating a generic guarantee.

A sandbox test does not prove every production behavior. Account configuration, retention windows, network topology, and provider policies may differ. The course's immediate deliverable is a clear contract and a deterministic local harness, not a live payment integration. No real charge, refund, credential, or external account is needed for the lesson.

The existing simulator remains useful for simple API demonstrations. It is not necessary to make it complex to teach advanced failure handling. A separate educational model can expose the hard cases while leaving the application's behavior unchanged. This preserves the user's source and makes the model's assumptions easier to inspect.

## Exercise MF-17A: audit simulator claims

List what the current simulator proves about cancellation, references, and success. List three properties it does not model. Give a test name that accurately describes its scope and a misleading name that would overstate it.

## Exercise MF-17B: build a replay-aware fake

Specify the synthetic ledger and three outcomes: first acceptance, identical replay, and conflicting replay. Add effect-before-exception and show the retry result. State the assumed key scope and how tenant identity enters that scope.

## Exercise MF-17C: design a provider contract suite

Choose six cases with explicit evidence and expected limitations. Distinguish tests that can run with no external resources from tests requiring a provider sandbox. Include malformed success and a gated cancellation rather than only happy-path success.

## Review checkpoint

A useful test double makes one uncertainty controllable. A misleading double erases uncertainty and lends its invented guarantees to the application. Strong submissions describe the simulator's exact behavior, state the proposed model's assumptions, and keep remote claims reserved for remote evidence.
