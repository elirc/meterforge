# M4 — What EF Core actually sends: split queries, tracking, compiled queries

**Context.** The lab branch's EF code:
- `SqlServerInvoiceSnapshotStore.GetAsync` loads an invoice with `.Include(i => i.Lines)`.
- `SqlServerUsageEventStore.FindByExternalIdAsync` is called once per ingested event (400/s at peak) and uses `AsNoTracking()`.
- `UsageReadModel.SummarizeAsync` uses `GroupBy` + `Sum` + `Count` projected into a record.

**Ticket.**
> We're about to put invoicing (100–5,000 lines per invoice) on SQL Server. Show me the SQL EF generates for each of these
> three paths, fix whatever is wasteful, and prove the fix with numbers. No "I read that split queries are better".

**Constraints.** Log SQL with `optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)` or `ToQueryString()`. Measure with
`SET STATISTICS IO` or a stopwatch over 1,000 iterations in the SqlLab harness (add a command). One PR-sized change.

**Definition of done.**
- [ ] The generated SQL for all three paths, pasted, with a one-line reading of each (joins, ORDER BY, which columns).
- [ ] `Include` single query vs `AsSplitQuery()` for an invoice with 2,000 lines: rows transferred, reads, time. And the consistency risk of split queries without a transaction.
- [ ] Tracking vs `AsNoTracking()` on the dedupe lookup: allocations/time over 1,000 calls, and which code path would **break** if someone removed `AsNoTracking()` (hint: `AddAsync` attaches a row with the same key).
- [ ] `EF.CompileAsyncQuery` for the dedupe lookup: measured gain, and why it's small in EF Core 10 (query cache) but not zero.
- [ ] One query that EF translates in a way that surprised you (e.g. `GroupBy` with `First()`, string `Equals` with `StringComparison`, `DateOnly` from `datetimeoffset`), with the SQL.

## Explain before touching
1. For `Include` on a one-to-many collection, what's the "cartesian explosion", and does it apply to one invoice + 2,000 lines?
2. What does the change tracker do on a tracked query that a no-tracking query skips? When is tracking correct?
3. What work does a compiled query skip? What does EF already cache without it?
4. Which of these three paths benefit from `TagWith`, and how does it help you in `sys.dm_exec_query_stats`?

<details><summary>Hints</summary>

- `Microsoft.EntityFrameworkCore.Database.Command` is the log category for SQL.
- A split query needs `ORDER BY` on the principal key in the first query so the second can be stitched back together.
</details>

Answer: [`_answers/M4.md`](../_answers/M4.md)
