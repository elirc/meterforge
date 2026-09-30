# 40. Capstone: entitlement reservations and release

This capstone extends the pure [EntitlementEvaluator](../../src/MeterForge.Domain/Entitlements/Entitlements.cs) into a proposed stateful admission protocol. Chapter 12 showed why a Boolean decision does not reserve capacity. Here the learner builds a disposable model that connects admission, work execution, consumption, release, expiry, and replay. The application evaluator remains unchanged.

Use synthetic feature units rather than real customer resources. The prototype should have a manual clock and controlled worker steps. It does not require a database or external service. Its output must distinguish a correct in-memory state model from the provider-backed concurrency evidence still required for a production implementation.

## Choose the unit and scope

Define one feature, one tenant, one allowance period, and a unit such as export jobs. The allowance identity includes the period or policy revision so that reservations cannot accidentally consume next month's balance. A feature string alone is not enough to identify the budget when limits change over time.

For a finite allowance, track committed consumption and active reservations separately. Available capacity is allowance minus both under the proposed policy. Unlimited remains a distinct state with no finite remainder, but accepted operations still need identity and lifecycle evidence. Unlimited does not mean untracked or unauthenticated.

The evaluator can calculate whether a request fits the supplied available state. The acceptance authority must then atomically record the reservation under the same relevant version or condition. Reusing the evaluator without that atomic link recreates the original two-caller race.

## Reservation identity and semantic equality

A reservation request includes stable operation identity, tenant, feature budget, requested units, and relevant policy version. First acceptance creates a reservation receipt. Exact replay returns it. Conflicting replay under the same identity rejects a different quantity or budget. The receipt lets a client recover after a lost response without consuming additional capacity.

Do not use an execution attempt identifier as the reservation identity. A job may retry several times while fulfilling one accepted request. If each retry reserves anew, temporary failures can exhaust allowance without additional customer value. Separate request cause, reservation, and execution attempt identities.

A reservation can be Active, Consumed, Released, or Expired under the model. Its terminal state records why capacity changed. A summary counter alone cannot tell whether a repeated release was already applied. The event or state record is the source of replay evidence, while counters are derived or transactionally maintained summaries.

## Work completion and failure

When the work succeeds, the reservation is consumed into committed usage exactly once. Available capacity may remain unchanged at that transition because reserved units become consumed units. A second completion notification returns the accepted outcome rather than incrementing consumption again.

When work explicitly fails before delivering the billable value, the policy may release the reservation. Some products charge attempts or partial work, so the capstone must state its chosen rule. For this exercise, charge only confirmed successful completion and keep unknown external outcomes unresolved until reconciliation.

An exception is not always explicit failure. A remote export service may have completed before its response was lost. Releasing immediately can allow another export while the first already consumed value. The model needs an Unknown or Reconciling execution outcome linked to the still-active reservation, or another policy that preserves the obligation until evidence arrives.

## Expiry races with completion

The janitor discovers an old Active reservation while a worker is completing. Both can observe Active before either writes. A conditional transition must choose one terminal outcome. If completion consumes first, expiry must not release the committed usage. If expiry wins, completion must follow the declared stale-completion policy rather than silently charging a released reservation.

One proposal treats expiry as loss of permission to finalize and requires the worker to reacquire capacity before delivering value. That only works if delivery can still be prevented. If an external effect may already have occurred, expiry may need to mark reconciliation-needed rather than free capacity. The policy depends on the execution boundary and cannot be solved by a clock comparison alone.

Use a claim generation to reject stale workers where appropriate, but remember that local rejection does not undo an external effect. The capstone should explicitly state which work is purely local and which hypothetical integration would need receiver or provider evidence.

## Period rollover and plan change

Suppose a reservation begins near the end of one allowance period and completes in the next. Choose whether consumption belongs to the reservation's original budget or the completion period. The first policy keeps admission and consumption aligned; the second may require transferring capacity under a defined rule. Do not let the answer depend accidentally on whichever date the completion handler reads.

A plan upgrade can increase allowance while reservations are active. A downgrade can make current consumed plus reserved exceed the new limit. The model needs an effective-date and grandfathering policy. It should not retroactively invalidate accepted work without a declared product rule.

Preserve the policy version accepted with the reservation. A later evaluator change should not reinterpret an old receipt silently. If a migration is necessary, make it a separate operation with an impact report and explicit handling of active reservations.

## Reconciliation and observability

A read-only reconciler compares reservations, committed consumption events, execution receipts, and summary counters. It classifies orphaned active reservations, duplicate completions, released reservations with confirmed work, and aggregate disagreement. The first version reports discrepancies without repairing them automatically.

Metrics should distinguish requested units, reserved units, consumed units, released units, and unresolved age. A high number of releases may indicate healthy cancellation or repeated downstream failure; the cause category matters. Do not label every reservation as consumption in customer usage reports.

## Exercise MF-40A: implement replay-safe admission

Model two three-unit requests against four available units and an exact retry of the winner. Assert one accepted reservation and stable receipt identity. Include conflicting replay with a changed quantity and explain the shared authority required beyond the pure evaluator.

## Exercise MF-40B: resolve expiry and completion

Create both interleavings of expiry and completion using a manual scheduler. Define terminal states, capacity changes, and unknown external outcomes. State when capacity may safely be released and when reconciliation is required.

## Exercise MF-40C: submit the reservation capstone

Provide the budget identity, state machine, period-rollover policy, plan-change policy, seven deterministic tests, and a read-only reconciliation report. Keep the prototype's in-memory evidence separate from proposed database enforcement and authenticated API behavior.

## Review checkpoint

Reservation correctness connects accepted intent to one eventual accounting outcome. A strong model preserves capacity through ambiguity, returns stable receipts on replay, and explains period and policy changes. It does not treat a preflight Boolean, a timeout, or an expiry timestamp as a complete state protocol.
