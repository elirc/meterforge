# Review 07. HTTP, architecture, acceptance, and operations

Use this review after chapters 22 through 26. These exercises ask for connected reasoning across boundaries. The strongest answer is not the one that names the most technologies. It is the one that explains which authority accepts an operation, which evidence survives failure, and which observation proves each public claim.

## MF-22A: classify authority evidence

The usage API test proves that a resource stored under one tenant key is not returned through a different tenant-key route. It does not authenticate the caller or verify membership in the chosen tenant. The inspected host has no authentication and authorization setup for these routes. Both statements can be true without dismissing the useful lookup isolation.

A proposed secured-host suite includes unauthenticated rejection, authenticated wrong-tenant rejection, authorized same-tenant success, and explicitly privileged cross-tenant behavior if the product supports it. The tests must construct verified identity and execute the middleware and policy path. Merely changing the route string in an anonymous test does not establish membership enforcement.

Full credit requires explaining where authoritative tenant context comes from and how it relates to the route. A design that trusts a caller-controlled tenant header without verification has only moved the same problem to another field. A design that removes tenant scoping from repositories because authentication exists also loses defense at a different boundary.

## MF-22B: map public errors

A missing payment key returns the endpoint's explicit BadRequest object. A usage idempotency conflict is a BusinessRuleException mapped to 409 by the custom handler. A subscription version-conflict business code follows the handler's other-business-rule mapping to 422 unless another path intervenes. A non-business exception is not handled by that custom handler and needs hosted observation under the remaining configuration.

Do not infer status from an error code's English name. The mapping is executable code. The payment's manual response and the handler's ProblemDetails path can have different shapes. A contract test should assert body fields, status, and trace evidence where promised, not merely that every failure is non-success.

The rubric rewards restraint for late or unhandled failures. When source inspection does not establish the exact final response, identify the smallest hosted fixture needed. A direct exception assertion is not a substitute for running middleware and serialization.

## MF-22C: build a test ladder

For a proposed billing acceptance endpoint, a domain test checks invoice guards, an application test checks calculation and persistence call order, a repository test checks exact and conflicting replay, a hosted test checks binding and trace headers, and a deployed or provider-backed test checks cross-process durability under the selected adapter. Each test reports its environment.

The hosted test can use a fake store to isolate HTTP behavior, but then it cannot claim PostgreSQL acceptance semantics. The repository test can use PostgreSQL without proving caller authentication. The ladder is complete when these claims are complementary and no result is used to certify a layer it never executed.

## MF-23A: audit the architecture assertion

The current test inspects the domain assembly's referenced assembly names and rejects selected web-framework or MeterForge outward references. It would catch a direct domain reference to an outer MeterForge project. It would miss a runtime registration that selects an inappropriate adapter, an application-to-infrastructure dependency not covered by this assertion, or a semantic replay defect inside an allowed adapter.

A passing test therefore supports a narrow dependency-direction claim. It does not prove complete architecture, security, durability, or behavioral substitutability. Full credit includes a concrete caught violation and a concrete missed composition mistake. Vague criticism that "tests cannot prove everything" is less useful than identifying the actual rule.

## MF-23B: specify adapter substitutability

For IInvoiceSnapshotStore, define whether identical replay succeeds, conflicting replay rejects, accepted documents are immutable, cancellation can leave an unknown commit outcome, and supplied collections are copied or trusted. The current PostgreSQL commands group writes in a transaction but do not compare complete payload equality under do-nothing conflicts. That gap must be addressed or documented.

For IProductRepository, define duplicate-key behavior, code uniqueness, tenant normalization, returned-object ownership, and process persistence. The current dictionary replaces an existing tuple key and permits repeated codes under different identifiers. A proposed adapter must not silently change these semantics while claiming to be a drop-in substitute unless the application contract changes deliberately.

## MF-23C: plan a durable adapter switch

Begin with a shared behavioral contract suite, then add provider-specific concurrency and restart evidence. Define canonical keys and conflict responses before migration. Identify what current in-memory data can be exported and what has already been lost. Switching dependency injection cannot reconstruct absent history.

A staged rollout can rehearse imports in a disposable target, validate relationships, then switch readers under a controlled policy. Rollback must account for newly accepted durable operations and published identifiers. A plan that simply toggles back to an empty in-memory store may discard accepted business meaning. Full credit treats deployment as a data and contract transition, not only a registration edit.

## MF-24A: define the accepted evidence set

Include tenant, subscription, billing period, accepted usage selection, price versions, adjustment policy, tax correspondence, lines, and finalization evidence according to the capstone's purpose. Late usage follows an explicit cutoff and correction policy. Today's prices do not replace accepted historical prices on replay.

Two payloads can both total100 while one charges usage A and the other usage B. They can also total100 under different tax and discount allocations. If those distinctions are part of accepted document meaning, they must conflict under the same operation key. Comparing only Total loses evidence even when the customer-facing number remains unchanged.

## MF-24B: model the acceptance race

