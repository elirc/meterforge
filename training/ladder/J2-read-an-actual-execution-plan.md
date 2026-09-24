# J2 — Read an actual execution plan for the usage summary

**Context.** `UsageReadModel.SummarizeAsync` (lab branch, `src/MeterForge.Infrastructure.SqlServer/UsageReadModel.cs`) powers
`GET /api/v1/tenants/{tenantId}/usage-summary`, the page a customer opens to see "what will I be billed for this week".
After `SqlLab seed`, `billing.usage_events` holds about 1M rows: `tenant-whale` has 900k, and `tenant-0001`…`tenant-0300` have 400 each.

**Ticket.**
> Support says the usage page is "sometimes slow". Before anyone adds an index, I want a written reading of the actual plan
> for one small tenant and for the whale, from the database itself. Numbers, not adjectives.

**Constraints.** Use `training/lab/sqlserver/sql/01-usage-summary-plan.sql`. You may save each plan as `.sqlplan` and open it
in Azure Data Studio or SSMS, or read the XML directly. Don't add or change indexes in this ticket.

**Definition of done** — a table you filled in from the real output:

| | tenant-0001 | tenant-whale |
|---|---|---|
| Access operator on `usage_events` (seek or scan, which index) | | |
| Key Lookup present? How many executions? | | |
| Estimated rows vs actual rows at the access operator | | |
| `logical reads` on `usage_events` (from STATISTICS IO) | | |
| CPU ms / elapsed ms (from STATISTICS TIME) | | |
| Parallelism? (look for `Parallelism` operators / `DegreeOfParallelism`) | | |

Plus 3 sentences: why the two tenants got **different** plans, and what `EF.Constant(tenantId)` in `SummarizeAsync` has to do with it.

## Explain before touching
1. Print the SQL EF sends: `dotnet run --project tools/MeterForge.SqlLab -- summary tenant-0001 --show-sql`. Which values are literals, and which are parameters?
2. Which index can serve `WHERE tenant_id = … AND occurred_at >= … AND occurred_at < …`? Which columns does the query need that this index does **not** contain?
3. What is a Key Lookup, physically? Why does its cost grow with the number of matching rows and not with table size?
4. What is the difference between "estimated plan" and "actual plan", and why is the actual one required here?

<details><summary>Hints</summary>

- Look at `ix_usage_events_tenant_occurred` in `MeterForgeDbContext.cs`: key columns only, no INCLUDE.
- In the XML, `RunTimeCountersPerThread ActualRows` is the truth; `EstimateRows` is what the optimizer believed.
- The clustered index is `seq`. A lookup from a nonclustered index finds the base row by `seq`.
</details>

Answer: [`_answers/J2.md`](../_answers/J2.md). Captured reference output: `training/lab/sqlserver/evidence/` (open after you finish).
