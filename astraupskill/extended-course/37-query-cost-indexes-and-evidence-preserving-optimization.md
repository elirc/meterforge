# 37. Query cost, indexes, and evidence-preserving optimization

Read [InMemoryUsageEventStore](../../src/MeterForge.Infrastructure/Usage/InMemoryUsageEventStore.cs), [InMemoryProductRepository](../../src/MeterForge.Infrastructure/Catalog/InMemoryProductRepository.cs), and [PriceSelector](../../src/MeterForge.Domain/Pricing/PriceVersion.cs). This chapter examines performance through concrete operations while preserving their correctness contracts. Optimization should not trade away accepted identity or ambiguity detection merely to make a benchmark look faster.

The usage store is keyed by tenant and external event identifier. GetOrAdd uses that dictionary key directly. FindById instead scans the dictionary values for tenant and internal identifier. The two operations therefore have different search structures. A successful small test does not establish that lookup cost remains appropriate as the event set grows.

## Identify the actual query shape

For FindById, the predicate checks tenant and Id while enumerating values. In an ordinary complexity model, finding a late or absent match can require examining many entries. This is a structural observation, not a measured latency claim. Actual time depends on data size, runtime, hardware, and concurrent activity.

Product lookup uses its tuple key directly, which supports a different access pattern. PriceSelector filters and materializes matching versions from a supplied collection, then checks the match count. A future database adapter may translate similar operations into indexed queries, but the current in-memory code does not automatically have those indexes.

Before proposing an optimization, state the workload: accepted events per tenant, readback frequency, replay frequency, price versions per product, and concurrency. A tiny teaching service may reasonably favor simple code. A durable high-volume deployment may need a secondary lookup structure or database index. The source alone does not supply a universal performance requirement.

## A second index creates a consistency obligation

Suppose a proposed usage store adds a dictionary keyed by tenant and internal Id for faster readback. Acceptance must update both the external-key winner map and the identifier map coherently. If one update succeeds and the other fails or is delayed, creation can return a Location that readback cannot resolve, or an identifier map can expose a losing candidate.

The accepted winner must populate the secondary index, not every candidate. Two callers with the same external key can construct different internal identifiers before GetOrAdd chooses one. Indexing both candidates would violate the resource identity contract. This is a concrete example of performance work depending on operation-boundary reasoning.

A process-local design can perform the necessary updates under one gate or choose a data structure with an atomic composite operation. A database design can enforce unique external identity and index accepted internal identity within one transaction. The correctness test should assert immediate readback of the stored winner and absence of losing candidate identities.

## Do not optimize away ambiguity detection

PriceSelector materializes all applicable matches so it can distinguish zero, one, and multiple results. Replacing it with FirstOrDefault might appear simpler or faster, but it would silently accept overlapping prices by choosing an arbitrary match. That changes the business contract, not merely the implementation cost.

A better optimization can preserve the three-way result. For example, a query may need only enough evidence to distinguish none, one, and at least two matches. A database can use appropriate indexes and a bounded match retrieval while retaining overlap detection. The proposal must prove that it still rejects ambiguity under the same scope and date predicate.

Likewise, entitlement lookup uses SingleOrDefault and therefore exposes duplicate feature ambiguity. Changing it to FirstOrDefault chooses a policy that the original method did not declare. If a collection validator guarantees uniqueness earlier, document and test that invariant before relying on a faster lookup representation.

## Benchmarks need representative questions

Measure new-event acceptance, exact replay, conflicting replay, successful readback, and absent readback separately. They execute different paths. Include several tenant distributions because one large tenant and many small tenants can stress structures differently. Report dataset size and adapter mode with results.

Do not benchmark only random unique events if the product expects heavy replay during outages. Do not benchmark readback only for the first enumerated item. A test that accidentally chooses the easiest path can produce a misleading performance conclusion. Stable synthetic seeds and explicit target positions make the experiment reproducible.

Keep correctness assertions in the benchmark setup or companion tests. A faster implementation that returns the wrong winner is not an optimization. Validate accepted identifier, replay flag, conflict preservation, and tenant scope before interpreting throughput. If timing noise dominates, report the limitation rather than publishing precise-looking numbers without confidence.

## Memory and retention are policy questions

The in-memory usage store retains accepted events without a visible eviction policy in this class. Adding eviction can break replay identity and readback. If an old external key disappears, a later replay may be accepted as a new event with a new identifier. A retention design therefore needs a durable receipt or tombstone policy, not just a cache-size limit.

Separate hot payload storage from replay evidence. A proposed system might archive full events while retaining key-to-accepted-result metadata for the required replay window. That window is a product contract and may differ from billing evidence retention. The course does not prescribe a legal retention period; it asks the learner to make the technical consequences explicit.

Caching price selections has similar invalidation concerns. A key must include product, date, and catalog revision or another stable publication identity. Caching only by product can return the wrong effective price. An optimization should preserve the versioned evidence used by accepted invoices, especially during catalog changes.

## Exercise MF-37A: characterize query work

Compare GetOrAdd, usage FindById, product GetAsync, and PriceSelector. Describe their visible data access without inventing measured latency. Propose a small synthetic workload that distinguishes successful early lookup from absent lookup and replay from new acceptance.

## Exercise MF-37B: add a secondary index safely

Design the proposed identifier index around the accepted usage winner. Work two candidates with one external key and different internal identifiers. Specify atomic updates and a test that the losing identifier never becomes a readable resource.

## Exercise MF-37C: review an optimization proposal

Evaluate replacing PriceSelector's match-count logic with FirstOrDefault and adding event eviction. Identify the changed guarantees and propose alternatives preserving overlap detection and replay evidence. Include benchmark and retention limitations.

## Review checkpoint

Performance work is strongest when it preserves the evidence relations the application relies on. A second index, cache, or retention policy introduces new consistency obligations. A strong review measures the intended workload and refuses optimizations that silently choose winners or forget accepted identities.
