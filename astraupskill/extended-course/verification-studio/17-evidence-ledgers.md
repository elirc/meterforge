# 17. Build an evidence ledger that survives review

An evidence ledger connects claims to observations. It is useful when a course contains source reading, executed pure functions, in-memory concurrency experiments and proposed database tests. Without that separation, a large collection of passing checks can accidentally imply guarantees that no experiment established.

## Start with a claim identifier

Give each claim a stable identifier, such as usage-equivalent-replay or snapshot-conflicting-replay. Write the claim as a sentence with preconditions. For usage, include the same tenant and external key, normalized equivalent payload and the selected store implementation. For snapshot replay, include the database schema and whether field equality is checked.

Avoid broad labels such as billing safe. They are too vague to falsify. A good claim has an observable outcome and a boundary: returned identity, preserved amount, raised code, row count, version increment or committed state. The ledger should let a reviewer ask what exact observation would contradict it.

## Record the evidence type

Use separate labels for source inspection, actual-source execution, copied-source experiment, independent model, proposed test and external integration. These labels describe how the observation was obtained. They do not automatically rank every result. An independent model can be excellent for reasoning about schedules while still not executing the application.

For the studio's 621 assertions, the evidence type is actual-source execution in a package-free .NET harness. The compiled sources cover Money, tier rating, usage construction, usage ingestion and the in-memory usage store. The result does not include the complete application composition root or a durable database. That sentence is more useful than merely writing all tests passed.

## Preserve inputs and environment

Record exact fixture values, deterministic seeds where used, source file hashes, compiler or runtime version, command and exit status. Store generated artifacts in the task's output area. For concurrency experiments, preserve the number of calls and the invariant checked; do not present an arbitrary wall-clock duration as a reproducibility guarantee.

Source hashes establish which file content was tested. They do not prove that the running production application uses those files or that every dependency is identical. A reproducibility record should be precise about the environment it captures. If a dependency or schema is omitted, name that omission in the experiment's scope.

## Keep expected and observed separate

A proposed database test may have a well-reasoned expected result based on source. Leave the observed result blank or explicitly unexecuted until it runs. Do not copy expected into observed merely to complete a table. That small reporting habit prevents a plan from becoming a fictional test report.

When an observation differs from expectation, retain both. Explain whether the implementation, oracle, fixture or environmental assumption was wrong. A corrected expectation should not erase the original discrepancy; the discrepancy is often the most educational part of the experiment.

## Link counterexamples

Every overbroad claim should have a nearby limiting example. For tier partition invariance, link the half-cent split counterexample. For every exception rolls back, link the live-reference callback failure scenario. For conflict-ignore means exact replay, link the changed-content and new-line-ID cases.

This does not make the report pessimistic. It makes the boundaries understandable. Readers can apply the established property confidently because they know where its assumptions end. A report without limits often forces reviewers to rediscover them independently.

## Review changes over time

When source changes, identify which claims depend on the changed files. Rerun the relevant experiments and inspect whether old characterization cases should become desired-contract cases or be retired. Retiring a characterization should include a reason and a replacement test, especially when the prior behavior affected historical data.

Maintain a small change log with claim ID, old observation, new observation and migration implication. If tenant normalization changes, replay identity may change. If rounding changes, historical invoice comparison may change. The evidence ledger should connect code evolution to those practical consequences.

## Exercise

Create eight ledger rows covering Money precision, tier coverage, usage normalization, concurrent winner identity, subscription callback failure, invoice finalization, snapshot construction and durable replay. At least three rows should remain proposed because this studio does not execute those boundaries. Have a reviewer identify one claim that is too broad and rewrite it with explicit preconditions.

[Studio index](README.md) · [Review](reviews/05-experimental-design.md)
