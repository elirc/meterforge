# 04 — Choose evidence that can catch the real failure

[Course home](README.md) · [Previous: worked change](03-WORKED-CHANGE.md) · [Next: practice](05-PRACTICE.md)

## Three layers of evidence

Domain tests show that a business object rejects invalid values. Application tests show that orchestration and storage policy return the right outcomes. API tests show that routing, binding, dependency injection, serialization, middleware and status mapping work together. None is a substitute for the others.

The new checks live in [UsageIngestionTests.cs](../tests/MeterForge.Application.Tests/Usage/UsageIngestionTests.cs) and [UsageApiTests.cs](../tests/MeterForge.Api.Tests/Usage/UsageApiTests.cs). Read the test name before its implementation. A good name states a trigger and an observable result; “works” gives the next engineer no useful clue when it fails.

| Scenario | Evidence to inspect | Why a weaker assertion misses a bug |
|---|---|---|
| Identical sequential retry | Same stored ID; only first result is new | Two non-null responses can contain different IDs |
| Many competing services | Exactly one creation; all responses share identity | Counting rows alone misses a false creation response |
| Changed subscription/meter/quantity/time | Stable conflict code; original remains unchanged | Catching any exception could hide an unrelated failure |
| Whitespace and timezone representation | Replay returns the original canonical event | Only testing already-normalized inputs misses key drift |
| Tenant namespace separation | Same external key can create one event per tenant | A global idempotency key would incorrectly merge tenants |
| Cancellation before work | Exception and no stored event | Checking cancellation without inspecting state misses a write |
| HTTP creation and read | Follow actual Location, compare response record | Manually constructing a URL does not test Location |
| HTTP conflict | 409, stable code, matching trace header/body | Service tests never exercise error middleware |

## Run narrow checks first

From the repository root after restoring dependencies:

```powershell
dotnet test tests/MeterForge.Application.Tests --no-restore --filter FullyQualifiedName~UsageIngestionTests
dotnet test tests/MeterForge.Api.Tests --no-restore --filter FullyQualifiedName~UsageApiTests
dotnet test MeterForge.slnx --configuration Release --no-restore
```

The first two commands shorten the feedback loop while you edit this slice. The final command checks the wider solution after the local behavior is right. Keep the output and distinguish compile/analyzer failures from executed-test failures. During this improvement, an xUnit analyzer correctly rejected a filtered `Assert.Single` style; fixing that assertion did not itself prove the business logic.

### Every test class, and how to run just that one

The full solution is 51 tests. While you are working on one prediction you want one class. Copy the line you need.

| Test class | Project | What it covers | Command |
|---|---|---|---|
| `UsageIngestionTests` | Application | The idempotency slice this course is about: the 64-task race, normalized replay, the four changed-payload cases, cancellation, cross-tenant keys | `dotnet test tests/MeterForge.Application.Tests --filter "FullyQualifiedName~UsageIngestionTests"` |
| `UsageApiTests` | Api | The same slice at the HTTP boundary: create, read back the `Location`, replay conflict, correlation header | `dotnet test tests/MeterForge.Api.Tests --filter "FullyQualifiedName~UsageApiTests"` |
| `UsageEventTests` | Domain | The entity's own validation and normalization rules | `dotnet test tests/MeterForge.Domain.Tests --filter "FullyQualifiedName~UsageEventTests"` |
| `ProductServiceTests` | Application | Catalog application rules | `dotnet test tests/MeterForge.Application.Tests --filter "FullyQualifiedName~ProductServiceTests"` |
| `InvoiceSnapshotTests` | Application | Invoice persistence snapshotting | `dotnet test tests/MeterForge.Application.Tests --filter "FullyQualifiedName~InvoiceSnapshotTests"` |
| `SubscriptionAmendmentTests` | Application | Subscription change rules | `dotnet test tests/MeterForge.Application.Tests --filter "FullyQualifiedName~SubscriptionAmendmentTests"` |
| `ProductApiTests`, `RatingApiTests`, `SubscriptionApiTests` | Api | The other HTTP surfaces | `dotnet test tests/MeterForge.Api.Tests --filter "FullyQualifiedName~RatingApiTests"` |
| `TieredRaterTests`, `InvoiceTests`, `PriceVersionTests`, `AdjustmentTests`, `EntitlementTests`, `PaymentAttemptTests`, `ReconciliationTests`, `SubscriptionTests`, `IntegrationModelTests`, `ProductTests` | Domain | Pure domain rules, no I/O, fastest in the repo | `dotnet test tests/MeterForge.Domain.Tests --filter "FullyQualifiedName~TieredRaterTests"` |
| `DependencyDirectionTests` | Architecture | One test asserting the layer dependency direction. Run it after any move between projects | `dotnet test tests/MeterForge.Architecture.Tests` |

To run a single case rather than a class, extend the filter with the method name:

```powershell
dotnet test tests/MeterForge.Application.Tests --filter "FullyQualifiedName~UsageIngestionTests.Ingest_NormalizedReplay_ReturnsOriginalIdentity"
```

