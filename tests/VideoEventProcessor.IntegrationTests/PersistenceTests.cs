using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using VideoEventProcessor.Domain;
using VideoEventProcessor.Infrastructure;

namespace VideoEventProcessor.IntegrationTests;

public sealed class PersistenceTests
{
    private static string LocalConnectionString()
    {
        var value = Environment.GetEnvironmentVariable("VIDEO_EVENTS_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Set VIDEO_EVENTS_TEST_CONNECTION_STRING for the local Supabase PostgreSQL database.");
        var connection = new NpgsqlConnectionStringBuilder(value);
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Integration tests require a local PostgreSQL host.");
        return value;
    }

    [Fact]
    public async Task SaveAsync_persists_event_and_handles_identical_and_conflicting_retries()
    {
        var options = new DbContextOptionsBuilder<VideoEventDbContext>()
            .UseNpgsql(LocalConnectionString()).Options;
        await using var db = new VideoEventDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var siteId = Guid.NewGuid();
        var cameraId = Guid.NewGuid();
        db.Sites.Add(new Site { SiteId = siteId, SiteName = "Test site" });
        db.Cameras.Add(new Camera { CameraId = cameraId, SiteId = siteId, CameraName = "Test camera" });
        await db.SaveChangesAsync();

        var store = new EventStore(db, NullLogger<EventStore>.Instance);
        var occurred = new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);
        var first = await store.SaveAsync(new CameraEvent
        {
            CameraId = cameraId, SiteId = siteId, SourceEventId = "test-42", EventType = "motion",
            OccurredAtUtc = occurred, ReceivedAtUtc = occurred.AddMinutes(1)
        });
        Assert.True(first.EventId > 0);
        Assert.False(first.IsDuplicate);

        var retry = await store.SaveAsync(new CameraEvent
        {
            CameraId = cameraId, SiteId = siteId, SourceEventId = "test-42", EventType = "motion",
            OccurredAtUtc = occurred, ReceivedAtUtc = occurred.AddMinutes(2)
        });
        Assert.True(retry.IsDuplicate);
        Assert.Equal(first.EventId, retry.EventId);

        await Assert.ThrowsAsync<EventIdentityConflictException>(() => store.SaveAsync(new CameraEvent
        {
            CameraId = cameraId, SiteId = siteId, SourceEventId = "test-42", EventType = "vehicle",
            OccurredAtUtc = occurred, ReceivedAtUtc = occurred.AddMinutes(3)
        }));

        var persisted = await db.CameraEvents.AsNoTracking().SingleAsync(value => value.CameraId == cameraId);
        Assert.Equal(siteId, persisted.SiteId);
        Assert.Equal("motion", persisted.EventType);
        Assert.Equal(occurred.AddMinutes(1), persisted.ReceivedAtUtc);
        Assert.Equal(1, await db.CameraEvents.CountAsync(value => value.CameraId == cameraId));
        // Disposing the transaction rolls back all test rows.
    }

    [Fact]
    public async Task Local_schema_has_history_index_and_database_constraints()
    {
        await using var connection = new NpgsqlConnection(LocalConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT count(*) FROM pg_indexes WHERE schemaname = 'public'
                AND tablename = 'camera_events' AND indexname = 'camera_events_site_occurred_idx') = 1
              AND (SELECT count(*) FROM pg_constraint WHERE conrelid = 'public.camera_events'::regclass
                AND contype = 'u' AND conname = 'camera_events_camera_id_source_event_id_key') = 1
              AND (SELECT count(*) FROM pg_constraint WHERE conrelid = 'public.camera_events'::regclass
                AND contype = 'f') = 2
              AND (SELECT count(*) FROM pg_constraint WHERE conrelid = 'public.cameras'::regclass
                AND contype = 'f') = 1
              AND NOT has_table_privilege('anon', 'public.sites', 'SELECT')
              AND NOT has_table_privilege('anon', 'public.cameras', 'SELECT')
              AND NOT has_table_privilege('anon', 'public.camera_events', 'SELECT')
              AND NOT has_table_privilege('authenticated', 'public.sites', 'SELECT')
              AND NOT has_table_privilege('authenticated', 'public.cameras', 'SELECT')
              AND NOT has_table_privilege('authenticated', 'public.camera_events', 'SELECT');
            """;
        Assert.Equal(true, await command.ExecuteScalarAsync());
    }
}
