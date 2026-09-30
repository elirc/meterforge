# 27. Integrated billing characterization lab

The [billing lab generator](lab/generate_billing_lab.py) copies nine current source files into a new external directory and compiles them with a small [test program](lab/BillingProgram.cs.txt). It joins several lessons without starting the API, connecting to PostgreSQL, or calling a payment provider. The goal is to observe exact local contracts and their limits before designing a larger acceptance workflow.

The prerequisite is chapters 12 through 26. Predict the eight checks before executing them. Some checks characterize behavior that a proposed production workflow must strengthen. In particular, two entitlement preflights can both allow, two individually valid refunds can exceed a cumulative limit, and a snapshot array can be changed independently of its source invoice. Passing these observations does not endorse them as complete business protocols.

## Generate the reference workspace

Run from the MeterForge root with Python and the .NET 10 SDK already installed. The generator does not install dependencies. It writes a package-free project and clears package sources in the generated directory. The output must be an absolute path that does not already exist and is outside the MeterForge project.

```powershell
$billingLab = Join-Path $env:TEMP ('meterforge-billing-' + [guid]::NewGuid().ToString('N'))
python .\astraupskill\extended-course\lab\generate_billing_lab.py --output $billingLab --mode reference
dotnet run --project (Join-Path $billingLab 'Lab.csproj')
```

Reference mode should report eight passed checks and exit zero. SOURCE-MAP.json records each original and copied hash. The copied files retain the current signatures and dependencies needed by the harness. This avoids asking learners to restore an incompatible historical snapshot over the application.

The first check distinguishes discount reduction from net amount. The next three establish entitlement behavior: no reservation side effect, nullable unlimited evidence, and duplicate normalized feature ambiguity. The refund check shows the difference between a per-record bound and a cumulative ledger. The snapshot check separates aggregate copying from deep collection immutability.

The final two checks inspect ambiguous payment retry state and signed reconciliation difference. The recording provider is explicitly a synthetic collaborator. Its recorded request represents the model's accepted external effect for this schedule; it is not a real charge. The actual PaymentService and PaymentAttempt source perform the local mutation being observed.

## Introduce the compatible defect

Starter mode changes only the copied discount expression so Apply computes the net percentage instead of the reduction percentage. The signatures remain unchanged. The generator requires exactly one matching source expression before mutation and refuses to guess if source evolution invalidates that assumption.

```powershell
$billingStarter = Join-Path $env:TEMP ('meterforge-billing-starter-' + [guid]::NewGuid().ToString('N'))
python .\astraupskill\extended-course\lab\generate_billing_lab.py --output $billingStarter --mode starter
dotnet run --project (Join-Path $billingStarter 'Lab.csproj')
```

The expected result is seven passed checks and one failed discount-role check, with a nonzero exit. A compilation failure is not the intended failure. Inspect the source map and copied Adjustments.cs, restore the reduction expression in that copied file, and rerun. The repaired workspace should return to eight passes without modifying the original project.

## Interpret the payment check carefully

The harness starts with a Failed attempt, invokes RetryAsync, and lets the fake record a request before throwing. Retry has already advanced the attempt to Pending and incremented its number. The test observes that local state and the preserved key. It does not prove that a real provider accepted a charge or that a durable ledger recorded the attempt.

This is still useful evidence. It demonstrates that the application's mutation order can leave a caller-held attempt pending after an ambiguous exception. A proposed reconciliation protocol must handle that state. A fake that threw before recording any effect would illustrate a different scenario and would not exercise the same uncertainty.

The harness uses synchronous waiting only at its tiny console test boundary to collect an asynchronous result deterministically. It is not a recommendation to block request threads in an application. Keep teaching harness mechanics separate from production threading guidance.

## Extend the experiment without changing its scope

Add a test for a successful provider result with a blank reference. Predict the BusinessRuleException and the remaining local state. Add another for a second Retry call immediately after the ambiguous exception. These extensions use the copied source and require no external resources. They deepen the state table rather than adding unrelated tests.

For snapshots, add a direct record construction that bypasses From and explain why it is representable. Then propose an acceptance boundary that validates or defensively copies it. Do not change the characterization test to claim the current record is deeply immutable. If implementing a proposed immutable representation in a separate model, give it a separate acceptance test.

## Exercise MF-27A: classify the eight observations

For each check, state the exact source mechanism and one larger claim it does not establish. Identify which observations are desired local contracts and which expose the need for a surrounding workflow. Explain why a green suite is not proof of durable billing correctness.

## Exercise MF-27B: repair the amount-role defect

Record the starter failure, source-map mutation, copied-file repair, and final exit code. Explain why a fifty-percent example would fail to distinguish reduction from net and why the chosen ten-percent example is more discriminating.

## Exercise MF-27C: add one recovery observation

Extend only the disposable harness with either invalid successful provider evidence or repeated retry from Pending. Predict state and exception before running. Report the observation separately from any proposed durable recovery behavior.

## Review checkpoint

The lab is successful when the learner can explain why every pass is narrow and why the intended failure is meaningful. Its source hashes, external output guard, and compatible mutation make the experiment reproducible. Its explicit limitations prevent a small console program from being mistaken for a complete financial integration test.
