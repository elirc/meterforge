# Sprint 5: Tiered usage rating

MeterForge converts aggregated quantity into itemized tier charges with an explicit currency and rounding policy. The response keeps tier evidence so invoice reviewers can reconstruct a total rather than trusting an opaque number.

This is the `v0.5.0` checkpoint: a tenant can define a product, start a subscription, select dated pricing, ingest usage idempotently, and rate it. The components are deliberately not hidden behind a generic billing engine.
