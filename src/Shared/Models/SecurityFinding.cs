namespace SkynetReview.Shared.Models;
/// <summary>
/// Represents a security finding identified during analysis.
/// </summary>
/// <param name="Id"></param>
/// <param name="Title"></param>
/// <param name="Description"></param>
/// <param name="SeverityLevel"></param>
/// <param name="FilePath"></param>
/// <param name="LineNumber"></param>
/// <param name="CodeSnippet"></param>
/// <param name="Remediation"></param>
public record SecurityFinding(
    string Id,
    string Title,
    string Description,
    Severity SeverityLevel,
    string FilePath,
    int? LineNumber,
    string? CodeSnippet,
    string Remediation
);