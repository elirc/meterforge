# 44. Integrated design assessment and review defense

This assessment asks you to defend a coherent billing design using the source and experiments developed throughout the course. It is not a request to implement every proposed production feature. The deliverable is a bounded review packet that connects current behavior, a chosen extension, executable evidence, and remaining integration work. Use the [main course](README.md) and the separate review guides after making your own predictions.

The central scenario is a synthetic tenant with one subscription, versioned prices, metered usage, a period-end invoice, a payment attempt, and a later correction. The scenario deliberately includes timing and identity ambiguity. A passing submission should preserve that ambiguity until evidence resolves it rather than producing a polished but unsupported success story.

## Scenario one: a trial becomes paid service

The subscription has a trial end date and remains Trialing in current object state. A scheduler proposes activation while an operator proposes cancellation. Both started from the same observed version. The scheduler also wants to create an initial billing obligation if activation is accepted.

First explain what the current Subscription methods permit and what they do not check. Then define the proposed temporal eligibility policy and the accepted ordering between cancellation and activation. Show the state and Version after each possible winner. Explain how the downstream billing obligation is preserved if the process stops after state acceptance.

The assessment does not accept "use a transaction" as a complete answer. Name the records in that transaction and the effects outside it. If the prototype uses only a lock and dictionary, label the evidence as process-local and specify the durable acceptance experiment still required. A clear limitation is preferable to a false distributed guarantee.

## Scenario two: late usage after invoice acceptance

The billing operation captured an accepted usage watermark and finalized a document. A later event has an occurrence date inside that billing period. The producer then retries that event with the same external key but a changed quantity after discovering a measurement error.

Explain the current ingestion response to the changed replay and why it protects accepted meaning. Propose a correction record rather than mutating the original event silently. Decide whether the late event and its correction affect a new adjustment document or another declared billing operation. Preserve the original invoice's evidence.

Your packet should include the accepted selection identity, meter normalization policy, price selection date, and correction cause. If any required historical field is absent from the current model, state the proposed addition. Do not invent a hidden event log simply to make the trace easy.

## Scenario three: a payment result is unknown

An accepted positive invoice triggers collection. The provider accepts the request, but the response is lost. The client retries its HTTP request. Meanwhile, support receives a complaint that the customer may have been charged twice.

Separate the current simulated provider behavior from the proposed real-provider protocol. The simulator's deterministic reference is not a charge ledger. For the proposed protocol, show stable collection identity, local unknown state, provider correspondence, and the query or replay that resolves it. Explain what the client can safely do with the current endpoint and what stronger operation resource would be needed.

The support packet must distinguish two transport attempts for one accepted effect from two accepted operations for one intended cause. Matching amounts alone is insufficient. Include the evidence source for each conclusion and avoid recommending another charge before the unknown outcome is reconciled.

## Scenario four: a credit and a refund diverge

The customer receives a service credit after the invoice was fully collected. A refund is requested, then its provider response is lost. Another operator opens the same case and proposes the same refund. At the same time, a reporting job calculates the customer's account balance.

Use the two-balance model to show corrected receivable, confirmed net cash, reserved refund capacity, and unresolved obligation. Explain why the second operator should receive the existing operation or a conflict rather than creating another refund. Show how the report avoids presenting pending money movement as completed.

If a later reversal is needed, preserve the original correction and refund histories. Do not make a completed external effect disappear by editing a local record. State the new operation that would be required and the authority or review boundary around it in a proposed production workflow.

## Scenario five: a deployment changes adapters

The team wants to replace in-memory usage and subscription stores with durable adapters while keeping public contracts stable. Existing tests pass against the old implementations. The readiness endpoint reports the new mode, but no cross-process test has run yet.

Explain which passing tests remain useful and which claims are still unproven. Include accepted winner identity, tenant normalization, subscription version conflict, callback exception semantics, and restart readback. A durable adapter may return reconstructed objects rather than shared references; identify any caller assumption that must change.

The migration plan should state what available in-memory data can be exported, what absent history cannot be recovered, and how new accepted operations are handled during rollback. A configuration flag is not a data migration. A ready response is not a proof of transactional semantics.

## Assemble the review packet

Begin with a one-page contract statement for the chosen extension. It should name stable identities, authority boundaries, accepted states, and non-goals. Follow it with state diagrams or ordered traces for the scenarios, a source map distinguishing current and proposed behavior, and an evidence table listing executed versus planned tests.

Include at least one intentionally failing compatible starter or a controlled counterexample. Explain why the failure is discriminating and how the proposed repair changes the accepted invariant. Do not merely show a red test caused by a compilation error or missing dependency. The existing disposable labs can supply a pattern, while the proposed protocol can have its own model test.

The test portfolio should contain local rule tests, deterministic interleavings, a serialization or restart model, and a provider-backed integration plan. You do not need to run a real financial provider or deploy infrastructure for this assessment. The limitation must be explicit, and the next experiment should be small enough to review.

## Defend tradeoffs with consequences

A reviewer may ask why the design uses a ledger instead of a mutable total, why it separates invoice and collection state, or why it retains unresolved capacity after a timeout. Answer with a concrete failure schedule. A design choice is more persuasive when the reviewer can see what would go wrong under the alternative.

Another reviewer may challenge complexity. Identify which mechanisms are necessary for the stated scope and which can be deferred. A demonstration mode can remain intentionally in memory if it advertises that boundary. A production acceptance workflow needs more evidence. Avoid prescribing every possible mechanism for every deployment without a requirement.

Cost matters too. Retaining receipts consumes storage; canonicalization and verification consume computation; recovery queues create operational work. Explain those costs and why they protect a declared invariant. A tradeoff analysis that mentions only benefits is incomplete.

## Exercise MF-44A: submit the integrated traces

Work all five scenarios with stable identities and explicit observation points. Mark each statement as current source behavior, proposed policy, executed evidence, or remaining hypothesis. Include one case where a correct response is to preserve uncertainty rather than force a success or failure label.

## Exercise MF-44B: defend one design decision

Choose a ledger, receipt, claim, watermark, or canonical payload decision. Present the smallest counterexample that requires it under your scope, its cost, and an alternative with a narrower guarantee. Explain why the chosen scope justifies the mechanism.

## Exercise MF-44C: produce a next-experiment plan

Identify the most consequential unproven claim in your packet. Specify a bounded experiment with setup, controlled schedule, observations, acceptance criteria, and cleanup scope. Do not run against existing customer data or real payment accounts. State what a passing result would still leave unproven.

## Assessment standard

A passing submission correctly explains current source behavior and proposes coherent extensions. A strong submission preserves meaning through retries, concurrency, and process loss while reporting evidence limits honestly. An excellent submission also makes the design economical: each mechanism protects a named invariant, each test has a discriminating oracle, and each unresolved question has a concrete next experiment.
