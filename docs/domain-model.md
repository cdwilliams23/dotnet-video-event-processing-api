# Domain model and relational design

## Relationships

- A Site has many Cameras. `Camera.SiteId` identifies a camera's current site.
- A Camera has many CameraEvents.
- `CameraEvent.SiteId` records the site associated with the event, so historical site queries do not change when a camera moves.

## Local PostgreSQL schema

The declarative source is `supabase/schemas/video_events.sql`. Versioned SQL migrations create the tables and add the site/time index. EF Core maps to this existing schema and does not run migrations.

| Table | Columns | Keys |
| --- | --- | --- |
| `sites` | `site_id` uuid, `site_name` text | `site_id` primary key |
| `cameras` | `camera_id` uuid, `site_id` uuid, `camera_name` text | `camera_id` primary key; `site_id` foreign key to `sites` |
| `camera_events` | `event_id` bigint identity, `camera_id` uuid, `site_id` uuid, `source_event_id` text, `event_type` text, `occurred_at_utc` timestamptz, `received_at_utc` timestamptz | `event_id` primary key; camera and site foreign keys; `(camera_id, source_event_id)` unique |

All listed columns are required. The SQL revokes table and event-sequence access from Supabase's `anon` and `authenticated` roles. The .NET service connects directly as the configured database user.

## Event identity and queries

- A camera reuses its `source_event_id` when retrying the same event. The database rejects concurrent duplicates on `(camera_id, source_event_id)`.
- `EventStore.SaveAsync` treats a retry as the same event when site, type, and occurrence time match. It returns the stored event ID; conflicting reuse throws an exception. Received time may change on a retry.
- `event_id` is the internal database-generated key.
- Site history and hourly summaries use `occurred_at_utc`; `received_at_utc` records arrival time.
- The `(site_id, occurred_at_utc)` index supports future site history over a time range. The unique camera/source index also supports the event writer's duplicate lookup.

## Decisions deferred

- Ingestion endpoints must validate allowed event types, timestamp plausibility, lengths, camera/site association, and return appropriate HTTP errors.
- Camera reassignment needs a way to identify the camera's site at occurrence time. A current `Camera.SiteId` alone cannot resolve an event delivered after a move.
- Source IDs differing only in letter case are distinct under PostgreSQL's usual text comparison; decide whether camera protocols require normalization.
- No production seed data is committed. Integration tests create and roll back their own fixtures.
