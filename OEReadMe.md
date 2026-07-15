# Over-engineering review

MeterForge is intentionally over-engineered as a **delivery curriculum** and mostly restrained as an application. Fifteen milestones, sixty issues, ordered commits, review commentary, two releases, and a planted-flaw narrative are excessive for this amount of product behavior. They preserve reasoning for a learner; they are not a recommended workflow for every small team.

## Code assessment

| Choice | Assessment | Guidance |
| --- | --- | --- |
| Five source projects and four test projects | Borderline early, useful once contracts and Npgsql/provider adapters exist | A small team could begin with API, core, and tests, then split contracts/infrastructure when consumers justify it |
| Framework-free domain and typed money | Proportionate to financial risk | Keep; do not turn every string into a value object |
| Use-case-specific ports | Proportionate where storage or providers genuinely vary | Keep narrow; avoid an interface for every class and never add a generic repository |
| Explicit HTTP mapping | More code, but compatibility remains visible | Keep until repetition proves a tiny helper; avoid reflection mapping |
| PostgreSQL adapter plus in-memory defaults | Useful teaching separation, but two adapters increase maintenance | Keep only while both have contract tests or distinct educational value |
| Architecture tests | Proportionate while protecting a few dependency rules | Do not encode every namespace/folder preference |
| Many preview endpoints | Educationally convenient but not a cohesive production API | Replace with persisted resource workflows if product use validates them |

## Pressure to watch

- `Program.cs` is becoming a registration inventory. Extract feature registration only when navigation or merge conflicts repeatedly cost the team.
- Validation and tenant checks repeat across endpoints. Introduce shared helpers only after the repeated semantics are genuinely identical.
- In-memory stores are test/demo adapters, not an architecture to wrap in transactions or caching.
- The raw Npgsql adapter demonstrates atomicity, but a product team needs a migration tool, integration contract tests, pooling/configuration, and operated restore—not additional repository layers.
- Billing, entitlements, tax, payments, webhooks, and reconciliation are broad. A real MVP should prioritize validated customer workflows instead of matching the curriculum breadth.

## Keep, simplify, defer

- **Keep:** financial provenance, tenant-bound ports, stable identities, expected versions, immutable correction records, regression tests, and the modular monolith.
- **Simplify:** project count and GitHub ceremony for a small team; preview-only endpoints; repeated mapping once a proven pattern exists.
- **Defer:** microservices, brokers, CQRS stores, rule engines, workflow engines, plugin systems, Kubernetes, and provider abstractions without a second implementation.

Extract a service only when separate deployment cadence, scaling, data/security ownership, or team ownership outweighs the compatibility, network failure, observability, and distributed-transaction cost.
