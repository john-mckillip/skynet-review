using Microsoft.Extensions.Options;

namespace SkynetReview.FileService.Services;

public class FileManager : IFileManager
{
    private readonly ILogger<FileManager> _logger;
    private readonly string _storagePath;

    public FileManager(
        ILogger<FileManager> logger,
        IOptions<FileStorageOptions> options)
    {
        _logger = logger;
        _storagePath = options.Value.Path;

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created storage directory at {Path}", _storagePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var fileId = Guid.NewGuid().ToString();
        var filePath = Path.Combine(_storagePath, fileId);

        _logger.LogInformation("Saving file {FileName} with ID {FileId}", file.FileName, fileId);

        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Save metadata
            var metadataPath = $"{filePath}.meta";
            await File.WriteAllTextAsync(metadataPath, System.Text.Json.JsonSerializer.Serialize(new
            {
                OriginalFileName = file.FileName,
                file.ContentType,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            }));

            _logger.LogInformation("File {FileId} saved successfully", fileId);
            return fileId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file {FileId}", fileId);
            throw;
        }
    }

    public async Task<string?> GetFileAsync(string fileId)
    {
        var filePath = Path.Combine(_storagePath, fileId);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File {FileId} not found", fileId);
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("Retrieved file {FileId}", fileId);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        var filePath = Path.Combine(_storagePath, fileId);
        var metadataPath = $"{filePath}.meta";

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File {FileId} not found for deletion", fileId);
            return false;
        }

        try
        {
            await Task.Run(() =>
            {
                File.Delete(filePath);
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }
            });

            _logger.LogInformation("Deleted file {FileId}", fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", fileId);
            throw;
        }
    }

    public async Task CleanupOldFilesAsync(TimeSpan maxAge)
    {
        _logger.LogInformation("Starting cleanup of files older than {MaxAge}", maxAge);

        var cutoffTime = DateTime.UtcNow - maxAge;
        var files = Directory.GetFiles(_storagePath);
        var deletedCount = 0;

        foreach (var file in files)
        {
            if (file.EndsWith(".meta"))
                continue;

            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTimeUtc < cutoffTime)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        File.Delete(file);
                        var metaFile = $"{file}.meta";
                        if (File.Exists(metaFile))
                        {
                            File.Delete(metaFile);
                        }
                    });

                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting old file {File}", file);
                }
            }
        }

        _logger.LogInformation("Cleanup complete. Deleted {Count} files", deletedCount);
    }
}