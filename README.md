# Video Event Processor

An independent .NET 8 backend portfolio project about camera events across customer sites. It is a learning and demonstration project, **not a claim of professional video-surveillance experience**.

## Current status

**In progress — site and camera registration (issue #8).** The API now exposes POST and GET routes for sites and cameras. Site and camera creation use asynchronous EF Core writes; GET routes use read-only queries. Camera registration checks that its site exists. These routes were exercised against local PostgreSQL, including successful creation, retrieval, and expected 400/404 responses. Event ingestion and background processing are not yet implemented.

| Area | Status |
| --- | --- |
| .NET 8 solution and domain types | Implemented: API, Domain, Infrastructure, and integration-test projects; `Site`, `Camera`, and `CameraEvent` |
| PostgreSQL schema | Implemented: versioned SQL migration for three tables, keys, camera/source-event uniqueness, plus a site/time index in a follow-up migration |
| EF Core persistence | In progress: mapped to the existing SQL schema and configured through DI; async event writes and matching-retry handling at the service layer |
| Health and logging | In progress: `/health` tests database connectivity; event writes log named fields through `ILogger` |
| Integration tests | In progress: local PostgreSQL tests for persistence, duplicate/conflicting retries, and key schema protections; execution requires local services |
| Site/camera REST routes | In progress: POST and GET for sites and cameras, with name validation and a site-existence check for cameras |
| Event ingestion, queue, history, summaries | Planned: no event HTTP route or `Channel<T>`/`BackgroundService` yet |

## Scenario and design

A monitoring service receives reports such as motion detection and camera outages from cameras at different sites. Each event has a database-generated `event_id` and a camera-provided `source_event_id`. The `(camera_id, source_event_id)` constraint protects against concurrent duplicate submissions. `EventStore.SaveAsync` returns the existing ID for a matching retry and throws `EventIdentityConflictException` if that ID is reused with different site, type, or occurrence time. Arrival time may differ between retries. The event retains its historical `site_id`, even if the camera later moves.

`occurred_at_utc` identifies when the event happened; `received_at_utc` captures its arrival. A `(site_id, occurred_at_utc)` index supports future site-history range queries. The database schema is managed by Supabase declarative SQL and versioned migrations. EF Core maps to the tables and **does not create or migrate them at application startup**. See [domain design](docs/domain-model.md).

The current event writer checks nonempty IDs, nonblank strings, and UTC offsets. HTTP request validation, allowed event types, timestamp plausibility, camera reassignment, and late-delivery rules remain open. The current code does not yet process events in the background.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/VideoEventProcessor.Api/` | ASP.NET Core startup, dependency injection, Swagger UI in development, site and camera registration/retrieval routes, and database health route |
| `src/VideoEventProcessor.Domain/` | Site, Camera, and CameraEvent types |
| `src/VideoEventProcessor.Infrastructure/` | EF Core table mappings and idempotent event writer |
| `tests/VideoEventProcessor.IntegrationTests/` | PostgreSQL integration tests |
| `supabase/schemas/` | Declarative PostgreSQL schema, source for future schema changes |
| `supabase/migrations/` | Versioned SQL to apply in order |

This is one service. Supabase Auth, Realtime, and publications are outside this phase.

## Run locally

Prerequisites: .NET 8 SDK, Node.js/npm, and Docker Desktop with its engine running. A hosted Supabase account is unnecessary. From the repository root, run each command separately:

```bash
npm ci
npx supabase start
npx supabase db reset
dotnet build VideoEventProcessor.sln
```

`db reset` recreates the **local** database and removes local rows; use it only when that is acceptable. On later runs, `npx supabase migration up` applies pending migrations without resetting local rows. Find the local PostgreSQL URL using `npx supabase status`; keep its password and CLI output out of Git. Set the `ConnectionStrings__VideoEvents` environment variable to that local PostgreSQL connection string using your shell or VS Code launch environment, then run:

```bash
dotnet run --project src/VideoEventProcessor.Api
```

Open `/health` at the printed API address to check PostgreSQL connectivity. Swagger UI at `/swagger` exposes the site and camera registration routes in development. For integration tests, set `VIDEO_EVENTS_TEST_CONNECTION_STRING` to the same **local** PostgreSQL URL and run:

```bash
dotnet test VideoEventProcessor.sln
```

The tests require the committed migrations to have been applied. They create unique test site/camera IDs and roll back their rows. Do not commit credentials, local CLI status output, or exploratory Studio data.

## Roadmap

1. **Complete:** Repository foundation, domain types, and relational design.
2. **Complete:** Local PostgreSQL schema and migration replay (`npx supabase db reset` passed for the initial schema in PR #6).
3. **Complete (foundation):** EF Core mapping, site/time index, service-level idempotency, database health check, and local integration tests. Issue #5 remains open for later event API integration and additional coverage.
4. **In progress:** Site and camera registration routes; automated route tests and further API polish remain. HTTP event ingestion is planned.
5. **Planned:** Bounded `Channel<T>` queue, `BackgroundService`, concurrency limits, and backpressure.
6. **Planned:** Event history and summaries, broader testing, operational logging, and performance review.
7. **Planned:** Further local tooling, GitHub Actions, and architecture documentation.

Each phase uses a focused issue and feature branch, validation, a reviewed diff, and a pull request before merge.
