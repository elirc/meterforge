# 9. Design schedules that explain concurrency

Concurrency learning improves when you replace repeated hopeful races with controlled schedules. Start from the expectedVersion check inside [MutateAsync](../../../src/MeterForge.Infrastructure/Subscriptions/InMemorySubscriptionRepository.cs). The check and callback run inside one shared lock. Your experiment should identify the property that this boundary is intended to protect and the assumptions under which it holds.

## Two amendments from one version

Create an Active subscription at version one. Prepare amendment A selecting product B and amendment B selecting product C, both expecting version one. Launch both through MutateAsync. Because each accepted ChangeProduct increments Version inside the locked callback, the expected outcome is one success and one subscription.version_conflict. The final version is two and the product corresponds to the successful amendment.

Do not require a particular winner unless your harness imposes an order. Scheduler timing does not promise that the task created first acquires the lock first. A robust oracle relates the final product to the recorded successful operation rather than hard-coding B. This is an example of an order-independent invariant.

## Sequential controls

Before the overlapping experiment, run A then B sequentially with the same expected version. This establishes the ordinary conflict path. Then run A expecting one followed by B expecting two; both should succeed, producing version three. These controls distinguish a concurrency failure from a basic fixture mistake such as using the wrong tenant or product identifier.

Include a missing-aggregate case. MutateAsync returns null when the tenant and ID do not identify a stored aggregate. That outcome differs from version conflict, even if an eventual API maps both to user-visible failure. Keeping repository outcomes separate helps the reviewer understand which branch was exercised.

## A barrier can deadlock the wrong design

A tempting harness places a two-party barrier inside each mutation callback. In this repository the first callback holds the single lock while waiting for the second callback, which cannot enter. The test deadlocks because its synchronization contradicts the implementation boundary. That does not prove the repository has a production deadlock.

Place start synchronization before the call, or use an external release signal for a single callback while observing that another call remains pending. Ensure every signal is released in a finally block and every wait has a finite diagnostic timeout. The timeout is a harness safeguard, not evidence that the underlying operation rolled back.

## Shared lock across aggregates

The lock field belongs to the repository instance, not an individual subscription. A deliberately slow callback for subscription A can delay a mutation to unrelated subscription B in the same repository. A controlled experiment can demonstrate this by holding A's callback on a test-owned signal and observing B has not completed before release.

Use this only in an isolated harness. Do not add artificial blocking to a running application. Report the finding as lock granularity and possible contention, not measured production throughput. A benchmark with trivial callbacks may conceal the issue; a worst-case blocked callback may exaggerate normal cost. Both observations need workload context.

## Cancellation timing

The method checks cancellation before acquiring the lock. A token cancelled before entry should reject. A token cancelled after that check while the call waits for the lock is not checked again in the inspected method. Therefore cancellation is not an atomic guarantee that no subsequent mutation can occur.

To test this boundary deterministically, the harness needs evidence that the second call passed its initial cancellation check before the token is cancelled. A simple sleep does not supply that evidence. You may need an instrumented copy or a controlled adapter for a precise schedule. Label a copy-based experiment as such and retain the original source hash. Do not claim the unmodified class exposes a synchronization hook that it does not have.

## Record histories, not just durations

For every operation record invocation, observed result and final state. A linearization argument proposes an order consistent with the method's atomic boundary and with real-time constraints. It is stronger than saying the test did not crash. In the two-amendment case, either A then B-conflict or B then A-conflict explains the intended history.

Add a deliberate outside-repository writer using a previously retrieved reference in a separate characterization. Now the same argument may no longer apply because not every mutation shares the lock. Keep that scenario separate so readers understand which assumption changed.

## Exercise

Design five schedules: same-version competition, fresh-version sequential success, unrelated-key blocking, pre-cancellation and callback failure after one mutation. For each, write one observation that supports the claimed property and one observation that would falsify it. Explain why an arbitrary delay cannot establish that a particular branch has already executed.

[Studio index](README.md) · [Review](reviews/03-subscriptions-and-snapshots.md)
