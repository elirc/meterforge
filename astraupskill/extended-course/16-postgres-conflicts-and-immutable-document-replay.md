# 16. PostgreSQL conflicts and immutable-document replay

Read [PostgresInvoiceSnapshotStore](../../src/MeterForge.Infrastructure/Persistence/PostgresInvoiceSnapshotStore.cs). The adapter opens a connection, starts a transaction, inserts an invoice header, inserts its lines, and commits. It uses parameters for values and conflict-do-nothing clauses for selected keys. This chapter asks a precise question: does avoiding duplicate rows also establish that a repeated snapshot is equivalent to the first accepted snapshot?

The answer must come from the statements and their surrounding protocol. A conflict clause can make a repeated insertion harmless under certain conditions, but it can also hide a conflicting payload. The distinction matters because the method name promises finalized snapshots, whose evidence should not silently become a mixture of two submissions.

## Trace the transaction boundary

The header command and every line command are assigned the same transaction. A successful commit groups those database writes. If a command fails before commit and the transaction is not committed, the adapter's transaction lifecycle governs rollback under the provider. This is a real database boundary, unlike the process-local stores discussed earlier.

The method does not include an external provider call or email send in that transaction. Do not expand its atomicity claim beyond the SQL operations shown. A caller that sends a payment before invoking SaveFinalizedAsync still has two separate resources and a recovery problem. The adapter's transaction cannot roll back a charge at another service.

Cancellation is forwarded to connection opening, transaction creation, command execution, and commit. An exception during commit may leave the caller uncertain about the accepted database outcome depending on the failure timing. A robust replay protocol should query by stable identity rather than treating every failed await as proof that nothing committed.

## What each conflict key means

The header conflict target is tenant_id and id. The line target is tenant_id, invoice_id, and line id. Repeating an identical snapshot with identical line identifiers attempts the same keys and can avoid duplicate rows through do nothing. This is a useful foundation for replay but does not compare the non-key values.

Suppose the second snapshot uses the same header key but a different period or currency. The header conflict branch does not verify equality or update the stored header. The method proceeds to line insertion. A caller may receive normal completion even though some submitted header values were not accepted. The exact resulting database state also depends on schema constraints, which should be inspected and tested rather than assumed.

Now suppose the second snapshot uses new line identifiers. Those keys do not conflict with the first lines, so the adapter attempts additional inserts under the existing invoice. The source does not first compare the complete accepted line set. An immutable-document replay protocol therefore needs more than per-row conflict suppression.

## Work the mixed-document example

Snapshot A has header H and lines L1 and L2 totaling one hundred. Snapshot B reuses H's identity but has lines L3 and L4 totaling ninety. Under the visible commands, the header insertion for B can do nothing while its new lines are attempted. If permitted by the schema, the stored document can contain lines from both submissions. This is a source-derived scenario to test against a disposable database, not a claim that such a row currently exists.

Even keeping line identifiers fixed does not fully solve the problem. If B changes L1's amount but uses the same line key, do nothing can preserve A's amount without informing the caller that B was rejected as different. A successful method return then means the commands completed, not necessarily that every supplied value matches stored evidence.

A stronger contract distinguishes exact replay from conflicting replay. Exact replay returns the accepted document. Conflicting replay produces a domain conflict and leaves the accepted document unchanged. A hash of canonical accepted content can help compare payloads, but the canonicalization must be defined and the comparison enforced with the same authority that accepts the document.

## Define canonical evidence carefully

Canonicalization should specify field order, line ordering, decimal representation, currency normalization, timestamp representation, null handling, and which metadata participates. If line order is not semantically meaningful, sort by stable line identity before hashing. If presentation order matters, preserve and include an explicit order value. An accidental serialization order should not decide replay equivalence.

Do not hash only the total. Two different line sets can sum to the same amount. Source references and price identities may be central audit evidence even when the monetary total is unchanged. A replay comparison should reflect the document contract, not merely a convenient checksum of one field.

The content hash is not a substitute for a database transaction. Two callers can both observe absence and propose different content. The acceptance mechanism must select one winner and bind its hash and rows atomically. The losing caller then reads the accepted hash and determines exact or conflicting replay. This is the same operation-boundary reasoning introduced for usage ingestion, now applied to an immutable document.

## Plan a provider-backed experiment

Use a separately provisioned disposable PostgreSQL database and the repository's intended schema. Do not run this lesson against an existing user database. The first test saves a snapshot and verifies header and line counts. The second repeats it identically. The third reuses identity with changed header data, and the fourth reuses it with new line identities. Record actual rows and caller outcomes.

Add a failure after one line insertion using a controlled database condition or a test-specific adapter seam. Verify whether the transaction leaves any partial document after rollback. Then test two concurrent submissions with the same document identity and different payloads under the proposed stronger protocol. The acceptance criterion is one coherent document, not merely a lack of duplicate primary keys.

The workbook does not provision PostgreSQL or execute these experiments in the application environment. A request-recording or pure model fixture can illustrate the command sequence, but it cannot prove relational constraints, transaction isolation, or commit recovery. Report that limitation prominently rather than describing a model result as a database result.

## Exercise MF-16A: classify replay outcomes

Compare identical replay, same keys with changed values, and same header with new line keys. Predict the visible SQL behavior and identify schema-dependent uncertainties. Explain why do nothing is not an equality check.

## Exercise MF-16B: design immutable acceptance

Define a canonical payload, stable operation key, accepted-content comparison, and transaction boundary. Work two competing snapshots and specify the winner, exact replay, and conflict responses. Include line evidence rather than hashing only Total.

## Exercise MF-16C: write the integration plan

Describe a disposable database fixture for partial failure, repeated save, conflicting replay, and concurrent acceptance. State setup requirements, observed rows, and expected caller outcomes. Keep the plan unexecuted until an isolated database is explicitly available.

## Review checkpoint

Idempotency is about preserving the meaning of an accepted operation, not merely suppressing duplicate inserts. The adapter's transaction is valuable, but immutable-document semantics require a complete acceptance contract. A good review can explain precisely what the SQL protects and what the surrounding workflow must still decide.
