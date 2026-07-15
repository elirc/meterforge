# Testing guide: from junior habits to mid-level judgment

MeterForge adds tests because billing failures hide at boundaries: one duplicated usage event, one unit counted twice, a cross-tenant lookup, an invoice changed after issue, two concurrent writers, or a retry with a new identity. A useful test names the risk and proves a guarantee another engineer can safely change around.

The goal is not 100% coverage. The goal is deterministic evidence that fails for the right reason.

## Test portfolio

| Layer | Protects | Examples |
| --- | --- | --- |
| Domain | dense rules without I/O | money, dates, tiers, state transitions, credits, refunds |
| Application | orchestration and port guarantees | tenant-scoped replay, expected versions, snapshots, provider calls |
| API | public compatibility and wiring | JSON, status codes, Problem Details, headers, health, OpenAPI |
| Architecture | a few costly dependency rules | framework-free domain and inward dependencies |

## How to add a test

1. State the risk: “What could be wrong even though this compiles?”
2. Choose the lowest layer that owns the answer.
3. Name behavior and outcome, such as `Rate_QuantityCrossingBoundary_ChargesEachUnitOnce`.
4. Arrange only meaningful facts with fixed dates, IDs, clocks, and provider results.
5. Make the counterexample fail before fixing a defect.
6. Assert observable state, value, error code, identity, or persisted evidence—not private methods.
7. Add a higher-level test only when routing, serialization, middleware, transactions, or provider wiring adds a distinct risk.
8. Refactor while green. A test that prevents internal restructuring without behavior change is too coupled.

## Lessons from the flaw harvest

- Tenant isolation needs a negative cross-tenant attempt; same-tenant success proves nothing about isolation.
- Tier arithmetic should assert conservation across boundaries, not only examples inside the first tier.
- Immutability needs a mutation attempt after finalization.
- Concurrency needs two writers against one version and an assertion that exactly one commits.
- Retry tests should compare stable identities before and after failure, not only the attempt counter.

## Common traps

- mocking every class and accidentally testing call choreography instead of behavior;
- copying the production algorithm into the expected-value calculation;
- depending on local time, test order, random values, delays, Docker, or live providers without isolating that dependency;
- repeating every domain permutation through slow HTTP tests;
- chasing coverage while important failures and boundaries remain untested;
- solving flakes with retries instead of fixing nondeterminism;
- accepting a snapshot diff without understanding the compatibility change.

## Commands

```powershell
dotnet restore MeterForge.slnx --locked-mode
dotnet test MeterForge.slnx --configuration Release
dotnet test tests/MeterForge.Domain.Tests --filter FullyQualifiedName~TieredRater
dotnet format MeterForge.slnx --verify-no-changes --no-restore
dotnet list MeterForge.slnx package --vulnerable --include-transitive
```

When CI fails, read the first meaningful failure, reproduce the focused project or class, and fix the owning boundary. Treat test code as maintained product code: clear reason, minimal setup, no hidden network, and useful failure output.
