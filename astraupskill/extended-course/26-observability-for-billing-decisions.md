# 26. Observability for billing decisions

Use [BusinessRuleExceptionHandler](../../src/MeterForge.Api/Errors/BusinessRuleExceptionHandler.cs), [PaymentService](../../src/MeterForge.Application/Payments/PaymentService.cs), and [InvoiceSnapshot](../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs) to design a proposed diagnostic vocabulary. The host already exposes trace identifiers in selected error evidence, but this chapter does not claim that all of the following events or metrics are implemented. The goal is to make billing decisions explainable without leaking payloads or confusing attempts with accepted outcomes.

The prerequisite is the distinction among calculation, acceptance, collection, and notification. A single metric named billing_success cannot explain which boundary succeeded. An incident can contain an accepted invoice, an ambiguous payment, and a failed webhook simultaneously. The diagnostic model must allow those facts to coexist.

## Start with support questions

Suppose a customer says a retry produced two invoices. The relevant evidence includes billing operation identity, accepted document identity, replay classification, and any conflicting payload fingerprint. A request trace alone may show two HTTP calls but not whether they refer to one accepted business operation. Correlation should connect transport attempts to stable business identity.

Suppose the reported amount differs from a preview. The investigation needs selected usage, price version, discount scope, tax calculation version, and accepted line evidence. Logging only the final total cannot distinguish a rounding policy change from a different input set. Preserve the accepted calculation references rather than dumping raw usage payloads into logs.

Suppose the provider charged but the application returned an error. The diagnostic bundle needs provider operation key, local attempt identity, the last confirmed phase, and any reconciliation result. It must preserve uncertainty after a timeout. A log event called payment_failed emitted for every exception would erase the distinction between explicit rejection and unknown external outcome.

## Name events by confirmed boundaries

Use separate names for calculation-started, quote-requested, quote-accepted, document-acceptance-attempted, document-accepted, collection-requested, collection-outcome-unknown, and collection-confirmed. These are proposed examples. Each event should state whether it records an attempt or a confirmed effect and when it is emitted relative to that effect.

An event emitted before CommitAsync cannot honestly prove durable document acceptance. An event emitted after commit can still be lost before reaching a logging system. Therefore operational logs should support diagnosis, while the durable receipt remains the authority for replay. The two records have complementary roles rather than interchangeable guarantees.

Trace identifiers help connect one request's logs and response. A replay can have a new trace identifier while preserving the same business operation identity. Store both relationships. Do not reuse a request trace as the only idempotency key, because transport retries and accepted operations have different lifetimes.

## Metrics need units and denominators

Count accepted documents separately from document requests. Count exact replays and conflicting replays separately. A high replay count may indicate healthy retry behavior under network instability rather than duplicated billing. A conflict rate should use an appropriate denominator and retain the machine error category for investigation.

Payment metrics should distinguish provider requests, explicit rejections, confirmed successes, and unresolved outcomes. A success percentage that excludes unresolved outcomes can look healthy while a growing reconciliation backlog harms users. Pair flow counters with a gauge of unresolved operations and an age distribution under a defined clock policy.

Keep metric labels bounded. Tenant or invoice identifiers can create unbounded cardinality and expose information in operational tooling. Use phase, outcome category, adapter mode, and selected bounded error codes as labels. Put individual identifiers in access-controlled diagnostic records when needed, subject to the deployment's data policy.

## Work an incident timeline

Operation B is accepted as invoice I. A collection request reaches the provider and the connection fails before a response. The caller receives an error. A second request retrieves the accepted invoice but sees unresolved collection state. A reconciliation task later confirms the existing provider effect. The final state is one invoice and one confirmed collection, with multiple transport attempts.

The incident report should state each confirmed fact and its source. Database receipt confirms invoice acceptance. Provider evidence confirms collection. Logs explain timing and attempted calls. A missing success log is not proof that either effect was absent. This source-by-source structure prevents a plausible narrative from outrunning the evidence.

Now alter the timeline so two different billing operation identities were created for the same intended customer action. The problem is no longer merely a provider timeout. The diagnostic model should expose the identity split and the application boundary that generated new operations. Reconciliation cannot reliably infer intent from equal amounts alone.

## Protect diagnostic data

Avoid logging authorization material, provider credentials, full webhook payloads, or customer content. A source reference or description can also contain sensitive information in a real system, so decide what is appropriate rather than assuming every domain field is safe. Hashes and internal identifiers can reduce exposure but still need a clear purpose and access policy.

Structured error categories are preferable to parsing human-readable messages. The current BusinessRuleException code is a useful starting point. Preserve the code and trace relationship while keeping public detail appropriate to the endpoint. A monitoring rule should not break when punctuation in an explanatory message changes.

A diagnostic export should be read-only and scoped to the requested operation or tenant under proper authority in a future secured host. It should not become a hidden repair tool. Keep proposed repair actions separate and reviewable, especially when they can create credits, refunds, or external calls.

## Exercise MF-26A: define the event vocabulary

Choose six events covering document acceptance and collection. For each, name the observation point, stable business identity, request trace relationship, and one conclusion it cannot support. Include an unknown-outcome event rather than mapping every exception to failure.

## Exercise MF-26B: design operational metrics

Define counters and gauges for accepted documents, exact replay, conflicting replay, unresolved collection, and reconciliation age. State units, bounded labels, and denominators. Explain how healthy retries differ from duplicated business operations.

## Exercise MF-26C: write an evidence-based incident report

Use the lost-provider-response timeline and separate facts, hypotheses, and next checks. Include two competing explanations for a missing confirmation event. Avoid payload secrets and do not prescribe a new charge until operation identity and provider evidence are resolved.

## Review checkpoint

Billing observability should make uncertainty explicit and trace accepted meaning across attempts. A strong report can explain why two requests do not necessarily mean two invoices, why an exception does not prove no charge, and which durable record is authoritative for each conclusion.
