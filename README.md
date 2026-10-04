# Video Event Processor

An independent .NET 8 backend portfolio project that models camera events across customer sites. This is a learning and demonstration project, **not a claim of professional video-surveillance experience**.

## Current status

**In progress — local PostgreSQL foundation (issue #5).** The solution contains an ASP.NET Core API scaffold, three domain classes, and a versioned PostgreSQL schema for sites, cameras, and camera events. The schema was applied to a local Supabase PostgreSQL instance and the three tables were inspected in Supabase Studio. The API does not yet connect to the database or expose camera-event routes; its Swagger UI has no business operations. There are no automated tests yet.

| Area | Status | What the repository demonstrates |
| --- | --- | --- |
| .NET 8 solution | Implemented | API, Domain, and Infrastructure projects with project references, nullable reference types, and implicit usings |
| Domain model | Implemented as simple types | `Site`, `Camera`, and `CameraEvent` classes; historical site ID on an event |
| PostgreSQL schema | Implemented locally | Declarative SQL and a versioned migration; primary keys, foreign keys, generated event ID, and unique `(camera_id, source_event_id)` |
| REST API and dependency injection | Planned | ASP.NET Core startup exists, but no business endpoints or persistence services are registered |
| EF Core, async database access, and health checks | Planned | No provider, `DbContext`, queries, or database health check yet |
| Queue, validation, logging, and tests | Planned | No ingestion workflow, background processing, validation rules, or automated tests yet |

## Scenario and design

A monitoring service receives reports such as motion detection or a camera going offline from cameras at multiple sites. Future API endpoints will register sites and cameras, accept events, handle retries, and return history and summaries.

Each `camera_events` row has an internal database-generated `event_id`. The camera supplies a `source_event_id` that must remain the same on a retry; the database enforces uniqueness on `(camera_id, source_event_id)`. The event stores `site_id` so its historical association does not change if a camera moves. `occurred_at_utc` represents event time, while `received_at_utc` will record arrival time. See [domain design](docs/domain-model.md) for relationships and deferred questions.

The schema does not yet enforce allowed event types, timestamp plausibility, or nonblank names. Camera reassignment and late delivery rules remain to be designed. The uniqueness constraint is a database safeguard; an idempotent API response has not been implemented.

## Repository layout

| Path | Responsibility today |
| --- | --- |
| `src/VideoEventProcessor.Api/` | ASP.NET Core entry point and development Swagger UI |
| `src/VideoEventProcessor.Domain/` | Site, Camera, and CameraEvent types |
| `src/VideoEventProcessor.Infrastructure/` | Project scaffold for future persistence code |
| `supabase/schemas/video_events.sql` | Declarative PostgreSQL schema, the source for future schema edits |
| `supabase/migrations/` | Versioned SQL migration generated from the declarative schema |
| `docs/domain-model.md` | Relationships, event identity, and unresolved design decisions |

This is one service. An Application project can be added when a distinct orchestration layer is justified. The database schema is currently managed with Supabase declarative SQL and migrations; EF Core is planned for .NET persistence, not used to create this migration.

## Run locally

Prerequisites: .NET 8 SDK, Node.js/npm, and Docker Desktop with its engine running. The Supabase CLI is a local npm development dependency; a hosted Supabase account is not needed.

From the repository root, run each command separately:

```bash
npm ci
npx supabase start
npx supabase migration up
dotnet build VideoEventProcessor.sln
dotnet run --project src/VideoEventProcessor.Api
```

`supabase start` runs local PostgreSQL in Docker and may apply pending migrations; `migration up` is safe to run afterward to apply any that remain. The local Studio URL is printed by the CLI (typically `http://127.0.0.1:54323`). In Studio's Table Editor, select the `public` schema to inspect `sites`, `cameras`, and `camera_events`. The API runs independently of PostgreSQL for now; its development Swagger UI is available at the URL printed by `dotnet run` with `/swagger` appended, but there are no camera-event operations to call yet.

No connection string or API key is required for the current .NET scaffold. Do not commit local credentials or CLI status output containing keys. Schema changes belong in `supabase/schemas/` and should be reviewed as generated migrations before applying them. Local rows inserted through Studio are exploratory data and are not part of the reproducible schema.

## Roadmap

1. **Complete:** Repository foundation and architecture.
2. **Complete:** Initial domain types and relational design document.
3. **In progress:** Local PostgreSQL schema and migration. Add an index for site/time queries, reproducible seed data, and migration replay validation.
4. **Planned:** EF Core/Npgsql integration, site and camera registration, and asynchronous database access.
5. **Planned:** Event ingestion, validation, retry idempotency, and event history and summaries.
6. **Planned:** Bounded `Channel<T>` queue, `BackgroundService`, controlled concurrency, and backpressure.
7. **Planned:** Structured logging, exception handling, health checks, unit and integration tests, and performance work.
8. **Planned:** Further Docker support, GitHub Actions, and architecture documentation.

Each meaningful phase uses a focused issue, a feature branch, build and test evidence where applicable, a reviewed diff, and a pull request before merge.
