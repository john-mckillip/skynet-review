using SkynetReview.Shared.Models;

namespace SkynetReview.SecurityAgent.Services;
/// <summary>
/// Defines the contract for a security analyzer service.
/// </summary>
public interface ISecurityAnalyzer
{
    /// <summary>
    /// Analyzes the provided request for security issues.
    /// </summary>
    /// <param name="request">The analysis request containing data to be analyzed.</param>
    /// <returns>An array of security findings resulting from the analysis.</returns>
    Task<SecurityFinding[]> AnalyzeAsync(AnalysisRequest request);

    /// <summary>
    /// Analyzes the provided request for security issues, streaming findings as they are discovered.
    /// </summary>
    /// <param name="request">The analysis request containing data to be analyzed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of security findings streamed as each file completes analysis.</returns>
    IAsyncEnumerable<SecurityFinding> AnalyzeStreamAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default);
}