Workers A and B prepare different payloads under one operation identity. One atomic acceptance binds the winning payload and receipt. The other reads that accepted evidence and receives a conflict unless its payload is equivalent under the declared canonicalization. Header and lines are accepted as one coherent document rather than merged per row.

If the winning response is lost after commit, a retry retrieves the accepted identifier and evidence. It does not generate another invoice or call Finalize blindly on an already finalized shared object. The provider-backed test should observe both caller outcomes and the complete stored line set, while the hosted test verifies replay response semantics.

## MF-24C: submit the capstone packet

The packet needs the state diagram, canonical input definition, persistence boundary, seven schedules, and limitations. Invoice acceptance, collection, and notification remain separate states. A positive accepted invoice can await collection; a zero invoice can follow a no-provider branch; an unavailable webhook does not rewrite the accepted invoice.

Grade the prototype on invariants and recovery obligations, not framework complexity. A deterministic model is acceptable when labeled honestly and paired with an integration plan. It loses credit if a timeout creates a new business operation by default, if line sets can merge, or if external payment success is claimed to roll back with a local transaction.

## MF-25A: classify migration evidence

Map catalog, subscription, usage, invoice, payment, and integration categories separately. Tenant normalization can change identity. Current ProductId cannot reveal a missing amendment timeline. A simulated provider reference must remain marked synthetic. These are semantic facts that a row-copying script cannot resolve automatically.

Read-only classifications include transferable, normalization collision, duplicate candidate, conflicting evidence, and unsupported historical gap. The report should preserve source identifiers and transformation policy. Full credit rejects the temptation to fill new historical columns with invented values merely to satisfy a target schema.

## MF-25B: rehearse interrupted acceptance

A migration run has a stable identity and each source record has a fingerprint and acceptance receipt. After interruption, the same run observes accepted receipts and continues with unaccepted records. Exact replay does not duplicate causes. A changed transformation policy creates a new reviewable proposal and explicitly handles records already accepted under the old policy.

The rehearsal uses a disposable target. It records rejected and unresolved categories without silently dropping them. The rubric should require evidence that replay preserves relationships and monetary values, not just that the process exits successfully on its second run.

## MF-25C: validate the cutover

Check tenant associations, subscription references, usage identities, line membership, currency agreement, accepted amounts, and unresolved categories. Row counts alone cannot detect swapped relationships. Reader switching should occur only under the declared validation threshold and with a plan for operations accepted during the transition.

Rollback may require a compensating or compatibility strategy rather than deletion if identifiers have been published. A dual-write proposal needs an authoritative side and reconciliation because the two stores do not share an automatic transaction. Strong submissions choose a bounded strategy they can explain under partial failure instead of adding parallel writes without a recovery model.

## MF-26A: define the event vocabulary

Useful events include calculation-started, quote-accepted, document-acceptance-attempted, document-accepted, collection-outcome-unknown, and collection-confirmed. Each names its observation point and carries a stable business identity plus request trace linkage where applicable. An attempted event cannot prove an accepted effect, and a missing confirmed event cannot prove absence if logging is lossy.

The durable receipt remains authoritative for replay. Logs support diagnosis and timing. A replay can have a new request trace while retaining one business identity. Full credit requires preserving this many-attempts-to-one-operation relationship rather than treating every trace as a new invoice.

## MF-26B: design operational metrics

Count accepted documents, exact replays, conflicts, provider requests, explicit rejections, and confirmed collections separately. Track unresolved collection as a gauge and its age under a defined clock policy. Use bounded phase and outcome labels, not an unbounded label per invoice. State denominators when reporting percentages.

Healthy retries increase transport attempts and replay counts without increasing accepted business operations. Duplicate business operations instead increase accepted identities for one intended cause. The metric design should help distinguish those patterns rather than treating all repeated requests as errors. A growing unresolved backlog can be important even when confirmed-success percentage looks high.

## MF-26C: write an evidence-based incident report

State that the database receipt confirms invoice acceptance, provider evidence confirms collection when available, and logs describe attempted phases. A missing confirmation log could mean the effect never occurred or that logging failed after the effect. The next check should query authoritative operation evidence before initiating a new charge.

Keep hypotheses separate from facts and proposed actions. Include safe identifiers and bounded categories, not credentials or full customer payloads. A strong report can explain what remains unknown and why. It does not turn uncertainty into a confident failure narrative simply because the client received an error.

## Integrated grading rubric

An excellent submission maintains stable identity across retries, distinguishes local and external authority, preserves accepted evidence, and chooses tests that execute the claimed mechanism. It explicitly labels proposed behavior and unexecuted experiments. A competent submission gets the local rules right but may need more precise recovery or concurrency evidence. A submission needing revision confuses previews with commits, route scoping with authentication, do-nothing conflicts with equality, or exceptions with proof of no external effect.

Use the rubric to choose the next experiment, not merely to assign a score. If a learner understands the model but lacks provider evidence, a small isolated integration test is the next step. If the model itself merges identities or loses evidence, more infrastructure will not fix the conceptual gap. Correct the protocol first, then broaden execution.
