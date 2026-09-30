# 11. Payment attempts and ambiguous provider outcomes

Read [PaymentAttempt](../../src/MeterForge.Domain/Payments/PaymentAttempt.cs) and [PaymentService](../../src/MeterForge.Application/Payments/PaymentService.cs). The central distinction is between the local attempt object and the external provider's effect. A local status can describe what the application knows, but it cannot by itself prove whether an external charge happened. This chapter develops that distinction using the actual service calls and a controlled fake provider.

The prerequisite is chapter 10's invoice boundary. CollectAsync receives an invoice identifier and Money amount; it does not load an Invoice in the inspected method. It therefore does not independently verify finalization, invoice ownership, or agreement between the supplied amount and the invoice total. Those responsibilities must be traced through callers before making a complete endpoint claim.

## Start with the constructor contract

PaymentAttempt requires nonempty payment and invoice identifiers, a positive amount, and nonblank tenant and provider idempotency key. It stores those values and begins Pending with AttemptNumber one. A zero invoice cannot be collected through this constructor as a zero payment. A workflow can treat a zero finalized invoice as settled without a provider call, but that is a proposal rather than behavior supplied by this class.

The provider idempotency key remains unchanged by Retry. This is a valuable source fact: a retry can present the same external identity. It is not a complete guarantee that the provider deduplicates correctly. The provider's key scope, retention, parameter comparison, and response semantics determine what repeated requests mean. The local service passes a key; it does not implement every property of the remote service.

CollectAsync creates a new attempt identifier each time it is called. Two calls with the same provider key therefore create distinct local objects even if the provider treats them as one external operation. A durable application needs a policy for associating those local requests with one recorded outcome. An in-memory object returned to one caller is not a cross-process receipt.

## Trace explicit success and explicit failure

SendAsync calls ChargeAsync with the attempt's key, amount, and invoice identifier. A result with Succeeded true leads to Succeed with the provider reference. Succeed requires the local state to be Pending and the reference to be nonblank. The null-forgiving operator at the call site does not manufacture a valid reference; an invalid successful result can still fail the domain guard.

A result with Succeeded false calls Fail. Fail currently assigns Failed without checking the previous status. The normal service path calls it after a pending attempt, but the domain method itself permits other transitions. Characterize the method separately from the intended workflow. A public mutator with a permissive body should not be described as enforcing a strict terminal-state machine.

RetryAsync first calls Retry, which requires Failed, increments AttemptNumber, and returns the state to Pending. Only then does it send to the provider. This ordering means a provider exception after Retry has already changed the local object. The attempt number can advance even when no explicit success or failure result is received.

## Work the lost acknowledgment

Use a fake provider that records an external effect and then throws a simulated timeout. CollectAsync does not catch and translate that exception in the inspected method. The newly created attempt remains inside the failing call and is not returned normally. The caller sees an exception, but the fake's effect record shows that a charge-like effect may already have occurred.

Now use RetryAsync on a Failed attempt. Retry moves it to Pending before the same ambiguous provider behavior. After the exception, the caller still holds the object and can observe Pending with an increased attempt number. Calling RetryAsync again immediately is rejected because Retry accepts only Failed. This trace exposes a workflow gap for ambiguous outcomes; it does not authorize arbitrarily marking every timeout failed and charging again.

A proposed protocol introduces an Unknown or Reconciling outcome, or records enough operation state to query the provider using stable identity. The exact model can vary. The essential requirement is to distinguish explicit rejection from absence of an acknowledgment. Treating every exception as proof of no charge can create duplicate financial effects.

## Design the fake provider carefully

A useful fake records requests, supports named response modes, and exposes a controlled gate. Modes include explicit success with a reference, explicit failure, exception before any effect, effect followed by exception, and success with invalid reference. These modes test different contracts. A fake that throws immediately for every failure cannot model the most important ambiguity.

Record provider requests before deciding the outcome, including key, amount, and invoice identifier. Use synthetic data only. The test can assert that Retry preserves the key and amount while incrementing the local attempt number. It can also assert that a constructor failure prevents any provider request, proving validation precedes the external call for that path.

Do not make the fake silently smarter than the real abstraction. If it deduplicates requests, document the simulated rule. If it rejects key reuse with different parameters, write that as a proposed provider contract unless the actual provider implementation establishes it. Otherwise a passing application test may merely reflect guarantees invented by the test double.

## Local persistence and external identity

A durable collection workflow would persist an operation identity before sending, then record the provider outcome under that identity. It would need a recovery path for a process that exits between external success and local persistence. An outbox or command record can help preserve work, but it does not eliminate ambiguous provider outcomes by itself.

Suppose a persisted operation says Pending and the provider says the key already succeeded. Recovery should adopt the provider evidence under a verified parameter match rather than issuing a semantically new charge. If the provider cannot answer by key, the product needs another reconciliation mechanism and an operational policy. The course should not promise exactly-once collection from a local enum alone.

Tenant scope is also a protocol question. The provider request record in this service includes key, amount, and invoice identifier but not tenant as a separate field. A caller may encode tenant scope in the key or use provider accounts, but that is not established by this request shape. A review should trace the actual integration before claiming that identical keys from different tenants cannot collide.

## Exercise MF-11A: build the state table

List constructor, Succeed, Fail, and Retry preconditions and mutations. Include Fail after Succeeded and a provider success with a blank reference. Separate the method behavior from the normal service sequence and identify where a proposed stricter state machine would differ.

## Exercise MF-11B: demonstrate ambiguous retry

Start from Failed, invoke RetryAsync with a fake that records an effect and throws, and predict state, attempt number, key, and caller exception. Explain why another immediate RetryAsync is not the same as a safe provider reconciliation. Keep the experiment in a disposable harness.

## Exercise MF-11C: design a durable collection receipt

Define operation identity, provider identity, accepted parameters, local states, and recovery queries. Work a crash after provider success but before local save. State which evidence would let recovery adopt success and which uncertainty requires review. Do not label an in-memory dictionary as durable storage.

## Review checkpoint

The key lesson is epistemic: local failure to receive a result is not evidence that an external effect did not happen. A strong design preserves identity and uncertainty long enough to reconcile them. A strong test deliberately models effect-before-exception rather than treating every thrown call as harmless.
