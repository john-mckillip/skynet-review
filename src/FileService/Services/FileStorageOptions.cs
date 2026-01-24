namespace SkynetReview.FileService.Services;
/// <summary>
/// Options for file storage configuration.
/// </summary>
public class FileStorageOptions
{
    /// <summary>
    /// Gets or sets the path where files will be stored.
    /// </summary>
    public string Path { get; set; } = "./temp-files";
}