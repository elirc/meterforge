# 3. Observe one winner without claiming distributed durability

[InMemoryUsageEventStore](../../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs) uses ConcurrentDictionary.GetOrAdd on the tenant/external-event tuple. The application compares the returned winner with its candidate. A concurrent experiment can verify the observed contract for this in-process store. It cannot prove persistence across restart, database uniqueness or cross-process coordination.

## Equivalent concurrent calls

Start many ingestion calls with the same normalized key and payload. Collect returned event IDs and replay flags. Under the intended contract, all results refer to one stored identity and exactly one candidate is reported as the original winner. The rest are replays. Count distinct IDs and false replay flags independently.

Use a start gate if you want all worker tasks ready before release. Task.Run alone can create concurrent scheduling opportunities but does not prove every call overlapped at the dictionary operation. Report the experiment accurately. The implementation's atomic dictionary contract and the observed run together support a scoped conclusion; they do not enumerate every machine schedule.

## Competing payloads

Now split callers between quantities two and three under the same key. One payload wins. Calls with equivalent winning payload can replay successfully, while calls with the losing payload should conflict. The test should not assume quantity two always wins. Capture the stored winner and derive the permitted result categories from that independently observed payload.

Avoid an oracle that simply accepts any error. Assert the specific business-rule code for conflicts and verify no unrelated exceptions. Confirm the stored event is one of the proposed candidates and has not blended fields from both. The winner is a whole UsageEvent, not a field-by-field merge.

## Different tenants

Run the same external ID in two tenants with different payloads. Each tenant should have its own winner under the tuple key. A global external-ID dictionary would incorrectly collapse them. Add tenant-scoped FindById checks so one tenant cannot retrieve another's event through this store method.

This establishes store scoping, not authenticated authority. A caller who can choose an arbitrary tenant string may still access the wrong scope through a higher layer. The API must derive or validate tenant authority separately. The experiment should state that it constructs trusted test tenant strings directly.

## Restart experiment

Create a new in-memory store instance after a successful ingestion. Reusing the same external ID can create a new event because the previous dictionary is gone. This is expected for the in-memory adapter. It demonstrates why a production durability claim needs a persistent unique constraint or equivalent storage contract.

A proposed durable adapter should atomically insert or return the winner, compare payload equivalence and preserve the stored identity across process restarts. A check-then-insert sequence without a constraint can race across processes. A database test should include concurrent connections, duplicate keys and restart/reconnect behavior under a disposable environment.

## Cancellation

The service and store check cancellation before their immediate work. A pre-canceled token should reject without adding an event. This does not imply a caller can cancel after a winner is stored and thereby retract usage. If a future asynchronous store observes cancellation after commit, the caller may need to recover the original event by its stable key.

## Deliverable

Submit equivalent, conflicting, cross-tenant and restart experiments with exact counts and scope. Include one mutation replacing GetOrAdd with an unsafe check-then-write in a disposable model and explain how a controlled schedule exposes it. A strong report uses the phrase in-memory winner contract and avoids turning it into a claim of globally exactly-once billing.

[Studio index](README.md) · [Review guide](reviews/01-oracles-and-usage.md)
