# 23. Composition roots and architecture-test limits

Read [Program](../../src/MeterForge.Api/Program.cs) and [DependencyDirectionTests](../../tests/MeterForge.Architecture.Tests/DependencyDirectionTests.cs). The host chooses concrete implementations, while the architecture test checks selected assembly references. Both are useful, but neither alone proves the full intended architecture. This chapter teaches how to reason about dependency direction, runtime composition, and evidence gaps without turning architecture into a collection of slogans.

The prerequisite is familiarity with interfaces such as IUsageEventStore, IProductRepository, and IPaymentProvider. These interfaces separate application calls from concrete adapters. Their existence permits substitution; it does not automatically make every substitution behaviorally equivalent or operationally suitable.

## The composition root selects behavior

Program registers the product, subscription, and usage stores as singleton in-memory implementations. It registers the simulated payment provider as a singleton and application services as scoped. These choices determine the default host's actual behavior. A production-looking adapter elsewhere in the tree is inactive unless composition selects it or another path invokes it.

Singleton lifetime means one registered instance is shared within the host's service provider. It does not mean one instance across all machines or process restarts. Scoped application services can all call the same singleton store while retaining their own per-request service objects. A concurrency claim must account for the adapter's internal synchronization, not merely its lifetime label.

Changing a registration can change semantics as well as persistence. An in-memory store may return shared references; a database adapter may return reconstructed objects. A local atomic method may rely on a lock, while a database implementation needs a transaction or conditional statement. Interface compatibility is necessary but not sufficient for contract compatibility.

## What the current architecture test checks

The test inspects referenced assemblies from the assembly containing Money. It rejects references whose names begin with Microsoft.AspNetCore or MeterForge. This protects the domain from selected outward project or web-framework dependencies. It is a concrete automated rule, not merely a diagram.

The rule does not inspect every source namespace or every package category. It does not prove that the application layer avoids infrastructure, that the API uses only approved contracts, or that all adapters satisfy behavioral requirements. It also does not verify runtime registration. A passing assembly-reference test should be reported as exactly that.

This precision makes the test more useful, not less. A narrow rule can be stable and easy to understand. Problems arise when a team describes a narrow test as proof of complete architectural purity and stops reviewing untested boundaries. A course submission should list both the enforced rule and plausible violations outside its scope.

## Interfaces carry semantic obligations

IInvoiceSnapshotStore exposes SaveFinalizedAsync. Its name suggests accepted finalized evidence, but the exact replay semantics need documentation and tests, as chapter 16 showed. IProductRepository.AddAsync does not state through its signature whether duplicate keys replace or reject. IPaymentProvider.ChargeAsync does not encode key retention or remote reconciliation capabilities.

A contract should describe outcomes, side effects, cancellation aftermath, and replay behavior in addition to parameter types. These semantics can be expressed through focused tests and documentation. Avoid adding a large interface simply to list every imagined future capability; begin with the obligations the application actually relies on.

For substitution, ask whether an application test that passes against one adapter remains meaningful against another. If it depends on reference identity or immediate process-local visibility, a database adapter may behave differently. Either the application is relying on an undocumented detail or the abstraction intentionally includes that detail. The review must decide which.

## A migration exercise in composition

Imagine replacing the usage store with a durable adapter. The application still needs exact replay and conflicting replay semantics. The durable adapter must preserve the accepted resource identity, not generate a fresh one after restart. It must enforce the same winner boundary across independent processes. Merely implementing the methods and registering the class does not establish those properties.

The migration plan should include a contract suite that runs against both adapters, plus provider-specific tests for durability and concurrency. It should also define how existing in-memory state is handled during deployment. A process restart loses that state; an adapter switch cannot retroactively make old unpersisted events durable. This is an operational fact to explain before rollout.

For product and subscription data, identity normalization and version semantics need the same attention. Database collation can differ from dictionary comparison. Transaction isolation can change conflict timing. A migration should state the canonical identity policy and conflict response rather than relying on accidental adapter behavior.

## Avoid architecture tests that mirror implementation

A test that asserts the exact current number of classes or methods is likely to resist harmless refactoring without protecting a business boundary. Prefer rules with an explained failure consequence. Preventing domain references to web types helps keep core rules executable without a host. Checking that a billing adapter uses accepted operation identity can protect replay semantics more directly than a folder-name convention.

Some rules belong in code review rather than brittle reflection tests. For example, whether a tax quote contains enough historical evidence requires semantic judgment. A static test can require a field, but it cannot prove the field is populated correctly or interpreted consistently. Combine mechanical checks with worked traces and contract tests.

The course's disposable labs exploit dependency direction constructively: selected domain and application files can compile without the full host when their dependencies are explicitly included. That is evidence of a useful seam for those files. It does not prove every file in the layer is equally isolated or that deployment configuration is correct.

## Exercise MF-23A: audit the architecture assertion

Write the exact rule enforced by DependencyDirectionTests and three claims it does not establish. Give one meaningful violation it would catch and one runtime composition mistake it would miss. Avoid describing untested architecture as proven.

## Exercise MF-23B: specify adapter substitutability

Choose IProductRepository or IInvoiceSnapshotStore. Define replay, duplicate, cancellation, ownership, and persistence semantics needed by a caller. Identify which current adapter behaviors satisfy the proposal and which require change or explicit limitation.

## Exercise MF-23C: plan a durable adapter switch

Design a staged migration from one in-memory store to a durable implementation. Include contract tests, cross-process evidence, normalization, existing-state limitations, and rollback behavior. Explain why changing dependency injection is only one step.

## Review checkpoint

Architecture is the set of boundaries that let important behavior remain understandable and substitutable. Tests can protect specific boundaries, while composition determines the implementation actually used. Strong submissions connect both to observable contracts and avoid treating a clean dependency graph as proof of durable business correctness.
