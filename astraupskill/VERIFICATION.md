# Verification and remaining boundaries

[Course home](README.md) · [Testing lesson](04-TESTING-AND-DEBUGGING.md)

Verified September 8, 2026 on Windows with .NET SDK 10.0.302, using an isolated copy of this repository. The locked packages were restored; missing packages were downloaded into the verification workspace. Existing global packages were available as a read-only fallback. The original project's databases, dependency folders and Git history were not replaced.

## Executed checks

```powershell
dotnet restore MeterForge.slnx --locked-mode
dotnet test MeterForge.slnx --no-restore --configuration Release --logger trx
```

The verification environment also set isolated `DOTNET_CLI_HOME` and `NUGET_PACKAGES` paths. The final test run passed **51 tests, zero failures and zero skips**:

| Suite | Passed |
|---|---:|
| Domain | 26 |
| Application | 16 |
| Architecture | 1 |
| API | 8 |

The application tests exercise competing service instances, normalized retries, four payload-conflict cases, tenant-separated keys and cancellation before insertion. The API test host exercises creation, the returned Location, resource reads, replay, conflict mapping, trace correlation and tenant-scoped missing reads. Release compilation and the repository's warnings-as-errors/analyzer settings were included; they were not disabled to make the run pass.

## Changed after the original verification run

One follow-up change was made to this repository after the evidence above was captured, and it is disclosed here rather than folded silently into the numbers.

`IUsageEventStore.FindByExternalIdAsync` was removed. After the repair it was called by no production code and only by two assertions in `UsageIngestionTests` — a test-only seam on a production contract, and the exact habit the course argues against elsewhere. It is gone from the interface and from `InMemoryUsageEventStore`, and those two assertions were rewritten to prove the same facts through the application's own behaviour: the race test now ingests the same key once more and asserts a replay carrying the winner's id, and the cancellation test ingests that key afterwards and asserts a *creation*, which is only true if nothing was stored.

The test count is unchanged at **51 passed, zero failed, zero skipped**, re-observed with `dotnet test MeterForge.slnx --configuration Release` after the change. No test was deleted or weakened; two assertions became stronger. See [01-CODEBASE-MAP.md](01-CODEBASE-MAP.md) for why the method's absence is now part of the design.

## Findings repaired during verification

The new integration test first exposed a missing trace header on handled errors. After the response-start callback was added, it exposed a second mismatch: the default Problem Details writer used a different trace identifier from the header. Explicit Problem Details customization now makes both use `HttpContext.TraceIdentifier`. The final test asserts that they match, rather than merely checking that both fields exist.

A first restore attempt under restricted network access could not fetch some locked packages. That environmental failure was resolved before the successful run. It is not counted as a passed test. Earlier failed test results were retained in the central improvement workspace separately from the final passing TRX results.

## Limits

- No live PostgreSQL usage adapter, payment provider, browser or deployed API was exercised. API integration uses `WebApplicationFactory` and the real ASP.NET pipeline in-process.
- The default memory store loses data and idempotency history on restart. Its atomicity guarantee is within one process.
- The route tenant is a data-scoping input, not proof of authenticated permission. This work does not install production identity or authorization.
- The usage use case does not validate that a persisted subscription belongs to the tenant. The practice course explains that separate extension explicitly.
- GET by internal event ID scans the memory collection; no performance target is claimed.
- Manual PowerShell examples are learner exercises. Their corresponding HTTP behavior is covered by the automated API lifecycle test, but a separate live-server terminal session is not claimed here.

When extending the feature, keep these limits visible and add evidence for any stronger guarantee you introduce. Do not rename an in-memory test “production verification.”
