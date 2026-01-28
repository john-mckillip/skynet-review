using SkynetReview.Shared.Models;

namespace SkynetReview.SecurityAgent.Configuration;
/// <summary>
/// Configuration for security rules used in code analysis.
/// </summary>
public class SecurityRulesConfig
{
    /// <summary>
    /// The AI model used for security analysis.
    /// </summary>
    public string Model { get; set; } = "gpt-5"; 
    /// <summary>
    /// The system prompt used for guiding the security analysis.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;
    /// <summary>
    /// Whether to include the rules list in the prompt sent to the AI model.
    /// When false, only the system prompt and code are sent.
    /// </summary>
    public bool IncludeRulesInPrompt { get; set; } = true;
    /// <summary>
    /// The list of security rules to be applied during analysis.
    /// </summary>
    public List<SecurityRule> Rules { get; set; } = [];
    /// <summary>
    /// The output format for the security analysis results.
    /// </summary>
    public string OutputFormat { get; set; } = string.Empty;
}