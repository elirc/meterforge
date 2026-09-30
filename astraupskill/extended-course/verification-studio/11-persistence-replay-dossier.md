# 11. Investigate an invoice replay without assuming equivalence

[PostgresInvoiceSnapshotStore](../../../src/MeterForge.Infrastructure/Persistence/PostgresInvoiceSnapshotStore.cs) inserts a header and its lines inside a transaction. Header conflicts on tenant and invoice ID do nothing. Line conflicts on tenant, invoice ID and line ID also do nothing. The inspected method does not compare existing values with the incoming snapshot. This workshop turns that source observation into a disciplined relational test proposal.

## Three meanings of retry

An exact retry uses the same invoice identity, line identities and field values. A conflicting retry reuses those identities with changed content. A reconstructed retry may preserve the invoice ID while generating new line IDs for conceptually equivalent charges. All three may be called retry in a casual incident report, but they exercise different database keys.

Create a dossier with two snapshots A and B. Write every key and every changed field before running a test. If the evidence says only that the second request contained the same invoice number, you do not yet know whether line identities were preserved. That missing detail can explain apparently duplicated charges without requiring the database to violate a unique constraint.

## Exact replay hypothesis

Save A with two lines, then save an exact copy. Under the intended conflict constraints, the second save should leave one header and two lines. Check row values, not only counts. Verify tenant isolation by using the same IDs under a different tenant and confirming that the key structure treats that as a separate document where the schema permits it.

The phrase under the intended constraints matters. Source SQL names conflict targets, but a live test must use the actual schema and migrations required for those targets. An in-memory dictionary imitation does not validate PostgreSQL conflict handling, foreign keys, transaction rollback or numeric column behavior.

## Same keys, changed content

Change A's header period or currency while preserving its key. Change one line amount while preserving its line ID. Save the changed snapshot. The source suggests conflicting rows are ignored rather than compared or replaced. The proposed oracle therefore checks whether the original values remain and whether the method reports success without identifying the mismatch.

This is not an instruction to accept silent mismatch as a desired business contract. It is a characterization hypothesis. A stronger desired contract would return exact replay only when canonical content matches, and return a conflict for changed content. Keep those two tests separate so the team can see both current behavior and proposed improvement.

## New line identity

Construct B with the same header key and one previously unseen line ID. The header insertion can do nothing while the new line insertion succeeds. If the schema permits it, the final document contains the new line alongside the original lines. A transaction makes the operations in this call commit together; it does not by itself prove that an already persisted finalized document cannot acquire additional lines on a later call.

Record the distinction between atomic call and immutable document. Atomicity is about all-or-nothing effects within a transaction. Immutability is about the allowed evolution across transactions. The two properties require different oracles and often different constraints or application checks.

## Failure after partial work

To test rollback, arrange a second line that violates a real schema constraint while the first line is valid. Execute through the actual store against a disposable test database, then inspect whether the new header and first line remain. The expected transactional result is rollback of the call, but only an executed relational test supports that claim for the configured path.

Avoid inventing a constraint merely because it seems reasonable. Read the schema first. If no suitable constraint exists, inject a controlled database failure in an isolated fixture or state that the proposed test requires a dedicated adapter. Do not alter production data to manufacture a failure.

## Unknown commit outcome

A connection loss around CommitAsync can leave the caller uncertain whether the transaction committed. The exception alone may not settle the durable result. A recovery design should query by stable document identity and compare expected content before deciding whether to retry, report conflict or escalate an unresolved state.

This is a proposal, not a claim that the current method implements a recovery protocol. The current source gives you the save transaction and conflict behavior. Your design must supply the missing receipt or reconciliation semantics explicitly.

## Deliverable

Submit six test cards: exact replay, changed header, changed existing line, new line ID, rollback after an earlier insert and uncertain commit. Each card needs database prerequisites, fixture identities, expected current behavior, desired contract if different, cleanup boundary and observations. Mark every unexecuted card clearly. A source-grounded test plan is useful; presenting it as a passed integration suite is not.

[Studio index](README.md) · [Review](reviews/04-persistence-and-evidence.md)
