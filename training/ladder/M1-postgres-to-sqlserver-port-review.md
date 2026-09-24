# M1 — Port review: what changes when MeterForge's schema moves from Postgres to SQL Server

**Context.** An enterprise customer's procurement requires SQL Server (Azure SQL). The Postgres schema is in
`src/MeterForge.Infrastructure/Persistence/Migrations/001_invoices.sql` and `002_outbox.sql` on `main`. The lab branch's
`InitialSqlServer` migration is someone's first port. Ports like this fail in ways that compile and pass tests.

**Ticket.**
> Before we promise anything to the customer, review the port column by column. For every difference, tell me whether it
> changes behaviour, and prove the risky ones with a query against the lab container.

**Constraints.** Proofs are short T-SQL snippets you run in the container, with their output pasted into your answer. No code changes in this ticket.

**Definition of done** — a table with one row per topic, each with *Postgres behaviour · SQL Server behaviour · MeterForge column(s) affected · risk · proof query*:

1. **Collation / case sensitivity**: `text` (Postgres, deterministic, case-sensitive) vs the server default `SQL_Latin1_General_CP1_CI_AS`. Which MeterForge keys are case-sensitive in meaning? (`external_event_id`, `tenant_id`, `meter`, product `code`, currency)
2. **Trailing spaces**: `'evt1' = 'evt1 '` in each database. Which lookup does this touch?
3. **Time**: `timestamptz` vs `datetimeoffset` (vs `datetime2`). What is stored, how are offsets compared, and what EF maps `DateTimeOffset` to.
4. **Money**: `numeric(19,2)` vs `decimal(19,2)`; what happens on insert of `27.765` through T-SQL, through a `decimal(18,2)` EF default mapping, and through `SqlParameter` with `Scale = 2`.
5. **Identity vs sequences**: `usage_events.seq` is `IDENTITY`. When would you want `CREATE SEQUENCE` instead (hint: ids known before insert, shared across tables, EF HiLo)? What happens to identity values on rollback?
6. **JSON**: `jsonb` (validated, binary, GIN-indexable) vs `nvarchar(max)` + `ISJSON` check. How would you index `payload.invoiceId` on SQL Server?
7. **Filtered vs partial indexes**: `ix_outbox_unpublished ... where published_at is null`. What query shapes can and cannot use the SQL Server filtered index?
8. **`on conflict do nothing`** in `PostgresInvoiceSnapshotStore.cs`: what's the SQL Server equivalent, and is `SqlServerInvoiceSnapshotStore`'s "check then insert" equivalent under concurrency?
9. **`char(3)` currency**: trailing-space padding and comparisons.

## Explain before touching
1. Which of these differences can silently change a **bill**? Rank them.
2. Which can be caught by the existing unit tests on `main`? Which only by tests against a real SQL Server (see J3)?
3. For each risky one: is the fix in the EF model, in the migration, in the domain, or in the API contract?

<details><summary>Hints</summary>

- `SELECT SERVERPROPERTY('Collation'), DATABASEPROPERTYEX('MeterForge','Collation')`.
- `SELECT CASE WHEN 'evt_aB3' = 'evt_Ab3' THEN 'same' ELSE 'different' END` — then add `COLLATE Latin1_General_100_BIN2`.
- `SELECT CAST(27.765 AS decimal(19,2))` rounds. SqlClient parameters with a smaller scale do something else. Test both.
</details>

Answer: [`_answers/M1.md`](../_answers/M1.md)
