# Agentic exercise rubric (M2)

| Score | Your explanation (Step 6) |
|---|---|
| 5 | Names the mechanism: the plan is compiled once per distinct query text with the first sniffed value; without a covering index the optimizer must choose between seek + key lookups (cheap for ~50 rows) and a scan (cheaper for ~115k rows), and that choice is wrong for one side. The covering index makes seek + range scan optimal for both, so plan reuse becomes safe. Quantifies the write cost (one more index maintained per insert, measured). Says when to go back: a query whose predicate cardinality varies wildly and which **can't** be covered. |
| 3 | Correct words ("sniffing", "covering") and numbers, but can't say why the index removes the risk, or what it costs writers. |
| 1 | "The index makes it faster and plan reuse is better." |

**Agent-output red flags you should have caught** (common in practice):
- Benchmarks run without `DBCC FREEPROCCACHE` between orders → the second run reuses the first run's plan and "proves" nothing.
- Migration builds the index offline (EF default) → on a 100M-row table that's a long blocking operation; needs `.IsCreatedOnline()`.
- Drops `ix_usage_events_tenant_occurred` "as redundant" without checking the other queries (the rollup job doesn't use it, but any tenant/time query without the included columns would still benefit from the narrower index, or a new one should replace it with a note).
- Claims "p95 improved" from a single run.
- Changes `SummarizeAsync`'s result ordering or shape (API contract drift).
