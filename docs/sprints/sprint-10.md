# Sprint 10: Payment-provider integration

The application owns the collection use case while `IPaymentProvider` isolates provider request/response shapes. Payment attempts retain invoice, amount, attempt number, idempotency identity, status, and provider evidence.

Timeouts are ambiguous: the provider may have committed even when the caller saw no response. Retry design must therefore preserve the logical payment identity and reconcile provider state rather than assuming failure.
