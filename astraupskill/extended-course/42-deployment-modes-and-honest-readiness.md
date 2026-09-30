# 42. Deployment modes and honest readiness

Read [Program](../../src/MeterForge.Api/Program.cs) and [PostgresInvoiceSnapshotStore](../../src/MeterForge.Infrastructure/Persistence/PostgresInvoiceSnapshotStore.cs). The current host explicitly chooses in-memory stores and a simulated provider, while exposing metadata that says the default is not durable. This chapter designs a proposed deployment-mode contract so demonstrations, integration environments, and durable deployments cannot be confused by operators or clients.

The prerequisite is the composition-root lesson. A class being compiled into a solution does not mean it is registered or invoked. Readiness must describe the selected mode and its required dependencies, not the most capable adapter file in the repository.

## Name capabilities rather than environments alone

Labels such as Development and Production are useful configuration categories, but they do not by themselves establish durability or real provider use. A production-named process could still use in-memory stores if registration does not change. A test environment could use a real disposable database. Capabilities should be derived from explicit composition and validated configuration.

A proposed mode descriptor can identify storage kind, durable acceptance support, provider mode, authentication policy, and enabled background workers. It should not expose connection strings or credentials. The purpose is to make operational assumptions inspectable, not to publish sensitive configuration.

The current readiness response's storage and durableDefault fields are a useful example of honest scope. The postgresAdapterAvailable field indicates availability, not active registration or database connectivity. A client or dashboard should not treat that Boolean as proof that accepted usage survives a restart.

## Required dependencies follow enabled workflows

A preview-only demonstration may not require PostgreSQL or a real payment provider. A durable invoice-acceptance deployment does. A mode that enables webhook dispatch needs its persisted work store and approved destination configuration. Readiness should check dependencies required by the enabled workflow rather than every optional component in the codebase.

Do not make a health probe perform a financial effect. A provider readiness check should use an appropriate nonconsequential capability if available, or report configuration and recent operational evidence under a documented policy. Sending a charge to prove readiness would be a serious semantic mismatch.

Database checks should be bounded and distinguish connectivity from schema compatibility. Opening a connection proves less than verifying required tables or migrations. A schema check proves less than validating the application's replay protocol. The mode contract should say which level of readiness is being reported and keep deeper correctness tests outside frequent probes.

## Fail configuration early

A proposed durable mode should reject missing required configuration at startup rather than silently fall back to in-memory storage. Silent fallback can turn a temporary database problem into accepted-but-ephemeral business data. If fallback is a deliberate product feature, its restrictions and user-visible consequences must be explicit.

Similarly, a real-provider mode should not silently select SimulatedPaymentProvider because a credential is absent. A simulation response can look structurally successful while representing no real money movement. Clear mode selection and startup validation prevent that category of operational confusion.

The course does not add configuration or read secrets. A disposable configuration model can use booleans and synthetic endpoint labels to test required combinations. The learner's submission should contain no live connection strings or provider material.

## Observe restart behavior honestly

In-memory mode loses its stores when the process is replaced. A readiness response after restart can still say ready for new demonstration work while old accepted resources are absent. That behavior should be documented so users do not mistake the demo for durable billing history.

A durable mode's restart rehearsal should accept a synthetic operation, stop and recreate the host against the same disposable store, and read the accepted result. This is stronger evidence than restarting a service object within one process while retaining the same singleton dictionary. The test should identify exactly which state survived and where it lived.

Cross-process behavior is another distinct test. Two hosts sharing the same durable authority should preserve one winner for an operation identity. Two independent in-memory hosts do not share dictionaries. A load balancer can therefore expose inconsistent readback if requests land on different instances. This follows from mode and topology, not from a bug in ConcurrentDictionary.

## Rollout and rollback contracts

Before changing modes, identify accepted data and active operations. A switch from in-memory to durable storage may require an explicit migration of available state. A rollback from durable to in-memory cannot simply discard newly accepted receipts without breaking replay and readback. The rollout plan should preserve an authoritative source or restrict accepted operations during transition.

Feature flags should gate complete workflows, not half of an acceptance protocol. Enabling a new endpoint while leaving its durable receipt store disabled can produce inconsistent guarantees. A mode validation test can assert that required components are enabled together.

The same principle applies to background workers. An outbox row without an active dispatcher can be recoverable pending work if the mode reports it honestly. A dispatcher without the intended persistence protocol can lose work on restart. Readiness and operational metrics should help distinguish backlog from lost work.

## Exercise MF-42A: define capability metadata

Design a safe mode descriptor for preview, integration, and durable acceptance modes. Include active storage and provider semantics without secrets. Explain why adapter availability is different from adapter selection and connectivity.

## Exercise MF-42B: reject unsafe fallback

Specify startup validation for a proposed durable mode with missing storage or provider configuration. Compare fail-fast behavior with an explicitly restricted fallback mode. State the user-visible guarantee that must not change silently.

## Exercise MF-42C: rehearse topology

Plan a disposable restart test and a two-host same-key test. Identify retained state, expected readback, and mode-specific limitations. Explain why a ready response alone proves neither historical persistence nor cross-process winner semantics.

## Review checkpoint

Operational honesty begins with naming the implementation actually selected. A strong deployment contract connects enabled workflows to required dependencies and refuses silent changes in durability or provider meaning. Readiness is useful evidence when its scope is explicit, not when it promises the whole system is correct.