Quoting matters and differs by shell. In PowerShell the double quotes above are required as soon as the filter contains `|`; in bash use single quotes (`--filter 'FullyQualifiedName~UsageIngestionTests'`). Add `--no-restore` once you have restored, and `--no-build` when you have not touched source since the last build — the two together turn a twelve-second loop into a two-second one.

A `[Theory]` filters as one name: `--filter "FullyQualifiedName~Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal"` runs all four `InlineData` cases, and the output names the failing case as `(field: "quantity")`. You cannot select one `InlineData` row from the command line, so when you want exactly one, read the case name from the failure output instead of trying to filter to it.

## Why the concurrency test uses separate services

The regression starts 64 tasks behind an asynchronous start signal. Each task creates its own application-service instance while sharing the store. This matches the important dependency-lifetime relationship in the API. It would expose an attempted fix that locked only one scoped service object.

The test checks both one creation flag and one distinct returned ID. It does not claim to explore every thread schedule. Its structural support is the atomic operation in the adapter plus tests of its public behavior. A test that sleeps for a guessed number of milliseconds is weaker: timing changes can make it pass or hang for reasons unrelated to correctness.

Do not increase the task count indefinitely to claim more confidence. A small scenario tied to a precise invariant is usually easier to diagnose than a huge stress test with no model of the failure.

## A debugging sequence for “the replay looks wrong”

1. Capture the route tenant, external ID and safe synthetic payload. Do not paste real customer data into a debugging note.
2. Compare raw input with the constructed candidate. Did trimming or UTC conversion change its representation?
3. Inspect the event returned by the store. Does its ID match the first response? Does it have the expected tenant/key?
4. Compare the four business fields. Which one makes this a conflict?
5. Inspect the HTTP boundary last. Does the domain code become the expected status and body? Does the trace header survive?
6. Write the smallest regression that would distinguish the suspected failure from a correct result.

For example, a 409 with a missing trace header points toward response/middleware behavior. A service returning `IsReplay = false` twice for one key points toward the storage/result contract. A 404 from a copied Location might be route wiring or host/store lifetime. These symptoms should lead you to different files.

## Deliberately break the change

On a practice branch, remove the payload comparison but leave atomic insertion — that is, delete this block from `UsageIngestionService.IngestAsync` and nothing else:

```csharp
if (stored.SubscriptionId != candidate.SubscriptionId || stored.Meter != candidate.Meter ||
    stored.Quantity != candidate.Quantity || stored.OccurredAt != candidate.OccurredAt)
{
    throw new BusinessRuleException("usage.idempotency_conflict", "This external event ID already identifies different usage. Use a new ID for a new event.");
}
```

**Write your prediction down first**: how many tests fail, in which projects, and what each one was defending. Then run `dotnet test MeterForge.slnx --configuration Release`.

Observed on a clean tree with exactly that block removed:

```text
Passed!  - Failed: 0, Passed: 26, Total: 26 - MeterForge.Domain.Tests.dll
Failed!  - Failed: 4, Passed: 12, Total: 16 - MeterForge.Application.Tests.dll
  UsageIngestionTests.Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal(field: "subscription")
  UsageIngestionTests.Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal(field: "meter")
  UsageIngestionTests.Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal(field: "quantity")
  UsageIngestionTests.Ingest_ChangedPayloadRejectsReplayWithoutChangingOriginal(field: "time")
Passed!  - Failed: 0, Passed:  1, Total:  1 - MeterForge.Architecture.Tests.dll
Failed!  - Failed: 1, Passed:  7, Total:  8 - MeterForge.Api.Tests.dll
  UsageApiTests.CreateReadReplayConflict_PreservesTheOriginalResource
```

Five failures out of fifty-one, from two layers. Three things to take from the shape of that result:

1. **The domain tests do not move.** `UsageEvent` is still perfectly valid; the rule you removed is an *application* rule about what a repeated external id means. Knowing which layer owns a rule tells you which suite will catch it — and if you had expected a domain failure here, your mental model of the layering was wrong, which is worth finding out now.
2. **The `[Theory]` fails four times, not once.** Each `InlineData` field is a separate case, and the output names them. That is what makes the theory worth more than a single test with four `Assert` calls: a single test would have stopped at the first assertion and told you nothing about `time`.
3. **Exactly one API test fails.** The HTTP layer is not where this rule lives, so it gets one integration-level witness rather than four. If you ever find yourself with four *API* tests for one application rule, you are paying integration-test prices for unit-test coverage.

Now restore the comparison and confirm `Failed: 0` everywhere before going further. Next, remove the trace callback and run the API lifecycle test — the same exercise, on a rule that lives at the other end of the request. These experiments teach what each test protects without pretending that a passing suite proves every requirement.

Do not remove assertions simply to get a green run. If a requirement changes, update its explanation, implementation and tests together. A review should be able to tell whether you repaired behavior or weakened the evidence.

## What these checks do not establish

They do not run a browser, PostgreSQL usage storage, a payment provider or multiple deployed API processes. They do not establish production authentication, durability, invoice correctness under this new usage flow or a performance target. Use the [verification page](VERIFICATION.md) for the recorded run, and describe any new environment separately when you extend it.
