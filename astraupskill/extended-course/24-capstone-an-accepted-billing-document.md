# 24. Capstone: an accepted billing document

This capstone combines the current Invoice, InvoiceSnapshot, discount, tax boundary, and payment contracts into a proposed acceptance workflow. Use [Invoice](../../src/MeterForge.Domain/Invoicing/Invoice.cs), [InvoiceSnapshot](../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs), and [TaxBoundary](../../src/MeterForge.Application/Tax/TaxBoundary.cs) as source anchors. Work in a disposable prototype. The course does not add this workflow to the application or claim that the preview endpoint already performs it.

The deliverable is a coherent protocol for accepting one billing document with reproducible evidence. Payment collection remains a later operation with its own identity. Keeping those operations distinct allows an invoice to exist before collection and avoids pretending that a database transaction can atomically include an external financial service.

## Define acceptance before defining classes

An accepted document binds tenant, subscription, billing period, selected usage, price versions, adjustment policy, tax evidence, line amounts, and finalization instant. Exact replay returns that document. A repeated operation key with different accepted inputs returns a conflict. A later correction creates a separate linked document or adjustment under the chosen policy.

This definition is stronger than returning a newly constructed preview. The current finalize-preview endpoint creates an invoice identifier for that request, adds supplied lines, finalizes, and returns summary fields. It does not invoke the snapshot store in its inspected body. The capstone must therefore introduce acceptance persistence explicitly rather than describing the preview as a durable invoice issuance operation.

Choose a stable billing operation identity. It may include subscription and period plus a run or correction identity, but do not assume one invoice per period is universally sufficient. Some products need multiple legally or operationally distinct documents. The proposal should explain which requests are replays and which are new business operations.

## Freeze calculation inputs without freezing the whole system

Capture the accepted usage set or a stable usage cutoff and selection rule. Record the price versions used, not merely the current product price. Capture adjustment decisions and tax quote correspondence. The rest of the system can continue accepting new usage, but that new usage must not silently change the already accepted calculation.

A cutoff timestamp alone may be insufficient if events can arrive late with earlier occurred times. Define whether the cutoff uses ingestion order, accepted sequence, event occurrence time, or a combination. A late event can be deferred to a correction or later bill under policy. The protocol should not include it inconsistently depending on when a retry happens.

Use a calculation-input version or canonical evidence identity. If the draft changes while a tax quote is in flight, acceptance checks correspondence and re-quotes or rejects the stale result. The goal is not to hold a long transaction while every calculation runs, but to bind the final accepted output to the inputs actually reviewed.

## Build the state machine

A proposed operation can move through Preparing, AwaitingExternalEvidence, ReadyToAccept, Accepted, and Rejected or Abandoned states. These names are illustrative; each transition needs a predicate and surviving evidence. Preparing can be retried from stored inputs. AwaitingExternalEvidence records which quote or calculation remains unresolved. Accepted is immutable under the document contract.

Avoid a generic Failed state that erases whether acceptance already committed. A caller timeout after commit is an unknown response outcome, not necessarily a failed business operation. The retry reads by operation identity and returns Accepted if that state exists. This is the receipt pattern applied to a richer document.

The transition to Accepted should atomically bind the operation receipt, header, lines, and required evidence references in the chosen persistence mechanism. The current Postgres adapter's per-row do-nothing behavior is a starting point for analysis, not a complete implementation of this protocol. The capstone should specify exact replay comparison and coherent line-set acceptance.

## Work a competing-calculation schedule

Two workers begin the same billing operation. Worker A selects one accepted input version; worker B observes a later draft change and produces a different payload. Both attempt acceptance under the same operation key. The protocol must select one coherent accepted document and make the other caller observe either exact replay or conflict. It must not merge their line sets.

Write the accepted payload comparison in terms of business evidence. A total-only comparison is insufficient because two line sets can have the same sum. Include source references, price identities, periods, and tax calculation version as required by the chosen contract. Presentation-only fields may have different comparison rules, but those rules must be explicit.

Then add process loss after commit but before response. The next request should retrieve the accepted identifier and document, not generate a new invoice simply because the earlier HTTP response was lost. A hosted test can verify response replay, while a provider-backed test establishes persistence and concurrency.

## Keep collection separate

Once accepted, the document can trigger a collection intent if its payable amount is positive. A zero document follows the declared no-provider branch. The collection operation references the accepted invoice and amount rather than trusting arbitrary request-supplied values. It has its own provider identity and reconciliation states from chapter 11.

If collection fails explicitly, the invoice does not become a draft again. If collection is ambiguous, the invoice remains accepted while the payment operation reconciles. This separation prevents financial transport failures from rewriting billing evidence. A UI can show accepted and awaiting collection as distinct facts.

An outbox message can announce acceptance under the proposed transaction, but receiver delivery remains asynchronous. The accepted invoice does not disappear because a webhook destination is unavailable. The integration protocol should preserve delivery obligations and expose their status without changing the document's historical meaning.

## Acceptance packet and assessment

Submit a state diagram, canonical input definition, persistence boundary, seven deterministic schedules, and a limitations section. The schedules should include exact replay, conflicting replay, late usage, stale tax quote, concurrent acceptance, lost response after commit, and zero payable amount. Each should assert both the document and any outstanding obligations.

The prototype may use a deterministic in-memory state machine to explain the protocol. Label that as a model. A separate integration plan must identify the database constraints and conditional writes required for real durability. Do not present a dictionary's survival within one test method as recovery after a process restart.

## Exercise MF-24A: define the accepted evidence set

Choose the exact fields and references that make two billing submissions equivalent. Explain treatment of late usage, price changes, discount scope, and tax version. Give two different payloads with the same total that should still conflict.

## Exercise MF-24B: model the acceptance race

Write a two-worker schedule with different payloads under one operation key. Specify the atomic decision and both caller outcomes. Add a lost response after commit and show how replay retrieves the accepted document without merging lines.

## Exercise MF-24C: submit the capstone packet

Build the bounded model and review materials described above. Separate invoice acceptance, payment collection, and webhook delivery states. State which current source contracts are reused and which protocols are proposed additions.

## Review checkpoint

The capstone succeeds when one accepted document has one explainable body of evidence and retries preserve that meaning. It should remain possible to distinguish calculation, acceptance, collection, and notification at every failure point. That separation is a practical correctness property, not merely an architectural preference.
