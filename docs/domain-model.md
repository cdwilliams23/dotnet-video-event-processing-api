# Domain model and relational design

## Relationships

- A Site has many Cameras. `Camera.SiteId` identifies a camera's current site.
- A Camera has many CameraEvents.
- `CameraEvent.SiteId` records the site associated with the event, so historical site queries do not change when a camera moves.

## Proposed SQL Server tables

| Table | Columns | Keys |
| --- | --- | --- |
| Sites | SiteId uniqueidentifier, SiteName nvarchar(200) | SiteId primary key |
| Cameras | CameraId uniqueidentifier, SiteId uniqueidentifier, CameraName nvarchar(200) | CameraId primary key; SiteId foreign key to Sites |
| CameraEvents | EventId bigint identity, CameraId uniqueidentifier, SiteId uniqueidentifier, SourceEventId nvarchar(128), EventType nvarchar(64), OccurredAtUtc datetimeoffset, ReceivedAtUtc datetimeoffset | EventId primary key; CameraId foreign key to Cameras; SiteId foreign key to Sites; (CameraId, SourceEventId) unique |

All listed columns are required.

## Event identity and queries

- A camera must reuse its SourceEventId when retrying the same event.
- `(CameraId, SourceEventId)` is unique. EventId is our separate internal key.
- Site history and hourly summaries use OccurredAtUtc.
- An index on `(SiteId, OccurredAtUtc)` supports site history over a time range.
- ReceivedAtUtc records when the service received the event and can reveal delivery delays.

## Decisions deferred to later phases

- Ingestion will validate nonblank strings, allowed event types, timestamp plausibility, and UTC normalization.
- Camera reassignment needs a way to identify the camera's site at the event's occurrence time. A current Camera.SiteId alone cannot resolve an event delivered after a move.
- The SQL collation or normalization rule for SourceEventId must be decided before enforcing uniqueness, especially if camera IDs distinguish letter case.
- EF Core mappings, migrations, and database connectivity are not implemented in this phase.
