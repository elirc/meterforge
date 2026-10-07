# Learner guide

This page is the map of the repository: what to read first, how one request travels through the code, who is allowed to do what, and which commands produce real evidence. The detailed lessons live elsewhere; this page tells you where.

## Where the learning material is

| Folder | What it is | When to use it |
| --- | --- | --- |
| [astraupskill/](astraupskill/README.md) | The main course: the usage-ingestion slice, a worked repair, testing, practice with solutions, a trace lab, and a 44-chapter extended course with 12 review pages | Start here |
| [docs/sprints/](docs/sprints) | Notes for the fifteen original sprints | Reference after you can trace a request |
| [docs/quality/](docs/quality/PRODUCTION-READINESS.md), [docs/security/](docs/security/THREAT-MODEL.md), [docs/operations/](docs/operations/RUNBOOK.md) | Readiness gaps, threat model, runbook | When a lesson asks "is this production-safe?" |
| [learn/](learn/README.md) and [training/](training/README.md) | A SQL Server apprenticeship pack (B07/B08) written against an **older snapshot** of `main` (`43ee18d`), with lab code on branches that are not on GitHub | Read the status note at the top of each README first |
| [TESTREADME.md](TESTREADME.md), [OEReadMe.md](OEReadMe.md) | How to choose tests; what is over-engineered here and why | Alongside lesson 04 and the sprint notes |

## Reading order for the code

Read these nine files in order. Each one answers one question. Lesson [01](astraupskill/01-CODEBASE-MAP.md) asks the same questions in more depth.

1. `src/MeterForge.Api/Program.cs`: what is registered and with which lifetime (stores are singletons at lines 31–34, services are scoped at lines 35–39), and which endpoint groups are mapped (lines 63–73).
2. `src/MeterForge.Contracts/V1/Usage/UsageContracts.cs`: the public request and response shapes.
3. `src/MeterForge.Api/Usage/UsageEndpoints.cs`: HTTP binding and the 201/200 choice.
4. `src/MeterForge.Domain/Usage/UsageEvent.cs`: validation and normalization in the constructor.
5. `src/MeterForge.Application/Usage/UsageIngestionService.cs`: the replay/conflict policy.
6. `src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs`: where the single winner is chosen.
7. `src/MeterForge.Api/Errors/BusinessRuleExceptionHandler.cs`: how a domain error becomes 409 or 422.
8. `tests/MeterForge.Application.Tests/Usage/UsageIngestionTests.cs`: the race, replay, conflict, cancellation and tenant tests.
9. `tests/MeterForge.Api.Tests/Usage/UsageApiTests.cs`: the same behaviour through the real ASP.NET pipeline.

Then pick any other feature folder (for example `Subscriptions/` in each project) and repeat the same order: Program → contract → endpoint → domain → service → store → tests.

## One request, traced

`POST /api/v1/tenants/tenant-a/usage-events` with an `externalEventId` (line numbers verified against `main` @ `4e3f344`):

| Step | Where | What happens |
| --- | --- | --- |
| 1 | `UsageEndpoints.cs:17-18` | The route binds `tenantId` from the URL and `IngestUsageRequest` from the JSON body. |
| 2 | `UsageEndpoints.cs:20-21` | The endpoint calls `UsageIngestionService.IngestAsync`. |
| 3 | `UsageIngestionService.cs:23` | A `UsageEvent` candidate is built with a new `Guid`. |
| 4 | `UsageEvent.cs:9-12` | Missing IDs or fields, or quantity ≤ 0, throw `BusinessRuleException` (`usage.identity_required`, `usage.field_required`, `usage.quantity_invalid`). |
| 5 | `UsageEvent.cs:17, 19-20` | `Meter` and `ExternalEventId` are trimmed and `OccurredAt` is converted to UTC, **before** the key is used. |
| 6 | `UsageIngestionService.cs:24` → `InMemoryUsageEventStore.cs:17-22` | `GetOrAddAsync` calls `ConcurrentDictionary.GetOrAdd` keyed by `(TenantId, ExternalEventId)`. The returned value is the winner, whether or not it is this request's candidate. |
| 7 | `UsageIngestionService.cs:25-29` | If the winner's subscription, meter, quantity or time differ from the candidate, it throws `usage.idempotency_conflict`. |
| 8 | `UsageIngestionService.cs:30` | `IsReplay` is true when the winner's `Id` is not the candidate's `Id`. |
| 9 | `UsageEndpoints.cs:22-24` | Replay → `200 OK`; first write → `201 Created` with a `Location` that the GET at lines 10-16 can read. |
| 10 | `BusinessRuleExceptionHandler.cs:13-14`, `Program.cs:43-53` | A conflict becomes `409`; every other business rule becomes `422`. The `X-Trace-Id` header (set in `OnStarting`) and the Problem Details `traceId` both use `HttpContext.TraceIdentifier`. |

