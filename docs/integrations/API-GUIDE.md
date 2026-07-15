# Integration guide

- All public teaching routes use `/api/v1/tenants/{tenantId}`. A path tenant is untrusted; production authorization must match authenticated tenant claims.
- Send `Idempotency-Key` for retryable usage/payment commands and reuse it for the same logical operation.
- Send `If-Match` with the version read before an amendment. On conflict, fetch current state and reconcile rather than overwriting.
- Parse Problem Details by status and stable `code`; do not branch on human `detail` wording.
- Record `X-Trace-Id` for support correlation without embedding PII or secrets.
- Treat `404` for a foreign tenant resource as intentional information minimization.
- Webhook consumers deduplicate stable message/delivery IDs and verify signatures/replay windows in a real implementation.
- Evolve v1 additively. Removing fields, changing meanings/requiredness, or altering idempotency/version semantics requires a new version and migration window.
