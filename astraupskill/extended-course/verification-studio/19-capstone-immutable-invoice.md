# 19. Capstone: make immutable invoice replay reviewable

The task is to design a stronger save contract around [InvoiceSnapshot](../../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs) and [PostgresInvoiceSnapshotStore](../../../src/MeterForge.Infrastructure/Persistence/PostgresInvoiceSnapshotStore.cs). The source currently inserts with conflict-ignore behavior inside a transaction. Your proposal should distinguish exact replay from conflicting content and prevent accidental extension of an already finalized document.

## Define document identity

State the tenant and invoice key, the role of line IDs and whether line order is meaningful. Define which fields belong to immutable content. Explain whether FinalizedAt is part of content equivalence and how equivalent timestamp offsets are compared. Do not let a serializer's incidental formatting make these decisions implicitly.

Include a direct-construction threat to your assumptions: a caller can construct the public snapshot record without using From. Decide where validation occurs before persistence. The design should validate the supplied snapshot at its actual trust boundary rather than assuming every object originated from the preferred factory.

## Save outcomes

Specify four outcomes: newly stored, exact replay, conflicting replay and unresolved durable outcome. Newly stored and exact replay may share a successful user experience while remaining distinct in diagnostics. Conflicting replay should identify a stable error category without leaking another tenant's document. Unresolved outcome should guide reconciliation rather than blindly creating a new invoice identity.

Explain how the store handles an existing header with new line IDs. A complete-document fingerprint or row-by-row equivalence check can support a policy, but the transaction must apply it consistently. Avoid a check-then-insert race that lets two different snapshots both pass a preliminary absence check.

## Atomicity and constraints

Describe the database constraints and transaction behavior your algorithm relies on. Use a concurrent same-key test with two different snapshots. One should become the stored document; the other should be classified as equivalent or conflicting relative to that winner. The algorithm should not produce a hybrid document containing the header from one candidate and extra lines from another.

Choose the appropriate locking or conditional-write strategy and explain it in database terms. A process-local lock alone is insufficient for multiple application instances. A unique key prevents duplicate identities but does not automatically compare all content. A transaction prevents partial effects within its scope but needs the right reads, predicates and isolation to enforce the chosen cross-call rule.

## Recovery after acknowledgment loss

If the commit may have succeeded but the response is lost, the client should recover by stable identity. The recovery path compares the expected document with the stored winner. Define how long a receipt or status result remains available and what happens when the document cannot yet be found.

Absence in one read may not settle every failure scenario, depending on routing, replication or the actual storage topology. Do not invent those deployment details. State the consistency assumption for the proposed recovery read and test it in the environment where the design will run.

## Historical data

Existing rows may already contain documents assembled through multiple saves. A new invariant cannot be assumed retroactively. Propose an audit that groups rows by tenant and invoice ID, checks evidence available for line membership, and classifies uncertain documents for review. Do not delete or merge lines automatically without a documented correction policy.

If a canonicalization version is introduced, decide how old documents receive a version and fingerprint. Preserve enough evidence to explain historical totals. A migration that merely hashes the current rows can freeze their present state, but it does not prove that state was the original finalized document.

## Acceptance evidence

Require actual relational tests for exact replay, changed header, changed line, new line IDs, concurrent conflicting saves, rollback and ambiguous acknowledgment recovery. Pure domain tests remain useful for invoice construction, but they cannot replace database evidence for the save algorithm.

Submit a sequence diagram, canonical comparison specification, migration plan and test ledger. Include one unresolved policy decision and the information needed to resolve it. The capstone is successful when the reviewer can explain why a finalized invoice cannot silently acquire new content through a later save and how the caller recovers without inventing a duplicate document.

[Studio index](README.md) · [Review](reviews/07-capstone-rubric.md)
