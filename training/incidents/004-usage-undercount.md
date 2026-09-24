# INC-004 — Customer says we billed 3% fewer API calls than they sent

**Branch:** `training/incidents/004-usage-undercount` (based on `training/lab/sqlserver`)

**Ticket from Customer Success (Wednesday)**
> Acme Robotics (`tenant-0042`) exported their own send log for September 1–14: 18,402,117 `api-calls` events, all acknowledged by our API
> (their SDK logs every response). Our usage page shows 17,851,322. They're happy to pay less, but their finance team wants to know which number
> the invoice will use, and why they differ. Two other tenants reported similar gaps after we asked around.

**What on-call found so far**
- No 5xx or 4xx spike on ingestion. Every one of the customer's events got a 2xx.
- The share of responses with `"isReplay": true` for Acme went from ~0.4% (normal SDK retries) to 3.4% starting September 1.
- Acme's SDK generates `external_event_id` as 8 random base62 characters (`[0-9A-Za-z]`), e.g. `evt_k2QwZ8aB`.
- The rollup job and the usage page agree with each other; both agree with `SELECT COUNT(*) FROM billing.usage_events WHERE tenant_id = 'tenant-0042'`.

**Sample from the customer's SDK log**
```
2026-09-03T11:02:17.221Z POST usage-events external_event_id=evt_7hQ2mZx1  -> 201 id=3c1d...  isReplay=false
2026-09-03T11:02:17.908Z POST usage-events external_event_id=evt_7Hq2mZX1  -> 200 id=3c1d...  isReplay=true
```

**Deploys around September 1**
- Aug 31, 22:00 UTC: release 2026.08.31 with the PR below and its migration (ran in 40 s, no errors).
```diff
 chore(db): drop per-column collation override on usage_events

 The finance reconciliation export joins usage_events to a #temp table of
 provider event ids and fails with "Cannot resolve the collation conflict
 between Latin1_General_100_BIN2 and SQL_Latin1_General_CP1_CI_AS in the
 equal to operation". ...

-            // Producer-supplied idempotency key. Producers treat it as opaque and case-sensitive.
-            usage.Property(e => e.ExternalEventId).HasColumnName("external_event_id").HasMaxLength(128).IsUnicode(false)
-                .UseCollation("Latin1_General_100_BIN2");
+            // Producer-supplied idempotency key.
+            usage.Property(e => e.ExternalEventId).HasColumnName("external_event_id").HasMaxLength(128).IsUnicode(false);
+ Migrations/20260924221417_UseDatabaseDefaultCollation.cs (AlterColumn external_event_id)
```

**Your job.** Reproduce the gap in the lab container (`SqlLab migrate` on this branch, then `SqlLab ingest <tenant> <id>` twice with ids that differ in case),
explain the mechanism and why nothing errored, fix it (including the reporting join that motivated the PR), and work out how to find and bill the
lost events for September. Write the postmortem.

Sealed: [`_answers/incidents/004-usage-undercount.md`](../_answers/incidents/004-usage-undercount.md)
