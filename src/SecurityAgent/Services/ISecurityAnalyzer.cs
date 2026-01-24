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
    /// <param name="request"></param>
    /// <returns></returns>
    Task<SecurityFinding[]> AnalyzeAsync(AnalysisRequest request);
}