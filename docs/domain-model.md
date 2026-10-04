# Domain model and relational design

## Relationships

- A Site has many Cameras. `Camera.SiteId` identifies a camera's current site.
- A Camera has many CameraEvents.
- `CameraEvent.SiteId` records the site associated with the event, so historical site queries do not change when a camera moves.

## Local PostgreSQL schema

The declarative source is `supabase/schemas/video_events.sql`; the versioned SQL migration under `supabase/migrations/` has been applied to local PostgreSQL. The current schema uses these columns:

| Table | Columns | Keys |
| --- | --- | --- |
| `sites` | `site_id` uuid, `site_name` text | `site_id` primary key |
| `cameras` | `camera_id` uuid, `site_id` uuid, `camera_name` text | `camera_id` primary key; `site_id` foreign key to `sites` |
| `camera_events` | `event_id` bigint identity, `camera_id` uuid, `site_id` uuid, `source_event_id` text, `event_type` text, `occurred_at_utc` timestamptz, `received_at_utc` timestamptz | `event_id` primary key; camera and site foreign keys; `(camera_id, source_event_id)` unique |

All listed columns are required. The declarative schema revokes access to these tables and the event identity sequence from Supabase's `anon` and `authenticated` roles. The .NET service is intended to access the database directly in a later phase.

## Event identity and queries

- A camera must reuse its `source_event_id` when retrying the same event. The database rejects a duplicate `(camera_id, source_event_id)`; API-level idempotent handling is not implemented.
- `event_id` is a separate internal, database-generated key.
- Site history and hourly summaries should use `occurred_at_utc`; `received_at_utc` records arrival time for delivery-delay analysis.
- A `(site_id, occurred_at_utc)` index is planned to support site history over a time range; it is not in the initial migration.

## Decisions deferred to later phases

- Ingestion must validate nonblank strings, allowed event types, timestamp plausibility, and UTC handling.
- Camera reassignment needs a way to identify the camera's site at the event's occurrence time. A current `Camera.SiteId` alone cannot resolve an event delivered after a move.
- Decide whether source IDs that differ only in letter case should represent the same camera event; the current unique constraint uses the database's text comparison rules.
- EF Core mappings, database connectivity from .NET, seed data, and automated tests are not implemented yet.
