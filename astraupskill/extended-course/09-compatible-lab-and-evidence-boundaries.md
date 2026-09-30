# 09. A compatible lab and its evidence boundaries

The [usage lab generator](lab/generate_usage_lab.py) copies four current source files into a new external directory: UsageEvent, UsageIngestionService, InMemoryUsageEventStore, and BusinessRuleException. It builds a small executable around those actual types. Reference mode preserves their bytes. Starter mode removes one quantity comparison only in the copied service, creating a compiling behavioral defect. This lesson teaches both repair and evidence reporting without restoring an incompatible historical snapshot.

The lab is deliberately narrower than the API. It does not start ASP.NET, inspect HTTP headers, authorize tenants, or connect to a durable database. It executes the current domain, application, and in-memory store boundary with synthetic values. That is enough to test normalized replay and conflict classification, but not enough to certify the entire billing product.

## Generate into an explicit new location

Run from the MeterForge root using Python and an installed .NET 10 SDK. Choose a fresh external directory. The generator refuses relative paths, existing directories, and paths inside the project. This protects both application files and an earlier learner's experiment. It reads all required source and the harness before creating the output.

```powershell
$usageReference = Join-Path $env:TEMP ("meterforge-reference-" + [guid]::NewGuid().ToString('N'))
python astraupskill/extended-course/lab/generate_usage_lab.py --output $usageReference --mode reference
dotnet run --project (Join-Path $usageReference 'Lab.csproj')

$usageStarter = Join-Path $env:TEMP ("meterforge-starter-" + [guid]::NewGuid().ToString('N'))
python astraupskill/extended-course/lab/generate_usage_lab.py --output $usageStarter --mode starter
dotnet run --project (Join-Path $usageStarter 'Lab.csproj')
```

The generated project has no external package references and clears package sources. It relies on installed SDK reference packs. A missing toolchain is a setup failure, not a failed business assertion. Do not install dependencies or alter the application's global configuration just to make this optional learning run appear successful without understanding the environment.

## Predict the deliberate failure

The starter still builds a candidate and obtains the stored winner. It compares subscription, meter, and time, but the quantity comparison is replaced with false. A retry carrying a changed quantity therefore escapes the intended conflict check and is classified as a replay. The stored event remains the original object, so checking only stored quantity would miss the incorrect successful outcome.

The harness's conflict test requires both rejection and preservation of the original quantity. That pair is important. A broken service that overwrites the event and then throws would fail preservation. A broken service that silently returns the old event would fail rejection. Tests should protect the complete operation contract rather than one convenient final value.

Reference mode reports five passes. Starter mode reports four passes and one intended failure in quantity conflict. The other checks cover whitespace/time normalization, tenant-key distinction, cancellation before ingestion, and independence of separate stores. The last check is a limitation observation: two stores legitimately create separate winners because they do not share authority.

## Repair the copy and retain provenance

Open UsageIngestionService.cs inside the generated starter directory and restore the quantity comparison. Do not edit the original project file. Rerun the generated project and record the actual result. Preserve SOURCE-MAP.json, which records source hashes, copied hashes, and the deliberate mutation. If you record a repaired hash, keep it as an additional artifact rather than rewriting the original mutation history.

A different repair can be acceptable if it preserves the complete payload comparison and all intended semantics. However, deleting the quantity-conflict fixture or returning a conflict for every replay is not a repair. Include a positive identical replay to distinguish correct rejection of changed data from indiscriminate rejection of all retries.

The generator checks that the expected comparison appears once before applying its mutation. If source changes and that condition fails, inspect the new implementation. Do not broaden replacement until it matches an unrelated expression. A safe starter remains tied to the current method signature and exact behavior under study.

## Interpret the result honestly

An executed reference pass establishes these five cases using copied current source. It does not execute the original xUnit suite, prove sixty-four-way concurrency, test restart durability, or establish HTTP response mapping. Those are different evidence layers. The source map lets another reviewer identify what was copied and compare it with the current checkout.

The recorded authoring check ran both modes successfully at the compilation level: reference passed five, starter failed only the intended quantity case. These results are evidence for this checkpoint, not a promise that every future source revision will compile in the same lab. Regenerate and inspect results after meaningful changes.

## Exercise MF-09A: make a prediction receipt

Before running, write the expected result of each named check and identify which one should fail in starter mode. After running, record SDK, exit status, named assertions, and source-map identity. If a build fails, keep that outcome separate from a semantic failure.

## Exercise MF-09B: strengthen a weak oracle

A proposed test only verifies that the stored quantity remains three after a retry with quantity four. Explain why it misses the starter defect. Add the smallest assertion that completes the intended contract, then identify another plausible wrong implementation that your combined assertions reject.

## Exercise MF-09C: choose the next evidence layer

For HTTP 201 versus 200, tenant authorization, shared-store contention, and restart durability, name the next test boundary beyond this lab. Explain why no one larger test automatically substitutes for all the smaller focused observations. Keep execution cost connected to the uncertainty being resolved.

Consult the [separate lab review](review-03-compatible-lab.md) after recording your predictions and observations.
