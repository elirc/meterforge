# MeterForge verification studio

A source-grounded route through independent oracles, decimal arithmetic, replay identity, reference ownership, snapshot persistence and experimental design. Read the main course alongside these workshops. Application code is unchanged.

## Learning route

Start with chapters 1–6 for the executed usage and arithmetic lab. Chapters 7–12 develop source-grounded subscription and persistence experiments. Chapters 13–17 teach mutation design, generated inputs and incident evidence. Chapters 18–21 provide capstones, performance planning and paired review.

## Workshops

- [1. Build an oracle before building a test](01-independent-oracles.md)
- [2. Partition usage inputs by identity and meaning](02-usage-equivalence-partitions.md)
- [3. Observe one winner without claiming distributed durability](03-winner-experiments.md)
- [4. Rounding can break an apparently obvious equality](04-rounding-counterexamples.md)
- [5. State the preconditions of a property](05-tier-coverage-and-properties.md)
- [6. Execute the usage and arithmetic contracts directly](06-actual-source-lab.md)
- [7. Derive a subscription history without replaying the code](07-subscription-history-oracles.md)
- [8. Observe what a repository reference really owns](08-reference-aliasing-experiment.md)
- [9. Design schedules that explain concurrency](09-deterministic-schedules.md)
- [10. Test the boundary between a finalized invoice and its snapshot](10-snapshot-boundaries.md)
- [11. Investigate an invoice replay without assuming equivalence](11-persistence-replay-dossier.md)
- [12. Specify canonical invoice comparison](12-canonical-comparison-design.md)
- [13. Use mutations to challenge the oracle](13-mutation-review-workshop.md)
- [14. Generate useful cases and shrink failures](14-generated-inputs-and-shrinking.md)
- [15. Work four usage-ingestion incidents](15-usage-incident-casebook.md)
- [16. Explain billing differences with arithmetic evidence](16-rating-incident-casebook.md)
- [17. Build an evidence ledger that survives review](17-evidence-ledgers.md)
- [18. Capstone: defend a usage replay contract](18-capstone-replay-contract.md)
- [19. Capstone: make immutable invoice replay reviewable](19-capstone-immutable-invoice.md)
- [20. Measure speed without losing the contract](20-performance-with-correctness.md)
- [21. Run a review session that produces evidence](21-review-and-teaching-session.md)

## Worked reviews

- [Review 1: independent expectations and usage identity](reviews/01-oracles-and-usage.md)
- [Review 2: money, allocation and rounding](reviews/02-money-and-tiers.md)
- [Review 3: histories, ownership and snapshot guarantees](reviews/03-subscriptions-and-snapshots.md)
- [Review 4: exact replay and canonical comparison](reviews/04-persistence-and-evidence.md)
- [Review 5: mutations, generators, ledgers and performance](reviews/05-experimental-design.md)
- [Review 6: diagnose without inventing certainty](reviews/06-incident-casebooks.md)
- [Review 7: capstone acceptance and teaching rubric](reviews/07-capstone-rubric.md)

## Executed lab and scope

[C# harness](lab/Program.cs) · [Project file](lab/Contract.csproj)

The standalone .NET 10 harness passed **621 assertions** against six actual source files. It covers pure Money/tier behavior and in-memory usage ingestion. It does not run the complete API, PostgreSQL, payment providers, or the proposed subscription/snapshot experiments. Mutation exercises in this studio are proposals, separate from any executed labs in the main course.

Run from this directory: `dotnet run --project lab/Contract.csproj`. To keep build output separate, add an absolute `--artifacts-path` pointing at your chosen task output directory.

[Main extended course](../README.md)
