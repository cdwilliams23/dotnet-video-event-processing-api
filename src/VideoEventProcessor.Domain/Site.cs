namespace VideoEventProcessor.Domain;

public sealed class Site
{
    public Guid SiteId { get; init; }
    public required string SiteName { get; init; }
}
