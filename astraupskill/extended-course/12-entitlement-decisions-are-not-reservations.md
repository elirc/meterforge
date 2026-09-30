# 12. Entitlement decisions are not reservations

Read [EntitlementEvaluator](../../src/MeterForge.Domain/Entitlements/Entitlements.cs), the [check endpoint](../../src/MeterForge.Api/Entitlements/EntitlementEndpoints.cs), and the [existing tests](../../tests/MeterForge.Domain.Tests/Entitlements/EntitlementTests.cs). This part of MeterForge answers whether a proposed request fits a supplied entitlement and usage amount. It does not record consumption. That distinction is the foundation for every concurrency and authorization claim in this chapter.

Start by separating three questions. Is the caller allowed to act for the tenant? Does the tenant's plan include the feature? Is enough included quantity available for this request? These questions can be related in a product workflow, but one answer does not automatically establish the other two. The evaluator takes values and returns a decision; it does not authenticate a caller or fetch an authoritative plan.

## Normalize the feature identity

Entitlement construction requires a nonblank feature and stores a trimmed lower-case value. IncludedQuantity can be null or nonnegative. Null means unlimited in the evaluator's branch; zero means a real finite allowance of zero. Conflating null with zero would turn an unlimited entitlement into a denial or an absent allowance into an unlimited permission, depending on the mistake.

The requested feature is trimmed and lower-cased before comparison. A normal synthetic example such as a feature with surrounding spaces and mixed case therefore matches its canonical form. This source fact establishes the intended normalization for ordinary strings. It does not establish a culture-independent product identifier policy for every possible character unless the chosen normalization and comparison are reviewed together.

The collection lookup uses SingleOrDefault. Two entitlements that normalize to the same feature can therefore cause an exception rather than a simple denial or a sum of their included quantities. The Entitlement record validates each item individually but does not validate uniqueness across the collection. Collection invariants need their own owner.

## Work the finite-allowance calculation

Take an included quantity of one hundred, consumed quantity eighty, and requested quantity twenty. Remaining before admission is twenty. Because the request fits exactly, the evaluator returns Allowed true and Remaining zero. With a request of twenty-one, it returns Allowed false and Remaining twenty. The meaning of Remaining therefore depends on the decision: after the proposed request when allowed, unchanged available quantity when denied.

The existing test explicitly checks these two cases. Read its assertions as an executable contract rather than merely a green indicator. A client displaying Remaining must understand whether it describes a committed balance or a hypothetical result. Here it is a calculation based on supplied values, not a persisted balance after an actual operation.

If consumed already exceeds included quantity, the remaining value is clamped to zero. The result can explain that no additional positive request fits, but it does not repair the earlier overage or record a billing event. A proposed policy may permit paid overage while denying included usage, yet that policy is not encoded merely by this evaluator's Boolean.

## Unlimited still validates usage inputs

The evaluator rejects negative consumed values and nonpositive requested values before looking up the entitlement. An unlimited feature does not bypass these basic input guards. This ordering is useful to test because a shortcut that returns unlimited immediately might accidentally accept an invalid request.

For a valid request and null included quantity, the result is Allowed true, Remaining null, and reason unlimited. That null is meaningful evidence, not a missing calculation. A response consumer should avoid converting it to zero through a defaulting expression. A UI can display unlimited, while a machine client can branch on the reason or nullable field under a documented contract.

For a missing feature, the result is a denial with zero remaining and a feature-not-entitled reason. That is a business decision, not an authentication failure. Mapping it to an access-control status without considering the endpoint's contract can confuse plan limits with caller identity. Keep the two vocabularies separate in logs and tests.

## The two-caller schedule

Assume an allowance of ten and consumed quantity six. Two callers independently ask for three. Each evaluates against six, computes four available, and receives Allowed true with one remaining. The evaluator has done exactly what its inputs specify. If both operations later consume three, total consumption becomes twelve. The defect would be in a workflow that treats a preflight result as an exclusive reservation.

A proposed admission protocol must atomically connect the decision to a reservation or accepted consumption record. One design conditionally increments a shared counter only if sufficient capacity remains. Another uses versioned state and retries the calculation after a conflict. Either needs operation identity so a retry after a lost response does not consume again.

An in-memory lock can demonstrate serial admission in one process. It does not establish behavior across two service replicas. A database conditional update or another shared authority must enforce the policy where independent callers meet. The exercise should report these boundaries instead of calling a pure function distributed-safe.

## The endpoint is a calculator boundary

The inspected endpoint constructs entitlements from the request and passes request-supplied consumed and requested values into the evaluator. It does not load a tenant plan or usage ledger in that body. The route includes tenantId, but the evaluator's decision is derived from the supplied collection. A route parameter's presence is not evidence that authoritative tenant state was consulted.

This can be a perfectly useful preview or teaching endpoint. The important requirement is naming and documentation that do not imply enforced consumption. A proposed production gate would obtain trusted entitlement and usage state, authorize the caller, then execute the atomic admission protocol. Those changes require more than renaming the response field.

## Build a discriminating test set

Cover exact fit, one unit over, already over limit, missing feature, unlimited feature, zero allowance, invalid consumed value, and invalid requested value. Add duplicate normalized features to expose collection ambiguity. For each test, assert Allowed, Remaining, and Reason together. A test that checks only the Boolean may miss a misleading remaining balance.

Then create the two-caller schedule as a separate model test. It should show both preflights passing and explain why that is expected for the pure evaluator. A proposed reservation implementation gets its own acceptance test asserting only one accepted reservation. Do not modify the evaluator test to pretend it owns state it never receives.

## Exercise MF-12A: explain remaining quantity

Write a decision table for exact fit, rejection, unlimited, and missing feature. State whether Remaining is hypothetical after-request capacity, unchanged capacity, or unbounded. Include a client rendering rule that does not convert null into zero.

## Exercise MF-12B: expose duplicate feature ambiguity

Construct two entitlements whose names normalize to the same feature. Predict the lookup behavior and propose a collection-level validation policy. Compare rejecting duplicates with deliberately merging them, including the meaning of one finite and one unlimited entry.

## Exercise MF-12C: design enforceable admission

Work the two-caller schedule with capacity ten and consumption six. Specify an operation identity, shared concurrency rule, and replay response for a proposed reservation service. Identify what the current evaluator contributes and what it does not prove.

## Review checkpoint

The evaluator is useful precisely because its calculation is small and inspectable. Keep that strength while refusing to attribute persistence, authentication, or reservation to it. A strong design composes the pure decision with explicit state authority instead of hiding stateful promises behind a Boolean.
