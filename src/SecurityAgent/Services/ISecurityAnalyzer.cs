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
}