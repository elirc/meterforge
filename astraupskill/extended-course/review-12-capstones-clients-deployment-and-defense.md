# Review 12. Capstones, clients, deployment, and design defense

This review covers chapters 39 through 44 and closes the main instructional route. These answers describe coherent proposed protocols while preserving the limits of the current application and executed labs. A learner can choose another policy, but it must remain consistent under the same failure schedules. The rubric values accepted meaning, recoverable uncertainty, and evidence at the correct layer over the number of components in a diagram.

## MF-39A: implement the claim schedule

Worker A claims generation one and sends, then pauses. After its claim expires, worker B obtains generation two and receives an accepted receiver receipt. B finalizes conditionally with generation two. A's later failure response cannot overwrite Succeeded because its generation no longer matches. The accepted state remains tied to current authority, not whichever worker returns last.

Both workers may still have contacted the receiver. Local claim fencing does not erase A's transmitted request or guarantee one receiver effect. The fake receiver must use stable message identity and an acceptance receipt. A complete test asserts sender state, claim generation, number of transport attempts, and number of synthetic receiver effects separately.

Full credit requires a deterministic schedule with explicit gates. Starting two tasks and observing one convenient outcome is weaker evidence. The provider-backed plan must show where the generation comparison is enforced atomically rather than as a stale preflight check in each worker.

## MF-39B: prove receiver effect identity

The fake receiver accepts one semantic payload under a scoped message identity and stores a receipt with the synthetic effect. Exact replay returns that receipt without applying another effect. A conflicting payload under the same identity rejects and preserves the winner. In accepted-with-lost-acknowledgment mode, the first caller sees an exception while the receiver ledger already contains one accepted effect.

The next delivery uses the same identity and obtains the stored receipt. The model should count two transport attempts but one business effect. This is stronger than returning the same formatted reference on every call without a ledger, which would resemble the stateless payment simulator rather than receiver idempotency.

A real receiver must persist the inbox receipt and business effect within one enforceable acceptance boundary. The model's dictionary demonstrates logic, not restart durability. The review should explicitly ask what happens if the receiver acknowledges before committing, because sender success is only as meaningful as the receiver's acknowledgment contract.

## MF-39C: submit the dispatcher capstone

The packet includes the work-state graph, claim generation, manual clock, retry eligibility, failure categories, receiver protocol, and dead-letter review fields. Unsupported schema is classified as a permanent compatibility issue under the chosen policy, while transient transport failure can schedule another attempt. Attempt count changes when a valid dispatch starts, not when an arbitrary duplicate notification arrives.

Operator replay preserves message identity for the same accepted event. Correcting payload meaning requires a new event or explicit correction contract. The review packet should expose the accepted payload fingerprint and schema version without dumping sensitive content. Read-only inspection must not send requests as a hidden side effect.

Grade the capstone on its schedules: stale finalizer, lost acknowledgment, duplicate delivery, conflicting identity, retry exhaustion, unsupported schema, and reviewed replay. A queue choice without these behaviors is incomplete. A small pure model with an honest integration plan can earn a stronger grade than a large network demo with unexamined ambiguity.

## MF-40A: implement replay-safe admission

With four units available, two independent three-unit requests cannot both reserve under the proposed finite-budget invariant. One conditional acceptance records a reservation and stable receipt, leaving one unit. An exact retry of that winner returns the same reservation without reducing capacity again. Reusing its operation key with a different quantity produces a conflict.

The evaluator contributes the arithmetic decision from supplied state. The shared reservation authority connects that decision to accepted state. The test should assert both caller outcomes, reservation identities, active reserved total, and remaining capacity. Checking only that one Boolean was false would miss duplicate receipts or incorrect accounting.

The budget identity includes tenant, feature, allowance period, and relevant policy revision. Otherwise a replay after period rollover could reserve against a different budget under the same cause. A real implementation needs a shared persistence mechanism; one process's lock is only the local model.

## MF-40B: resolve expiry and completion

If completion conditionally consumes the Active reservation first, expiry observes the changed state and does not release committed usage. If expiry wins, the worker follows the declared stale-completion policy. It cannot silently consume a reservation that no longer grants capacity. The correct policy depends on whether the billable external value could already have been delivered.

For unknown external outcomes, immediate release may be unsafe because another operation could consume capacity while the first already produced value. Keep a reconciliation obligation or another explicitly conservative state until authoritative evidence resolves it. An expiry timestamp alone cannot prove that the external effect did not occur.

Use both deterministic interleavings and assert accounting conservation. Available, reserved, and consumed quantities must agree with accepted states. A test that only inspects the reservation enum can miss double release or double consumption. The integration plan should coordinate independent contexts around the conditional transition.

## MF-40C: submit the reservation capstone

The packet defines budget scope, replay equality, reservation lifecycle, completion identity, expiry behavior, period rollover, and plan changes. A reservation crossing a period boundary retains its declared budget or follows an explicit transfer protocol. A downgrade does not silently reinterpret already accepted work without policy. Unlimited remains distinct from a finite zero allowance.

