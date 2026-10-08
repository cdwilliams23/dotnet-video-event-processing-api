using Microsoft.EntityFrameworkCore;
using VideoEventProcessor.Domain;

namespace VideoEventProcessor.Infrastructure;

public sealed class VideoEventDbContext(DbContextOptions<VideoEventDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<CameraEvent> CameraEvents => Set<CameraEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>(entity =>
        {
            entity.ToTable("sites", "public");
            entity.HasKey(site => site.SiteId).HasName("sites_pkey");
            entity.Property(site => site.SiteId).HasColumnName("site_id").ValueGeneratedNever();
            entity.Property(site => site.SiteName).HasColumnName("site_name").HasColumnType("text").IsRequired();
        });

        modelBuilder.Entity<Camera>(entity =>
        {
            entity.ToTable("cameras", "public");
            entity.HasKey(camera => camera.CameraId).HasName("cameras_pkey");
            entity.Property(camera => camera.CameraId).HasColumnName("camera_id").ValueGeneratedNever();
            entity.Property(camera => camera.SiteId).HasColumnName("site_id");
            entity.Property(camera => camera.CameraName).HasColumnName("camera_name").HasColumnType("text").IsRequired();
            entity.HasOne<Site>().WithMany().HasForeignKey(camera => camera.SiteId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("cameras_site_id_fkey");
        });

        modelBuilder.Entity<CameraEvent>(entity =>
        {
            entity.ToTable("camera_events", "public");
            entity.HasKey(cameraEvent => cameraEvent.EventId).HasName("camera_events_pkey");
            entity.Property(cameraEvent => cameraEvent.EventId).HasColumnName("event_id").ValueGeneratedOnAdd();
            entity.Property(cameraEvent => cameraEvent.CameraId).HasColumnName("camera_id");
            entity.Property(cameraEvent => cameraEvent.SiteId).HasColumnName("site_id");
            entity.Property(cameraEvent => cameraEvent.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(cameraEvent => cameraEvent.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(cameraEvent => cameraEvent.SourceEventId).HasColumnName("source_event_id").HasColumnType("text").IsRequired();
            entity.Property(cameraEvent => cameraEvent.EventType).HasColumnName("event_type").HasColumnType("text").IsRequired();

            entity.HasOne<Camera>().WithMany().HasForeignKey(cameraEvent => cameraEvent.CameraId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("camera_events_camera_id_fkey");
            entity.HasOne<Site>().WithMany().HasForeignKey(cameraEvent => cameraEvent.SiteId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("camera_events_site_id_fkey");
            // PostgreSQL already enforces the unique constraint; its backing index has this name.
            entity.HasIndex(cameraEvent => new { cameraEvent.CameraId, cameraEvent.SourceEventId })
                .IsUnique().HasDatabaseName("camera_events_camera_id_source_event_id_key");
            entity.HasIndex(cameraEvent => new { cameraEvent.SiteId, cameraEvent.OccurredAtUtc })
                .HasDatabaseName("camera_events_site_occurred_idx");
        });
    }
}
