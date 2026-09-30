# 21. Catalog normalization and tenant-key consistency

Read [Product](../../src/MeterForge.Domain/Catalog/Product.cs), [ProductService](../../src/MeterForge.Application/Catalog/ProductUseCases.cs), and [InMemoryProductRepository](../../src/MeterForge.Infrastructure/Catalog/InMemoryProductRepository.cs). The catalog is small enough to trace completely, which makes it a useful lesson in identity normalization. A value can be valid and still be stored under a different key from the one a later lookup uses.

The prerequisite is the usage identity chapters. This chapter applies the same discipline to catalog keys but does not assume that every domain type normalizes tenant identifiers identically. Read each boundary. A shared string type does not establish a shared normalization policy.

## Follow creation into storage

Product construction requires a nonempty identifier, nonblank tenant, code, and name, and a nonnegative monthly price. It trims TenantId, trims and upper-cases Code, and trims Name. The service generates a new identifier, constructs the Product through these guards, adds it to the repository, and returns it.

The repository stores the object under the tuple of product.TenantId and product.Id. This means the tenant portion of the stored key is the normalized value supplied by Product. A creation request containing surrounding tenant whitespace can therefore produce a stored key without that whitespace. The returned Product exposes the normalized value.

GetAsync, however, passes its supplied tenant string to the dictionary lookup without the same trim in that method. A caller using the original whitespace-bearing string may not find the item, while a caller using the normalized tenant string can. This is a precise source-level identity mismatch to demonstrate with synthetic input. It is not a claim that ordinary route values always contain such whitespace.

## Decide where canonicalization belongs

A coherent policy can canonicalize tenant identity once at an authenticated boundary and pass a typed canonical value internally. Another can require every repository entry point to apply the same function. A third can reject noncanonical input rather than accepting and transforming it. Each can work if creation and lookup agree.

The dangerous approach is accidental normalization in some constructors but not in queries. Tests then pass for ordinary lowercase tenant names while edge inputs behave inconsistently. A table should include canonical input, surrounding whitespace, case variation, and blank input. For each, state whether the system rejects, normalizes, or treats it as a distinct identity.

Do not automatically lower-case tenants because product codes are upper-cased. Tenant identifiers and product codes may have different semantics. A change to tenant comparison can merge previously distinct keys. That is a migration and authority decision, not a cosmetic cleanup. The exercise should propose a policy and identify collision handling before changing stored data.

## Product code is not the repository key

The repository key uses tenant and product identifier, not product code. The constructor normalizes code, but the inspected AddAsync does not enforce uniqueness of code within a tenant. Two products can therefore have the same normalized code and different identifiers in this store. Whether that is acceptable is a catalog policy question.

If codes are human-friendly unique names, a proposed repository needs an additional uniqueness invariant and a conflict response. If codes are labels that may repeat, consumers must use identifiers for unambiguous references. Do not assume that uppercase normalization itself enforces uniqueness; it merely defines a representation.

The service generates a fresh identifier on every create call. A repeated request with identical fields therefore creates another Product unless a surrounding operation protocol prevents it. This contrasts with usage ingestion's explicit replay identity. Not every endpoint in a codebase inherits idempotency just because one endpoint implements it well.

## Dictionary assignment is an upsert-like local effect

AddAsync assigns through the dictionary indexer. If a caller supplies a Product with an existing tuple key, the stored reference is replaced. The method name Add does not imply a duplicate rejection. In the normal service path, a fresh Guid makes collision unlikely, but tests and other callers can deliberately reuse an identifier.

A characterization test can construct two products with the same tenant and identifier but different names, add both, and read the key. The second object is the observable stored value under this implementation. A proposed immutable catalog identity contract might reject that replacement, while an update workflow might permit it under version control. The decision belongs in an explicit API, not in assumptions about a method name.

The ConcurrentDictionary provides thread-safe operations on its entries. It does not make a sequence of lookup, validation, and assignment into one business transaction automatically. If a new uniqueness check first scans for a code and then assigns, two callers can race. The operation boundary must include the check and acceptance under a shared authority.

## Ownership and process lifetime

Product exposes only getters for its current fields, and Money is a value representation. Returning the same Product reference is therefore less mutation-prone than returning a mutable Subscription reference. This comparison is useful: repository reference ownership risks depend on the object's API, not only on whether the container is concurrent.

The store remains in memory. A successful AddAsync does not prove survival after process restart or availability in another process. The application's singleton registration shares the repository within one host instance, which is different from a durable shared catalog. A demonstration can be correct within that scope while requiring a different adapter for production persistence.

Cancellation is checked before assignment and lookup. There is no long asynchronous database call in these methods. A pre-canceled token can prevent the local operation, but the completed Task shape does not imply remote I/O. Tests should describe the actual operation rather than assuming all Async methods have the same timing characteristics.

## Exercise MF-21A: trace canonical keys

Create a product using a tenant string with surrounding whitespace. Predict the stored TenantId and compare lookup using the raw and normalized strings. Propose one canonicalization policy and specify how all entry points enforce it.

## Exercise MF-21B: distinguish code and identity

Construct two products with the same normalized code and different identifiers, then two products with the same tuple key. Predict each repository outcome. Define a proposed unique-code or duplicate-code policy and the concurrent acceptance test it requires.

## Exercise MF-21C: design a catalog creation receipt

Add a proposed stable operation key to product creation. Define exact replay, conflicting replay, canonical tenant scope, and response identity. Explain what must persist across restarts and which current in-memory behavior can be reused only as a local model.

## Review checkpoint

Normalization is part of identity, not just presentation. A strong catalog review follows the exact value from constructor through repository key to lookup and response. It distinguishes thread-safe storage from business-level uniqueness and process-local success from durable acceptance.
