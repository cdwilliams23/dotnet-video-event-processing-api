# Video Event Processor

An independent .NET 8 backend portfolio project that simulates receiving and processing events from remote security cameras. It is a learning and demonstration project, **not a claim of professional video-surveillance experience**.

## Business scenario

A cloud monitoring service receives event reports from cameras installed at multiple customer sites. Reports may describe motion, a person or vehicle, an offline or restored camera, possible fire or smoke, or tampering. The service will eventually accept events, handle retries safely, process accepted events asynchronously, and offer history and site summaries. Event types and API routes remain provisional until the domain-model phase.

## Planned technical features

- ASP.NET Core Web API on .NET 8 with REST endpoints and OpenAPI/Swagger
- C# asynchronous operations, `Channel<T>`, `BackgroundService`, bounded queue backpressure, and controlled concurrency
- SQL Server with Entity Framework Core, asynchronous queries, schema design, and indexes
- Duplicate-event prevention and idempotent ingestion
- Built-in dependency injection, `ILogger` structured logging, validation, error handling, and health checks
- xUnit unit and integration tests, plus documentation of tradeoffs and limitations

This will remain one service. Docker and GitHub Actions are later phases, after the local application works.

## Provisional architecture

| Project | Responsibility | References |
| --- | --- | --- |
| `src/VideoEventProcessor.Api` | HTTP entry point and dependency registration | Domain, Infrastructure |
| `src/VideoEventProcessor.Domain` | Business concepts and rules | None |
| `src/VideoEventProcessor.Infrastructure` | Persistence and external implementations | Domain |

An `Application` project may be introduced when ingestion and processing workflows justify a separate orchestration layer. Test projects will be added alongside behavior worth testing. This keeps the initial solution small without fixing the final design prematurely.

## Roadmap

1. Repository foundation and architecture
2. Domain model and database design
3. Site and camera registration
4. Camera-event ingestion and validation
5. Idempotency and duplicate protection
6. Asynchronous processing with a bounded queue
7. Event queries, filtering, and summaries
8. Logging, exception handling, and health checks
9. Unit and integration testing
10. Database performance and indexing
11. Docker-based local environment
12. GitHub Actions build and test workflow
13. Architecture documentation and portfolio polish

Each meaningful phase gets a focused issue, a `feature/` branch, validation, a reviewed diff, and a PR before merge.

## Current status

Phase 1 is in progress on feature/repository-foundation (issue #1). The .NET 8 SDK is installed, the API, Domain, and Infrastructure projects have been scaffolded with their project references, and dotnet build VideoEventProcessor.sln succeeds with 0 warnings and 0 errors. No camera-event features, database connectivity, or tests have been implemented yet.
