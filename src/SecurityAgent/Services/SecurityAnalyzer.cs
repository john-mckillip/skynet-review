using GitHub.Copilot.SDK;
using SkynetReview.Shared.Models;
using SkynetReview.SecurityAgent.Configuration;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SkynetReview.SecurityAgent.Services;

public class SecurityAnalyzer : ISecurityAnalyzer
{
    private readonly ILogger<SecurityAnalyzer> _logger;
    internal SecurityRulesConfig _rulesConfig;

    public SecurityAnalyzer(
        ILogger<SecurityAnalyzer> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        _rulesConfig = LoadSecurityRules(configuration);
    }

    public async Task<SecurityFinding[]> AnalyzeAsync(AnalysisRequest request)
    {
        _logger.LogInformation("Starting security analysis for {FileCount} files", request.FilePaths.Length);

        var findings = new List<SecurityFinding>();

        await using var client = new CopilotClient();
        await client.StartAsync();

        if (_rulesConfig.EnableBatching && request.FilePaths.Length > 1)
        {
            var batches = CreateBatches(request.FilePaths, request.FileContents);
            _logger.LogInformation("Created {BatchCount} batches for analysis", batches.Count);

            foreach (var batch in batches)
            {
                var batchFindings = await AnalyzeBatchAsync(client, batch);
                findings.AddRange(batchFindings);
            }
        }
        else
        {
            foreach (var filePath in request.FilePaths)
            {
                if (!request.FileContents.TryGetValue(filePath, out var content))
                {
                    _logger.LogWarning("File content not found for {FilePath}", filePath);
                    continue;
                }

                var fileFindings = await AnalyzeFileAsync(client, filePath, content);
                findings.AddRange(fileFindings);
            }
        }

        _logger.LogInformation("Security analysis complete. Found {FindingCount} issues", findings.Count);
        return [.. findings];
    }

    public async IAsyncEnumerable<SecurityFinding> AnalyzeStreamAsync(
        AnalysisRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting streaming security analysis for {FileCount} files", request.FilePaths.Length);

        await using var client = new CopilotClient();
        await client.StartAsync(cancellationToken);

        var useBatching = _rulesConfig.EnableBatching && request.FilePaths.Length > 1;
        var findings = useBatching
            ? StreamBatchedAnalysis(client, request, cancellationToken)
            : StreamIndividualAnalysis(client, request, cancellationToken);

        await foreach (var finding in findings)
        {
            yield return finding;
        }

        _logger.LogInformation("Streaming security analysis complete");
    }

