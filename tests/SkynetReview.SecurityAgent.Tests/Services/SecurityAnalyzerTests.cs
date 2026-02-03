using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SkynetReview.SecurityAgent.Configuration;
using SkynetReview.SecurityAgent.Services;
using SkynetReview.Shared.Models;

namespace SkynetReview.SecurityAgent.Tests.Services;

[TestFixture]
public class SecurityAnalyzerTests
{
    private Mock<ILogger<SecurityAnalyzer>> _loggerMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private SecurityAnalyzer _analyzer = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<SecurityAnalyzer>>();
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["SecurityRules:ConfigPath"]).Returns((string?)null);

        _analyzer = new SecurityAnalyzer(_loggerMock.Object, _configurationMock.Object);
    }

    #region GetLanguageFromFilePath Tests

    [TestCase("test.cs", "csharp")]
    [TestCase("test.js", "javascript")]
    [TestCase("test.ts", "typescript")]
    [TestCase("test.jsx", "jsx")]
    [TestCase("test.tsx", "tsx")]
    [TestCase("test.py", "python")]
    [TestCase("test.java", "java")]
    [TestCase("test.go", "go")]
    [TestCase("test.rs", "rust")]
    [TestCase("test.rb", "ruby")]
    [TestCase("test.php", "php")]
    [TestCase("test.swift", "swift")]
    [TestCase("test.kt", "kotlin")]
    [TestCase("test.kts", "kotlin")]
    [TestCase("test.scala", "scala")]
    [TestCase("test.c", "c")]
    [TestCase("test.h", "c")]
    [TestCase("test.cpp", "cpp")]
    [TestCase("test.cc", "cpp")]
    [TestCase("test.cxx", "cpp")]
    [TestCase("test.hpp", "cpp")]
    [TestCase("test.sql", "sql")]
    [TestCase("test.html", "html")]
    [TestCase("test.htm", "html")]
    [TestCase("test.css", "css")]
    [TestCase("test.scss", "scss")]
    [TestCase("test.json", "json")]
    [TestCase("test.xml", "xml")]
    [TestCase("test.yaml", "yaml")]
    [TestCase("test.yml", "yaml")]
    [TestCase("test.sh", "bash")]
    [TestCase("test.bash", "bash")]
    [TestCase("test.ps1", "powershell")]
    [TestCase("test.md", "markdown")]
    // Mixed case extensions
    [TestCase("TEST.CS", "csharp")]
    [TestCase("Test.Js", "javascript")]
    [TestCase("test.PY", "python")]
    // Full path
    [TestCase("/src/services/MyService.cs", "csharp")]
    public void GetLanguageFromFilePath_KnownExtensions_ReturnsCorrectLanguage(string filePath, string expectedLanguage)
    {
        var result = SecurityAnalyzer.GetLanguageFromFilePath(filePath);

        Assert.That(result, Is.EqualTo(expectedLanguage));
    }

    [TestCase("test.unknown")]
    [TestCase("test.xyz")]
    [TestCase("test")]
    [TestCase("")]
    public void GetLanguageFromFilePath_UnknownExtensions_ReturnsText(string filePath)
    {
        var result = SecurityAnalyzer.GetLanguageFromFilePath(filePath);

        Assert.That(result, Is.EqualTo("text"));
    }

    #endregion

    #region BuildSecurityPrompt Tests

    [Test]
    public void BuildSecurityPrompt_IncludeRulesInPromptTrue_IncludesRulesInPrompt()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test system prompt",
            IncludeRulesInPrompt = true,
            Rules =
            [
                new SecurityRule { Category = "SQL Injection", Description = "SQL Injection vulnerabilities", Enabled = true },
                new SecurityRule { Category = "XSS", Description = "Cross-site scripting", Enabled = true }
            ],
            OutputFormat = "Return JSON"
        };

        var result = _analyzer.BuildSecurityPrompt("test.cs", "var x = 1;");

        Assert.That(result, Does.Contain("Focus on:"));
        Assert.That(result, Does.Contain("- SQL Injection vulnerabilities"));
        Assert.That(result, Does.Contain("- Cross-site scripting"));
    }

    [Test]
    public void BuildSecurityPrompt_IncludeRulesInPromptFalse_ExcludesRulesFromPrompt()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test system prompt",
            IncludeRulesInPrompt = false,
            Rules =
            [
                new SecurityRule { Category = "SQL Injection", Description = "SQL Injection vulnerabilities", Enabled = true }
            ],
            OutputFormat = "Return JSON"
        };

        var result = _analyzer.BuildSecurityPrompt("test.cs", "var x = 1;");

        Assert.That(result, Does.Not.Contain("Focus on:"));
        Assert.That(result, Does.Not.Contain("- SQL Injection vulnerabilities"));
    }

    [Test]
    public void BuildSecurityPrompt_OnlyIncludesEnabledRules()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test system prompt",
            IncludeRulesInPrompt = true,
            Rules =
            [
                new SecurityRule { Category = "SQL Injection", Description = "SQL Injection vulnerabilities", Enabled = true },
                new SecurityRule { Category = "XSS", Description = "Cross-site scripting", Enabled = false }
            ],
            OutputFormat = "Return JSON"
        };

        var result = _analyzer.BuildSecurityPrompt("test.cs", "var x = 1;");

        Assert.That(result, Does.Contain("- SQL Injection vulnerabilities"));
        Assert.That(result, Does.Not.Contain("- Cross-site scripting"));
    }

    [Test]
    public void BuildSecurityPrompt_IncludesSystemPrompt()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "You are a security expert",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return JSON"
        };

        var result = _analyzer.BuildSecurityPrompt("test.cs", "var x = 1;");

        Assert.That(result, Does.Contain("You are a security expert"));
    }

    [Test]
    public void BuildSecurityPrompt_IncludesOutputFormat()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return ONLY JSON array"
        };

        var result = _analyzer.BuildSecurityPrompt("test.cs", "var x = 1;");

        Assert.That(result, Does.Contain("Return ONLY JSON array"));
    }

    [Test]
    public void BuildSecurityPrompt_IncludesFilePathAndContent()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return JSON"
        };

        var result = _analyzer.BuildSecurityPrompt("MyService.cs", "public class MyService { }");

        Assert.That(result, Does.Contain("File: MyService.cs"));
        Assert.That(result, Does.Contain("public class MyService { }"));
    }

    [Test]
    public void BuildSecurityPrompt_UsesCorrectLanguageTag()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return JSON"
        };

        var result = _analyzer.BuildSecurityPrompt("script.py", "print('hello')");

        Assert.That(result, Does.Contain("```python"));
    }

    [Test]
    public void BuildSecurityPrompt_JavaScriptFile_UsesJavaScriptTag()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return JSON"
        };

        var result = _analyzer.BuildSecurityPrompt("app.js", "const x = 1;");

        Assert.That(result, Does.Contain("```javascript"));
    }

    #endregion

    #region CreateBatches Tests

    [Test]
    public void CreateBatches_SingleFile_ReturnsSingleBatch()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            BatchSize = 5,
            MaxBatchTokens = 100000
        };

        var filePaths = new[] { "file1.cs" };
        var fileContents = new Dictionary<string, string> { { "file1.cs", "code" } };

        var batches = _analyzer.CreateBatches(filePaths, fileContents);

        Assert.That(batches, Has.Count.EqualTo(1));
        Assert.That(batches[0], Has.Count.EqualTo(1));
        Assert.That(batches[0].ContainsKey("file1.cs"), Is.True);
    }

    [Test]
    public void CreateBatches_FilesWithinBatchSize_ReturnsSingleBatch()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            BatchSize = 5,
            MaxBatchTokens = 100000
        };

        var filePaths = new[] { "a.cs", "b.cs", "c.cs" };
        var fileContents = filePaths.ToDictionary(p => p, p => "code");

        var batches = _analyzer.CreateBatches(filePaths, fileContents);

        Assert.That(batches, Has.Count.EqualTo(1));
        Assert.That(batches[0], Has.Count.EqualTo(3));
    }

    [Test]
    public void CreateBatches_FilesExceedBatchSize_SplitsIntoBatches()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            BatchSize = 2,
            MaxBatchTokens = 1000000
        };

        var filePaths = new[] { "a.cs", "b.cs", "c.cs", "d.cs", "e.cs" };
        var fileContents = filePaths.ToDictionary(p => p, p => "code");

        var batches = _analyzer.CreateBatches(filePaths, fileContents);

        Assert.That(batches, Has.Count.EqualTo(3));
        Assert.That(batches[0], Has.Count.EqualTo(2));
        Assert.That(batches[1], Has.Count.EqualTo(2));
        Assert.That(batches[2], Has.Count.EqualTo(1));
    }

    [Test]
    public void CreateBatches_FilesExceedTokenLimit_SplitsIntoBatches()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            BatchSize = 100,
            MaxBatchTokens = 100
        };

        var filePaths = new[] { "a.cs", "b.cs" };
        var fileContents = new Dictionary<string, string>
        {
            { "a.cs", new string('x', 300) },
            { "b.cs", new string('y', 300) }
        };

        var batches = _analyzer.CreateBatches(filePaths, fileContents);

        Assert.That(batches, Has.Count.EqualTo(2));
    }

    [Test]
    public void CreateBatches_MissingFileContent_SkipsFile()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            BatchSize = 5,
            MaxBatchTokens = 100000
        };

        var filePaths = new[] { "a.cs", "b.cs", "c.cs" };
        var fileContents = new Dictionary<string, string>
        {
            { "a.cs", "code" },
            { "c.cs", "code" }
        };

        var batches = _analyzer.CreateBatches(filePaths, fileContents);

        Assert.That(batches, Has.Count.EqualTo(1));
        Assert.That(batches[0], Has.Count.EqualTo(2));
        Assert.That(batches[0].ContainsKey("b.cs"), Is.False);
    }

    [Test]
    public void CreateBatches_EmptyFilePaths_ReturnsEmptyList()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            BatchSize = 5,
            MaxBatchTokens = 100000
        };

        var batches = _analyzer.CreateBatches([], new Dictionary<string, string>());

        Assert.That(batches, Is.Empty);
    }

    #endregion

    #region BuildBatchedSecurityPrompt Tests

    [Test]
    public void BuildBatchedSecurityPrompt_IncludesAllFiles()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test prompt",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return JSON"
        };

        var files = new Dictionary<string, string>
        {
            { "file1.cs", "public class A { }" },
            { "file2.py", "def foo(): pass" }
        };

        var result = _analyzer.BuildBatchedSecurityPrompt(files);

        Assert.That(result, Does.Contain("File: file1.cs"));
        Assert.That(result, Does.Contain("File: file2.py"));
        Assert.That(result, Does.Contain("public class A { }"));
        Assert.That(result, Does.Contain("def foo(): pass"));
    }

    [Test]
    public void BuildBatchedSecurityPrompt_UsesCorrectLanguageTags()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return JSON"
        };

        var files = new Dictionary<string, string>
        {
            { "app.cs", "code" },
            { "script.py", "code" }
        };

        var result = _analyzer.BuildBatchedSecurityPrompt(files);

        Assert.That(result, Does.Contain("```csharp"));
        Assert.That(result, Does.Contain("```python"));
    }

    [Test]
    public void BuildBatchedSecurityPrompt_IncludesSystemPrompt()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "You are a security expert",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return JSON"
        };

        var files = new Dictionary<string, string> { { "test.cs", "code" } };

        var result = _analyzer.BuildBatchedSecurityPrompt(files);

        Assert.That(result, Does.Contain("You are a security expert"));
    }

    [Test]
    public void BuildBatchedSecurityPrompt_IncludesEnabledRulesWhenConfigured()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test",
            IncludeRulesInPrompt = true,
            Rules =
            [
                new SecurityRule { Category = "SQL Injection", Description = "SQL vulnerabilities", Enabled = true },
                new SecurityRule { Category = "XSS", Description = "Cross-site scripting", Enabled = false }
            ],
            OutputFormat = "Return JSON"
        };

        var files = new Dictionary<string, string> { { "test.cs", "code" } };

        var result = _analyzer.BuildBatchedSecurityPrompt(files);

        Assert.That(result, Does.Contain("Focus on:"));
        Assert.That(result, Does.Contain("- SQL vulnerabilities"));
        Assert.That(result, Does.Not.Contain("- Cross-site scripting"));
    }

    [Test]
    public void BuildBatchedSecurityPrompt_IncludesOutputFormat()
    {
        _analyzer._rulesConfig = new SecurityRulesConfig
        {
            SystemPrompt = "Test",
            IncludeRulesInPrompt = false,
            Rules = [],
            OutputFormat = "Return ONLY JSON array with filePath"
        };

        var files = new Dictionary<string, string> { { "test.cs", "code" } };

        var result = _analyzer.BuildBatchedSecurityPrompt(files);

        Assert.That(result, Does.Contain("Return ONLY JSON array with filePath"));
    }

    #endregion

    #region ParseBatchedCopilotResponse Tests

    [Test]
    public void ParseBatchedCopilotResponse_ValidResponse_ParsesAllFindings()
    {
        var validPaths = new HashSet<string> { "file1.cs", "file2.cs" };
        var response = """
            [
                {"filePath": "file1.cs", "ruleId": "SQL-001", "title": "SQL Injection", "description": "desc", "severity": "High", "lineNumber": 10, "remediation": "fix"},
                {"filePath": "file2.cs", "ruleId": "XSS-001", "title": "XSS", "description": "desc", "severity": "Medium", "lineNumber": 5, "remediation": "fix"}
            ]
            """;

        var findings = _analyzer.ParseBatchedCopilotResponse(response, validPaths);

        Assert.That(findings, Has.Count.EqualTo(2));
        Assert.That(findings[0].FilePath, Is.EqualTo("file1.cs"));
        Assert.That(findings[0].Title, Is.EqualTo("SQL Injection"));
        Assert.That(findings[1].FilePath, Is.EqualTo("file2.cs"));
        Assert.That(findings[1].Title, Is.EqualTo("XSS"));
    }

    [Test]
    public void ParseBatchedCopilotResponse_MissingFilePath_SkipsFinding()
    {
        var validPaths = new HashSet<string> { "file1.cs" };
        var response = """
            [
                {"filePath": "file1.cs", "ruleId": "SQL-001", "title": "Valid", "description": "desc", "severity": "High", "remediation": "fix"},
                {"ruleId": "XSS-001", "title": "No FilePath", "description": "desc", "severity": "Medium", "remediation": "fix"}
            ]
            """;

        var findings = _analyzer.ParseBatchedCopilotResponse(response, validPaths);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Title, Is.EqualTo("Valid"));
    }

    [Test]
    public void ParseBatchedCopilotResponse_UnknownFilePath_SkipsFinding()
    {
        var validPaths = new HashSet<string> { "file1.cs" };
        var response = """
            [
                {"filePath": "file1.cs", "ruleId": "SQL-001", "title": "Valid", "description": "desc", "severity": "High", "remediation": "fix"},
                {"filePath": "unknown.cs", "ruleId": "XSS-001", "title": "Unknown File", "description": "desc", "severity": "Medium", "remediation": "fix"}
            ]
            """;

        var findings = _analyzer.ParseBatchedCopilotResponse(response, validPaths);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Title, Is.EqualTo("Valid"));
    }

    [Test]
    public void ParseBatchedCopilotResponse_EmptyArray_ReturnsEmptyList()
    {
        var validPaths = new HashSet<string> { "file1.cs" };
        var response = "[]";

        var findings = _analyzer.ParseBatchedCopilotResponse(response, validPaths);

        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void ParseBatchedCopilotResponse_NoJsonArray_ReturnsEmptyList()
    {
        var validPaths = new HashSet<string> { "file1.cs" };
        var response = "No issues found in the code.";

        var findings = _analyzer.ParseBatchedCopilotResponse(response, validPaths);

        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void ParseBatchedCopilotResponse_JsonWithSurroundingText_ExtractsArray()
    {
        var validPaths = new HashSet<string> { "file1.cs" };
        var response = """
            Here are the findings:
            [{"filePath": "file1.cs", "ruleId": "SQL-001", "title": "Issue", "description": "desc", "severity": "High", "remediation": "fix"}]
            End of analysis.
            """;

        var findings = _analyzer.ParseBatchedCopilotResponse(response, validPaths);

        Assert.That(findings, Has.Count.EqualTo(1));
    }

    [Test]
    public void ParseBatchedCopilotResponse_ParsesSeverityCorrectly()
    {
        var validPaths = new HashSet<string> { "file1.cs" };
        var response = """
            [{"filePath": "file1.cs", "ruleId": "TEST", "title": "Test", "description": "desc", "severity": "Critical", "remediation": "fix"}]
            """;

        var findings = _analyzer.ParseBatchedCopilotResponse(response, validPaths);

        Assert.That(findings[0].SeverityLevel, Is.EqualTo(Severity.Critical));
    }

    #endregion

    #region FindMatchingFilePath Tests

    [Test]
    public void FindMatchingFilePath_ExactMatch_ReturnsPath()
    {
        var validPaths = new HashSet<string> { "src/file.cs", "src/other.cs" };

        var result = SecurityAnalyzer.FindMatchingFilePath("src/file.cs", validPaths);

        Assert.That(result, Is.EqualTo("src/file.cs"));
    }

    [Test]
    public void FindMatchingFilePath_CaseInsensitiveMatch_ReturnsOriginalPath()
    {
        var validPaths = new HashSet<string> { "src/File.cs" };

        var result = SecurityAnalyzer.FindMatchingFilePath("src/file.cs", validPaths);

        Assert.That(result, Is.EqualTo("src/File.cs"));
    }

    [Test]
    public void FindMatchingFilePath_BackslashToForwardSlash_Matches()
    {
        var validPaths = new HashSet<string> { "src/folder/file.cs" };

        var result = SecurityAnalyzer.FindMatchingFilePath("src\\folder\\file.cs", validPaths);

        Assert.That(result, Is.EqualTo("src/folder/file.cs"));
    }

    [Test]
    public void FindMatchingFilePath_PartialPathSuffix_Matches()
    {
        var validPaths = new HashSet<string> { "c:/project/src/services/MyService.cs" };

        var result = SecurityAnalyzer.FindMatchingFilePath("src/services/MyService.cs", validPaths);

        Assert.That(result, Is.EqualTo("c:/project/src/services/MyService.cs"));
    }

    [Test]
    public void FindMatchingFilePath_NoMatch_ReturnsNull()
    {
        var validPaths = new HashSet<string> { "src/file.cs" };

        var result = SecurityAnalyzer.FindMatchingFilePath("other/file.cs", validPaths);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchingFilePath_EmptyValidPaths_ReturnsNull()
    {
        var validPaths = new HashSet<string>();

        var result = SecurityAnalyzer.FindMatchingFilePath("file.cs", validPaths);

        Assert.That(result, Is.Null);
    }

    #endregion
}