The read-only reconciler compares reservations, consumption events, execution receipts, and aggregates. It classifies unresolved active work, duplicate completion, released-but-confirmed work, and summary disagreement. It does not automatically repair uncertain records. The seven tests should include these accounting relations as well as happy-path admission.

Full credit labels the model's persistence limits and identifies the next database experiment. A serialized temporary file can demonstrate restart representation, but it does not establish concurrent atomic acceptance. The answer should also distinguish entitlement from authenticated authority and specify where a future secured host obtains trusted plan state.

## MF-41A: implement usage recovery

The client journal records stable external event identity and intended payload before sending. After a lost response, it remains sent-unknown. Reposting the same identity and semantic values lets the current server return the accepted winner as replay. The client then stores the accepted internal identifier and available Location evidence and marks the command resolved.

If the first response was lost, the client may not know the internal identifier for GET. The current API does not expose an external-key lookup in the inspected endpoints, so stable repost is the supported recovery mechanism being modeled. Do not invent another query simply to simplify the client algorithm.

The journal's own durability matters. An in-memory entry lost on client restart cannot preserve the key. A disposable file model can illustrate the required representation while a production client needs secure, atomic storage. Unresolved entries should not be deleted merely because a short timer elapsed.

## MF-41B: separate edit from retry

Changing quantity after the first send changes semantic meaning. Under the same external key, it can conflict with an accepted event. Preserve the original sent journal entry and create a linked correction or new event under the declared product policy. Editing an unsent draft is different because no server acceptance may yet exist.

After a subscription version conflict, choosing another product based on freshly read state is a new command with a new expected version. Retrying the original command preserves its original intent. A client should not blur these actions by mutating one journal payload whenever the user clicks again.

Full credit includes a UI explanation that distinguishes unknown submission from editable draft. The client need not expose every internal identifier to the user, but it must retain them internally to recover safely. Automatic generation of a new key for every retry is specifically incorrect for accepted usage semantics.

## MF-41C: compare endpoint guarantees

Usage offers accepted winner identity, exact and conflicting replay by tenant and external event key, and readback by internal identifier under the current in-memory adapter. Subscription amendment offers an expected-version precondition and current-state response, but no separate command receipt. Payment requires and forwards an idempotency header while using the current stateless simulator by default.

These are different capabilities. A client cannot assume that payment collection has usage's local receipt semantics or that subscription readback proves which command caused the current product. Unknown outcomes should remain unresolved where the API lacks authoritative evidence. A proposed operation resource can close that gap, but it must be labeled as an extension.

The capability table should include adapter lifetime. Even usage's correct process-local replay behavior is not durable across restart in the default host. A client contract that requires persistence needs the server's deployed mode and acceptance guarantees, not just familiar endpoint names.

## MF-42A: define capability metadata

A safe descriptor reports active storage mode, durable acceptance support, provider simulation or real integration mode, enabled workflow categories, and authentication policy presence under the proposed deployment contract. It omits secrets and connection details. Preview, integration, and durable modes can each be valid if their guarantees are explicit.

Adapter availability means code can be selected. Adapter selection means composition chose it. Connectivity means a bounded check reached its dependency. Schema compatibility and business acceptance semantics are further claims. The descriptor should not collapse them into one green status that users interpret as complete correctness.

Full credit uses the current readiness response as honest evidence of in-memory defaults while recognizing its limits. The postgresAdapterAvailable field is not proof that PostgreSQL stores accepted usage or that a database connection was tested by that route.

## MF-42B: reject unsafe fallback

A proposed durable mode validates required storage and provider configuration at startup and refuses to accept durable business operations if those requirements are missing. Silent fallback to in-memory stores changes persistence guarantees while preserving successful-looking responses. Silent fallback to the simulator changes the meaning of payment success. Both require explicit restriction if allowed at all.

An intentionally restricted fallback can expose preview-only operations and advertise its mode clearly, but it must not accept the same consequential commands under weaker hidden semantics. Tests should cover incompatible component combinations and verify that the host fails configuration or disables the affected workflow according to policy.

The rubric rewards concrete startup predicates and user-visible guarantees. A generic warning log is insufficient if the process continues accepting operations that callers believe are durable. No live credentials need to be read or stored in the teaching model.

## MF-42C: rehearse topology

For restart, accept a synthetic operation through one host using a disposable durable store, terminate that host, create a new host against the same store, and read the accepted result. Keeping the same dictionary instance while recreating a service object does not test process persistence. Record exactly which state survives and where.

For cross-process acceptance, coordinate two hosts submitting the same operation identity and assert one accepted winner and stable replay or conflict outcomes. Two independent in-memory hosts cannot share their dictionaries merely because both registrations are singleton. Load-balanced readback can therefore differ by host in that mode.

A ready response establishes only its declared probe behavior. It does not replace these tests. Full credit includes the topology, adapter, cleanup scope, and remaining limitations, such as untested network partitions or provider behavior. The experiment should be bounded rather than presented as proof against every possible distributed failure.

## MF-43A: build an evidence bundle

For a payment complaint, separate the accepted invoice receipt, local collection operation state, provider correspondence, request traces, and the customer's report. Label source and observation time. Classify explicit rejection, confirmed effect, unknown outcome, or missing evidence without forcing an uncertain case into a definitive category.

