namespace SkynetReview.FileService.Services;
/// <summary>
/// Interface for managing file storage operations.
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// Saves a file and returns its identifier.
    /// </summary>
    /// <param name="file">The file to save.</param>
    /// <returns>The identifier of the saved file.</returns>
    Task<string> SaveFileAsync(IFormFile file);
    /// <summary>
    /// Retrieves the file path for a given file identifier.
    /// </summary>
    /// <param name="fileId">The identifier of the file.</param>
    /// <returns>The file path if found; otherwise, null.</returns>
    Task<string?> GetFileAsync(string fileId);
    /// <summary>
    /// Deletes a file with the given identifier.
    /// </summary>
    /// <param name="fileId">The identifier of the file to delete.</param>
    /// <returns>True if the file was deleted; otherwise, false.</returns>
    Task<bool> DeleteFileAsync(string fileId);
    /// <summary>
    /// Cleans up files older than the specified maximum age.
    /// </summary>
    /// <param name="maxAge">The maximum age of files to keep.</param>
    Task CleanupOldFilesAsync(TimeSpan maxAge);
}