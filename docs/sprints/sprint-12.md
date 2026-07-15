# Sprint 12: Entitlements and usage limits

Entitlement evaluation answers a commercial question: did this tenant purchase a feature or included quantity? It returns allowed/denied, remaining quantity, and a stable reason. Authentication and role authorization remain separate security concerns.

The evaluator is deterministic and side-effect free. A consuming use case must still atomically record usage if oversubscription at a concurrency boundary would violate the commercial limit.
