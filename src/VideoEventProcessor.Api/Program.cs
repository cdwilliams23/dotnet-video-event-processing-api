using Microsoft.EntityFrameworkCore;
using VideoEventProcessor.Api;
using VideoEventProcessor.Api.Contracts;
using VideoEventProcessor.Domain;
using VideoEventProcessor.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("VideoEvents")
    ?? throw new InvalidOperationException("Set ConnectionStrings__VideoEvents to a local PostgreSQL connection string.");
builder.Services.AddDbContext<VideoEventDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<EventStore>();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapHealthChecks("/health");

app.MapPost("/api/sites", async (
    CreateSiteRequest request,
    VideoEventDbContext db,
    CancellationToken cancellationToken) =>
{
    var name = request.SiteName?.Trim();

    if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
    {
        return Results.BadRequest(new { error = "Site name must be 1–200 characters." });
    }

    var site = new Site
    {
        SiteId = Guid.NewGuid(),
        SiteName = name
    };

    db.Sites.Add(site);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created(
        $"/api/sites/{site.SiteId}",
        new { site.SiteId, site.SiteName });
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/api/sites/{siteId:guid}", async (
    Guid siteId,
    VideoEventDbContext db,
    CancellationToken cancellationToken) =>
{
    var site = await db.Sites
        .AsNoTracking()
        .SingleOrDefaultAsync(s => s.SiteId == siteId, cancellationToken);

    if (site is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { site.SiteId, site.SiteName });
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/cameras", async (
    CreateCameraRequest request,
    VideoEventDbContext db,
    CancellationToken cancellationToken) =>
{
    var name = request.CameraName?.Trim();

    if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
    {
        return Results.BadRequest(new { error = "Camera name must be 1–200 characters." });
    }

    if (request.SiteId == Guid.Empty)
    {
        return Results.BadRequest(new { error = "SiteId is required." });
    }

    var siteExists = await db.Sites
        .AsNoTracking()
        .AnyAsync(s => s.SiteId == request.SiteId, cancellationToken);

    if (!siteExists)
    {
        return Results.NotFound(new { error = "Site not found." });
    }

    var camera = new Camera
    {
        CameraId = Guid.NewGuid(),
        SiteId = request.SiteId,
        CameraName = name
    };

    db.Cameras.Add(camera);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created(
        $"/api/cameras/{camera.CameraId}",
        new { camera.CameraId, camera.SiteId, camera.CameraName });
})
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/cameras/{cameraId:guid}", async (
    Guid cameraId,
    VideoEventDbContext db,
    CancellationToken cancellationToken) =>
{
    var camera = await db.Cameras
        .AsNoTracking()
        .SingleOrDefaultAsync(c => c.CameraId == cameraId, cancellationToken);

    if (camera is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { camera.CameraId, camera.SiteId, camera.CameraName });
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();
