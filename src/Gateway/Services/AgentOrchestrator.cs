using Microsoft.Extensions.Options;
using SkynetReview.Shared.Models;

namespace SkynetReview.Gateway.Services;

public class AgentOrchestrator(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceEndpoints> endpoints,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptions<ServiceEndpoints> _endpoints = endpoints;
    private readonly ILogger<AgentOrchestrator> _logger = logger;

    public async Task<AnalysisResult[]> AnalyzeAsync(AnalysisRequest request)
    {
        _logger.LogInformation("Starting analysis orchestration for {FileCount} files", request.FilePaths.Length);
        
        // If FileContents is empty but we have FilePaths, try to fetch from File Service
        if (request.FileContents.Count == 0 && request.FilePaths.Length > 0)
        {
            _logger.LogInformation("No file contents provided, fetching from File Service");
            request = await EnrichRequestWithFileContentsAsync(request);
        }
        
        var results = new List<AnalysisResult>();
        
        // Call Security Agent
        var securityResult = await CallSecurityAgentAsync(request);
        results.Add(securityResult);
        
        // Future: Add other agents here (Performance, Standards, etc.)
        
        _logger.LogInformation("Analysis orchestration complete. Total agents: {AgentCount}", results.Count);
        return results.ToArray();
    }

    private async Task<AnalysisRequest> EnrichRequestWithFileContentsAsync(AnalysisRequest request)
    {
        var fileContents = new Dictionary<string, string>();
        var client = _httpClientFactory.CreateClient();

        foreach (var filePath in request.FilePaths)
        {
            try
            {
                // Assume filePath is actually a fileId when FileContents is empty
                var url = $"{_endpoints.Value.FileServiceUrl}/api/files/{filePath}";
                _logger.LogInformation("Fetching file content from {Url}", url);

                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch file {FileId} from File Service: {StatusCode}", 
                        filePath, response.StatusCode);
                    continue;
                }

                var fileResponse = await response.Content.ReadFromJsonAsync<FileContentResponse>();
                
                if (fileResponse?.Content != null)
                {
                    fileContents[filePath] = fileResponse.Content;
                    _logger.LogInformation("Retrieved content for file {FileId}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching file {FileId} from File Service", filePath);
            }
        }

        return request with { FileContents = fileContents };
    }

    private async Task<AnalysisResult> CallSecurityAgentAsync(AnalysisRequest request)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_endpoints.Value.SecurityAgentUrl}/api/security/analyze";
        
        _logger.LogInformation("Calling Security Agent at {Url}", url);
        
        try
        {
            var response = await client.PostAsJsonAsync(url, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Security Agent returned {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                
                return new AnalysisResult(
                    AgentType: "Security",
                    Findings: Array.Empty<SecurityFinding>(),
                    Duration: TimeSpan.Zero,
                    Success: false,
                    ErrorMessage: $"Security Agent returned {response.StatusCode}"
                );
            }
            
            var result = await response.Content.ReadFromJsonAsync<AnalysisResult>();
            
            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize Security Agent response");
            }
            
            _logger.LogInformation("Security Agent returned {FindingCount} findings in {Duration}", 
                result.Findings.Length, result.Duration);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Security Agent");
            return new AnalysisResult(
                AgentType: "Security",
                Findings: Array.Empty<SecurityFinding>(),
                Duration: TimeSpan.Zero,
                Success: false,
                ErrorMessage: ex.Message
            );
        }
    }
    /// <summary>
    /// Response model for file content retrieval from File Service.
    /// </summary>
    private sealed class FileContentResponse
    {
        /// <summary>
        /// The unique identifier of the file.
        /// </summary>
        public string FileId { get; set; } = string.Empty;
        /// <summary>
        /// The content of the file as a string.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}