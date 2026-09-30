# Review 3: histories, ownership and snapshot guarantees

Use this guide for chapters seven through ten. These exercises are source-grounded designs and predictions unless a separate execution record says otherwise. They should not be confused with the usage and arithmetic assertions in the studio's executed harness.

## Trialing history answer

Begin Trialing at version one with product A. ChangeProduct to B succeeds and advances to version two without changing status. Resume rejects because the state is not Paused, leaving version two. Activate succeeds, producing Active at version three. A same-product ChangeProduct to B succeeds and advances to version four in the current implementation. Cancel succeeds, producing Cancelled at version five.

The same-product operation is the detail most likely to be assumed away. There is no equality early return in ChangeProduct. A proposed no-op policy can differ, but the current characterization must reflect the inspected code. Rejected operations in this sequence do not consume a version.

## Shared-reference answer

Two GetAsync calls for the same stored aggregate return the same instance in the in-memory repository. A public mutation through one reference is visible through the other. This demonstrates aliasing and the ability of such a caller to bypass MutateAsync's expectedVersion boundary. It does not by itself prove that an untrusted HTTP caller obtains such a reference.

The application review must trace who receives the aggregate and how it is mapped. A reviewer should reward this scope distinction. The repository observation is concrete; the reachability question requires separate evidence.

## Failed callback answer

A callback that pauses an Active subscription and then throws leaves the live object Paused with an incremented version, because the repository does not restore a prior copy. A callback that attempts invalid Activate from Active rejects before changing that operation's fields. Both throw, but their state effects differ.

This is the counterexample to the claim that every failed mutation callback rolls back. The lock serializes access through that method; it is not a transactional undo mechanism. A candidate redesign should explain ownership, copying and atomic replacement or durable transaction behavior explicitly.

## Schedule answer

Two same-version product amendments through MutateAsync can have either winner. One accepted change advances the version, and the later contender sees a mismatch. A valid oracle relates final product to the successful operation. A barrier requiring both callbacks to enter simultaneously would deadlock the harness because the first callback holds the shared lock.

The shared lock also serializes unrelated subscription mutations within one repository instance. A controlled blocked-callback test can demonstrate granularity, but it is not a production throughput measurement. Cancellation is checked before the lock; an exact later-cancellation schedule needs a suitable observation point rather than an arbitrary sleep.

## Invoice matrix answer

Draft conversion through From rejects. Empty finalization rejects. A zero-valued line is allowed, a negative line rejects, and a mismatched currency rejects. Repeated finalization rejects without replacing the original timestamp. A later AddLine rejects after finalization. These predictions follow the domain methods' guards and assignment order.

From copies line values into a new array of snapshot records. Replacing an element of that array through a compatible cast can change the snapshot collection view without changing the original invoice line. The public snapshot constructor can also bypass the From guard. Therefore immutable through the domain's public mutation methods is narrower than deeply immutable under every possible object construction and reference path.

## Review completion

Require a table identifying each guarantee's enforcing boundary. Domain transition validation, repository expectedVersion, reference ownership, snapshot factory validation and database persistence are separate rows. The learner passes when they can explain why a correct domain state machine still needs repository and persistence tests, and why a read-only interface type does not automatically establish deep immutability.

[Studio index](../README.md)
