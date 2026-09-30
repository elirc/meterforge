# MeterForge: reliable CRUD and usage ingestion

This course takes you from understanding a request to defending a change that behaves correctly under retries, competing requests and invalid input. It uses the actual MeterForge C# application. Start with the [project README](../README.md) for the wider billing product; use this folder for a detailed, practical route through the code.

You do not need to understand every billing module first. You should be comfortable with functions, classes, HTTP methods and basic SQL. If `Task<T>`, interfaces or dependency injection are unfamiliar, read the first two lessons slowly and reproduce each example before moving on.

## Prerequisites and glossary

**Prerequisites:** C# records and `async`/`await`; interfaces and constructor injection; 201, 200, 409 and the `Location` header; enough SQL to picture a unique index. The sibling `netgum` course is a natural predecessor.

| term | meaning | where in this project |
| --- | --- | --- |
| entity identity | the surrogate id the system assigns to a stored event | the `Id` property of `UsageEvent` in `src/MeterForge.Domain/Usage/UsageEvent.cs` |
| request identity | the caller's own key for the fact, which makes a retry recognisable | `ExternalEventId` on `UsageEvent`, keyed with `TenantId` |
| normalization | trimming and converting before the key is compared | `meter.Trim()` and `occurredAt.ToUniversalTime()` in the `UsageEvent` constructor |
| validation guard | constructor rules that make an invalid event impossible to build | the `BusinessRuleException` throws in the `UsageEvent` constructor |
| atomic get-or-add | one operation that stores or returns the winner; never check-then-write | `GetOrAddAsync` in `src/MeterForge.Application/Usage/UsageIngestionService.cs` |
| winner | the single stored event a race resolves to, seen by every caller | `_events.GetOrAdd` in `src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs` |
| replay | an identical retry answered 200 with the original event | `IsReplay` on `UsageIngestionResult` in `UsageIngestionService.cs` |
| idempotency conflict | the same key with different business data, which is a 409 | the `usage.idempotency_conflict` throw in `UsageIngestionService.IngestAsync` |
| error code | the stable contract string mapped to a problem response | `BusinessRuleException` in `src/MeterForge.Domain/Common/`, handled in `src/MeterForge.Api/Errors/` |
| tenant scope | the tenant id in the route and in every key; scoping is not authorization | the `tenantId` route parameter in `src/MeterForge.Api/Usage/UsageEndpoints.cs` |
| readable Location | the GET that makes the URL returned by a 201 resolvable | the tenant-scoped GET and `Results.Created` in `UsageEndpoints.cs` |
| durable default | the honest signal that the default store is memory and loses events | `durableDefault = false` in `src/MeterForge.Api/Program.cs` |

## What has been implemented

Usage ingestion now makes the repository choose one stored event atomically. Identical retries return that event with `IsReplay = true`; a reused event key carrying different business data returns HTTP 409. The application normalizes the event before looking it up, so whitespace in a meter/event ID cannot manufacture a second apparent creation. A tenant-scoped GET endpoint makes the `Location` returned by POST readable. Regression tests cover the application boundary and the actual ASP.NET request pipeline.

These changes are already present. Exercises later in the course are proposed work for you, not claims of additional delivered functionality.

## Reading and practice order

| Lesson | What you will produce | Suggested focused time |
|---|---|---:|
| [01 Codebase map](01-CODEBASE-MAP.md) | A request map with one responsibility per layer | 60–90 min |
| [02 Concepts](02-CONCEPTS.md) | Predictions about identity, atomicity and retry outcomes | 90–120 min |
| [03 Worked change](03-WORKED-CHANGE.md) | An explanation of the actual before/after behavior | 90 min |
| [04 Testing and debugging](04-TESTING-AND-DEBUGGING.md) | A failing regression, a diagnosis and a verified repair | 90–120 min |
| [05 Practice](05-PRACTICE.md) | Small changes followed by one independent feature | 4–6 h |
| [06 Solutions and review](06-SOLUTIONS-AND-REVIEW.md) | A reviewed design and evidence-based self-assessment | 90 min |
| [07 Trace lab](07-TRACE-LAB.md) | A reproducible create/read/replay/conflict transcript | 60 min |
| [Verification](VERIFICATION.md) | A clear account of what has and has not been checked | 15 min |

The times are estimates, not a deadline or evidence of mastery. Spend longer on a failed prediction. Keep a notebook with four columns: prediction, observation, explanation, next check. An unexplained passing test is weaker evidence than a failed prediction that you can now explain accurately.

## Set up a disposable learning run

The repository pins .NET SDK `10.0.302` in [global.json](../global.json). Use the locked dependency graph:

```powershell
dotnet --version
dotnet restore MeterForge.slnx --locked-mode
dotnet test MeterForge.slnx --configuration Release --no-restore
dotnet run --project src/MeterForge.Api --no-launch-profile -- --urls http://127.0.0.1:5281
```

The final command stays running. Open a second terminal for the trace lab. The default API uses memory: restarting loses the events you create. Its readiness response says `durableDefault: false`. The PostgreSQL invoice adapter is a separate teaching boundary and does not make usage ingestion durable. Do not point practice commands at any real service or database.

If restore fails, first record the SDK version, failing package and exact message. A download failure is an environment problem, not evidence that business-rule tests passed or failed. The verification page describes the environment used for this improvement.

## What mid-level ownership looks like here

You can take a small feature from request contract through business rules and storage, choose tests based on the ways it can fail, explain the HTTP response, and identify the next risk without guessing. You can distinguish “this store is thread-safe” from “this operation is atomic,” and “the query is tenant-scoped” from “the caller is authorized.”

This application has teaching limits: tenant IDs come from routes, usage references are not checked against a persisted subscription in this use case, and the default store survives neither restarts nor multiple independent API processes. A good explanation states those limits precisely. Completing this course does not make the app a production billing service or establish that you have operated one.


Continue with the [extended engineering course](extended-course/README.md) for the new source-grounded chapters, separate reviews, capstones, and disposable labs. The extended index reports its continuing scope.
