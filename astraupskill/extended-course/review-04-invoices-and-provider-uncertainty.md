# Review 04. Invoices and provider uncertainty

## MF-10A: trace a zero invoice

An empty draft cannot finalize. A zero-valued line is allowed because AddLine rejects negative amounts, not zero. With that line present, finalization succeeds and Total remains zero. PaymentAttempt rejects a nonpositive amount, so a workflow cannot blindly collect every finalized invoice through that constructor. A proposed zero-invoice branch can record that no provider collection is required while preserving the finalized document and its evidence.

Do not weaken the payment guard merely to make the two models look identical. They answer different questions. The invoice can document a billing period with no amount due, while a payment attempt represents a positive collection request. Full credit requires explaining that distinction and showing no provider call for the proposed zero branch.

## MF-10B: test evidence replay

Two AddLine calls create two distinct line identifiers and include both amounts in Total, even when SourceReference is identical. The current method does not enforce evidence uniqueness. A proposed cause identity can include the accepted usage operation and the rated period segment, allowing separate legitimate segments while rejecting the same accepted cause twice.

Description is unsuitable as the identity because two valid charges can share a label and one replay can use different wording. The test should hold cause identity constant while varying presentation text, then vary cause identity while holding description constant. A persistence-backed implementation also needs enforcement under concurrent additions, not just a sequential preflight query.

## MF-10C: design finalization persistence

After an in-memory Finalize followed by a failed save, calling Finalize again on the same object violates the Draft guard. A proposed application workflow can reload persisted state, discover whether the operation committed, and either return the accepted finalized snapshot or reconstruct the draft and finalize under the same stable operation identity. Preserve the originally accepted finalization time when replaying an already committed result.

A fresh invoice identifier on every retry can create duplicate business documents unless another invariant prevents it. An in-memory prototype can demonstrate the decision tree but cannot prove recovery after process loss. The review packet should name the durable receipt or uniqueness condition needed for that stronger claim.

## MF-11A: build the state table

Construction produces Pending at attempt one after validating identity, positive amount, tenant, and key. Succeed requires Pending and a nonblank reference, then stores the reference and marks Succeeded. Fail unconditionally sets Failed in the inspected body. Retry requires Failed, increments the attempt number, and sets Pending without changing the provider key.

Therefore Fail after Succeeded is currently permitted by that method, even if the normal service flow does not intend it. A provider result marked successful with a blank reference fails Succeed's guard. The null-forgiving operator does not bypass runtime validation. A stricter proposed state machine should have explicit tests for terminal-state transitions rather than relying on caller discipline alone.

## MF-11B: demonstrate ambiguous retry

RetryAsync mutates the Failed attempt to Pending and increments the number before awaiting the provider. If the fake records an effect and throws, the object remains Pending and the exception propagates. The key and amount remain unchanged. An immediate second RetryAsync fails its local Retry precondition because the attempt is no longer Failed.

Marking it Failed just to bypass that guard does not resolve whether the external effect happened. A safe protocol queries or reconciles by stable provider identity and verifies accepted parameters. The fake's effect record is the discriminating evidence in the teaching experiment. A throw-before-effect fake would exercise a different scenario and should be reported separately.

## MF-11C: design a durable collection receipt

The receipt binds a local operation identity to tenant, invoice, amount, currency, and provider key. Its states distinguish not-yet-sent, sent with unknown outcome, explicit failure, and accepted success under the chosen protocol. Recovery after external success and local-save loss queries the provider or another authoritative record, verifies the parameter match, and stores the existing reference rather than creating a new logical collection.

An outbox can preserve an intended call, but replay safety still depends on provider identity semantics. A dictionary survives neither a process restart nor independent callers by itself. Full credit requires stating the remaining uncertainty when the provider cannot answer and proposing an operational review path instead of claiming a mathematically impossible certainty from missing acknowledgments.
