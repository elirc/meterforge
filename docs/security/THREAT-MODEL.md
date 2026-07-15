# Threat model

## Protected assets

Tenant billing configuration, usage, invoices, credits, payment references, webhook secrets, and financial/audit history. The teaching repository contains synthetic data and no card or bank details.

## Trust boundaries and abuse cases

- HTTP tenant IDs are untrusted; production must derive tenant access from authenticated claims and enforce scope before every use case.
- Idempotency keys are tenant-scoped and bounded; global keys enable cross-tenant interference and unbounded retention enables storage exhaustion.
- Usage quantity, price tiers, discounts, and provider results are hostile input until validated.
- Webhook endpoints create SSRF and exfiltration risk; production needs allow/deny policy, DNS/IP revalidation, HTTPS, HMAC signatures, replay windows, and secret rotation.
- Logs and traces must not contain provider tokens, webhook secrets, customer details, or complete financial payloads.
- PostgreSQL credentials belong in a managed secret store. Migrations, least privilege, encryption, retention, restore, and access audit are deployment responsibilities.

## Current limits

The API has no authentication and defaults to volatile in-memory adapters. It is a curriculum, not a deployable payment or financial system.
