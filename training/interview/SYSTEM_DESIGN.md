# System design — take MeterForge to 50× scale, multi-region, enterprise tenants

**Prompt (interviewer reads).** MeterForge today: one API process, in-memory storage, and a SQL Server lab adapter. Target in 12 months:
20,000 tenants, 50× today's volume (peak 150k usage events/s), EU and US data residency, and enterprise tenants who need hard caps,
audit-grade invoices and a 99.95% ingestion SLO. Design ingestion → storage → rollups → invoicing.

**Expected spine of a good answer.**
1. Ingestion API stateless behind a load balancer; accept → validate → durable append (log/queue or partitioned table) → 202/201; dedupe by `(tenant_id, external_event_id)`.
2. Storage: per-region databases (residency), tenant → region routing table; `usage_events` partitioned by time; separate dedupe key table.
3. Rollups: streaming or micro-batch aggregation, idempotent by event id; rollups are the source for invoicing after a lateness window closes.
4. Invoicing: per-tenant jobs, immutable snapshots, provenance to rollup versions.
5. Enterprise: hard caps via atomic counters (see the change request), audit trail, per-tenant SLO dashboards.

## Pressure points (interviewer: pick 4–5, push on mechanism)
- "Your dedupe index is per region. A tenant's SDK fails over from EU to US mid-retry. What stops a double charge?"
- "150k events/s into one SQL Server table: what's the first thing that breaks? (last-page latch on an identity key, log throughput, the GUID index)"
- "The whale tenant is 30% of all traffic. How do you keep it from hurting the other 19,999 on shared databases?"
- "A partition switch for retention needs aligned unique indexes. Your dedupe uniqueness isn't aligned. Now what?"
- "Rollups lag 10 minutes during the end-of-hour peak. The customer's hard cap is enforced from rollups. What happens?"
- "An EU tenant's usage page is served from a read replica. What consistency does the customer see right after sending an event?"
- "How do you move one enterprise tenant to its own database with zero downtime?" (link S1)
- "How do you prove to an auditor that invoice #X's total came from exactly these events?"

**Scoring.** Strong: every component has a failure story (what if it's slow, down, or runs twice). Weak: boxes and arrows with "Kafka" and "sharding" but no answer to the failover-dedupe question.
