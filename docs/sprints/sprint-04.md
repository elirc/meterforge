# Sprint 4: Idempotent usage ingestion

Usage events carry a caller-owned external identity. Replaying a logical event returns the original record rather than double-counting quantity. The endpoint distinguishes first acceptance (`201`) from replay (`200`).

The design keeps occurrence time separate from receipt time and uses UTC timestamps. Later hardening will challenge the exact scope and atomicity required by the deduplication guarantee.
