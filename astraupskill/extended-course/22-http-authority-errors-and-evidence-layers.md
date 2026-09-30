# 22. HTTP authority, errors, and evidence layers

Read [Program](../../src/MeterForge.Api/Program.cs), [BusinessRuleExceptionHandler](../../src/MeterForge.Api/Errors/BusinessRuleExceptionHandler.cs), [PaymentEndpoints](../../src/MeterForge.Api/Payments/PaymentEndpoints.cs), and the [usage API tests](../../tests/MeterForge.Api.Tests/Usage/UsageApiTests.cs). This chapter distinguishes routing, tenant scoping, authentication, business validation, and public error contracts. These layers often appear together in conversation, but the source must establish each one separately.

The inspected host registers in-memory repositories and the simulated payment provider. Its readiness response explicitly reports in-memory-default storage and durableDefault false. This is honest operational metadata. Do not describe the mere presence of a PostgreSQL adapter file as proof that the running default host uses it.

## A tenant route is not caller authentication

Endpoints use a tenantId route segment and pass it into services or repository lookups. That can scope data access by the supplied key. It does not by itself prove that the caller is authorized to choose that tenant. The inspected Program does not register and invoke an authentication and authorization pipeline for these routes.

The usage API test verifies that reading an accepted usage event through a different tenant route returns not found. This is valuable tenant-key isolation evidence. It is not a test of a signed identity claim or a user-to-tenant membership policy. State exactly what the test proves rather than weakening it or overstating it.

A proposed authenticated host would resolve an authoritative tenant context from verified identity and policy, then compare or replace the route value under a declared rule. It would test unauthenticated, wrong-tenant, authorized, and privileged cross-tenant paths separately. Those are future implementation requirements, not behavior supplied by the current route shape.

## Business errors have a selective mapping

BusinessRuleExceptionHandler handles BusinessRuleException and returns false for other exception types. It maps usage.idempotency_conflict to conflict and other business-rule codes to unprocessable entity. This means a subscription version-conflict code does not automatically receive the same special mapping merely because its name contains conflict.

The handler writes ProblemDetails with status, title, detail, code, and traceId. The host also customizes ProblemDetails and uses OnStarting to add X-Trace-Id. The usage test checks that the conflict response's header and body identifiers agree. This is a concrete observable contract that a direct domain test cannot establish.

Do not extrapolate the handler's mapping to every possible exception. A duplicate entitlement feature can produce a different exception type from a BusinessRuleException. A malformed successful provider response can trigger a business guard, while another provider exception may follow a different host path. A hosted fixture is needed to observe the resulting public response under the configured middleware.

## Manual responses form another path

The payment endpoint checks the Idempotency-Key header and returns BadRequest with a small anonymous object when it is missing. That response is not constructed by throwing BusinessRuleException through the custom handler. Its body shape can therefore differ from the ProblemDetails path. A contract review should include both paths rather than assuming all validation errors are uniform.

After the header check, the endpoint constructs Money and calls PaymentService. It forwards the key but does not create a local durable replay receipt in that body. The simulated provider returns a deterministic reference, yet repeated endpoint calls can create new local PaymentAttempt identifiers. The HTTP header name alone does not prove end-to-end idempotency.

A proposed public contract can standardize error envelopes and document which errors are retryable. It should preserve stable machine codes while avoiding sensitive internal details. The implementation must then be tested through the host, because endpoint returns and exception-handler returns are different control paths.

## Build an execution ladder

A domain test proves constructor guards and state transitions. An application test with a recording fake proves collaborator calls and their order. A repository test proves the chosen adapter's operation boundary. A hosted API test proves binding, routing, middleware, status, headers, and response serialization for that configuration. A deployed integration test adds topology and external-service behavior.

No layer is universally superior. Use the smallest layer that executes the mechanism being claimed. For example, a Money rounding assertion does not need a host, while trace-header agreement does. A host using an in-memory repository still cannot prove database durability or cross-process conflicts. The report should identify the adapter and environment used.

The existing usage API test is a good model of several connected observations: creation, Location readback, exact replay, conflicting replay, preserved original resource, and alternate-tenant absence. It checks the relationship among responses rather than merely asserting one status. New endpoint tests should similarly connect state and public evidence.

## Health responses are not business readiness proofs

The live endpoint returns a healthy response, and the ready endpoint returns descriptive default-storage metadata. Neither body performs a provider charge or a PostgreSQL query in the inspected mapping. A successful response proves that the host handled that route, not that every optional integration is available.

A proposed readiness policy should state which dependencies are required for the deployed mode. A demonstration using in-memory defaults may not require PostgreSQL. A durable billing deployment would need stronger checks and careful behavior when a dependency is unavailable. Avoid a health check that causes financial effects or performs expensive work on every probe.

## Exercise MF-22A: classify authority evidence

Compare tenant-key lookup isolation with authenticated tenant membership. Use the existing usage test to state the proven claim and the missing claim. Design four hosted authorization cases for a proposed secured host without claiming they currently execute.

## Exercise MF-22B: map public errors

Trace a missing payment key, usage replay conflict, subscription version conflict, and non-business exception. Identify the exact response path supported by source and the cases requiring hosted observation. Explain why names alone do not determine status codes.

## Exercise MF-22C: build a test ladder

Choose a new billing endpoint and assign five claims to domain, application, repository, hosted, or deployed tests. Include a response header claim and a durability claim. State the adapter and environment each test must use.

## Review checkpoint

HTTP correctness is a relationship between authority, business state, and public evidence. A strong answer neither treats a tenant route as authentication nor dismisses useful tenant-scoping tests. It states the exact claim each layer supports and adds the missing layer deliberately.
