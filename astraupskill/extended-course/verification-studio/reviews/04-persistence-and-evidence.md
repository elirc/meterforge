# Review 4: exact replay and canonical comparison

This review covers chapters eleven and twelve. The relational scenarios are proposed tests based on inspected SQL. No PostgreSQL execution is supplied by the studio harness, so the answer should keep source predictions distinct from observed database results.

## Exact replay

An exact repeat with the same header key, line keys and values is expected to leave one header and the original lines under the intended conflict constraints. The proposed test must inspect values as well as counts and must run against the actual schema. A fake store cannot establish PostgreSQL conflict or rollback behavior.

The phrase same invoice is insufficient. Require tenant, invoice ID, every line ID and content. If line IDs are regenerated, a retry may contain new insertable keys even when descriptions and amounts look familiar. Identity evidence is essential to diagnosis.

## Conflicting replay

The inspected SQL ignores a conflicting header or line key rather than comparing incoming values with stored values. A changed amount under an existing line ID therefore motivates a characterization test expecting original data to remain without a mismatch-specific result from this method. A new line ID under an existing header motivates a test for document extension.

A stronger contract should classify exact equality and conflict separately. A transaction alone does not make an existing document immutable across future transactions. The proposal must ensure that the complete document comparison and persistence decision cannot race into a hybrid result.

## Rollback and acknowledgment

A rollback experiment needs a real failure after some work has occurred inside the transaction, plus inspection of durable rows afterward. The fixture should rely on an actual known schema constraint or an explicit isolated fault mechanism. An imagined constraint does not make a valid test plan.

An exception around commit may leave the caller uncertain about the durable outcome. Recovery by stable identity and content comparison is a proposed way to resolve that uncertainty. Do not classify every exception as definitely rolled back without evidence about where and how the failure occurred.

## Canonicalization answer

A defensible comparison specification names all immutable fields, normalizes decimal and timestamp representations intentionally, decides line-order significance, validates duplicate line IDs and uses an unambiguous encoding. It also records a canonicalization version. A hash is useful only as a representation of a defined content contract and when compared at the appropriate boundary.

Equivalent timestamp offsets should compare equal if the contract is based on instants. Decimal scale variants should compare equal if amounts are numerical values. Permuted lines compare equal only if ordering is explicitly irrelevant. Description whitespace is a policy choice that should not be hidden in a generic trim operation.

## Invalid vectors

Duplicate line IDs with different content should have an explicit rejection policy before hashing. Unsupported canonicalization versions should not be silently treated as current. Missing evidence and malformed currency require validation at the chosen boundary. A directly constructed snapshot cannot be assumed to have passed Invoice.AddLine or InvoiceSnapshot.From.

An answer that says the hash proves correctness is incomplete. The hash can identify content consistently while the content itself uses an unauthorized price or incorrect source evidence. Provenance, authority, arithmetic and replay equality remain distinct checks.

## Review completion

Have the learner present ten comparison vectors and three save histories: exact repeat, changed existing line and new line under an existing header. Ask how concurrent candidates are resolved and how historical data is audited before applying a new invariant. The learner passes when the proposed contract prevents a mixed document and its evidence plan includes actual relational tests still required for implementation.

[Studio index](../README.md)
