namespace VideoEventProcessor.Domain;

public sealed class CameraEvent
{
    public Guid CameraId { get; init; }
    public Guid SiteId { get; init; }
    public long EventId { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
    public required string SourceEventId { get; init; }
    public required string EventType { get; init; }
}
