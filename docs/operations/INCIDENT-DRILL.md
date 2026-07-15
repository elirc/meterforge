# Simulated incident: duplicate collection after timeout

A fictional provider commits a charge but its response times out. The original implementation generates a new provider idempotency key on retry, allowing a duplicate collection. Reconciliation finds two provider charges for one internal payment.

Containment disables collection for the tenant while preserving invoice, payment ID, both provider references, trace IDs, and UTC timeline. Recovery refunds the duplicate with a new linked record, then replays only after the retry key is stable.

Sprint 14 repairs the root cause: the logical provider idempotency key remains unchanged while attempt number increments. The regression compares identity before and after retry. No real provider, customer, or money is involved in this drill.
