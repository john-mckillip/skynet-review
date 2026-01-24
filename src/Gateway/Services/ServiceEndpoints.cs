namespace SkynetReview.Gateway.Services;
/// <summary>
/// Configuration for service endpoints.
/// </summary>
public class ServiceEndpoints
{
    /// <summary>
    /// URL of the Security Agent service.
    /// </summary>
    public string SecurityAgentUrl { get; set; } = string.Empty;
    /// <summary>
    /// URL of the File Service.
    /// </summary>
    public string FileServiceUrl { get; set; } = string.Empty;
}