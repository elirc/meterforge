# MeterForge extended billing engineering course

This complete source-grounded course extends the [existing Astra pack](../README.md) with a main route and a complementary [verification studio](verification-studio/README.md). Together they exceed the requested 85,590 new instructional prose words, excluding README navigation and fenced code. The organizer report records exact counts, preserved source hashes, and executed evidence.

Read each chapter before its separate review. Current implementation, proposed extensions, and unexecuted integration plans are explicitly distinguished. Application source, dependencies, configuration, and earlier lessons remain unchanged. The default host uses in-memory stores and a simulated payment provider; the course does not present these as durable financial infrastructure.

## Progressive route

Chapters 01-11 establish identity, atomic acceptance, money, pricing, rating, subscriptions, invoices, and payment uncertainty. Chapters 12-23 deepen entitlements, adjustments, reconciliation, snapshot persistence, integrations, tax, proposed proration, catalog identity, HTTP authority, and architecture. Chapters 24-28 connect these through acceptance, migration, observability, a copied-source lab, and correction accounting. Chapters 29-38 examine lifecycle orchestration, temporal selection, numeric limits, response semantics, cancellation, batch APIs, optimization, and exception atomicity. Chapters 39-44 conclude with dispatcher and reservation capstones, client recovery, deployment modes, support planning, and integrated assessment.

The [verification studio](verification-studio/README.md) adds 21 workshops and seven review guides on independent oracles, controlled experiments, properties, mutations, and incident evidence. Its lab report is separate from the main-course lab reports.

## Main chapters

- [01. Three identities in one usage request](01-three-identities-in-one-usage-request.md)
- [02. The winner is an operation boundary](02-the-winner-is-an-operation-boundary.md)
- [03. HTTP receipts and lost responses](03-http-receipts-and-lost-responses.md)
- [04. First capstone: design a durable usage receipt](04-capstone-a-durable-usage-receipt.md)
- [05. Money values and rounding policy](05-money-values-and-rounding-policy.md)
- [06. Price time is a half-open interval](06-price-time-is-a-half-open-interval.md)
- [07. Tiered rating and unallocated units](07-tiered-rating-and-unallocated-units.md)
- [08. Subscription version and reference ownership](08-subscription-version-and-reference-ownership.md)
- [09. A compatible lab and its evidence boundaries](09-compatible-lab-and-evidence-boundaries.md)
- [10. Invoice finalization freezes a specific body of evidence](10-invoice-finalization-and-evidence-ownership.md)
- [11. Payment attempts and ambiguous provider outcomes](11-payment-attempts-and-ambiguous-provider-outcomes.md)
- [12. Entitlement decisions are not reservations](12-entitlement-decisions-are-not-reservations.md)
- [13. Discounts, credits, and the direction of money](13-discounts-credits-and-direction-of-money.md)
- [14. Reconciliation, refunds, and missing correspondence](14-reconciliation-refunds-and-missing-correspondence.md)
- [15. Snapshot conversion and trust boundaries](15-snapshot-conversion-and-trust-boundaries.md)
- [16. PostgreSQL conflicts and immutable-document replay](16-postgres-conflicts-and-immutable-document-replay.md)
- [17. Simulators, test doubles, and provider contracts](17-simulators-test-doubles-and-provider-contracts.md)
- [18. Outbox records and webhook delivery protocols](18-outbox-records-and-webhook-delivery-protocols.md)
- [19. Tax quotes as versioned calculation evidence](19-tax-quotes-as-versioned-calculation-evidence.md)
- [20. Proposed proration and calendar segmentation](20-proposed-proration-and-calendar-segmentation.md)
- [21. Catalog normalization and tenant-key consistency](21-catalog-normalization-and-tenant-key-consistency.md)
- [22. HTTP authority, errors, and evidence layers](22-http-authority-errors-and-evidence-layers.md)
- [23. Composition roots and architecture-test limits](23-composition-roots-and-architecture-test-limits.md)
- [24. Capstone: an accepted billing document](24-capstone-an-accepted-billing-document.md)
- [25. Migration rehearsal and historical evidence](25-migration-rehearsal-and-historical-evidence.md)
- [26. Observability for billing decisions](26-observability-for-billing-decisions.md)
- [27. Integrated billing characterization lab](27-integrated-billing-characterization-lab.md)
- [28. Capstone: a correction ledger with two balances](28-capstone-a-correction-ledger-with-two-balances.md)
- [29. Trial dates and lifecycle orchestration](29-trial-dates-and-lifecycle-orchestration.md)
- [30. Amendment intent, resource state, and command replay](30-amendment-intent-resource-state-and-command-replay.md)
- [31. Price catalogs as validated timelines](31-price-catalogs-as-validated-timelines.md)
- [32. Late usage and billing watermarks](32-late-usage-and-billing-watermarks.md)
- [33. Numeric bounds and arithmetic failure policy](33-numeric-bounds-and-arithmetic-failure-policy.md)
- [34. Response contracts and loss of evidence](34-response-contracts-and-loss-of-evidence.md)
- [35. Cancellation and the point of accepted work](35-cancellation-and-the-point-of-accepted-work.md)
- [36. Batch ingestion and partial-acceptance contracts](36-batch-ingestion-and-partial-acceptance-contracts.md)
- [37. Query cost, indexes, and evidence-preserving optimization](37-query-cost-indexes-and-evidence-preserving-optimization.md)
- [38. Mutation callbacks and exception atomicity](38-mutation-callbacks-and-exception-atomicity.md)
- [39. Capstone: a recoverable webhook dispatcher](39-capstone-a-recoverable-webhook-dispatcher.md)
- [40. Capstone: entitlement reservations and release](40-capstone-entitlement-reservations-and-release.md)
- [41. Client recovery and command journals](41-client-recovery-and-command-journals.md)
- [42. Deployment modes and honest readiness](42-deployment-modes-and-honest-readiness.md)
- [43. Read-only support tools and repair plans](43-read-only-support-tools-and-repair-plans.md)
- [44. Integrated design assessment and review defense](44-integrated-design-assessment-and-review-defense.md)

