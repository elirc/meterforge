# 43. Read-only support tools and repair plans

This chapter designs proposed support tooling around accepted usage, invoice snapshots, payment attempts, and correction records. Use [UsageEvent](../../src/MeterForge.Domain/Usage/UsageEvent.cs), [InvoiceSnapshot](../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs), and [PaymentAttempt](../../src/MeterForge.Domain/Payments/PaymentAttempt.cs) as evidence anchors. The current repository does not provide the complete support console described here. The exercise is to make a repair plan concrete and reviewable before any consequential action occurs.

The prerequisite is the incident-evidence chapter. Diagnosis and repair should be separate operations because a query may reveal uncertainty rather than an actionable defect. A tool that automatically retries payments while displaying an incident can turn inspection into an unreviewed side effect.

## Start with an evidence bundle

For a usage complaint, gather accepted event identity, tenant scope, external key, normalized semantic payload, replay or conflict observations, and any billing document references available under the proposed system. For an invoice complaint, gather accepted line evidence and calculation policy versions. For a payment complaint, gather local operation state and provider correspondence.

The bundle should distinguish authoritative records from logs and user reports. A client screenshot can be useful context but does not replace accepted ledger evidence. A trace can show an attempted call but not necessarily its committed result. Label each fact with its source and observation time so a reviewer can understand confidence and freshness.

Use safe identifiers and bounded categories in exported reports. Avoid provider credentials, authentication tokens, and unnecessary customer payloads. Descriptions and source references may themselves contain sensitive information, so a real implementation needs scoped access and redaction policy. The teaching exercise uses synthetic values only.

## Classify before proposing action

Useful categories include exact replay misunderstood as duplication, conflicting identity reuse, missing accepted resource, ambiguous provider outcome, mismatched invoice evidence, stale client version, and unsupported historical gap. Each category has different next checks. A generic "retry failed operation" button is too coarse.

For exact replay, the repair may be no data change at all: explain that multiple requests resolved to one accepted resource. For conflicting identity reuse, the next step may be correcting the producer's key policy, not overwriting the accepted event. For an ambiguous provider outcome, reconciliation precedes any new collection attempt.

The classification should allow unknown. A support tool that forces every incident into a known category can create false certainty. Record the missing evidence and the read-only query that would resolve it. If no authoritative provider query exists, the plan may require a separate operational review rather than automated action.

## A repair plan is a first-class proposal

A proposed repair plan names the target identities, observed preconditions, intended state change, external effects, operation key, expected result, rollback or compensation limits, and validation checks. It should be immutable once approved for execution under a future production workflow, or versioned when its assumptions change.

The plan must include a freshness precondition. State can change between diagnosis and execution. For example, a payment initially unresolved may become confirmed through normal reconciliation. Executing a stale plan to charge again would be unsafe. The executor should compare the expected operation state or version and stop when the premise no longer holds.

Dry-run output should be generated from the same selection and validation logic that execution would use where feasible. A separate approximate dry run can mislead reviewers about the actual targets. The course's prototype can share a pure planning function and keep effectful execution behind a fake boundary.

## Work a duplicate-looking invoice case

Two customer-visible responses show different request traces but the same accepted invoice identifier. The evidence bundle finds one document receipt and two transport attempts. The correct classification is repeated request or replay, not duplicate billing. The plan contains an explanatory response and perhaps a client UX improvement, with no financial mutation.

Now change the evidence to two accepted invoice identifiers under different operation keys for one intended billing cause. The tool should identify the split cause and gather line and collection evidence for both. It cannot safely delete one invoice merely because totals match. A correction or voiding policy may be needed, especially if one has been collected or published externally.

This comparison teaches why support tools should preserve cause identity and downstream relationships. A simplistic duplicate detector based on tenant, total, and date can flag legitimate separate invoices or miss semantically duplicated ones with different rounding. The report should expose its matching rule and uncertainty.

## Execution receipts and repeated repair

If a repair is eventually executed, it needs its own stable operation identity and receipt. A support browser can lose its response just like an ordinary client. Repeating the action should retrieve the accepted repair outcome rather than applying the credit, release, or provider call twice.

The executor should record which plan version it accepted and which preconditions it checked. If the plan contains multiple effects, each needs accountable progress and recovery. A local database update and external refund remain separate boundaries even when an operator initiated them through one button.

Do not promise rollback where only compensation is possible. A completed external refund cannot be made nonexistent by deleting a local row. A compensating collection would be a new consequential operation requiring its own authority and policy. The repair plan should state these limits before execution.

## Exercise MF-43A: build an evidence bundle

Choose a usage, invoice, or payment complaint and list authoritative facts, diagnostics, user observations, and missing evidence. Define a read-only classification and safe report fields. Include an unknown category rather than forcing a guessed diagnosis.

## Exercise MF-43B: write a repair proposal

Create a synthetic plan with targets, freshness preconditions, stable identity, effects, expected result, and validation. Work a normal process changing the target state before execution and show how the stale plan is rejected or revised.

## Exercise MF-43C: distinguish duplicate appearances

Compare two traces for one accepted invoice with two accepted invoices for one intended cause. State the evidence needed before proposing a correction. Explain why matching total and date alone cannot authorize deletion or refund.

## Review checkpoint

Support tooling should make diagnosis safer and repair more deliberate. A strong design produces a concrete plan with current preconditions and replay-safe execution evidence. It does not hide consequential actions inside read-only views or treat an uncertain match as permission to rewrite history.
