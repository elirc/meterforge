# Sprint 7: PostgreSQL-shaped durable adapter

The first durable boundary stores a finalized invoice and all lines in one PostgreSQL transaction. Composite tenant/resource keys make ownership part of relational identity, and decimal/check constraints defend important invariants below the application.

The API still defaults to in-memory adapters so CI has no external dependency. The PostgreSQL adapter is real, parameterized code with a migration and local Compose service; wiring it for deployment requires configuration, migration ownership, credentials, backup, and restore evidence.
