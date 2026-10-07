# S1 — Move MeterForge's billing data from Postgres to Azure SQL with zero downtime and a rollback

**Scenario.** MeterForge runs on Postgres (`001_invoices.sql`, `002_outbox.sql`, plus usage events you design on the lab branch's model).
A contract with an enterprise tenant group requires Azure SQL Database in their region within one quarter. Current numbers:
1.2 billion usage events (40 GB), 400 inserts/s average and 3,000/s peak (end of hour), 6M invoices with 90M invoice lines,
and a monthly invoicing run on day 1 that must not slip. Ingestion must keep accepting events throughout (SDKs retry for 24 h, and dedupe is by `external_event_id`).

**Write a design doc (max 3 pages)** that answers:
1. **Target schema.** Which lab-branch choices you keep (clustered `seq`, BIN2 collation on `external_event_id`, `datetimeoffset`, `decimal(19,4)` quantity / `decimal(19,2)` money, JSON in `nvarchar(max)`) and which you change for Azure SQL (compat level, RCSI default, `json` type availability, service tier).
2. **Data movement.** Bulk backfill vs change data capture (logical replication/Debezium into Azure SQL, or dual-write from the app). How you get a consistent cut-over point for an append-only table vs mutable tables (`invoices` are immutable once finalized; the outbox is mutable).
3. **Cut-over sequence** for reads and for writes, as numbered steps with the guard for each step (what you check before continuing). Where are the app flags?
4. **Dedupe across the move.** An SDK retries an event on the new database that was first stored on the old one. What stops a double charge?
5. **Verification.** Row counts aren't enough. Per tenant per day `SUM(quantity)` and per invoice `SUM(amount)` checksums, collation-sensitive key comparisons, and money scale checks.
6. **Rollback.** Up to which step you can go back, how writes made on Azure SQL flow back to Postgres, and the point of no return.
7. **Invoicing run.** How you guarantee day 1 invoices read exactly one source of truth.
8. **Failure modes.** Replication lag at the 3,000/s peak, a long-running backfill blocking ingestion, identity values colliding during dual-write, a timezone bug in `occurred_at`.

**Constraints.** You don't get to pause ingestion. You don't get to change the public API (`src/MeterForge.Contracts/V1`).
Budget: one extra Azure SQL tier for 6 weeks.

## Explain before touching
- Which MeterForge data is append-only and which is mutable? Why does that decide your replication strategy per table?
- What does "zero downtime" mean for the SDK (which retries) vs for the finance user (who runs invoicing)?

Link: 07 — zero-downtime migrations (apprenticeship page `07-zero-downtime-migrations`, not in this repo), 13 — release safety (apprenticeship page `13-release-safety`, not in this repo).
Answer (reference design + rubric): [`_answers/S1.md`](../_answers/S1.md)
