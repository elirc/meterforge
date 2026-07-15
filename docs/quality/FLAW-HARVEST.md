# Deliberate flaw harvest

Five plausible defects were planted during normal feature work, then exposed by counterexamples and repaired at the boundary that owned the guarantee.

| Introduced | Flaw | Repair | Regression |
| --- | --- | --- | --- |
| Sprint 4 | Usage deduplication keyed only by external ID | Make tenant part of the store port and composite adapter key | two tenants may use the same external ID without replaying each other |
| Sprint 5 | The first unit after a tier boundary was counted twice | Use non-overlapping quantity intervals | 150 units across 100/$1 and remainder/$0.50 totals exactly $125 |
| Sprint 6 | `AddLine` remained callable after invoice finalization | Reject all finalized line mutation | finalized invoice returns `invoice.finalized` |
| Sprint 8 | Expected-version read and save were separate operations | Make compare-and-mutate one repository operation | two writers at version 1 produce one commit and one conflict |
| Sprint 10 | Payment retry generated a new provider idempotency key | Preserve the logical payment key and increment attempt only | retry keeps key and advances attempt |

The lesson is not “add a framework.” Each repair strengthens one concrete contract: tenant scope, interval arithmetic, immutability, atomic compare-and-swap, or stable external identity.
