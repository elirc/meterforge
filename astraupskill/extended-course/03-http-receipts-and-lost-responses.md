# 03. HTTP receipts and lost responses

The ingestion service produces an event and an attempt classification. The HTTP endpoint turns those into status codes, a response body, and sometimes a Location header. This chapter follows that translation and examines what a client can infer after a timeout. It also separates tenant-scoped lookup from verified caller authority, because those concerns are easy to conflate in a route that contains a tenant identifier.

Read [UsageEndpoints](../../src/MeterForge.Api/Usage/UsageEndpoints.cs), [Program](../../src/MeterForge.Api/Program.cs), and [the API tests](../../tests/MeterForge.Api.Tests/Usage/UsageApiTests.cs). The source defines POST and GET routes beneath a tenant segment. POST delegates to UsageIngestionService. GET asks IUsageEventStore to find an internal event Id under the supplied tenant. The current course does not alter these endpoints.

## A creation response contains a follow-up path

When IsReplay is false, POST returns Created with a relative Location containing tenant and stored event Id. The response body includes the event payload and the attempt flag. When IsReplay is true, POST returns Ok with the stored event. These are distinct successful outcomes, not two different business facts.

A client that receives a creation response can follow Location to read the stored event. That GET is scoped by tenant and internal Id. The in-memory store searches its values for both fields. A lookup under another tenant returns no match even if the internal Id is known. This supports a tenant-scoping claim at the repository boundary, but the route value itself is not proof that the caller is authorized to use that tenant.

The GET response sets IsReplay false. That is consistent with treating replay as a classification of ingestion attempts rather than a property saved on the entity. A client should not compare a GET body and a replay POST body and conclude the underlying fact changed solely because that flag differs. Its semantics need to be understood alongside the operation that produced it.

## Trace four client observations

In the first timeline, the client sends a new event and receives Created. It knows the server accepted the fact and can retain the returned Id. In the second, the server accepts the event but the response is lost. The client knows only that its request outcome is uncertain. Retrying the same scoped external key and identical payload can recover the winner as a replay while the store remains alive.

In the third timeline, the client times out before insertion and retries. The retry may become the creation. The client should accept either a creation or replay outcome as successful recovery of one intended fact. Treating a replay as an error would make ordinary network ambiguity hard to resolve. Treating a new external key as a retry would instead risk recording the fact twice.

In the fourth timeline, the server restarts between attempts. The default dictionary no longer contains the first event. An identical retry may become another creation. The endpoint's replay behavior therefore does not establish a durable receipt across process lifetimes. The readiness response makes this limitation visible, but a client integration still needs to understand it.

## A conflict is not a transport failure

Reusing a scoped key with changed business data leads to the stable usage.idempotency_conflict business error. That is different from a timeout with unknown outcome. Repeating the same conflicting payload does not repair the disagreement. The producer should investigate whether it reused a key incorrectly, changed the fact after transmission, or needs an explicit correction workflow.

An integration design should classify retryable uncertainty separately from deterministic rejection. Blindly retrying every non-success response can amplify load without making progress. The correct policy depends on the API error mapping and client context; this chapter does not prescribe retries for every exception. It asks for a concrete decision table that preserves the original business intent.

Store the request identity before sending if a client needs recovery after its own restart. Generating a new key each time a network function runs loses the ability to correlate attempts. A proposed producer-side pending-event journal can retain the scoped key and immutable payload until a response resolves the outcome. That journal is a new feature design, not a component currently supplied by this ingestion service.

## Authority must be added deliberately

The inspected Program registers services and maps routes, but does not establish a production authentication and tenant-authorization flow for these endpoints. A future design must bind the caller's permitted tenant context to the requested route. Merely passing tenantId to the store prevents accidental cross-tenant lookup within that call; it does not establish that the caller may choose that value.

Design tests at the layer that enforces the claim. A store test can prove that tenant A cannot retrieve an event by asking the store under tenant B. A hosted authorization test is needed to prove that a principal cannot submit another tenant's route. A direct service call with a string cannot establish the latter. The distinction protects both security reasoning and test scope.

If the product permits operators to act across tenants, define that authority explicitly rather than making all callers unrestricted. A claim, policy, or tenant-resolution service should have a clear source of truth and tests for allowed and rejected transitions. This is proposed work for later installments, not a claim of delivered authentication.

## Design a receipt packet for debugging

A useful synthetic incident record includes the intended scoped external key, immutable business payload, attempt sequence, observed statuses, returned stored Id, and process-lifetime assumptions. It does not need credentials or full request headers. A trace identifier can help connect server logs, but it identifies an execution rather than the producer's durable fact.

Use different labels for producer key, stored Id, and trace Id. If all three are called request ID in a report, a reviewer cannot tell whether a retry should preserve or replace them. The producer key should remain stable for the fact; a stored Id comes from the winning event; a trace Id may change for every HTTP attempt. Their lifetimes explain their different roles.

## Exercise MF-03A: write a recovery table

Describe client actions after Created, replay Ok, a deterministic payload conflict, and a timeout with unknown outcome. For each, state which identifiers should remain stable and what additional observation can resolve uncertainty. Include a process restart between attempts and explain the current limitation.

## Exercise MF-03B: distinguish scope from authority

Specify one repository test for tenant scoping and one proposed hosted test for tenant authorization. Give fixtures that would fail if the relevant boundary were broken. Explain why the first test cannot substitute for the second and why a route tenant string is not itself authenticated identity.

## Exercise MF-03C: design a producer journal

Propose a minimal pending-event journal that survives client restarts. Define when the external key is generated, which fields become immutable, and when an entry is marked resolved. Include a timeout, a replay response, and a conflict. Do not implement it in the server or claim it solves the server's in-memory lifetime limitation.

## Review checkpoint

A strong answer explains what each response proves and what a missing response leaves unknown. It treats retry identity as a producer responsibility, recognizes the server's current lifetime boundary, and chooses authorization evidence at the appropriate layer. It can communicate a failure without merging transport uncertainty, payload conflict, and permission rejection into one generic error.
