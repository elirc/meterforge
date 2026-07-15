# Sprint 8: Subscription amendments and optimistic concurrency

Plan changes require the caller's expected aggregate version through `If-Match`. A stale writer receives a stable conflict code instead of silently overwriting a decision made after its read.

Optimistic concurrency is an end-to-end contract: HTTP carries the expectation, the application compares it, the aggregate advances it, and the persistence boundary must eventually make comparison plus mutation atomic.
