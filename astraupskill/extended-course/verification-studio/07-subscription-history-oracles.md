# 7. Derive a subscription history without replaying the code

The central source for this workshop is [Subscription](../../../src/MeterForge.Domain/Subscriptions/Subscription.cs). Read its public operations and then close the file while constructing a state table. This separation matters: a model that duplicates every implementation branch can repeat its mistakes. Your table should describe allowed transitions, observable state and version effects using domain vocabulary.

## The initial condition is part of the contract

A subscription with no trial end starts Active at version one. A subscription with a trial end starts Trialing at version one, including a trial end equal to the start date. A trial end before the start date is rejected. The inspected constructor does not automatically activate a subscription because wall-clock time has passed its trial end. It stores dates and chooses initial status from the presence of the trial end.

That difference gives you two independent axes for the fixture: date validity and initial status. Do not replace a nullable date with a Boolean named expired and then treat that as the source contract. A test clock does not cause a transition unless an actual component consumes it. A test that waits until tomorrow would be slow and would test no timer in this class.

Write four initialization examples: no trial, trial ending on the start day, trial ending later and trial ending earlier. Use fixed calendar values. Expect the first three to construct, with Active, Trialing and Trialing respectively; expect the fourth to reject. Keep constructor identity and tenant validation in separate examples so a wrong date does not mask an empty identifier.

## Build the transition matrix

Across the four statuses, Activate is allowed only from Trialing, Pause only from Active, and Resume only from Paused. Cancel is allowed from every status except Cancelled. ChangeProduct is allowed from every non-cancelled status if the supplied product identifier is nonempty. Each accepted operation increments Version once. Rejected operations validate before the assignments in these methods and therefore leave that operation's state unchanged.

Now distinguish a rejected single operation from a failed sequence. Pause followed by Activate has one successful operation and one rejection. The first change remains unless an enclosing boundary supplies rollback. An answer that says the whole sequence is atomic merely because each method validates its input has confused two levels of behavior.

Use a pencil table with columns step, status before, version before, command, result, status after and version after. Keep the current product in an additional column for amendment scenarios. This table should be readable independently of any test framework. When implementation and oracle disagree, the row identifies the first divergence rather than merely showing that a final assertion failed.

## Worked history

Start an Active subscription at version one using product A. Pause succeeds, producing Paused at version two. ChangeProduct to B succeeds while paused, producing Paused at version three. Resume succeeds, producing Active at version four. Activate now rejects because the subscription is no longer Trialing; version remains four. Cancel succeeds, producing Cancelled at version five. A second Cancel rejects and a ChangeProduct to C rejects. The final product remains B and version remains five.

Notice that the product change did not implicitly resume service, and cancelling did not erase the product identity. Those are distinct observables. A weak test asserting only the final version could miss either mistake. A good history checks the status and product after every accepted step, and preservation after every rejection.

## Same-product amendments

Calling ChangeProduct with the already selected product still increments Version in the inspected implementation. The method has no equality early return. This is a useful characterization because teams may disagree about whether a semantic no-op should consume a version. Do not silently change the oracle to your preference.

Write two specifications. The current one says every accepted ChangeProduct command advances the version. A proposed alternative says changing to the same product preserves the version. Discuss command receipt behavior under each. If a client retries an amendment after losing its response, an equality check alone does not identify whether the original command ran or whether a different actor happened to select the same product. State equality and command idempotency remain different concepts.

## Exhaustion and generated sequences

A small state machine is suitable for exhaustive short histories. Generate command sequences up to a chosen depth using fixed products A, B and the empty identifier. After each step, compare status, version and product against the independent table. A rejected command should not prevent the remaining sequence from running; record the rejection and continue from the unchanged state.

Keep the generator's limits explicit. Depth four explores many local interactions but says nothing about integer version overflow after enormous histories. It also does not establish repository atomicity, authorization, or durable persistence. Those belong to other boundaries. The experiment is strongest when its report names exactly what it covers.

## Exercise

Derive a Trialing history containing an accepted product change, a rejected Resume, an accepted Activate and a final Cancel. Add a same-product amendment before cancellation. Predict the version after every step. Then explain why a repository test is still needed even if all domain histories pass. Submit the table and one counterexample to the claim that every failed multi-step callback leaves state unchanged.

[Studio index](README.md) · [Review](reviews/03-subscriptions-and-snapshots.md)
