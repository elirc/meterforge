# 25. Migration rehearsal and historical evidence

This chapter designs a read-only-first migration rehearsal from MeterForge's default in-memory behavior toward a proposed durable billing deployment. It uses [Program](../../src/MeterForge.Api/Program.cs), [PostgresInvoiceSnapshotStore](../../src/MeterForge.Infrastructure/Persistence/PostgresInvoiceSnapshotStore.cs), and [InvoiceSnapshot](../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs) as anchors. No migration is applied by this course, and no existing database is accessed.

The prerequisite is understanding that the default host explicitly reports durableDefault false. A migration cannot recover data that was never persisted and has already disappeared with a process. The plan must begin with available evidence, not an assumption that every earlier API success can be reconstructed from memory after restart.

## Inventory semantics before inventorying rows

List the data categories and their meanings: catalog products, subscription current state, accepted usage, invoice evidence, payment operations, and integration delivery obligations. For each, identify the current implementation and any available export path. A type existing in source is not proof that the running host stores instances of it durably.

Then classify fields as identity, normalized value, mutable current state, historical evidence, or derived summary. Product code normalization, tenant tuple keys, subscription Version, usage replay identity, and invoice source references have different roles. A migration that treats every field as ordinary text can accidentally change identity or lose conflict semantics.

Document gaps explicitly. If only current subscription ProductId is available, historical product-change segments cannot be invented reliably. If provider references are simulated, they must not be relabeled as real settlement evidence. If a snapshot lacks calculation inputs, a migration cannot claim that those inputs were preserved merely by adding empty columns.

## Build a synthetic migration dataset

Use a small set of synthetic records with canonical and noncanonical tenant strings, duplicate normalized product codes, exact usage replays, conflicting usage payloads, finalized invoices, and ambiguous payment outcomes. The purpose is to test the migration's policy, not to imitate the entire production volume.

For each record, define the expected classification: directly transferable, requires normalization review, duplicate candidate, conflicting evidence, or unsupported historical gap. The first tool should produce this classification without writes. A reviewer can then inspect how the policy handles edge cases before any durable adapter accepts data.

Do not silently merge records because their display names match. Identity should be based on the declared keys and accepted evidence. Two products with one code may be legitimate under the current store's behavior. A proposed unique-code target schema needs an explicit resolution policy rather than choosing whichever record appears first in an enumeration.

## Preserve source identity through transformation

Assign a migration run identity and preserve each source record's identity and fingerprint. A target record should be traceable to its source and transformation rule. Repeating the same run should not duplicate accepted data. A changed transformation policy should produce a new reviewable proposal rather than quietly rewriting the meaning of an earlier run.

Canonicalization can change keys, so collision detection must precede acceptance. Trimming tenant identifiers can map two source strings to one target identity. Case folding can create additional collisions. The migration report should show the category and affected identifiers without exposing sensitive payloads. A policy decision is required before merging authority scopes.

For monetary fields, preserve decimal meaning and currency normalization. Avoid converting through binary floating-point or formatted display strings. A test should compare exact accepted values before and after serialization. Historical rounding decisions belong to evidence; recalculating every amount under a new algorithm can produce a different document rather than a faithful migration.

## Rehearse acceptance and rollback

Use a disposable target store for the first write rehearsal. Record which records were accepted, rejected, or left unresolved. Then repeat the same input and verify exact replay behavior. Introduce a failure after a subset of operations and confirm that the run can resume without duplicating accepted causes.

Rollback is not always deletion. If a migration created external effects or published identifiers to users, deleting target rows may break references. A safer proposed rollout can keep migrated data inactive until validation completes, then switch readers under a controlled cutover. The design must distinguish rehearsal data from accepted business records.

Dual-write strategies require particular care. Writing to old and new stores does not create a distributed transaction. One write can succeed while the other fails. If used, the plan needs a reconciliation log, authoritative side, and replay policy. A simpler staged export and controlled cutover may be easier to reason about for a bounded teaching system.

## Validate relationships, not only counts

Matching row counts is a useful smoke check but a weak correctness proof. Verify tenant associations, subscription references, usage operation identities, invoice line membership, currency agreement, and accepted totals. A migration can preserve counts while swapping relationships or merging keys incorrectly.

Use conservation equations where the policy supports them. The sum of accepted invoice lines should equal the stored document total if the target stores one. Accepted usage identities should have a one-to-one correspondence with migrated receipts under the chosen rule. Payment references should retain their provenance category, especially when synthetic and real sources must remain distinguishable.

Sample review should target risk categories rather than only random rows. Inspect every normalization collision in the synthetic fixture, every conflicting replay, and every unsupported historical gap. Random sampling can supplement these checks at scale, but it should not replace known boundary cases.

## Exercise MF-25A: classify migration evidence

Create a source-to-target map for six data categories. Identify one field whose normalization changes identity, one historical fact unavailable from current state, and one synthetic provider value that must retain its provenance. Define read-only classifications for each issue.

## Exercise MF-25B: rehearse interrupted acceptance

Design a migration run identity and per-record receipt. Interrupt after several accepted records, resume, and show why no cause is accepted twice. Include a changed transformation policy and explain how it is reviewed separately from exact replay.

## Exercise MF-25C: validate the cutover

Specify relationship checks, monetary checks, unresolved-category thresholds, and reader-switch behavior for a disposable rehearsal. Explain rollback limits if identifiers have already been published. Do not run the plan against a user database.

## Review checkpoint

A safe migration preserves meaning and provenance, not merely bytes and counts. It begins with a read-only report, exposes ambiguity, and rehearses interruption before cutover. Strong submissions acknowledge unavailable history instead of manufacturing it to make the target schema look complete.
