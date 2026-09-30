# 10. Test the boundary between a finalized invoice and its snapshot

Read [Invoice](../../../src/MeterForge.Domain/Invoicing/Invoice.cs) and [InvoiceSnapshot](../../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs). The domain object allows lines while Draft, rejects later additions, and finalizes only a nonempty draft. InvoiceSnapshot.From checks finalized status and timestamp, then copies line values into snapshot records. These are related boundaries with different observable guarantees.

## Construct the smallest valid invoice

Use fixed tenant and subscription identities, a start date and a strictly later end date. Currency is normalized through Money. Add one line with a nonblank description, nonblank source reference and nonnegative amount in that currency. A zero-valued line is accepted; negative money can exist as a Money value but is rejected by AddLine.

That distinction is an excellent oracle exercise. Money is a representation type, while Invoice imposes a use-specific rule. A learner who assumes every Money value must be nonnegative will write the wrong constructor test. A learner who assumes acceptance by Money implies acceptance by every aggregate will write the wrong invoice test.

Predict how padded descriptions and source references appear in the line. AddLine trims them. PriceVersionId may be null. Do not assert that every line necessarily points to a catalog price; the domain signature permits lines with other evidence through SourceReference.

## Finalization matrix

Try finalizing an empty draft and expect invoice.empty. Add a valid line and finalize at an explicit offset timestamp. Expect Finalized status and a UTC timestamp representing the same instant. Try finalizing again and expect invoice.finalize_invalid. Try adding another otherwise valid line after finalization and expect invoice.finalized.

After each rejection inspect the line count, total, status and timestamp. This checks preservation, not just the exception code. Use a different proposed timestamp for the repeated finalization so the test can detect an accidental overwrite. A test that repeats the same timestamp would miss that class of defect.

## Snapshot creation

Calling From on a draft rejects even when it has lines. Calling From on a finalized invoice copies identity, tenant, subscription, currency, period and timestamp, then creates a new array of line snapshots. Compare each copied field explicitly. An equality assertion over a hand-built expected object can be useful, but ensure its construction does not merely call From again.

Line IDs are generated when AddLine runs. Capture the observed domain line ID before snapshotting rather than expecting a predetermined GUID. The invariant is preservation of identity across this conversion. Random identity generation does not prevent deterministic assertions about relationships between objects.

## Immutability has several meanings

The domain object prevents additions after finalization through its public methods. Snapshot record properties use the record's normal initialization semantics. The Lines property is typed as IReadOnlyCollection, but From supplies an array. Read-only interface access does not guarantee that no caller can obtain and mutate the underlying array reference through a compatible cast.

Likewise, the public positional record constructor can create an InvoiceSnapshot without passing through From. It can supply a collection assembled elsewhere. The From guard is therefore a factory-path guarantee, not a universal validation rule for every representable snapshot. Explain this scope without implying that an inspected application endpoint necessarily exposes arbitrary construction to an untrusted caller.

Design a copy-only experiment that casts Lines to the actual array type and replaces one element. Predict whether the original Invoice line changes and whether the snapshot view changes. The expected distinction is that value copying separates the domain line from the snapshot array, while the snapshot collection itself remains a mutable container behind its interface.

## Serialization and persistence are further boundaries

A JSON representation may preserve values but change concrete collection types on deserialization. A database store may accept records without rechecking the original domain rules. Neither behavior can be established by the From test alone. Trace the actual construction and save path before claiming that all stored invoices passed domain finalization.

Propose a stronger snapshot contract if needed: internal construction, immutable collection ownership, defensive copying, explicit validation at persistence, and a canonical fingerprint. Evaluate each separately. A fingerprint detects changed content only when compared; it does not prevent mutation by itself. Defensive copying protects against later caller edits but still needs validation of the copied values.

## Exercise

Produce a matrix of eight cases covering draft conversion, empty finalization, zero line, negative line, mismatched currency, repeated finalization, snapshot-array replacement and direct record construction. Mark which are current characterization cases and which express proposed policy. For each, identify the exact boundary that enforces or fails to enforce the rule.

[Studio index](README.md) · [Review](reviews/03-subscriptions-and-snapshots.md)
