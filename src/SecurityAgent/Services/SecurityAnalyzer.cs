using GitHub.Copilot.SDK;
using SkynetReview.Shared.Models;
using System.Text;
using System.Text.Json;

namespace SkynetReview.SecurityAgent.Services;

public class SecurityAnalyzer(ILogger<SecurityAnalyzer> logger) : ISecurityAnalyzer
{
    private readonly ILogger<SecurityAnalyzer> _logger = logger;

    public async Task<SecurityFinding[]> AnalyzeAsync(AnalysisRequest request)
    {
        _logger.LogInformation("Starting security analysis for {FileCount} files", request.FilePaths.Length);

        var findings = new List<SecurityFinding>();

        // Let SDK manage the CLI process automatically
        await using var client = new CopilotClient();
        await client.StartAsync();

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

        _logger.LogInformation("Security analysis complete. Found {FindingCount} issues", findings.Count);
        return [.. findings];
    }

    private async Task<List<SecurityFinding>> AnalyzeFileAsync(
        CopilotClient client,
        string filePath,
        string content)
    {
        var findings = new List<SecurityFinding>();

        await using var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = "gpt-5"
        });

        var prompt = BuildSecurityPrompt(filePath, content);
        var responseContent = new StringBuilder();
        var done = new TaskCompletionSource();

        session.On(evt =>
        {
            if (evt is AssistantMessageEvent msg)
            {
                responseContent.Append(msg.Data.Content);
            }
            else if (evt is SessionIdleEvent)
            {
                done.SetResult();
            }
        });

        try
        {
            await session.SendAsync(new MessageOptions { Prompt = prompt });
            await done.Task;

            _logger.LogInformation("Received response from Copilot for {FilePath}", filePath);

            // Parse the response
            findings = ParseCopilotResponse(responseContent.ToString(), filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file {FilePath}", filePath);
        }

        return findings;
    }

    private static string BuildSecurityPrompt(string filePath, string content)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a security analysis expert. Analyze the following code for security vulnerabilities.");
        sb.AppendLine("Focus on:");
        sb.AppendLine("- SQL Injection vulnerabilities");
        sb.AppendLine("- Hardcoded secrets or credentials");
        sb.AppendLine("- Authentication and authorization issues");
        sb.AppendLine("- Input validation problems");
        sb.AppendLine("- Insecure cryptography");
        sb.AppendLine("- CORS misconfigurations");
        sb.AppendLine("- Exposure of sensitive data");
        sb.AppendLine();
        sb.AppendLine($"File: {filePath}");
        sb.AppendLine("```csharp");
        sb.AppendLine(content);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Respond with ONLY a JSON array of findings in this exact format (no markdown, no explanation):");
        sb.AppendLine("[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"ruleId\": \"SQL-001\",");
        sb.AppendLine("    \"title\": \"Potential SQL Injection\",");
        sb.AppendLine("    \"description\": \"Detailed description\",");
        sb.AppendLine("    \"severity\": \"High\",");
        sb.AppendLine("    \"lineNumber\": 5,");
        sb.AppendLine("    \"codeSnippet\": \"var query = ...\",");
        sb.AppendLine("    \"remediation\": \"Use parameterized queries\"");
        sb.AppendLine("  }");
        sb.AppendLine("]");
        sb.AppendLine();
        sb.AppendLine("If no issues found, return an empty array: []");

        return sb.ToString();
    }

    private List<SecurityFinding> ParseCopilotResponse(string response, string filePath)
    {
        var findings = new List<SecurityFinding>();

        try
        {
            // Extract JSON from response (Copilot might wrap it in markdown)
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

                if (parsedFindings != null)
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

    private sealed class CopilotFinding
    {
        public string RuleId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int? LineNumber { get; set; } = null;
        public string? CodeSnippet { get; set; } = null;
        public string Remediation { get; set; } = string.Empty;
    }
}