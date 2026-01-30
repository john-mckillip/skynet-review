using SkynetReview.SecurityAgent.Services;
using SkynetReview.Shared.Models;
using System.Text.Json;

// Configure JSON serialization to use camelCase for API responses
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Register the security analyzer
builder.Services.AddScoped<ISecurityAnalyzer, SecurityAnalyzer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/security/analyze", async (
    AnalysisRequest request,
    ISecurityAnalyzer analyzer) =>
{
    try
    {
        var startTime = DateTime.UtcNow;
        var findings = await analyzer.AnalyzeAsync(request);
        var duration = DateTime.UtcNow - startTime;

        return Results.Ok(new AnalysisResult(
            AgentType: "Security",
            Findings: findings,
            Duration: duration,
            Success: true
        ));
    }
    catch (Exception ex)
    {
        return Results.Ok(new AnalysisResult(
            AgentType: "Security",
            Findings: [],
            Duration: TimeSpan.Zero,
            Success: false,
            ErrorMessage: ex.Message
        ));
    }
})
.WithName("AnalyzeSecurity");

app.MapPost("/api/security/analyze/stream", async (
    AnalysisRequest request,
    ISecurityAnalyzer analyzer,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");

    var startTime = DateTime.UtcNow;
    var findingCount = 0;

    try
    {
        await foreach (var finding in analyzer.AnalyzeStreamAsync(request, cancellationToken))
        {
            findingCount++;
            var json = JsonSerializer.Serialize(finding, jsonOptions);
            await context.Response.WriteAsync($"event: finding\ndata: {json}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }

        var duration = DateTime.UtcNow - startTime;
        var summary = new { AgentType = "Security", FindingCount = findingCount, Duration = duration.ToString(), Success = true };
        var summaryJson = JsonSerializer.Serialize(summary, jsonOptions);
        await context.Response.WriteAsync($"event: complete\ndata: {summaryJson}\n\n", cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // Client disconnected - this is expected
    }
    catch (Exception ex)
    {
        var error = new { Success = false, ErrorMessage = ex.Message };
        var errorJson = JsonSerializer.Serialize(error, jsonOptions);
        await context.Response.WriteAsync($"event: error\ndata: {errorJson}\n\n", CancellationToken.None);
    }
})
.WithName("AnalyzeSecurityStream");

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "security-agent" }))
    .WithName("HealthCheck");

await app.RunAsync();
