# MeterForge

MeterForge is an original fifteen-sprint C#/.NET teaching repository for a multi-tenant usage-based subscription billing platform. The repository teaches a junior engineer how to grow toward mid-level ownership through business rules, durable persistence, API contracts, concurrency, integrations, tests, and operations.

It is not a Stripe clone, a payment processor, or production financial software. All providers, prices, customers, and events are fictional teaching fixtures.

## Architecture

```text
HTTP contracts -> application use cases -> domain model
                         ^                    |
                         |                    v
              persistence/provider adapters and outbox
```

MeterForge begins as a modular monolith. It deliberately avoids MediatR, AutoMapper, generic repositories, microservices, and an internal framework.

## Run

```powershell
dotnet restore MeterForge.slnx --locked-mode
dotnet run --project src/MeterForge.Api
dotnet test MeterForge.slnx
```

Start with the [learner guide](LEARNER-GUIDE.md) and [curriculum specification](docs/curriculum/SPEC.md).
