# Review 7: capstone acceptance and teaching rubric

Use this rubric for the replay-contract capstone, immutable-invoice capstone and final paired review. It assesses the completeness of a proposal and the honesty of its evidence. It does not certify production readiness or replace implementation review.

## Usage contract criteria

The proposal must define first acceptance, equivalent replay, conflicting replay and correction. It must name the storage key and normalization rules, including the current tenant-string behavior. It should provide a response-loss recovery path with tenant authorization and distinguish a stable event identity from a newly minted correction identity.

A strong migration note includes two current keys that would collide under proposed normalization and explains how conflicting payloads are reviewed. A weak note simply trims all strings and declares the problem solved. Require downstream accounting semantics for corrections so a fresh key does not accidentally become an extra charge.

## Invoice contract criteria

The proposal must define complete immutable content, line identity and ordering semantics. Exact replay and conflicting replay must have distinct classification. The save algorithm must explain concurrent same-key candidates and prevent a hybrid document. Direct snapshot construction must be considered at the persistence validation boundary.

Require actual database evidence before calling the design implemented. The proposed test portfolio should include changed header, changed existing line, new line IDs, rollback and response-loss reconciliation. A transaction keyword or unique key alone is not a sufficient explanation of complete-document immutability.

## Evidence criteria

Every important claim should link to source inspection, an executed result or a clearly marked proposed test. The learner should know which studio checks actually ran. Counterexamples should limit overbroad claims: rounding partition behavior, live-reference callback failure and conflict-ignore replay are particularly useful.

An excellent packet preserves discrepancies between prediction and observation and explains their resolution. A weak packet edits the expected result after execution without documenting why. The review values understanding and reproducibility over a large passing count.

## Failure and recovery criteria

The failure table needs last confirmed effect, unknown state and safe next action for each boundary. A timeout must not automatically mean no effect. A failed callback must not automatically mean rollback. A missing record must be interpreted under a stated read-consistency assumption rather than an invented deployment topology.

Recovery should use stable identity and comparison where appropriate. If the proposal cannot yet resolve an outcome, it should expose an unresolved state and an operator path. Hiding uncertainty behind a generic success or retry message is not acceptable for the capstone.

## Communication criteria

The paired review should produce an explanation a teammate can act on. Technical detail belongs where it changes the decision: which key, which version, which rounding boundary or which persisted observation. The learner should translate those details into a concise operator note without losing uncertainty.

Require one explicit falsifying observation for the proposed next step. This tests whether the design responds to evidence. If the learner cannot imagine any observation that changes the recommendation, ask whether it is a fixed policy decision, an assumption or an unsupported certainty.

## Completion decision

Accept the capstone when its current-state account is accurate, its proposal is internally consistent, its oracle is independent and its remaining evidence is clearly identified. Request revision when it confuses identity with content, in-memory behavior with durability, or source prediction with executed integration results.

For further study, choose the smallest unresolved boundary. A learner struggling with schedules should return to deterministic concurrency. A learner struggling with arithmetic should revisit generated inputs and midpoint examples. A learner struggling with handoff quality should repeat the incident casebook using shorter, evidence-linked operator notes.

[Studio index](../README.md)
