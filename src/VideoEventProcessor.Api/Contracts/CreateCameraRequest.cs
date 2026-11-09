namespace VideoEventProcessor.Api.Contracts;

public sealed record CreateCameraRequest(string CameraName, Guid SiteId);