Safe fields include internal identities, bounded status and error categories, accepted amounts and currencies where authorized, and fingerprints or references to protected evidence. Avoid credentials, bearer capabilities, and unnecessary payload content. The course uses synthetic examples, while a real support tool needs scoped access and redaction policy.

The next check should be read-only and discriminating. If a missing log could mean either no effect or lost logging, query authoritative operation evidence rather than initiating another charge. Full credit preserves unknown as a valid diagnosis and names what evidence would resolve it.

## MF-43B: write a repair proposal

A plan names targets, observed state or version, intended change, stable repair identity, expected effects, validation, and compensation limits. Before execution, the executor rechecks freshness. If ordinary reconciliation has already confirmed the payment, a stale plan to collect again must stop or be revised. Approval of an old premise is not authority to ignore new state.

The plan version and execution receipt are retained so a lost support response can be recovered safely. Exact repeated execution returns the accepted result. External effects remain separate obligations with their own identity and progress; a local transaction cannot erase a completed refund.

Full credit keeps planning and effectful execution separate. A dry-run view should not secretly mutate records or send provider requests. If dry-run and execution use different selection logic, the learner must explain how they avoid targeting different records; sharing a pure planning function is often clearer.

## MF-43C: distinguish duplicate appearances

Two traces with one accepted invoice identifier can be ordinary replay of one business operation. Two accepted invoice identifiers under different operation keys for one intended cause require deeper review. Equal totals and dates do not prove duplication because legitimate separate documents can share those values, and semantically duplicated documents can differ slightly.

Gather cause identity, line evidence, collection state, publication history, and correction relationships before proposing action. Deleting one row may break an issued document or hide a completed collection. A correction or voiding workflow should preserve history under the product's policy rather than making an apparent duplicate disappear without evidence.

The rubric asks whether the tool explains its matching rule and uncertainty. A detector can produce candidates for review without authorizing refunds. The safest first outcome can be an explanatory report with no data change when accepted identity shows that the customer saw two responses for one invoice.

## MF-44A: submit the integrated traces

The five scenarios should preserve separate identities for lifecycle command, usage event, billing acceptance, collection, correction, and delivery. Mark current behavior from source, proposed policy, executed lab evidence, and untested integration assumptions distinctly. For activation versus cancellation, show accepted ordering. For late usage, preserve the original billing selection and use a correction path.

For unknown payment outcome, retain uncertainty until provider evidence resolves it. For credit and refund divergence, show receivable, confirmed cash, reserved capacity, and unresolved obligation separately. For adapter migration, identify which existing tests remain useful and which new cross-process and restart claims need evidence.

A passing packet must not invent hidden persistence or historical fields. If the current model lacks an amendment ledger or acceptance sequence, the proposal should name that addition. An excellent packet makes those additions economical and shows the exact failure schedule each one addresses.

## MF-44B: defend one design decision

Choose a minimal counterexample. For a receipt, use a committed effect with a lost response. For a watermark, use late usage arriving between calculation and replay. For a claim generation, use a stale worker returning after a newer worker succeeds. For a correction ledger, use a credit whose cash refund is still unknown. The mechanism should protect a named invariant in that example.

Then state its cost: storage, additional comparisons, operational backlog, or protocol complexity. Offer a narrower alternative when appropriate, such as a clearly labeled single-process demonstration that does not promise restart recovery. This shows judgment rather than assuming every system needs every mechanism.

The strongest defense avoids slogans. "Use an outbox" is weaker than explaining which intent is stored with which state, how delivery repeats, and how the receiver prevents duplicate effects. A reviewer should be able to challenge the schedule and see the invariant remain intact.

## MF-44C: produce a next-experiment plan

Select the most consequential unproven claim, such as coherent snapshot acceptance under two PostgreSQL writers. Specify a disposable schema, two independent contexts, gates before acceptance, different payloads under one identity, observed rows, caller outcomes, and cleanup confined to the created test resources. A passing result proves that schedule under that provider configuration.

It does not prove every crash point, network partition, or future schema revision. State those limits and the next priority if they matter. The experiment should close one important evidence gap without becoming a broad uncontrolled environment exercise. No real payment account or existing customer database is needed.

## Final review rubric

Assess correctness, evidence, and economy separately. Correctness asks whether identities and state transitions preserve accepted meaning. Evidence asks whether executed tests reach the claimed boundary and whether untested assumptions remain visible. Economy asks whether each mechanism protects a requirement that the chosen scope actually has. A large design can fail all three, while a small honest prototype can demonstrate strong engineering judgment.

Require separate observations for caller outcome, persisted or modeled state, and external obligations. A thrown exception is not a complete state report. A successful response is not proof of every downstream effect. A green characterization test is not endorsement of every current limitation. These distinctions should appear repeatedly in the learner's reasoning because they connect the whole course, not because they substitute for source-specific analysis.

The final packet should leave the reviewer able to answer three questions: what does the current code do, what does the proposal deliberately change, and what evidence supports the change? When those answers are precise, implementation can proceed in bounded steps. When they are vague, more code or a larger test suite will not repair the missing contract.
