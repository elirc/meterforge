# 6. Execute the usage and arithmetic contracts directly

The [isolated lab project](lab/Contract.csproj) compiles six actual MeterForge source files: Money, BusinessRuleException, TieredRating, UsageEvent, UsageIngestionService and InMemoryUsageEventStore. It targets .NET 10 for this standalone harness and has no external package references. It does not start the API, connect to PostgreSQL or call a payment provider.

## Run the harness

From the studio directory, run `dotnet run --project lab/Contract.csproj` with a compatible SDK. The program reports its assertion count and scope. Read the expected values before execution. The integer tier oracle is calculated independently from the two-tier fixture, while selected fractional cases expose midpoint and partition behavior.

The project includes source by relative path rather than copying a parallel implementation. That makes changes to the real pure/domain code visible to the harness. It also means a future source change may require reviewing the contract. Do not automatically update expected values to match new behavior without deciding whether the change is intended.

## What is checked

Money cases cover normalization, two-decimal validation, currency mismatch and current acceptance of negative amounts and three-character numeric currency text. The latter two are characterization cases, not endorsements of every pricing use. The tier loop checks two hundred and one integer quantities for total arithmetic, unit coverage and reversed-tier invariance under a complete fixture.

Fractional cases check two midpoint directions, the one-tier versus split-tier rounding counterexample and finite coverage. Usage cases check normalized replay, changed-quantity conflict, preservation of the stored winner, tenant-scoped lookup, untrimmed tenant characterization and a wave of equivalent calls. A pre-canceled token is rejected before ingestion work.

## Interpret the concurrent wave

The wave uses tasks to submit equivalent calls and asserts one stored ID and one original winner. It is an observed in-process experiment. It does not control every instruction interleaving and does not prove cross-process durability. The dictionary implementation supplies the atomic operation under test; a durable adapter needs its own database constraints and integration campaign.

If the wave fails, preserve the returned IDs, replay flags and exception categories. A broad retry of the test until it passes would hide useful evidence. If it passes, report exactly the fixture and implementation exercised. Avoid the phrase billing exactly once, which would include downstream rating, invoices, payment and external delivery beyond this lab.

## Mutation practice

Copy the lab and source into a disposable directory. Remove payload comparison, omit tenant from the store key, change midpoint rounding or replace atomic GetOrAdd with a controlled unsafe sequence. For each mutation, identify the expected failing assertion. Some unsafe concurrency mutations need an explicit barrier to reproduce deterministically; the existing wave alone may not guarantee that schedule.

Do not edit production source in place for this exercise. The studio's source inclusion is read-only during normal compilation, but a text editor could still alter the included file. Keep mutation copies clearly separate and record their origin hash. A clean reference run should remain available for comparison.

## Lab report

Record SDK version, command, exit result, assertion count and source hashes. Separate pure arithmetic from in-memory storage evidence. List untested boundaries: HTTP authority, subscription ownership, durable uniqueness, relational transactions, invoice replay and provider outcomes. The report is stronger when those limits are explicit because another engineer can choose the next meaningful test.

[Studio index](README.md) · [Review guide](reviews/02-money-and-tiers.md)
