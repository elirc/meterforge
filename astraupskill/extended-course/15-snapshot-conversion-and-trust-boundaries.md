# 15. Snapshot conversion and trust boundaries

Read [InvoiceSnapshot](../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs) with the [snapshot tests](../../tests/MeterForge.Application.Tests/Persistence/InvoiceSnapshotTests.cs). The conversion method is deliberately small: it accepts a finalized Invoice and copies its evidence into record values. The learning challenge is to distinguish the guarantees of that factory from the guarantees of the public record type and its consumers.

The prerequisite is chapter 10's finalization contract. A finalized invoice cannot accept further lines through AddLine. That makes it a sensible source for persistence. It does not automatically make every object named InvoiceSnapshot validated or deeply immutable. Construction paths and collection ownership still matter.

## Follow the conversion field by field

From rejects an invoice unless its status is Finalized and FinalizedAt is non-null. It then copies invoice identity, tenant, subscription, currency, period, finalization time, and a projected array of line snapshots. Each line contains its identity, description, decimal amount, currency, optional price-version identity, and source reference.

The projection makes a new collection rather than exposing the invoice's internal line wrapper. That breaks collection aliasing with the aggregate. Subsequent access to the snapshot's array does not mutate the aggregate's private list. This is a useful property and should be tested separately from the fact that the invoice is already finalized.

The existing successful test checks invoice identity, one line, and tenant. The rejected-draft test checks the business error code. Those are real assertions, but they do not exhaust the conversion contract. A richer test can compare every evidence field and multiple lines, including optional PriceVersionId values and normalized currency.

## Factory guarantees do not cover every constructor

InvoiceSnapshot is a public positional record. Callers can construct one directly instead of invoking From. The record declaration does not repeat the invoice's constructor guards or From's finalization check. Therefore a consumer accepting an arbitrary InvoiceSnapshot should not assume that it necessarily came from a valid finalized Invoice.

This does not make the record inherently wrong. Application-internal data carriers often rely on trusted construction paths. The question is where the trust boundary lies. A persistence adapter invoked only by a controlled application service can document that precondition. A deserializer or external API accepting snapshots would need stronger validation before treating the fields as authoritative evidence.

Work a synthetic direct construction with an empty line collection and an arbitrary finalization timestamp. The type can represent it even though From would not produce it from an empty draft. A test should name this as a construction-path distinction rather than saying the factory failed. The factory enforces its own entry condition; the public type admits a broader set of values.

## Read-only interface versus immutable storage

Lines is typed as IReadOnlyCollection of line snapshots. From supplies an array. That interface prevents mutation through its declared members, but an array remains mutable through an appropriate reference or cast. A caller can replace an array element in the snapshot's collection without mutating the original Invoice. These are two separate aliasing questions.

A proposed deeply immutable snapshot can use an immutable collection, a defensive copy at the persistence boundary, or another representation whose mutation policy is explicit. Each choice has costs. Defensive copying at every boundary can allocate repeatedly; trusting a private construction path can be efficient but needs a narrow API and tests. The right answer depends on who can retain and mutate the collection.

Even immutable elements do not make a mutable container immutable. InvoiceLineSnapshot is a record, so a caller can create a changed copy through a with expression and replace an array slot. The original element remains unchanged, but the collection now points to a different element. Teach the distinction between object identity, value copying, and container ownership with a small controlled example.

## Evidence completeness and schema evolution

The snapshot records amounts and source references, but it does not independently include every possible upstream calculation input. Whether that is sufficient depends on the audit and replay contract. A future calculation may need quantity, tier boundaries, tax quote version, discount allocation, or usage cutoff. Adding such fields is a schema-evolution decision, not a reason to claim they exist now.

Define the purpose before expanding the snapshot. If it is an immutable issued-document representation, it should preserve exactly what was accepted and displayed. If it is a reproducible calculation record, it may need enough inputs and algorithm versions to recompute. If it is a storage DTO, it may remain narrower while other evidence records hold provenance. One type need not serve every purpose, but the relationships must be explicit.

A replay should not silently rerate historical usage using today's price table and call that the original invoice. Preserve accepted price-version identities and amounts. If a correction is required, create a new correction event or document under the product policy. Historical evidence and a newly computed proposal are different outputs even when their totals happen to match.

## Test conversion without running a database

Construct a finalized invoice with two lines, distinct source references, one optional price identity, and a non-UTC input timestamp. Convert it and assert all copied values and the normalized finalization instant. Verify draft rejection separately. These are cheap deterministic tests that execute the exact conversion code.

Then demonstrate the ownership boundary by retaining the snapshot's concrete array and replacing one element in a disposable fixture. Observe that the Invoice remains unchanged while the snapshot collection changes. The test is a characterization of the current representation, not an instruction to mutate application records. A proposed immutable representation should have a different acceptance test.

Finally, create a direct snapshot with deliberately inconsistent fields and pass it only to a recording fake store. This shows that a fake accepting any object does not validate persistence preconditions. The lesson is not to make the fake perform hidden business logic, but to identify the application boundary where validation should occur.

## Exercise MF-15A: audit the conversion map

List every invoice and line field copied by From and identify which assertions the current tests cover. Design three additional discriminating assertions without repeating the same identity check under different names. Include timestamp normalization and optional price evidence.

## Exercise MF-15B: prove collection ownership

Demonstrate the difference between the invoice's line collection and the snapshot's projected array using synthetic data. Propose a boundary that prevents later snapshot mutation, and explain its allocation and API consequences. Do not call IReadOnlyCollection deeply immutable without evidence.

## Exercise MF-15C: choose a snapshot purpose

Choose issued-document evidence, reproducible calculation evidence, or storage transfer as the primary purpose. Define required fields and links to other evidence. Work a later price change and explain why the original accepted amount must remain distinguishable from a new calculation.

## Review checkpoint

A snapshot is trustworthy because its construction, ownership, and persistence contracts are understood. Its name alone supplies none of those guarantees. A strong answer identifies which invariants the factory enforces, which the public record permits callers to bypass, and which a persistence workflow must enforce independently.