    private async IAsyncEnumerable<SecurityFinding> StreamBatchedAnalysis(
        CopilotClient client,
        AnalysisRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var batches = CreateBatches(request.FilePaths, request.FileContents);
        _logger.LogInformation("Created {BatchCount} batches for streaming analysis", batches.Count);

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Processing batch of {FileCount} files", batch.Count);

            foreach (var finding in await AnalyzeBatchAsync(client, batch))
            {
                yield return finding;
            }
        }
    }

    private async IAsyncEnumerable<SecurityFinding> StreamIndividualAnalysis(
        CopilotClient client,
        AnalysisRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var filePath in request.FilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!request.FileContents.TryGetValue(filePath, out var content))
            {
                _logger.LogWarning("File content not found for {FilePath}", filePath);
                continue;
            }

            _logger.LogInformation("Analyzing file: {FilePath}", filePath);

            foreach (var finding in await AnalyzeFileAsync(client, filePath, content))
            {
                yield return finding;
            }
        }
    }

    /// <summary>
    /// Loads security rules configuration from a YAML file.
    /// </summary>
    /// <param name="configuration">The application configuration instance.</param>
    /// <returns>A SecurityRulesConfig object loaded from the YAML file or default settings if the file is not found or an error occurs.</returns>
    private SecurityRulesConfig LoadSecurityRules(IConfiguration configuration)
    {
        var configPath = configuration["SecurityRules:ConfigPath"] ?? "security-rules.yml";
        
        if (!File.Exists(configPath))
        {
            _logger.LogWarning("Security rules config not found at {Path}, using defaults", configPath);
            return GetDefaultConfig();
        }

        try
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var config = deserializer.Deserialize<SecurityRulesConfig>(yaml);
            _logger.LogInformation("Loaded security rules from {Path}", configPath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading security rules config, using defaults");
            return GetDefaultConfig();
        }
    }

    /// <summary>
    /// Provides a default security rules configuration.
    /// </summary>
    /// <returns>A SecurityRulesConfig object with default settings.</returns>
    private static SecurityRulesConfig GetDefaultConfig()
    {
        return new SecurityRulesConfig
        {
            Model = "gpt-5 mini",
            SystemPrompt = "You are a security analysis expert. Analyze the following code for security vulnerabilities.",
            IncludeRulesInPrompt = true,
            Rules =
            [
                new() { Category = "SQL Injection", Description = "SQL Injection vulnerabilities", Enabled = true },
                new() { Category = "Hardcoded Secrets", Description = "Hardcoded secrets or credentials", Enabled = true },
                new() { Category = "Authentication & Authorization", Description = "Authentication and authorization issues", Enabled = true },
                new() { Category = "Input Validation", Description = "Input validation problems", Enabled = true }
            ],
            OutputFormat = "Respond with ONLY a JSON array of findings."
        };
    }

    /// <summary>
    /// Analyzes a single file for security issues using Copilot.
    /// </summary>
    /// <param name="client">The Copilot client instance.</param>
    /// <param name="filePath">The path of the file being analyzed.</param>
    /// <param name="content">The content of the file to analyze.</param>
    /// <returns>A list of SecurityFinding objects representing the issues found.</returns>
    private async Task<List<SecurityFinding>> AnalyzeFileAsync(
        CopilotClient client,
        string filePath,
        string content)
    {
        var findings = new List<SecurityFinding>();

        await using var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = _rulesConfig.Model,
            Streaming = true
        });
        
        var prompt = BuildSecurityPrompt(filePath, content);
        var responseContent = new StringBuilder();
        var done = new TaskCompletionSource();

        session.On(evt =>
        {
            _logger.LogDebug("Copilot event received: {EventType}", evt.GetType().Name);

            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    // Incremental text chunk
                    responseContent.Append(delta.Data.DeltaContent);
                    break;
                case AssistantReasoningDeltaEvent reasoningDelta:
                     // Incremental reasoning chunk (model-dependent)
                    responseContent.Append(reasoningDelta.Data.DeltaContent);
                    break;
                case AssistantMessageEvent msg:
                    // Final complete message
                    responseContent.Append(msg.Data.Content);
                    break;
                case AssistantReasoningEvent reasoning:
                    // Final reasoning content
                    responseContent.Append(reasoning.Data.Content);
                    break;
                case SessionIdleEvent:
                    _logger.LogDebug("Session idle. Response length: {Length}", responseContent.Length);
                    done.SetResult();
                    break;
                case SessionErrorEvent error:
                    _logger.LogError("Copilot session error: {Error}", error.Data?.Message ?? "Unknown error");
                    done.TrySetException(new Exception(error.Data?.Message ?? "Copilot session error"));
                    break;
                default:
                    _logger.LogWarning("Unhandled Copilot event type: {EventType}", evt.GetType().FullName);
                    break;
            }
        });

        try
        {
            await session.SendAsync(new MessageOptions { Prompt = prompt });
            await done.Task;

            _logger.LogInformation("Received response from Copilot for {FilePath}", filePath);

            findings = ParseCopilotResponse(responseContent.ToString(), filePath);

            // Filter out findings for disabled rule categories
            if (_rulesConfig.IncludeRulesInPrompt)
            {
                findings = FilterFindingsByEnabledRules(findings);
            }  
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file {FilePath}", filePath);
        }

        return findings;
    }

    /// <summary>
    /// Builds the security analysis prompt for Copilot.
    /// </summary>
    /// <param name="filePath">The path of the file being analyzed.</param>
    /// <param name="content">The content of the file to analyze.</param>
    /// <returns>A string containing the prompt for Copilot.</returns>
    internal string BuildSecurityPrompt(string filePath, string content)
    {
        var sb = new StringBuilder();
        
        // Add system prompt from config
        sb.AppendLine(_rulesConfig.SystemPrompt);
        sb.AppendLine();

        // Add enabled rules if configured
        if (_rulesConfig.IncludeRulesInPrompt)
        {
            sb.AppendLine("Focus on:");
            foreach (var rule in _rulesConfig.Rules.Where(r => r.Enabled))
            {
                sb.AppendLine($"- {rule.Description}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"File: {filePath}");
        sb.AppendLine($"```{GetLanguageFromFilePath(filePath)}");
        sb.AppendLine(content);
        sb.AppendLine("```");
        sb.AppendLine();
        
        // Add output format from config
        sb.AppendLine(_rulesConfig.OutputFormat);

        return sb.ToString();
    }

    internal string BuildBatchedSecurityPrompt(Dictionary<string, string> files)
    {
        var sb = new StringBuilder();

        // Add system prompt from config
        sb.AppendLine(_rulesConfig.SystemPrompt);
        sb.AppendLine();

        // Add enabled rules if configured
        if (_rulesConfig.IncludeRulesInPrompt)
        {
            sb.AppendLine("Focus on:");
            foreach (var rule in _rulesConfig.Rules.Where(r => r.Enabled))
            {
                sb.AppendLine($"- {rule.Description}");
            }
            sb.AppendLine();
        }

        foreach (var (filePath, content) in files)
        {
            sb.AppendLine($"File: {filePath}");
            sb.AppendLine($"```{GetLanguageFromFilePath(filePath)}");
            sb.AppendLine(content);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Add output format from config
        sb.AppendLine(_rulesConfig.OutputFormat);

        return sb.ToString();
    }

    /// <summary>
    /// Analyzes a batch of files for security issues using a single Copilot session.
    /// </summary>
    private async Task<List<SecurityFinding>> AnalyzeBatchAsync(
        CopilotClient client,
        Dictionary<string, string> batch)
    {
        var findings = new List<SecurityFinding>();
        var validFilePaths = batch.Keys.ToHashSet();

        _logger.LogInformation("Analyzing batch of {FileCount} files: {Files}",
            batch.Count,
            string.Join(", ", batch.Keys.Select(Path.GetFileName)));

        await using var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = _rulesConfig.Model,
            Streaming = true
        });

        var prompt = BuildBatchedSecurityPrompt(batch);
        var responseContent = new StringBuilder();
        var done = new TaskCompletionSource();

        session.On(evt =>
        {
            _logger.LogDebug("Copilot event received: {EventType}", evt.GetType().Name);

            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    responseContent.Append(delta.Data.DeltaContent);
                    break;
                case AssistantReasoningDeltaEvent reasoningDelta:
                    responseContent.Append(reasoningDelta.Data.DeltaContent);
                    break;
                case AssistantMessageEvent msg:
                    responseContent.Append(msg.Data.Content);
                    break;
                case AssistantReasoningEvent reasoning:
                    responseContent.Append(reasoning.Data.Content);
                    break;
                case SessionIdleEvent:
                    _logger.LogDebug("Session idle. Response length: {Length}", responseContent.Length);
                    done.SetResult();
                    break;
                case SessionErrorEvent error:
                    _logger.LogError("Copilot session error: {Error}", error.Data?.Message ?? "Unknown error");
                    done.TrySetException(new Exception(error.Data?.Message ?? "Copilot session error"));
                    break;
            }
        });

        try
        {
            await session.SendAsync(new MessageOptions { Prompt = prompt });
            await done.Task;

            _logger.LogInformation("Received batch response from Copilot for {FileCount} files", batch.Count);

            findings = ParseBatchedCopilotResponse(responseContent.ToString(), validFilePaths);

            if (_rulesConfig.IncludeRulesInPrompt)
            {
                findings = FilterFindingsByEnabledRules(findings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing batch of {FileCount} files", batch.Count);

            // Fallback: try analyzing files individually
            _logger.LogWarning("Falling back to individual file analysis for failed batch");
            foreach (var (filePath, content) in batch)
            {
                try
                {
                    var fileFindings = await AnalyzeFileAsync(client, filePath, content);
                    findings.AddRange(fileFindings);
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "Error analyzing file {FilePath}", filePath);
                }
            }
        }

        return findings;
    }

    /// <summary>
    /// Parses the Copilot response for batched multi-file analysis.
    /// </summary>
    internal List<SecurityFinding> ParseBatchedCopilotResponse(
        string response,
        HashSet<string> validFilePaths)
    {
        var findings = new List<SecurityFinding>();
        var parsedFindings = ExtractFindingsFromJson(response);

        if (parsedFindings is null)
            return findings;

        foreach (var finding in parsedFindings)
        {
            var converted = TryConvertToSecurityFinding(finding, validFilePaths);
            if (converted is not null)
                findings.Add(converted);
        }

        return findings;
    }

    /// <summary>
    /// Extracts and deserializes findings from a JSON response string.
    /// </summary>
    private CopilotFinding[]? ExtractFindingsFromJson(string response)
    {
        var jsonStart = response.IndexOf('[');
        var jsonEnd = response.LastIndexOf(']');

        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            _logger.LogWarning("No JSON array found in batched response");
            return null;
        }

        try
        {
            var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            _logger.LogDebug("Parsing batched JSON response: {Json}", json);

            return JsonSerializer.Deserialize<CopilotFinding[]>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse batched Copilot response");
            return null;
        }
    }

    /// <summary>
    /// Converts a CopilotFinding to a SecurityFinding, validating the file path.
    /// </summary>
    private SecurityFinding? TryConvertToSecurityFinding(
        CopilotFinding finding,
        HashSet<string> validFilePaths)
    {
        var filePath = finding.FilePath ?? string.Empty;

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogWarning("Finding '{Title}' has no filePath, skipping", finding.Title);
            return null;
        }

        var matchedPath = FindMatchingFilePath(filePath, validFilePaths);

        if (matchedPath == null)
        {
            _logger.LogWarning("Finding '{Title}' references unknown file '{FilePath}', skipping",
                finding.Title, filePath);
            return null;
        }

        return new SecurityFinding(
            Id: finding.RuleId,
            Title: finding.Title,
            Description: finding.Description,
            SeverityLevel: ParseSeverity(finding.Severity),
            FilePath: matchedPath,
            LineNumber: finding.LineNumber,
            CodeSnippet: finding.CodeSnippet,
            Remediation: finding.Remediation
        );
    }

    /// <summary>
    /// Finds a matching file path from the valid paths, handling path variations.
    /// </summary>
    internal static string? FindMatchingFilePath(string responsePath, HashSet<string> validPaths)
    {
        if (validPaths.Contains(responsePath))
        {
            return responsePath;
        }

        var normalizedResponsePath = responsePath.Replace('\\', '/');

        foreach (var validPath in validPaths)
        {
            var normalizedValidPath = validPath.Replace('\\', '/');

            if (normalizedResponsePath.Equals(normalizedValidPath, StringComparison.OrdinalIgnoreCase))
            {
                return validPath;
            }

            if (normalizedValidPath.EndsWith(normalizedResponsePath, StringComparison.OrdinalIgnoreCase))
            {
                return validPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses the Copilot response to extract security findings.
    /// </summary>
    /// <param name="response">The raw response string from Copilot.</param>
    /// <param name="filePath">The file path associated with the findings.</param>
    /// <returns>A list of SecurityFinding objects parsed from the response.</returns>
    private List<SecurityFinding> ParseCopilotResponse(string response, string filePath)
    {
        var findings = new List<SecurityFinding>();

        try
        {
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                
                _logger.LogInformation("Parsing JSON response: {Json}", json);
                
                var parsedFindings = JsonSerializer.Deserialize<CopilotFinding[]>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsedFindings is not null)
                {
                    foreach (var finding in parsedFindings)
                    {
                        findings.Add(new SecurityFinding(
                            Id: finding.RuleId,
                            Title: finding.Title,
                            Description: finding.Description,
                            SeverityLevel: ParseSeverity(finding.Severity),
                            FilePath: filePath,
                            LineNumber: finding.LineNumber,
                            CodeSnippet: finding.CodeSnippet,
                            Remediation: finding.Remediation
                        ));
                    }
                }
            }
            else
            {
                _logger.LogWarning("No JSON array found in response: {Response}", response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Copilot response: {Response}", response);
        }

        return findings;
    }
    
    /// <summary>
    /// Gets the markdown language identifier for a file based on its extension.
    /// </summary>
    /// <param name="filePath">The file path to get the language for.</param>
    /// <returns>A markdown language identifier string.</returns>
    internal static string GetLanguageFromFilePath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".jsx" => "jsx",
            ".tsx" => "tsx",
            ".py" => "python",
            ".java" => "java",
            ".go" => "go",
            ".rs" => "rust",
            ".rb" => "ruby",
            ".php" => "php",
            ".swift" => "swift",
            ".kt" or ".kts" => "kotlin",
            ".scala" => "scala",
            ".c" or ".h" => "c",
            ".cpp" or ".cc" or ".cxx" or ".hpp" => "cpp",
            ".sql" => "sql",
            ".html" or ".htm" => "html",
            ".css" => "css",
            ".scss" => "scss",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".sh" or ".bash" => "bash",
            ".ps1" => "powershell",
            ".md" => "markdown",
            _ => "text"
        };
    }

    /// <summary>
    /// Parses the severity string into a Severity enum.
    /// </summary>
    /// <param name="severity">The severity level as a string.</param>
    /// <returns>A Severity enum value corresponding to the input string.</returns>
    private static Severity ParseSeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "critical" => Severity.Critical,
            "high" => Severity.High,
            "medium" => Severity.Medium,
            "low" => Severity.Low,
            _ => Severity.Info
        };
    }
    
    /// <summary>
    /// Filters findings based on enabled security rules.
    /// </summary>
    /// <param name="findings">The list of security findings to filter.</param>
    /// <returns>A filtered list of security findings that match enabled rules.</returns>
    private List<SecurityFinding> FilterFindingsByEnabledRules(List<SecurityFinding> findings)
    {
        var enabledCategories = _rulesConfig.Rules
            .Where(r => r.Enabled)
            .Select(r => NormalizeForMatching(r.Category))
            .ToHashSet();

        return [.. findings.Where(finding =>
        {
            // Normalize all finding fields for comparison
            var findingTitle = NormalizeForMatching(finding.Title);
            var findingId = NormalizeForMatching(finding.Id);
            var findingDescription = NormalizeForMatching(finding.Description);

            // Try to match finding to a rule category using keyword overlap
            var matchesCategory = enabledCategories.Any(category =>
            {
                // Extract key words from category (split and filter short words)
                var categoryWords = category.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3)
                    .ToList();

                // Check if any category keyword appears in the finding
                var hasKeywordMatch = categoryWords.Any(word =>
                    findingTitle.Contains(word) ||
                    findingDescription.Contains(word) ||
                    findingId.Contains(word));

                // Also check full category match
                return hasKeywordMatch ||
                    findingTitle.Contains(category) ||
                    category.Contains(findingTitle) ||
                    findingDescription.Contains(category);
            });

            if (!matchesCategory)
            {
                _logger.LogInformation("Filtering out finding {Id} - {Title} (no matching enabled category)",
                    finding.Id, finding.Title);
            }

            return matchesCategory;
        })];
    }

    /// <summary>
    /// Splits files into batches based on batch size and token limits.
    /// </summary>
    internal List<Dictionary<string, string>> CreateBatches(
        string[] filePaths,
        Dictionary<string, string> fileContents)
    {
        var batches = new List<Dictionary<string, string>>();
        var currentBatch = new Dictionary<string, string>();
        var currentTokenEstimate = 0;

        foreach (var filePath in filePaths)
        {
            if (!fileContents.TryGetValue(filePath, out var content))
            {
                continue;
            }

            var fileTokenEstimate = EstimateTokens(filePath, content);

            var wouldExceedBatchSize = currentBatch.Count >= _rulesConfig.BatchSize;
            var wouldExceedTokenLimit = currentTokenEstimate + fileTokenEstimate > _rulesConfig.MaxBatchTokens;

            if (currentBatch.Count > 0 && (wouldExceedBatchSize || wouldExceedTokenLimit))
            {
                batches.Add(currentBatch);
                currentBatch = new Dictionary<string, string>();
                currentTokenEstimate = 0;
            }

            currentBatch[filePath] = content;
            currentTokenEstimate += fileTokenEstimate;
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// Estimates the token count for a file including delimiters.
    /// </summary>
    private static int EstimateTokens(string filePath, string content)
    {
        var contentTokens = content.Length / 4;
        var overhead = 50 + (filePath.Length / 4);
        return contentTokens + overhead;
    }

    /// <summary>
    /// Normalizes text for fuzzy matching by lowercasing, replacing ampersand with 'and', and removing special characters.
    /// </summary>
    private static string NormalizeForMatching(string text)
    {
        return text.ToLower()
            .Replace("&", "and")
            .Replace("-", " ")
            .Replace("_", " ");
    }

    /// <summary>
    /// Internal class to represent the structure of findings returned by Copilot.
    /// </summary>
    private sealed class CopilotFinding
    {
        public string? FilePath { get; set; } = null;
        /// <summary>
        /// The unique identifier of the security rule.
        /// </summary>
        public string RuleId { get; set; } = string.Empty;
        /// <summary>
        /// The title of the security finding.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// The detailed description of the security finding.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// The severity level of the security finding.
        /// </summary>
        public string Severity { get; set; } = string.Empty;
        /// <summary>
        /// The line number where the issue was found.
        /// </summary>
        public int? LineNumber { get; set; } = null;
        /// <summary>
        /// The code snippet related to the finding.
        /// </summary>
        public string? CodeSnippet { get; set; } = null;
        /// <summary>
        /// The recommended remediation for the finding.
        /// </summary>
        public string Remediation { get; set; } = string.Empty;
    }
}