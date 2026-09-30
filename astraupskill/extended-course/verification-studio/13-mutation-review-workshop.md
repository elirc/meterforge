# 13. Use mutations to challenge the oracle

A passing test demonstrates agreement between a program and an oracle for selected inputs. Mutation testing asks whether the oracle notices a deliberately altered behavior. This workshop proposes copy-only mutations to the actual classes used by [the studio harness](lab/Program.cs). The baseline harness has been executed separately; the mutations described here are learner exercises unless their execution is recorded in a result artifact.

## Pick a meaningful mutation

Change one semantic decision at a time in a disposable copy. Examples include replacing midpoint-to-even rounding with midpoint-away-from-zero, removing the tenant component from an in-memory key, omitting quantity from usage equivalence, skipping the subscription version increment, or allowing a line after finalization. Each mutation should correspond to a plausible defect and a specific contract.

Do not treat syntax errors or missing references as successful detection. A compiler failure says the mutated program could not run. A useful killed mutant compiles, reaches the relevant behavior and fails a meaningful assertion. Record compile failure, runtime infrastructure failure and assertion failure as different outcomes.

## Predict the killing example

Before executing a mutation, name the smallest input expected to reveal it. Midpoint rounding changes should be caught by a half-cent product whose even digit rounds down under the original rule. A tenant-key mutation needs identical external IDs under two tenants. Removing quantity comparison needs a replay key with only quantity changed. A skipped version increment needs an accepted operation followed by a stale expected-version attempt.

This prediction is a test of your own understanding. If you cannot identify a distinguishing input, the mutation may be irrelevant to the selected contract or the test design may be too vague. Running thousands of random inputs does not replace a simple explanatory counterexample.

## Surviving mutants are questions

Suppose replacing tier sorting with input order survives the suite. Possible explanations include every fixture already being sorted, the generated quantities never reaching the affected tiers, or an assertion checking only a field unaffected by allocation. A survivor does not automatically mean the production code is wrong. It means the current experiment has not distinguished the altered behavior.

Add an unsorted but otherwise valid tier fixture with unequal prices and a quantity spanning tiers. Calculate expected allocation independently. Then rerun the baseline and mutation. If the baseline fails too, fix the test or revisit the assumed contract before claiming improved mutation detection.

## Equivalent mutations

Some textual changes do not alter behavior within the tested domain. Reordering two independent validations may be equivalent for inputs violating only one rule, but not for inputs violating both if error precedence is part of the contract. A mutation that changes an unreachable branch under declared preconditions can also be equivalent for the selected scope.

Explain the scope before excluding an equivalent mutant. Do not discard every inconvenient survivor as equivalent. Supply a reasoning argument or an exhaustive finite-domain check where practical. If the equivalence is uncertain, leave it unresolved and state what additional analysis would decide it.

## Avoid teaching to the mutant

A test written solely to recognize one injected constant can become brittle without improving the domain contract. Prefer an invariant that naturally catches the mutant and nearby defects. For example, a tier allocation test should compare per-tier units and rounded charges, not inspect whether a particular source line contains a sorting call.

Source-level checks can be appropriate for structural rules, but label them accordingly. They do not establish runtime semantics. The strongest learning artifact pairs a mutation with a domain explanation: what would a customer or operator observe if this behavior changed, and why should the test reject it?

## Isolation and reproducibility

Place copied sources under a dedicated output directory with a manifest of original paths and hashes. Refuse to overwrite a nonempty unrelated directory. Keep generated binaries outside the application tree. Record the compiler version, mutation description, command, exit status and failed assertion. Remove or archive only the verified task-owned output when cleaning up.

Never mutate the live application source merely to see whether the course tests react. Copy-only experiments preserve the user's work and make it clear that the application itself was not repaired by the lesson. A suggested repair belongs in an exercise solution or separate proposal until implementation is requested.

## Exercise

Choose four mutants covering arithmetic, identity, versioning and finalization. For each, write the predicted killing input, run the unmodified baseline first, and classify the result. Include one survivor analysis. Your report should explain what the suite learned, rather than presenting a mutation percentage as a universal quality score.

[Studio index](README.md) · [Review](reviews/05-experimental-design.md)
