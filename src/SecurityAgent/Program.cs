using SkynetReview.SecurityAgent.Services;
using SkynetReview.Shared.Models;

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

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "security-agent" }))
    .WithName("HealthCheck");

await app.RunAsync();
