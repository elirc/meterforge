# Sprint 11: Transactional outbox and webhooks

Outbox messages carry stable event identity, tenant, type, schema version, subject, UTC occurrence time, and payload. The PostgreSQL migration supports ordered unpublished work. A real use case must write business state and its outbox message in the same transaction.

Webhook retry preserves delivery and message identity while incrementing attempt. Consumers can therefore deduplicate a logical delivery even when network responses are lost.
