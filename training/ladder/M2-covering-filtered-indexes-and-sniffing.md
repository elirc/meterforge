# M2 — The usage page at 1M rows: covering index, filtered index, and plans that depend on the tenant

**Context.** From J2 you have numbers for `UsageReadModel.SummarizeAsync`: a small tenant does an index seek plus key lookups,
and the whale does something else entirely. `SummarizeAsync` inlines the tenant with `EF.Constant(tenantId)`, so every tenant
gets its own compiled plan. The rollup job (`UsageRollupJob.cs`) finds work through the filtered index
`ix_usage_events_unrolled (occurred_at) WHERE rolled_up = 0`.

**Ticket.**
> 1. Make the usage summary cheap for **every** tenant size, with one index change. Show before/after logical reads for tenant-0001 and tenant-whale.
> 2. The DBA wants `EF.Constant` gone ("40k single-use plans"). Tell me whether we can remove it after your index lands, and prove it.
> 3. Prove the rollup job's filtered index is used, and show one realistic query change that would silently stop it being used.

**Constraints.** The index change is an EF Core model change + a new migration on your own branch off `training/lab/sqlserver`.
Write the migration so it can run on a busy 100M-row table (read the generated SQL: `dotnet ef migrations script`).
Use `sql/01`, `sql/02` and `sql/04` for evidence.

**Definition of done.**
- [ ] New index designed as key columns + `INCLUDE` columns; the migration SQL builds it `ONLINE = ON` (and you know which editions support that).
- [ ] Before/after table: logical reads, CPU ms, plan shape, for both tenants.
- [ ] With `EF.Constant` removed on a scratch branch, run `SqlLab bench-sniff --first tenant-whale` and `--first tenant-0001`, **before and after** your index. Paste the four results.
- [ ] Filtered index: the rollup's plan uses it; then show a variant (e.g. `rolled_up = @p` from EF, or `rolled_up <> 1`) that doesn't.
- [ ] Write-cost paragraph: what your index adds to every `INSERT` into `usage_events`, and how you would measure it.

## Explain before touching
1. Which columns go in the key, in what order, and which go in `INCLUDE`? Why is `quantity` never a key column here?
2. What does "parameter sniffing" mean, exactly? When is the plan compiled, with which value, and when is it thrown away?
3. List four ways to deal with a parameter-sensitive query (e.g. `OPTION (RECOMPILE)`, `OPTIMIZE FOR`, literal inlining, a covering index, PSP optimization at compatibility level 160, Query Store plan forcing). What does each cost?
4. Why can't a filtered index `WHERE rolled_up = 0` serve a query written `WHERE rolled_up = @p`?

<details><summary>Hints</summary>

- `HasIndex(...).IncludeProperties(...)` and `.IsCreatedOnline()` in EF Core's SQL Server provider.
- A covering index removes the plan *choice*: seek + range scan is right for 50 rows and for 115k rows.
- `SqlLab bench-sniff` flushes the plan cache first, then runs the "first" tenant, then 50 small tenants.
</details>

Answer: [`_answers/M2.md`](../_answers/M2.md)
