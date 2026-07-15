# Fifteen-sprint retrospective

The model grew from one fixed-price product into subscriptions, temporal pricing, usage, rating, invoices, PostgreSQL, concurrency, adjustments, payments, outbox/webhooks, entitlements, and reconciliation without moving core rules into controllers or provider adapters.

The most valuable teaching moments were the five counterexamples. Each changed a concrete guarantee and stayed local: tenant-scoped deduplication, non-overlapping tier arithmetic, invoice immutability, atomic compare-and-swap, and stable payment retry identity.

The process history is deliberately expensive. A normal small team should reduce milestones, issues, review narration, and project boundaries to what coordination and risk require. Read [OEReadMe](../../OEReadMe.md) before copying the structure.

The best next exercise is to wire PostgreSQL for a complete invoice/outbox use case, add Testcontainers adapter contracts, introduce authenticated tenant claims, implement a webhook worker, then perform load and restore drills. Do not add microservices until deployment, scaling, ownership, or security evidence requires them.
