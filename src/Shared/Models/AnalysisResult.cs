namespace SkynetReview.Shared.Models;
/// <summary>
/// Represents the result of an analysis operation.
/// </summary>
/// <param name="AgentType"></param>
/// <param name="Findings"></param>
/// <param name="Duration"></param>
/// <param name="Success"></param>
/// <param name="ErrorMessage"></param>
public record AnalysisResult
(
    string AgentType,
    SecurityFinding[] Findings,
    TimeSpan Duration,
    bool Success,
    string? ErrorMessage = null
);