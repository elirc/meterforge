# 28. Capstone: a correction ledger with two balances

This capstone designs a proposed correction workflow around [CreditNote](../../src/MeterForge.Domain/Adjustments/Adjustments.cs), [Refund](../../src/MeterForge.Domain/Reconciliation/Reconciliation.cs), and [PaymentAttempt](../../src/MeterForge.Domain/Payments/PaymentAttempt.cs). It addresses a problem different from accepting an invoice: a billing correction and a movement of money can occur at different times and for different reasons. The prototype must preserve both meanings instead of representing every correction as one negative number.

Work only in a disposable model. The application is unchanged, and no refund is sent. Use synthetic customers and amounts. The current domain records supply local validation examples; the cumulative ledger, authority rules, and external-refund protocol described here are proposed learner work.

## Separate receivable correction from cash return

Suppose an accepted invoice is one hundred and has been fully collected. A credit of twenty reduces the amount the business says was owed under the chosen correction policy. A refund of twenty returns money already collected. The two events are related, but one is not automatically evidence that the other happened.

Now suppose the invoice was never collected. The same twenty-unit credit can reduce the outstanding receivable without any cash refund. A workflow that blindly sends a refund for every credit would attempt to return money that may not have been received. Conversely, a refund for an overpayment can occur without changing the underlying invoice's billed amount.

Define two teaching balances: corrected receivable and net collected cash. Corrected receivable begins from the accepted invoice and changes through accepted credit or debit corrections under policy. Net collected cash begins from confirmed collections and changes through confirmed refunds. Their difference helps explain what remains due or owed, but it should not erase the event histories that produced it.

## Work three distinct cases

Case one is a fully paid invoice of one hundred with a twenty-unit service credit. The correction ledger accepts the credit. A linked refund intent may then request twenty. Before the provider confirms the refund, corrected receivable is eighty while net collected cash remains one hundred. The model should show a pending return obligation rather than pretending cash already moved.

Case two is an unpaid invoice of one hundred with the same credit. Corrected receivable becomes eighty and net collected cash stays zero. No refund intent is necessary under this teaching policy. A user-facing explanation can say that the amount due was reduced, not that money was returned.

Case three is an invoice of one hundred with confirmed collections totaling one hundred ten because of an independent overpayment. A ten-unit refund can restore net collected cash to one hundred without creating a credit against the invoice. The reason and correspondence differ from a service credit. A single field called adjustment would hide that distinction.

## Give each event a stable cause

A correction operation identifies the business decision, tenant, invoice, magnitude, currency, and reason category. A cash-refund operation identifies the provider-facing return and the collection it reverses. A link can connect them when the refund fulfills a credit-related obligation. The identities remain distinct because one correction may be fulfilled through several partial refunds or a future account balance.

Exact replay returns the accepted event. Conflicting replay under the same cause identity rejects different amounts or references. Free-form reason text can be retained as explanation, but it should not be the only deduplication key. A spelling correction should not accidentally create another financial event, and two legitimate decisions may share a standard reason phrase.

The current constructors generate no shared cumulative view. A proposed application service must load authoritative invoice and payment evidence, authorize the operation, and enforce the relevant remaining balance under concurrency. The local positive-amount and currency guards remain useful, but they do not replace that shared decision.

## Define cumulative limits with policy exceptions

For the first model, forbid total accepted credits above the eligible invoice amount and forbid confirmed plus reserved refunds above the eligible confirmed collection amount. Reservations matter because two external refund requests can be in flight simultaneously. Checking only confirmed refunds leaves room for both to pass before either returns.

Use a conditional acceptance transaction that records a refund reservation and its operation identity together. A later provider confirmation consumes the reservation into a confirmed refund. An explicit rejection releases it under the chosen policy. An unknown outcome keeps a reconciliation obligation; it must not automatically free capacity for another refund when the first may already have happened.

Some real products may permit goodwill credits beyond an invoice or transfers across an account. That is outside this teaching policy unless the learner extends it explicitly. A stronger permission or a separate event type can express such exceptions without weakening every ordinary limit. The rubric rewards declared scope rather than a claim that one rule fits every business.

## Work the concurrent-refund schedule

An eligible collection has one hundred remaining. Two callers request seventy each. Both local Refund constructors can be valid. The shared reservation authority accepts one seventy and leaves thirty available. The other receives a limit result or a conflict requiring a new request, depending on the chosen API. It does not silently shrink seventy to thirty without caller agreement.

Now the accepted provider call times out after the external effect. Its seventy remains unresolved and reserved. A second seventy must not be admitted just because no local confirmed refund exists yet. Reconciliation by stable provider identity later confirms or rejects the original operation and resolves the reservation accordingly.

The model should also test explicit provider rejection before any effect. That outcome can release the reservation, allowing another legitimate operation. These two schedules demonstrate why Unknown and Failed are not interchangeable. A Boolean success flag cannot carry all the recovery information needed by the cumulative limit.

## Keep corrections append-only in meaning

The original finalized invoice remains unchanged. A correction links to it and explains the new business decision. If a correction itself was mistaken, create a reversal or replacement according to policy rather than silently editing the accepted record. The term append-only describes the evidence model here; the prototype still needs a real persistence protocol before claiming durable immutability.

A reversal must refer to the event it reverses and preserve its own identity. Repeating the reversal should not undo twice. Reversing a credit also does not automatically reverse a completed cash refund; that would require another collection or account adjustment under separate authority. The two-balance model keeps those consequences visible.

For reporting, derive balances from accepted event categories and states. Do not sum every positive Money value indiscriminately. Pending reservations, confirmed cash movements, credits, and reversals have different roles. A reconciliation report can compare the derived summaries with stored aggregates and classify discrepancies without repairing them automatically.

## Build the bounded prototype

Start with immutable synthetic events and a pure projection for the two balances. Add a deterministic acceptance function that checks cause identity and cumulative limits. Then add a fake provider with explicit rejection and effect-before-exception modes. Finally, write a restart simulation by serializing the model's accepted records to a disposable file and reloading them, while clearly stating that this is not a database concurrency proof.

The restart simulation tests whether the proposed state representation contains enough information to resume. It does not make an ordinary file a production financial ledger. A later provider-backed implementation needs atomic writes, concurrent acceptance, crash behavior, and access control. The educational model's purpose is to expose missing information before those mechanisms are selected.

## Exercise MF-28A: calculate two balances

Work the fully paid credit, unpaid credit, and overpayment refund cases. Show corrected receivable, net collected cash, and pending obligations after each event. Explain why one generic adjustment total cannot distinguish the three histories.

## Exercise MF-28B: protect unresolved capacity

Use two seventy-unit requests against one hundred eligible cash. Specify reservation acceptance, explicit rejection, unknown provider outcome, and later confirmation. State which transition releases capacity and why a timeout alone is insufficient.

## Exercise MF-28C: submit the correction capstone

Provide the event schema, two-balance projection, cumulative acceptance rule, six deterministic schedules, and integration limitations. Include exact replay, conflicting replay, concurrent requests, ambiguous provider outcome, reversal replay, and a correction without any refund. Keep application source and real accounts untouched.

## Review checkpoint

The capstone succeeds when the learner can explain both what the customer should owe and what money has actually moved, including unresolved obligations. Stable identities connect the histories without collapsing them. That distinction makes corrections, support explanations, and recovery materially safer to reason about.
