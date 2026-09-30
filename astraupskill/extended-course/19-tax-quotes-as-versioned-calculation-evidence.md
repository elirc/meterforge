# 19. Tax quotes as versioned calculation evidence

Read [TaxBoundary](../../src/MeterForge.Application/Tax/TaxBoundary.cs). The inspected file defines TaxQuote and ITaxQuoteProvider. A quote includes provider reference, Money tax, jurisdiction, and calculation version. The provider method receives tenant, country code, taxable amount, an as-of date, and cancellation. This is a boundary contract, not an implemented tax engine or a statement of legal tax rules.

This chapter is an engineering design exercise about preserving calculation evidence. It does not advise on tax compliance, rates, or jurisdictional obligations. All examples use synthetic rates and invented jurisdictions for arithmetic. The application source provides no rate table here, so do not infer one or fill the gap with unsupported claims.

## Identify what the contract already names

The taxable amount is distinct from the returned tax. The as-of date makes time an explicit input. Jurisdiction and calculation version provide explanatory metadata, while provider reference can connect the result to an external quote. These fields are valuable because a monetary result alone cannot explain why a calculation changed later.

The record declaration does not contain explicit constructor validation. A TaxQuote can therefore represent a blank reference, a tax currency different from the taxable amount, or a calculation version whose meaning is unknown to the caller unless another boundary checks it. The interface's existence does not enforce semantic compatibility between request and response.

A proposed application service should decide which response facts it must validate before accepting the quote. At minimum, currency agreement, required evidence fields, and correspondence to the requested operation are natural questions. Whether negative tax is allowed depends on the operation's role, such as a correction, and should not be guessed from Money alone.

## Work a synthetic quote trace

Take a taxable base of one hundred units in a synthetic currency context and a teaching rate of seven percent. The arithmetic tax is seven. Record the base, rate assumption, as-of date, jurisdiction label, calculation version, and provider reference. The result is a worked example, not a claim about a real tax jurisdiction.

Now repeat the request after the provider changes its calculation version. Even if the returned amount remains seven, the evidence differs. A historical invoice should retain the accepted quote identity and calculation version rather than silently replacing them with the latest result. Equal amounts do not make two calculations the same accepted evidence.

Change the taxable base through a discount. The workflow must decide whether the discount reduces the taxable base and how line allocation works under its chosen external policy. This repository boundary does not answer that legal or product question. The engineering task is to preserve the chosen inputs and avoid recalculating with different assumptions without an explicit correction operation.

## Date and instant are different inputs

The provider receives DateOnly asOf, while invoices record a finalization instant. A date is not an instant in a timezone. Converting between them requires a business calendar rule. If finalization occurs near midnight UTC, the business date may differ in another timezone. The application must choose the intended date rather than calling an arbitrary local clock conversion.

A deterministic test supplies the date directly and checks that it reaches the provider unchanged. A separate policy test can verify how a billing workflow selects that date. Keeping those tests separate makes failures intelligible: one tests parameter forwarding, the other tests calendar interpretation.

Do not use current wall time in every test. A fixed date near a month or year boundary is more revealing, and a supplied clock or date policy makes the behavior reproducible. The interface already accepts an explicit date, so the teaching model can exploit that design rather than hiding time inside the fake provider.

## Quote acceptance and invoice finalization

A quote can be obtained before the invoice is finalized, but the workflow must bind it to the accepted taxable base. If lines change after quoting, the old quote may no longer apply. A proposed draft version or canonical base hash can identify the exact calculation input. Finalization then checks that the accepted quote corresponds to the final draft evidence.

Work two actors: one requests a quote for draft version three, while another adds a line and creates version four. The first quote returns. Accepting it blindly would attach version-three evidence to version four. A conditional acceptance rule can reject or re-quote when the draft version changed. This is a proposed concurrency protocol; the current Invoice class does not provide a draft version field for it.

The external call should not require a long database transaction to remain open while waiting. A staged workflow can capture input identity, request the quote, then conditionally accept it. This creates a recovery problem if the process exits after the provider calculated but before local acceptance. Stable operation identity and stored quote evidence help make a retry explainable.

## Failure classification

Distinguish explicit unsupported jurisdiction, transient provider unavailability, malformed response, canceled request, and unknown outcome. A generic exception does not tell the billing workflow whether it can finalize without tax evidence. That fallback policy is a product requirement and should be explicit, especially when the amount affects a customer-facing document.

A teaching fake can return a valid quote, a mismatched currency, a missing version, or a delayed result. It should record the request inputs. A test can assert that a malformed quote is not accepted into the proposed finalized snapshot. This demonstrates boundary validation without making a live provider call.

Caching is another policy. A cache key based only on country and amount can omit tenant, date, currency, or calculation version. Whether those dimensions matter depends on the provider contract, but the interface already names several of them. A proposed cache must justify every omitted input rather than assuming tax is a timeless percentage function.

## Exercise MF-19A: validate quote correspondence

Define the request and response evidence required to accept a quote. Include currency, date, provider reference, jurisdiction, calculation version, and draft identity. Mark which fields are present in the current boundary and which are proposed additions.

## Exercise MF-19B: resolve a stale quote

Draw the draft-version-three quote request and version-four line change. Specify the conditional acceptance rule, the caller response, and the retry identity. Explain why equal tax amounts do not establish that the quote belongs to the later draft.

## Exercise MF-19C: design a tax-boundary fake

Create four synthetic modes: valid evidence, wrong currency, missing calculation version, and delayed response after draft change. State the expected proposed workflow outcome for each. Keep all arithmetic fictional and avoid presenting the exercise as tax advice or an implemented provider.

## Review checkpoint

The interface is a useful seam for isolating an external calculation, but accepting its result requires evidence and policy. A strong submission preserves the inputs, version, and correspondence that make a historical amount explainable. It does not substitute a current recalculation for the original accepted quote.
