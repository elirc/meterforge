# 14. Reconciliation, refunds, and missing correspondence

Read [PaymentReconciler, Refund, and PaymentDispute](../../src/MeterForge.Domain/Reconciliation/Reconciliation.cs), the [reconciliation endpoint](../../src/MeterForge.Api/Reconciliation/ReconciliationEndpoints.cs), and the [existing tests](../../tests/MeterForge.Domain.Tests/Reconciliation/ReconciliationTests.cs). This chapter distinguishes an amount comparison from a reconciliation process. Two equal numbers can belong to unrelated payments; two different numbers can represent a known fee policy. A complete process needs correspondence and interpretation around the arithmetic.

The source's Compare method first requires equal currencies. It computes provider amount minus internal amount as Money. A zero difference produces matched; any nonzero difference produces amount_mismatch. This is a precise, useful calculation. It does not fetch provider statements, match transaction identities, or decide whether a mismatch should be repaired automatically.

## The sign has a declared orientation

For internal one hundred and provider ninety-nine, Difference is negative one. The provider amount is lower than the internal amount under this orientation. Reverse the inputs and the sign reverses. A support report that merely says "difference one" discards information needed to interpret the discrepancy.

The existing test explicitly checks the negative result. Preserve that sign in exports and diagnostics. A UI can display a friendly explanation, but its underlying data should retain which side was subtracted from which. Absolute-value comparisons are appropriate only when the product intentionally measures magnitude and stores direction elsewhere.

Currency mismatch is not an amount mismatch with a conversion factor. The method rejects it before subtracting. That is appropriate for a calculator that has no exchange-rate evidence. A proposed foreign-currency reconciliation process needs rate source, effective time, rounding policy, and settlement currency. Do not invent those facts by passing two currencies into the current method and hoping the result explains them.

## Equality does not prove correspondence

Imagine two local payments of fifty each and two provider records of fifty each. Every pair matches by amount, but there are multiple possible correspondences. Matching by amount alone can associate the wrong provider reference with a local payment. The current Compare method intentionally receives only Money values, so another layer must establish that the pair belongs together.

A proposed reconciliation key can include provider account, provider transaction identifier, local operation identity, tenant, and currency. Not every integration supplies every field, so the process needs an ambiguity category. Unmatched and ambiguous should remain distinct from amount mismatch. A guessed match can be more harmful than a clearly unresolved record.

The endpoint accepts internal and provider amounts from one request and uses the same requested currency for both Money values. It therefore exercises an amount calculator, not an independent retrieval of authoritative ledgers. Its response can be useful for previewing a comparison, but it does not certify that the supplied provider amount came from a provider.

## Refund validation is local and per record

Refund requires nonempty identifiers, matching currency between the supplied original payment and refund amount, a positive amount no greater than the supplied original amount, tenant, and reason. It normalizes the created timestamp to UTC and trims the reason. These guards reject a single refund of 101 against an original 100, as the existing test demonstrates.

They do not prevent two separate refunds of seventy against the same payment. Each constructor sees an original amount of one hundred and no earlier refund ledger. The cumulative invariant belongs to a service that can observe and atomically update accepted refunds. A per-record upper bound is necessary but not sufficient for cumulative safety.

The constructor also trusts the supplied originalPayment Money as an input. It does not load the referenced payment identifier or prove that the payment succeeded. A proposed refund workflow must fetch authoritative payment evidence, authorize the tenant, inspect previous refunds and disputes, and define what amount remains refundable. Passing the expected number in a unit test does not establish those workflow steps.

## Refund intent and external effect

Creating a Refund record is not the same as sending a refund to a provider. A durable workflow needs to connect local intent, provider request identity, provider response, and accepted local outcome. The lost-acknowledgment problem from chapter 11 appears again: an external refund may happen before the application receives or saves confirmation.

Use a synthetic schedule in which local intent is saved, provider refund succeeds, and the process exits before recording success. Recovery should query or replay with the same provider identity according to the provider contract. Generating a new provider request key on every restart can turn one intended refund into multiple external effects.

Do not automatically reuse the charge's idempotency key for the refund. A charge and a refund are different operations, and provider key scopes vary. A proposed protocol should assign a stable refund-operation identity and preserve the original charge reference as correspondence evidence. The key must remain stable across retries of that refund while distinguishing separate legitimate refunds.

## Disputes are a separate record shape

PaymentDispute is a positional record in the inspected source. Unlike Refund, it does not declare the same explicit constructor guards in this file. Do not attribute Refund's validation to it simply because both appear nearby. A record declaration can carry data without enforcing every business invariant.

A dispute also differs semantically from a voluntary refund. It may have a provider case identity, evidence deadlines, and contested outcomes. Those lifecycle rules are proposed future work, not implemented by the record shape. A course exercise can design them while clearly marking the source's current boundary.

## Build a reconciliation worksheet

Use synthetic rows with an exact match, amount mismatch, currency mismatch, missing provider record, duplicate provider record, and ambiguous equal-amount candidates. The current Compare method can handle the amount calculation once a valid pair is chosen. A proposed coordinator classifies the other categories before invoking it.

The worksheet should be read-only initially. It reports evidence and possible next checks rather than issuing refunds or rewriting amounts. Automatic repair requires a separate authorization and policy review because the side effects differ by category. An amount mismatch caused by an expected fee is not repaired the same way as a duplicated charge.

## Exercise MF-14A: preserve sign and identity

Work internal100/provider 99 and internal99/provider 100, then construct two equal-amount payments with ambiguous correspondence. Explain which facts Compare establishes and which facts a reconciliation coordinator must establish first.

## Exercise MF-14B: enforce cumulative refund limits

Design a two-caller schedule for two seventy-unit refunds against a one-hundred-unit payment. Define the shared invariant, stable operation keys, and caller outcomes. Include a retry after a lost provider acknowledgment without assuming the refund did not happen.

## Exercise MF-14C: classify a statement import

Create six synthetic statement cases covering the worksheet categories. Return a read-only classification with evidence fields and next checks. Keep PaymentDispute's current unguarded record shape distinct from any proposed dispute workflow.

## Review checkpoint

Reconciliation begins by establishing correspondence, then compares values under a declared policy. Refund safety begins with local validation but extends to cumulative limits and external effect identity. The strongest answers leave ambiguity visible until authoritative evidence resolves it.
