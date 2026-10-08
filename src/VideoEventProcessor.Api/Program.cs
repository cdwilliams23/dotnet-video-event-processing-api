using Microsoft.EntityFrameworkCore;
using VideoEventProcessor.Api;
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

app.Run();
