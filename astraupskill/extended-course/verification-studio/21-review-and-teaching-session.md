# 21. Run a review session that produces evidence

This final workshop turns the studio into a practical learning session. Work in pairs: one learner owns the contract and another challenges its evidence. Rotate roles halfway through. The goal is to leave with a corrected explanation, a reproducible experiment and one clearly scoped next step, rather than merely finishing a checklist.

## Opening explanation

Choose one boundary: usage replay, tier rounding, subscription mutation or invoice persistence. The owner explains current behavior in three minutes using one concrete input. The reviewer asks which source or executed result supports each important claim. If the answer depends on an unexecuted proposal, label it immediately.

The owner should be able to distinguish identity, state and evidence. For example, the same invoice ID does not imply the same line content, and an exception does not always imply no effect. A clear explanation names the stored winner, the attempted operation and the last confirmed observation.

## Counterexample round

The reviewer supplies one changed assumption. Examples include a padded tenant, a finite final tier, a callback that mutates before throwing, or a retry that generates new line IDs. The owner predicts behavior before opening the source. Together they compare the prediction with the implementation or an existing executed harness.

An incorrect prediction is a useful learning outcome when it is corrected precisely. Record the mistaken assumption and the observation that changed it. Do not rewrite the notes to make the final conclusion appear obvious from the start. That record helps identify patterns in reasoning, such as assuming every read is detached or every transaction enforces immutability.

## Experiment round

Design the smallest experiment that distinguishes two explanations. If a tier total differs, compare rounding boundaries rather than running the entire application. If durable replay is uncertain, a pure arithmetic harness is insufficient; specify the relational fixture. Choose the boundary that can actually answer the question.

Write expected outcomes for both explanations before execution. If every possible result would be interpreted as support for the owner's claim, the experiment is not discriminating. Add a falsifying observation and decide how it would change the next action.

## Operator translation

Turn the technical conclusion into a short operator note. It should state what is known, what remains uncertain and what action follows. For a response-loss incident, the note might direct an authorized lookup by stable identity before a retry. For a rounding difference, it might identify the per-tier midpoint rule and request the schedule version for comparison.

Avoid exposing unnecessary raw payloads in the note. Include the identifiers and values needed for the decision, with tenant scope maintained. A useful handoff is both specific and proportionate; it does not require copying every log line into a ticket.

## Improvement proposal

Choose one change and write its acceptance condition. A tenant normalization proposal needs collision handling. A snapshot equivalence proposal needs concurrent-save behavior. A detached-read proposal needs ownership and version semantics. An arithmetic policy change needs historical comparison and an effective version.

List evidence already available and evidence still required. This prevents a local course exercise from being mistaken for production readiness. The proposal can be strong while still identifying unexecuted database or provider tests. Honest boundaries make the next implementation step easier to review.

## Assessment

Evaluate four dimensions: source accuracy, independent oracle quality, experiment design and communication. A learner passes when they can correct an overbroad claim, reproduce one concrete result and explain what that result does not establish. A high assertion count without a defensible contract is insufficient.

Finish by selecting the next lesson based on the observed gap. Difficulty with rounding suggests the arithmetic casebook. Difficulty with failed callbacks suggests reference ownership and schedules. Difficulty with replay identity suggests the two capstones. This creates a learning path from demonstrated needs rather than repeating every chapter regardless of what the learner already understands.

[Studio index](README.md) · [Review](reviews/07-capstone-rubric.md)
