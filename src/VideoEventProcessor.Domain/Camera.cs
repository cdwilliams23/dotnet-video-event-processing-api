namespace VideoEventProcessor.Domain;

public sealed class Camera
{
    public Guid CameraId { get; init; }
    public Guid SiteId { get; init; }
    public required string CameraName { get; init; }
}
