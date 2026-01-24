using SkynetReview.Gateway.Services;
using SkynetReview.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

// Register orchestrator
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

// Service URLs from configuration
builder.Services.Configure<ServiceEndpoints>(
    builder.Configuration.GetSection("ServiceEndpoints"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/analyze", async (
    AnalysisRequest request,
    IAgentOrchestrator orchestrator) =>
{
    var results = await orchestrator.AnalyzeAsync(request);
    return Results.Ok(results);
})
.WithName("AnalyzeFiles");

app.MapGet("/api/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "gateway",
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck");

await app.RunAsync();