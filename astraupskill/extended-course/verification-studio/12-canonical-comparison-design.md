# 12. Specify canonical invoice comparison

This lesson is a design exercise prompted by the replay behavior in [the snapshot store](../../../src/MeterForge.Infrastructure/Persistence/PostgresInvoiceSnapshotStore.cs). It does not add a hashing or equivalence implementation to MeterForge. The goal is to define what exact replay should mean before choosing serialization and cryptographic tools.

## Identity is not content

Start with two questions. Does this request refer to the same logical invoice? Does it contain the same immutable invoice content? Tenant plus invoice ID can answer the first within the application's identity model. It cannot answer the second. A stable ID with changed amount is the smallest counterexample.

List candidate content fields from [InvoiceSnapshot](../../../src/MeterForge.Application/Persistence/InvoiceSnapshot.cs): subscription, currency, period, finalized instant and every line's identity, description, amount, currency, optional price version and source reference. Decide whether line order is meaningful. The snapshot type exposes a collection, but the business contract must determine whether ordering is part of identity or presentation only.

## Normalize only by an explicit rule

Decimal values 1.0 and 1.00 may represent the same numerical amount, yet naïve textual encodings can differ. UTC and another offset can represent the same instant. Currency normalization already occurs on the domain construction path, but a directly constructed snapshot may carry different text. The comparison contract must say which representations are equivalent.

Do not trim every string reflexively. SourceReference may encode an external identifier whose whitespace is significant in a future adapter. Domain AddLine trims at construction today, but that does not justify arbitrary transformations in every incoming record. Define normalization per field and explain why it preserves meaning.

## Sorting and duplicate lines

If line order is not meaningful, sort by a stable unique identity before encoding. First validate that identities are actually unique within the snapshot. Otherwise two lines with the same ID and different content create ambiguity: sorting alone does not determine whether the payload is valid, which duplicate wins, or whether both should be rejected.

Use an explicit field order and a format that cannot confuse separators inside descriptions with field boundaries. Length-prefixing, a well-defined canonical structured encoding, or a tightly specified serializer can each work if implemented and tested correctly. A hand-built string with unescaped delimiters is an easy source of collisions at the representation level even before any hash function is considered.

## Version the contract

Canonicalization rules evolve. Adding a tax jurisdiction field or changing time precision can alter the fingerprint for otherwise identical old documents. Include a canonicalization version and retain enough original values to compare or migrate old records. A bare hash without an algorithm and schema version is a weak long-term audit artifact.

Explain the migration path. Existing snapshots might receive fingerprints under version one. New saves might use version two. A replay of an old document should compare under its recorded rules or use an explicit compatibility procedure. Silently recomputing every old fingerprint under new rules can make historical evidence appear to change.

## Hashes do not supply authorization

A content hash can help detect mismatch. It does not prove that the caller was allowed to create an invoice, that a price was authorized, or that the source evidence was true. A perfectly stable fingerprint can faithfully identify an incorrect invoice. Keep provenance, authorization, arithmetic correctness and replay equivalence as separate review questions.

Likewise, comparing a client-supplied hash without independently encoding the supplied content trusts the client's assertion. The server should compute or validate the representation at its own boundary if the hash influences persistence decisions. This is a design requirement for the proposal, not an assertion about a current MeterForge endpoint.

## Test vectors

Prepare pairs that should compare equal: decimal scale variants, equivalent timestamp offsets and permuted line order if order is declared irrelevant. Prepare pairs that should differ: changed amount, changed subscription, changed source reference, changed line identity and changed period. Prepare invalid cases: duplicate line IDs, missing required evidence and unsupported canonicalization version.

Every vector should include the expected normalized field representation before a hash is calculated. This allows a reviewer to inspect semantics without trusting a digest. A second implementation can then consume the same vectors as an interoperability check. Do not generate the expected digest using the implementation under test during each assertion.

## Exercise

Write a one-page canonical comparison specification and ten vectors. Include one ambiguous case for which the team must choose policy, such as description whitespace or line ordering. Then design the save response for exact replay, conflicting replay and unknown durable outcome. The response should help a caller choose a safe next action without pretending that a hash resolves every uncertainty.

[Studio index](README.md) · [Review](reviews/04-persistence-and-evidence.md)
