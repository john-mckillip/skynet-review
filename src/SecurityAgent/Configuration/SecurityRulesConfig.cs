using SkynetReview.Shared.Models;

namespace SkynetReview.SecurityAgent.Configuration;
/// <summary>
/// Configuration for security rules used in code analysis.
/// </summary>
public class SecurityRulesConfig
{
    /// <summary>
    /// The number of code files to process in a single batch.
    /// </summary>
    public int BatchSize { get; set; } = 5;
    /// <summary>
    /// The maximum number of tokens allowed in a single batch for AI processing.
    /// </summary>
    public int MaxBatchTokens { get; set; } =  100000;
    /// <summary>
    /// Whether to enable batching of code files for analysis.
    /// </summary>
    public bool EnableBatching { get; set; } = true;
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