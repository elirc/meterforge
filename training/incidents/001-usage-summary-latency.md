# INC-001 — Usage page slow for small tenants since Tuesday

**Branch:** `training/incidents/001-usage-summary-latency` (based on `training/lab/sqlserver`)

**Alert (Tuesday 14:05 UTC)**
```
[P2] usage-summary p95 > 1500 ms for 15 min
GET /api/v1/tenants/{tenantId}/usage-summary   p95 1,940 ms   (7-day baseline 45 ms)   error rate 0.0%
```

**What on-call saw**
- Support: 11 tickets since the morning, all small or trial tenants ("usage page spins for 2 seconds"). `tenant-whale` (the largest customer, ~90% of all events) says its page is fine.
- Database CPU 22% → 71% during business hours. No blocking alerts.
- Monday night: routine patching restarted the SQL Server instance (failover to the secondary and back, 02:10 UTC).
- Deploys this week: only the PR below (Monday 16:30 UTC). It went out 21 hours before the alert, and the first 10 hours after it looked normal.

**Metrics excerpt (usage-summary, per hour, UTC)**
```
hour   calls   p50_ms  p95_ms  db_cpu%
Mon 15   9.1k     11      42     20
Mon 17   9.4k     12      44     21   <- after deploy
Tue 01   1.2k     10      39     12
Tue 03   0.9k   1,350   1,720     35   <- after the 02:10 restart
Tue 09   8.8k   1,380   1,910     69
Tue 13   9.6k   1,420   1,940     71
```

**Application log (sampled)**
```
14:02:11 INF usage-summary tenant=tenant-0187 groups=9  elapsed=1391ms
14:02:11 INF usage-summary tenant=tenant-0042 groups=9  elapsed=1402ms
14:02:12 INF usage-summary tenant=tenant-whale groups=90 elapsed=1288ms
14:02:12 INF usage-summary tenant=tenant-0003 groups=6  elapsed=1377ms
```

**Suspect PR (merged Monday 16:30)**
```diff
 perf(usage): parameterize tenant in usage summary query

 DBA flagged ~38k single-use plans for the usage-summary query in the plan
 cache (one per tenant, 1.9 GB of plan cache). Send the tenant id as a
 parameter like every other predicate so SQL Server reuses one plan.

--- a/src/MeterForge.Infrastructure.SqlServer/UsageReadModel.cs
+++ b/src/MeterForge.Infrastructure.SqlServer/UsageReadModel.cs
-            .Where(e => e.TenantId == EF.Constant(tenantId) && e.OccurredAt >= from && e.OccurredAt < to)
+            .Where(e => e.TenantId == tenantId && e.OccurredAt >= from && e.OccurredAt < to)
```

**Your job.** Find the root cause, prove it against the lab container, fix it without bringing back 38k plans, and add a guard so it can't
silently come back. Reproduce with `SqlLab seed`, then `SqlLab bench-sniff` (read its options). Write the postmortem.

Sealed: [`_answers/incidents/001-usage-summary-latency.md`](../_answers/incidents/001-usage-summary-latency.md)
