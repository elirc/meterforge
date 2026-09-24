# Change request — "Usage caps: reject ingestion over a hard limit"

**From the product manager.**
> Enterprise tenants want a hard monthly cap per meter (e.g. 10M `api-calls`). When a subscription reaches its cap, new usage
> events for that meter must be rejected with a clear error, so the customer's app can stop calling. Soft warnings at 80% and 100%
> go out by webhook. The cap must hold even when their SDK sends from 40 servers at once.

**Before any agent writes code, you write down (1 page max):**
1. **Where it belongs.** Which project and file gets the cap rule, which gets the check, which gets storage. Name existing types you'd reuse (`Domain/Entitlements/Entitlements.cs` has `EntitlementEvaluator`; is it the right home?).
2. **Data changes.** Where caps are stored, and where "consumed so far this period" comes from (raw `usage_events`? `usage_daily_rollups`? a new counter row?). On SQL Server: which table, which key, which index.
3. **The concurrency story.** 40 servers, one cap. What stops 41 events getting through when 1 unit is left? (Link: [03](../../../opusorganize/apprenticeship/curriculum/03-concurrency-races.md); don't re-derive it, apply it.) What does that do to ingestion throughput and to deadlock risk with the rollup job?
4. **Replays.** An event that was accepted before the cap was hit is retried after. Accept or reject?
5. **API contract.** Status code and `code` for "cap reached"; is it a `V1` change or `V2`?
6. **Webhooks.** How the 80%/100% notifications are emitted exactly once per period (the outbox table exists; nothing writes it).
7. **Tests.** Which tests at which layer: domain rule, application, SQL Server integration (J3 pattern), and a concurrency test.

Then hand your design to an implementer agent (see `training/agentic/WORKFLOW.md`) and review what comes back against it.

Sealed reference placement: [`_answers/change-request.md`](../_answers/change-request.md)
