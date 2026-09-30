# 2. Partition usage inputs by identity and meaning

The usage key is a tuple of tenant and external event ID. The payload comparison includes subscription, normalized meter, quantity and occurrence instant. The surrogate event ID identifies the stored winner but is newly generated for every candidate. A good test suite explores these dimensions separately so one passing replay case does not conceal a changed-payload bug.

## Equivalent representations

Take tenant T, external ID E, subscription S, meter requests, quantity two and instant 12:00 UTC. Repeat with spaces around the meter and external ID. The constructor trims those fields, so the normalized candidate should match. Repeat with an offset representation of the same instant, such as 14:00 at plus two hours. Conversion to UTC preserves the instant, so the payload comparison should still match.

These are representation changes, not new usage. The stored event identity should remain the first winner and IsReplay should be true for later equivalent calls. The raw formatting need not be retained under this contract. If an audit requirement needs raw input, that would be a separate proposed field with its own privacy and retention considerations.

## Meaningful changes under the same key

Change subscription, meter, quantity and occurrence instant one at a time while keeping tenant and external ID fixed. Each should trigger usage.idempotency_conflict after the store returns the existing winner. Inspect the store afterward to verify the original payload remains unchanged. A conflict response alone does not prove no accidental overwrite happened first.

Use a quantity change that is numerically meaningful, not merely a different decimal scale representing the same value. Decimal equality treats values such as 2 and 2.00 as equal. A test that expects those to conflict would be testing representation identity rather than the current numeric contract. State that distinction explicitly.

## New key, same payload

Change external ID while keeping the payload identical. The store admits a new event under the current contract because the caller declares a new identity. This can be legitimate repeated usage or an accidental duplicate if the caller generates a fresh ID on every network retry. The server's tuple-key deduplication cannot infer that two different IDs were intended to represent the same real-world event.

Design a client receipt policy that retains the same external ID across retries of one event. Define when a new ID is generated and how changed intent is handled. Do not use a timestamp alone as a unique event key without analyzing collisions and retry retention. The learning focus is caller identity discipline, not one preferred ID format.

## Tenant normalization boundary

The constructor validates tenant non-whitespace but does not trim it. T and a padded T are distinct dictionary keys in the inspected store. That may be acceptable if an earlier trusted context supplies canonical tenant IDs. It becomes a risk if arbitrary headers are treated as authoritative tenant identity without normalization and authentication.

Build a characterization pair for these strings and label the result. Then investigate the API tenant source before proposing a fix. Normalizing tenant identifiers in only one service can disagree with catalog or subscription repositories. A consistent identity policy belongs at a shared boundary with migration implications for existing keys.

## Rejected input before storage

Zero or negative quantity, empty identifiers and whitespace-only required strings fail during UsageEvent construction. A fake store can count calls to prove invalid input is rejected before winner selection. This is different from changed-payload conflict, which requires finding an existing winner. Distinguish validation failures from identity conflicts in both tests and user-facing errors.

## Worksheet

Create at least twelve input pairs covering the partitions above. For each, state shared key or new key, normalized equality or difference, expected replay flag or error, and expected stored count. Include a canceled token before ingestion and verify no store call. A strong worksheet makes every changed dimension visible and avoids relying on one broad “duplicate request” example.

[Studio index](README.md) · [Review guide](reviews/01-oracles-and-usage.md)
