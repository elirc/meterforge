# 35. Cancellation and the point of accepted work

Read [UsageIngestionService](../../src/MeterForge.Application/Usage/UsageIngestionService.cs), [InMemoryUsageEventStore](../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs), [InMemorySubscriptionRepository](../../src/MeterForge.Infrastructure/Subscriptions/InMemorySubscriptionRepository.cs), and [PaymentService](../../src/MeterForge.Application/Payments/PaymentService.cs). CancellationToken appears across these APIs, but each implementation observes it at particular points. This chapter treats cancellation as a phase-dependent request rather than an automatic rollback mechanism.

The prerequisite is the operation-boundary model. An operation may be unstarted, validated, accepted locally, sent externally, or confirmed. A caller stopping its wait does not move every resource back to the first state. Correct recovery depends on knowing which boundary was crossed and which evidence remains discoverable.

## Pre-cancellation has a useful narrow guarantee

UsageIngestionService checks cancellation before constructing the candidate. The in-memory store checks again before GetOrAdd. A token already canceled at those points prevents the subsequent local acceptance path. This is a clear testable behavior, and a small fixture can assert that no candidate reaches a recording store after the service's initial check.

The check is not a continuous subscription to cancellation. If cancellation arrives after the store's check, the synchronous dictionary operation can still complete. The method returns a completed Task because it has no remote I/O in this implementation. A test should not assume that the Async suffix creates an interruption point between every line.

For a durable adapter, the cancellation semantics may differ. A database command can observe cancellation while a transaction outcome is uncertain. The application must still use stable operation identity to determine whether acceptance occurred. Replacing the adapter requires reviewing this aftermath, not merely forwarding the same token parameter.

## Waiting for a lock is another phase

The subscription repository checks cancellation before entering lock. The lock statement itself does not observe the token while waiting. A caller can become canceled after the initial check and still enter the critical section later. The current method does not recheck the token inside the lock before invoking the mutation.

A deterministic fixture can hold the mutation gate through one controlled operation while another caller begins waiting, then cancel the second token before releasing the first. Because the gate is private, a safe test should use public operations with controlled callbacks rather than reflection that changes application state. The experiment belongs in a copied-source or disposable repository instance.

The resulting policy question is whether a canceled waiter should still mutate after acquiring the lock. A proposed implementation can recheck inside, but that changes observable behavior and should be documented. It still cannot undo a mutation that was already accepted before cancellation. Distinguish preventing future work from reversing completed work.

## Payment cancellation crosses an external boundary

PaymentService constructs or mutates a local attempt before awaiting the provider. It passes the token to ChargeAsync. The provider decides where it observes that token. The simulated provider checks immediately, while a real adapter could be waiting on network I/O after an external service has already accepted the request.

A canceled request can therefore leave local and external evidence in different states. In RetryAsync, the local attempt has already moved from Failed to Pending and incremented AttemptNumber before the call. Cancellation does not automatically restore Failed. A proposed operation ledger should retain the last confirmed phase and reconcile an unknown provider outcome.

Avoid a catch block that always marks Failed and releases reserved collection or refund capacity on cancellation. If the external effect may have occurred, releasing capacity can permit another effect while the first remains unresolved. Cancellation is a reason to stop waiting; it is not authoritative evidence that the provider did nothing.

## Separate request lifetime from business lifetime

Some accepted business operations should continue after a client disconnects, while others should remain cancelable until acceptance. A proposed API can accept durable work and return an operation resource. Once accepted, a background worker owns its lifecycle under a separate token and recovery policy. The original request token should not be the only authority governing long-lived accepted work.

This proposal does not mean ignoring all cancellation. Before acceptance, cancellation can avoid unnecessary calculation. During graceful shutdown, workers can stop claiming new work and preserve recoverable claims. External calls can have bounded timeouts. The policy should name each phase and who owns the token at that phase.

A cancellation endpoint is also different from transport cancellation. A user command to cancel an unfulfilled business operation needs authorization, state preconditions, stable identity, and an outcome. It may race with acceptance or external execution. Closing a browser tab does not automatically issue that business command under the current API.

## Design a phase table

For usage ingestion, list before validation, after candidate creation, before atomic store acceptance, after acceptance, and before response. For payment, add before provider send, after possible external effect, and after local confirmation. For each phase, state what is known, what can still be prevented, and what retry should query.

Use controlled gates rather than sleeps. A recording collaborator can signal that a phase was reached and wait for the test to release it. The test then cancels at an exact point and observes state and calls. This is more informative than hoping cancellation lands during a fast dictionary method.

Keep test names precise. PreCanceledUsageDoesNotCallStore is a narrow useful claim. CancellationAlwaysRollsBackBilling is an unsupported promise across multiple resources. The report should identify the adapter and whether the test modeled external acceptance or only local cancellation propagation.

## Exercise MF-35A: map observation points

List the cancellation checks in the four source paths and identify synchronous work between them. Explain what a pre-canceled token proves and what cancellation after the final check may still allow.

## Exercise MF-35B: test a canceled lock waiter

Design a disposable two-caller fixture using controlled callbacks to hold the repository mutation gate. Cancel the waiting caller after its initial check. Predict current behavior and a proposed recheck policy, including the distinction between waiting cancellation and accepted mutation.

## Exercise MF-35C: separate business cancellation

Define a proposed cancel-operation command for accepted billing work. Include authority, state race, stable identity, and response evidence. Explain why request-token cancellation cannot substitute for that command after an external effect may have occurred.

## Review checkpoint

Cancellation handling is correct when phase, ownership, and surviving effects are explicit. A strong answer knows where a token is observed and where authoritative acceptance must be queried. It does not promise rollback from a parameter or erase an ambiguous external outcome.
