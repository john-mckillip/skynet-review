using SkynetReview.FileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register file manager
builder.Services.AddSingleton<IFileManager, FileManager>();

// Configure file storage path
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection("FileStorage"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/files/upload", async (
    HttpRequest request,
    IFileManager fileManager) =>
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

    var fileIds = new Dictionary<string, string>();

    foreach (var file in files)
    {
        var fileId = await fileManager.SaveFileAsync(file);
        fileIds[file.FileName] = fileId;
    }

    return Results.Ok(new { fileIds });
})
.WithName("UploadFiles")
.DisableAntiforgery(); // For testing; enable proper CSRF protection in production

app.MapGet("/api/files/{fileId}", async (
    string fileId,
    IFileManager fileManager) =>
{
    var fileContent = await fileManager.GetFileAsync(fileId);
    
    if (fileContent == null)
    {
        return Results.NotFound(new { error = "File not found" });
    }

    return Results.Ok(new { fileId, content = fileContent });
})
.WithName("GetFile");

app.MapDelete("/api/files/{fileId}", async (
    string fileId,
    IFileManager fileManager) =>
{
    var deleted = await fileManager.DeleteFileAsync(fileId);
    
    if (!deleted)
    {
        return Results.NotFound(new { error = "File not found" });
    }

    return Results.Ok(new { fileId, deleted = true });
})
.WithName("DeleteFile");

app.MapGet("/api/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "file-service",
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck");

await app.RunAsync();