To check your own trace, run the application tests listed below. `Ingest_NormalizedReplay_ReturnsOriginalIdentity` covers step 5, `Ingest_CompetingServices_ReturnExactlyOneCreationAndOneStoredIdentity` covers step 6, and the four `Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal` cases cover step 7.

## Who may do what (authorization table)

There is **no authentication or authorization** in this API. `grep -rn "Authoriz" src` finds nothing. The tenant is whatever the caller puts in the URL. Tenant scoping protects data from accidental mixing. It does not prove who the caller is.

| Endpoint (all under `/api/v1/tenants/{tenantId}`) | File | Identity check | Other guard |
| --- | --- | --- | --- |
| `POST /usage-events`, `GET /usage-events/{id:guid}` | `Usage/UsageEndpoints.cs` | none; tenant from route | idempotency key `(tenant, externalEventId)`; GET filters by tenant |
| `POST /products`, `GET /products/{productId:guid}` | `Catalog/ProductEndpoints.cs` | none | tenant passed to the service |
| `POST /subscriptions`, `GET /subscriptions/{id:guid}` | `Subscriptions/SubscriptionEndpoints.cs` | none | tenant passed to the service |
| `PUT /subscriptions/{id:guid}/product` | `Subscriptions/SubscriptionEndpoints.cs:22-30` | none | `If-Match` required (400 `subscription.if_match_required`); stale version → `subscription.version_conflict` (thrown at `InMemorySubscriptionRepository.cs:35`, returned as **422**, not 409) |
| `POST /payments` | `Payments/PaymentEndpoints.cs:11-18` | none | `Idempotency-Key` header required (400), but `PaymentService.cs:18` creates a new attempt on every call. Duplicate collection is only prevented if the provider honours the key. |
| `POST /pricing/select`, `/rating/tiered`, `/discounts/preview`, `/entitlements/check`, `/invoices/finalize-preview`, `/webhooks/retry-preview`, `/reconciliation/payments` | one file per feature folder | none | stateless calculators over the request body; nothing is persisted |
| `GET /health/live`, `GET /health/ready` (no tenant) | `Program.cs:55-62` | none | readiness always reports `durableDefault = false` |

[docs/quality/PRODUCTION-READINESS.md](docs/quality/PRODUCTION-READINESS.md) and [docs/security/THREAT-MODEL.md](docs/security/THREAT-MODEL.md) discuss these gaps. Exercise 5 in [astraupskill/05-PRACTICE.md](astraupskill/05-PRACTICE.md) works on one of them: checking that the subscription belongs to the tenant.

## Tests: what exists and how to run it

51 test cases: 45 methods (41 `[Fact]` and 4 `[Theory]` with 10 `InlineData` rows), counted from the source. The recorded run in [astraupskill/VERIFICATION.md](astraupskill/VERIFICATION.md) observed the same 51.

| Project | Cases | Focus |
| --- | ---: | --- |
| `tests/MeterForge.Domain.Tests` | 26 | money, tiers, prices, invoices, payments, entitlements, adjustments, reconciliation |
| `tests/MeterForge.Application.Tests` | 16 | usage ingestion (9), subscription amendment, product service, invoice snapshot |
| `tests/MeterForge.Api.Tests` | 8 | real pipeline via `WebApplicationFactory<Program>` in the `Testing` environment |
| `tests/MeterForge.Architecture.Tests` | 1 | the Domain assembly references neither ASP.NET nor other MeterForge projects |

The SDK is pinned in `global.json`. These commands match `.github/workflows/ci.yml`:

```powershell
dotnet restore MeterForge.slnx --locked-mode
dotnet test MeterForge.slnx --configuration Release --no-restore
dotnet test tests/MeterForge.Application.Tests --no-restore --filter "FullyQualifiedName~UsageIngestionTests"
dotnet test tests/MeterForge.Api.Tests --no-restore --filter "FullyQualifiedName~UsageApiTests"
dotnet format MeterForge.slnx --verify-no-changes --no-restore
```

None of these tests needs Docker or PostgreSQL. `PostgresInvoiceSnapshotStore` is never registered in `Program.cs`, and no test runs it against a database. [astraupskill/04-TESTING-AND-DEBUGGING.md](astraupskill/04-TESTING-AND-DEBUGGING.md) has a per-class command table and a break-it experiment with its observed failures.

## Studying the original sprints

Once you can read a test and trace the request above, use the sprint notes like this:

1. Read the milestone and issues before the code.
2. Read commits in order and predict the next design pressure.
3. Start at a failing business-rule test, then trace the use case, adapter, endpoint, and contract.
4. Read review comments as design evidence, not style instructions.
5. Change one invariant and confirm the test fails for a useful reason.
6. Explain the transaction, tenant, retry, and compatibility boundaries in your own words.

Ask continuously: what business fact must remain true, who owns it, which failure can be retried, and what evidence would justify another abstraction?
