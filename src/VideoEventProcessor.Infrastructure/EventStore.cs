using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using VideoEventProcessor.Domain;

namespace VideoEventProcessor.Infrastructure;

public sealed record EventWriteResult(long EventId, bool IsDuplicate);

public sealed class EventIdentityConflictException : Exception
{
    public EventIdentityConflictException() : base("The camera already used this source event ID for a different event.") { }
}

// One scoped DbContext per request/work item. The database constraint resolves concurrent retries.
public sealed class EventStore(VideoEventDbContext db, ILogger<EventStore> logger)
{
    public async Task<EventWriteResult> SaveAsync(CameraEvent cameraEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cameraEvent);

        if (cameraEvent.CameraId == Guid.Empty || cameraEvent.SiteId == Guid.Empty ||
            string.IsNullOrWhiteSpace(cameraEvent.SourceEventId) || string.IsNullOrWhiteSpace(cameraEvent.EventType) ||
            cameraEvent.OccurredAtUtc.Offset != TimeSpan.Zero || cameraEvent.ReceivedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("An event needs camera and site IDs, nonblank event fields, and UTC timestamps.", nameof(cameraEvent));
        }

        db.CameraEvents.Add(cameraEvent);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Stored camera event {EventId} from camera {CameraId}", cameraEvent.EventId, cameraEvent.CameraId);
            return new EventWriteResult(cameraEvent.EventId, false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "camera_events_camera_id_source_event_id_key" })
        {
            db.Entry(cameraEvent).State = EntityState.Detached;

            var stored = await db.CameraEvents.AsNoTracking().SingleAsync(
                value => value.CameraId == cameraEvent.CameraId && value.SourceEventId == cameraEvent.SourceEventId,
                cancellationToken);

            if (stored.SiteId != cameraEvent.SiteId || stored.EventType != cameraEvent.EventType ||
                stored.OccurredAtUtc != cameraEvent.OccurredAtUtc)
            {
                logger.LogWarning("Conflicting source event ID {SourceEventId} for camera {CameraId}",
                    cameraEvent.SourceEventId, cameraEvent.CameraId);
                throw new EventIdentityConflictException();
            }

            logger.LogInformation("Duplicate source event ID {SourceEventId} for camera {CameraId}; stored event {EventId}",
                cameraEvent.SourceEventId, cameraEvent.CameraId, stored.EventId);
            return new EventWriteResult(stored.EventId, true);
        }
    }
}
