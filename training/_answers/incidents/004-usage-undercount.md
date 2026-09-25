# INC-004 — sealed answer: a case-insensitive collation merged distinct event ids

## Root cause
The migration `UseDatabaseDefaultCollation` changed `billing.usage_events.external_event_id` from `Latin1_General_100_BIN2` (binary, case-sensitive)
to the database default `SQL_Latin1_General_CP1_CI_AS` (**case-insensitive**). From then on:
- the dedupe lookup `WHERE tenant_id = @t AND external_event_id = @e` matches `evt_7hQ2mZx1` when asked for `evt_7Hq2mZX1`;
- `UsageIngestionService` treats the new event as a **replay**, returns the old event with `200 isReplay: true`, and never stores it;
- the unique index `ux_usage_events_tenant_external` would also reject it (2601) if two such events raced.

The customer's ids are 8 random base62 characters: ids that differ only by case collide far more often than true duplicates, and the rate grows
with the tenant's volume (birthday effect over 62 vs 36 symbols per position). No error anywhere: the system did exactly what the collation says.

**Why the migration didn't fail:** EF's `AlterColumn` with a collation change on SQL Server drops and recreates the dependent indexes; the unique
index rebuild succeeds because no two **existing** rows collided case-insensitively at that moment. Had they, the migration would have failed with
"CREATE UNIQUE INDEX statement terminated because a duplicate key was found", which would have been the better outcome.

## Diagnostic path
1. All 2xx, counts consistent inside our system → loss happens at acceptance (dedupe), not in rollups.
2. `isReplay` rate jumped on a date → correlate with deploys (Aug 31 migration).
3. The SDK sample shows two ids that differ only by case → `SELECT CASE WHEN 'evt_7hQ2mZx1' = 'evt_7Hq2mZX1' THEN 'same' END` returns `same`.
4. `SELECT name, collation_name FROM sys.columns WHERE object_id = OBJECT_ID('billing.usage_events')` shows the new collation.

## Fix
1. Revert the model and add a migration restoring `UseCollation("Latin1_General_100_BIN2")` (online index rebuild if possible).
2. Fix the reporting join the right way: `... JOIN #provider_ids p ON p.external_event_id = e.external_event_id COLLATE Latin1_General_100_BIN2`,
   or create the temp table column with the same collation (`external_event_id varchar(128) COLLATE Latin1_General_100_BIN2`). Temp tables take `tempdb`'s collation by default, not your database's: that's the real source of the "collation conflict" (and on Azure SQL Database, `tempdb` follows the database collation, so the error may not even occur there).
3. Regression test: J3's `Ingest_ExternalIdsThatDifferOnlyByCase_AreTwoEvents` against a real SQL Server.

## Recovering the lost usage
The lost events were never stored, so the database can't list them. Sources: the customer's send log (they have one), and our API access
logs / structured logs for `isReplay=true` responses since Aug 31 22:00 where the request's `externalEventId` differs **in case** from the
returned event's `externalEventId` (the response body includes both). Re-ingest those events after the collation fix (they'll be accepted as new),
then issue invoice adjustments for any closed period (credit/debit notes, never edit finalized invoices).

## Lab reproduction
**Offline-verified:** `evidence/inc-004-migration.sql`: the migration drops the unique index, runs `ALTER COLUMN [external_event_id] varchar(128) NOT NULL` with no `COLLATE` (so the database default applies), and recreates the unique index, in one transaction.
**UNVERIFIED (needs a live SQL Server):** on the incident branch, `SqlLab migrate`, `SqlLab ingest tenant-0042 evt_7hQ2mZx1`, `SqlLab ingest tenant-0042 evt_7Hq2mZX1` → expected `CREATED` then `REPLAY` (lab branch: `CREATED` twice).

## Postmortem (model)
- **Impact:** ~3% of Acme's events (and other high-volume tenants with mixed-case ids) silently dropped as replays for 14 days → under-billing.
- **Root cause:** collation change on an opaque case-sensitive key; the reason for the per-column collation wasn't written anywhere except a code comment that the PR deleted.
- **Why not caught:** no test against a real SQL Server; EF InMemory compares ordinally; the migration diff looked like a no-op "cleanup".
- **Actions:** restore BIN2; real-DB test; alert on replay-rate change per tenant; reviewers read generated migration SQL; comment → ADR for the collation decision.
