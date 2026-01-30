using SkynetReview.Gateway.Services;
using SkynetReview.Shared.Models;
using System.Text.Json;

// Configure JSON serialization to use camelCase for API responses
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure HttpClient with extended timeout for long-running analysis
builder.Services.AddHttpClient("SecurityAgent", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10); // Extended timeout for AI analysis
});
builder.Services.AddHttpClient(); // Default client for other uses

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

app.MapPost("/api/analyze/stream", async (
    AnalysisRequest request,
    IAgentOrchestrator orchestrator,
    HttpContext context,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");

    var startTime = DateTime.UtcNow;
    var findingCount = 0;

    try
    {
        // Send initial event to confirm stream is active
        await context.Response.WriteAsync($"event: started\ndata: {{\"fileCount\":{request.FilePaths.Length}}}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        await foreach (var finding in orchestrator.AnalyzeStreamAsync(request, cancellationToken))
        {
            findingCount++;
            var json = JsonSerializer.Serialize(finding, jsonOptions);
            await context.Response.WriteAsync($"event: finding\ndata: {json}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }

        var duration = DateTime.UtcNow - startTime;
        var summary = new { TotalFindings = findingCount, Duration = duration.ToString(), Success = true };
        var summaryJson = JsonSerializer.Serialize(summary, jsonOptions);
        await context.Response.WriteAsync($"event: complete\ndata: {summaryJson}\n\n", cancellationToken);
    }
    catch (OperationCanceledException ex)
    {
        // Client disconnected - expected behavior, log as debug
        logger.LogDebug(ex, "Stream cancelled by client");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in streaming analysis");
        try
        {
            var error = new { Success = false, ErrorMessage = ex.Message };
            var errorJson = JsonSerializer.Serialize(error, jsonOptions);
            await context.Response.WriteAsync($"event: error\ndata: {errorJson}\n\n", CancellationToken.None);
        }
        catch
        {
            // Response may already be closed - ignore
        }
    }
})
.WithName("AnalyzeFilesStream");

// Upload files and analyze
app.MapPost("/api/analyze/upload", async (
    HttpRequest request,
    IAgentOrchestrator orchestrator,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Request must be multipart/form-data");
    }

    var form = await request.ReadFormAsync();
    var files = form.Files;

    if (files.Count == 0)
    {
        return Results.BadRequest("No files provided");
    }

    // Upload files to File Service
    var fileServiceUrl = configuration["ServiceEndpoints:FileServiceUrl"];
    var client = httpClientFactory.CreateClient();
    
    var content = new MultipartFormDataContent();
    foreach (var file in files)
    {
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", file.FileName);
    }

    var uploadResponse = await client.PostAsync($"{fileServiceUrl}/api/files/upload", content);
    
    if (!uploadResponse.IsSuccessStatusCode)
    {
        return Results.Problem("Failed to upload files to File Service");
    }

    var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadResult>();
    
    if (uploadResult?.FileIds == null)
    {
        return Results.Problem("Invalid response from File Service");
    }

    // Create analysis request with file IDs
    var analysisRequest = new AnalysisRequest(
        FilePaths: [.. uploadResult.FileIds.Values],
        FileContents: new Dictionary<string, string>() // Empty - will be fetched from File Service
    );

    // Analyze
    var results = await orchestrator.AnalyzeAsync(analysisRequest);
    
    return Results.Ok(results);
})
.WithName("AnalyzeUploadedFiles")
.DisableAntiforgery();

app.MapGet("/api/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "gateway",
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck");

await app.RunAsync();