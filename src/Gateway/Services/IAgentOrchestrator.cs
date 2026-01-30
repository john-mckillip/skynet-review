using SkynetReview.Shared.Models;

namespace SkynetReview.Gateway.Services;
/// <summary>
/// Orchestrates agent-based analysis tasks.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Analyzes the provided request using agent-based processing.
    /// </summary>
    /// <param name="request">The analysis request containing files and parameters.</param>
    /// <returns>An array of analysis results from the agents.</returns>
    Task<AnalysisResult[]> AnalyzeAsync(AnalysisRequest request);

    /// <summary>
    /// Analyzes the provided request, streaming security findings as they are discovered.
    /// </summary>
    /// <param name="request">The analysis request containing files and parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of security findings streamed as each file completes analysis.</returns>
    IAsyncEnumerable<SecurityFinding> AnalyzeStreamAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default);
}