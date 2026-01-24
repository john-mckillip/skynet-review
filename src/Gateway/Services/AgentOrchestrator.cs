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
        
        var results = new List<AnalysisResult>();
        
        // Call Security Agent
        var securityResult = await CallSecurityAgentAsync(request);
        results.Add(securityResult);
        
        // Future: Add other agents here (Performance, Standards, etc.)
        
        _logger.LogInformation("Analysis orchestration complete. Total agents: {AgentCount}", results.Count);
        return [.. results];
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
            
            var result = await response.Content.ReadFromJsonAsync<AnalysisResult>() ?? throw new InvalidOperationException("Failed to deserialize Security Agent response");

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
}