# Sprint 14: Security, performance, and flaw harvest

Counterexamples expose five defects from earlier sprints before the fixes are committed. The repairs strengthen tenant-bound deduplication, non-overlapping tier arithmetic, finalized invoice immutability, atomic subscription mutation, and stable payment retry identity.

The threat model identifies authentication, SSRF, secrets, PII/financial telemetry, resource exhaustion, and database operations as real deployment work. Performance guidance remains evidence-led: bound collections and payloads, index tenant/time queries, load-test rating and ingestion, and avoid caching mutable financial decisions without versioned keys.
