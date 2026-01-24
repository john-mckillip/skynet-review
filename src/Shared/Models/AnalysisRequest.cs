namespace SkynetReview.Shared.Models;
/// <summary>
/// Represents a request for analysis of code files.
/// </summary>
/// <param name="FilePaths"></param>
/// <param name="FileContents"></param>
/// <param name="RepositoryContext"></param>
public record AnalysisRequest(
    string[] FilePaths,
    Dictionary<string, string> FileContents,
    string? RepositoryContext = null
);