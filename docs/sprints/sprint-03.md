# Sprint 3: Effective-dated price versions

Prices are immutable versions selected by billing date. Validity uses half-open ranges `[from, to)` so adjacent versions compose without sharing a day. Selection rejects missing and overlapping policy rather than guessing.

Exercise: change the end comparison to inclusive, run the boundary test, and explain how a one-character temporal bug can alter invoice provenance.
