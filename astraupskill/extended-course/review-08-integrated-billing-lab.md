# Review 08. Integrated billing lab

## MF-27A

The discount check proves reduction semantics, not invoice-wide allocation. Entitlement checks prove calculation and lookup behavior, not reservation or caller authority. The refund check proves that individual constructors lack cumulative context, not that a real refund service accepts over-refunds. The snapshot check proves array ownership behavior, not database mutability. The payment check proves local mutation before an exception, not a real provider charge. Reconciliation proves signed subtraction, not transaction correspondence.

These distinctions should appear in the learner's report. Passing a characterization that exposes a limitation is evidence for designing the next boundary. It is not a reason to label the limitation a desired production invariant. A proposed cumulative-refund service should reject the combined excess while preserving the existing constructor's useful local guards.

## MF-27B

At fifty percent, reduction and net are equal, so a test could pass under the wrong interpretation. At ten percent of 19.99, the correct reduction is 2.00 while the net is 17.99. The starter computes the latter role in the former method. Restoring the copied percentage expression repairs the intended defect without changing application source or public signatures. Record the expected nonzero starter exit separately from the successful repaired run.

## MF-27C

A successful provider result with blank reference reaches Succeed and fails its evidence guard. A repeated Retry from Pending fails Retry's state guard before another provider request. The test should assert both the exception code and whether the collaborator was called, so it distinguishes local rejection from external failure. Neither observation supplies a durable reconciliation protocol; that remains proposed work requiring stable operation state and provider correspondence.
