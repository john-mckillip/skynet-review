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
}