## Main separate solutions and rubrics

- [Review 01: identity, winners, and receipts](review-01-identity-winners-and-receipts.md)
- [Review 02: money, pricing, rating, and ownership](review-02-money-pricing-rating-and-ownership.md)
- [Separate lab review](review-03-compatible-lab.md)
- [Review 04. Invoices and provider uncertainty](review-04-invoices-and-provider-uncertainty.md)
- [Review 05. Decisions, adjustments, and snapshots](review-05-decisions-adjustments-and-snapshots.md)
- [Review 06. Simulation, integration, tax, and catalog](review-06-simulation-integration-tax-and-catalog.md)
- [Review 07. HTTP, architecture, acceptance, and operations](review-07-http-architecture-acceptance-and-operations.md)
- [Review 08. Integrated billing lab](review-08-integrated-billing-lab.md)
- [Review 09. Correction ledger](review-09-correction-ledger.md)
- [Review 10. Lifecycle, time, and numeric policy](review-10-lifecycle-time-and-numeric-policy.md)
- [Review 11. Contracts, cancellation, batches, and mutation](review-11-contracts-cancellation-batches-and-mutation.md)
- [Review 12. Capstones, clients, deployment, and design defense](review-12-capstones-clients-deployment-and-defense.md)

## Executed labs and their limits

The [usage lab](09-compatible-lab-and-evidence-boundaries.md) and [integrated billing lab](27-integrated-billing-characterization-lab.md) copy selected current source into new absolute external directories. Their starter mutations preserve signatures and have explicit expected failures. Reference, intended-failure, repaired-copy, and output-guard checks passed. Python and the .NET 10 SDK must already be available. No application source is overwritten and no provider account or database is used.

The studio's actual-source harness passed 621 pure arithmetic and in-memory usage assertions. Its mutation and broader integration exercises remain proposed unless individually marked executed. Main billing characterization passed 8 reference checks, demonstrated its one intended starter failure, and passed 8 after repair; the earlier usage lab passed 5 reference checks and its corresponding failure/repair sequence.

These results do not prove hosted authentication, PostgreSQL isolation, cross-process durability, actual provider behavior, or every proposed capstone. Characterization tests document current behavior, including limitations; passing them does not endorse every behavior as a desired production policy. The course provides the separate evidence plans needed to investigate those boundaries.

## Capstone submissions

Choose a main-course capstone in durable usage receipts, accepted billing documents, correction ledgers, webhook dispatch, or entitlement reservations. Submit its deterministic traces, independent exercises, separate self-review, and explicit evidence limits. The final integrated assessment ties those protocols together without requiring live financial effects or changes to this application.
