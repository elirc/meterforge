# Review 5: mutations, generators, ledgers and performance

This guide covers chapters thirteen, fourteen, seventeen and twenty. These lessons assess whether an experiment can distinguish a meaningful behavior change. They also assess whether its report communicates the limits of the evidence accurately.

## Mutation answers

A rounding mutant should be challenged with a midpoint that distinguishes to-even from away-from-zero. A tenant-key mutant needs the same external ID under two tenants. A removed quantity comparison needs only quantity to change under the same key. A skipped subscription version increment needs an accepted mutation followed by an operation still carrying the old version.

A compile failure is not a killed behavioral mutant. Record it separately. A surviving mutant may reveal inadequate input diversity, weak assertions, an equivalent change or an unexecuted path. The reviewer should require an explanation before accepting equivalent as the classification.

## Generator answers

A valid complete tier generator should preserve strictly increasing finite bounds and an unbounded final tier if it uses allocation conservation as an invariant. Boundary-biased quantities improve coverage around thresholds. Fractional price products are needed to exercise rounding behavior; whole quantities alone may miss it.

Permutation invariance is reasonable for distinct valid bounds because the implementation sorts. Unrestricted split invariance is false under per-tier rounding. Additivity across separately rated quantities is also generally false because tiers and rounding can make the operation nonlinear. Good answers state preconditions beside every property.

## Shrinking answer

The half-cent split counterexample can be reduced to one price, total quantity 0.10 and a split at 0.05. A shrinker must preserve the property premise and the same failure. Removing complete coverage while investigating a complete-schedule property changes the question. Keep the concrete failing input even when a seed is recorded, because future generator versions may interpret the seed differently.

## Evidence-ledger answer

Each row should contain claim, preconditions, evidence type, source or fixture identity, expected result, observed result, command or inspection reference and limitations. A proposed test leaves observation unexecuted. A source hash identifies inspected content but does not prove that a deployed service uses it.

The studio's 621 assertions belong to actual-source execution of selected pure and in-memory classes. Subscription aliasing and PostgreSQL replay exercises in this studio remain source-grounded proposals unless separately executed. A learner should not merge every lesson into the passed count simply because the files sit in the same folder.

## Performance answer

Separate new-key ingestion, equivalent replay and conflicting replay. Separate independent subscription keys from one hot key. Record schedule size for rating and retained unique keys for memory growth. Keep correctness validation in the experiment, with clear timing boundaries so assertion overhead is understood.

Reject an optimization that removes payload comparison merely because conflicting requests become faster. Reject a retention proposal that forgets how deleting replay keys changes later retry behavior. Performance and correctness are connected through the operation contract; they are not independent scoreboards.

## Falsifiability check

For any claimed improvement, ask what observation would cause the learner to reject it. If no possible result could change the conclusion, the experiment is advocacy rather than investigation. A useful benchmark comparison or mutation experiment has a decision rule established before the result is known.

## Review completion

Require one mutation card, one generated-property card, one shrink trace, one evidence-ledger row and one benchmark plan. Each should identify a limitation. The learner passes when they can distinguish a failure of the program, oracle, fixture and environment, and can revise the appropriate artifact without erasing the evidence that led to the revision.

[Studio index](../README.md)
