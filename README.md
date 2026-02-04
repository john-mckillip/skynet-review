# Skynet Review 🤖🔍

A microservices-based code quality and security analysis platform powered by AI. Skynet Review uses GitHub Copilot's AI capabilities to automatically detect security vulnerabilities in your code through specialized analysis agents.

## 🌟 Features

- **AI-Powered Security Analysis**: Leverages GitHub Copilot SDK for intelligent vulnerability detection
- **Multi-Agent Architecture**: Extensible system supporting multiple specialized analysis agents
  - Security Agent: Detects vulnerabilities, hardcoded secrets, and security anti-patterns
  - Ready for expansion: Performance analysis, code standards, best practices, and more
- **Local Development Tool**: Run comprehensive security scans directly from your terminal
- **Beautiful CLI**: Rust-based command-line interface with colored output and intuitive commands
- **Microservices Architecture**: Scalable, distributed system with independent, containerized services
- **Flexible Analysis Workflows**:
  - Direct file content analysis for quick scans
  - **Git diff analysis** - analyze only changed files in your repository
  - File upload workflow for larger codebases
  - Multi-file batch analysis support
- **Docker-First Design**: Fully containerized for consistent deployment and easy local setup
- **Security Hardened CLI**: HTTPS enforcement, TLS 1.2+, command injection prevention, and optional API key authentication
- **Developer-Friendly**: Designed for integration into local development workflows and CI/CD pipelines

## 💡 Use Cases

- **Pre-Commit Security Checks**: Scan staged changes before committing with `--git-diff --staged`
- **Branch Security Review**: Analyze all changes since branching from main with `--git-diff --commit main`
- **Local Development**: Analyze code as you write without leaving your terminal
- **Code Review Assistance**: Get AI-powered security insights during pull request reviews
- **Security Audits**: Batch analyze entire codebases for comprehensive security assessment
- **Learning Tool**: Understand security vulnerabilities with detailed remediation guidance
- **CI/CD Integration**: Automate security scanning in your deployment pipelines (coming soon)

## 🏗️ Architecture
```
┌──────────────┐
│   Rust CLI   │
└──────┬───────┘
       │
       ▼
┌──────────────┐      ┌─────────────────┐      ┌──────────────┐
│   Gateway    │─────▶│ Security Agent  │      │ File Service │
│  (Port 5000) │      │   (Port 5001)   │      │ (Port 5002)  │
└──────────────┘      └─────────────────┘      └──────────────┘
                              │
                              ▼
                      ┌─────────────────┐
                      │ GitHub Copilot  │
                      │      SDK        │
                      └─────────────────┘
```

### Services

- **Gateway**: Orchestrates requests between services and handles routing
- **Security Agent**: Performs AI-powered security analysis using GitHub Copilot
- **File Service**: Manages file uploads and temporary storage
- **Rust CLI**: User-friendly command-line interface

## 🚀 Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for running services)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for local development)
- [Node.js](https://nodejs.org/) (for installing Copilot CLI)
- [GitHub Copilot CLI](https://github.com/features/copilot/cli) - Install with: `npm install -g @github/copilot`
- [Rust](https://rustup.rs/) (for CLI development)
- [GitHub Copilot subscription](https://github.com/features/copilot) (required for AI analysis)
- [GitHub CLI](https://cli.github.com/) (for authentication) - Install with: `gh auth login`

**Note**: When running with Docker, the Copilot CLI is automatically installed in the Security Agent container. Local installation is only needed if you want to run the services outside of Docker.

### Quick Start with Docker

1. **Clone the repository**
```bash
   git clone https://github.com/yourusername/skynet-review.git
   cd skynet-review
```

2. **Set up your GitHub token**
   
   Get your GitHub token:
```bash
   gh auth token
```

   Create a `.env` file in the root directory:
```bash
   echo "GITHUB_TOKEN=your_token_here" > .env
```

3. **Start all services**
```bash
   docker compose up
```

   Services will be available at:
   - Gateway: http://localhost:5000
   - Security Agent: http://localhost:5001
   - File Service: http://localhost:5002

4. **Build the CLI**
```bash
   cd src/Cli
   cargo build --release
```

5. **Run your first analysis**
```bash
   # Create a test file
   echo 'public class Test {
       string apiKey = "hardcoded-secret";
       var query = "SELECT * FROM Users WHERE Id = " + userId;
   }' > test.cs

   # Analyze it
   cargo run --release -- analyze test.cs
```
## 📦 Installing the CLI

After building the CLI, you have several options to make it easily accessible:

### Option 1: Install with Cargo (Recommended)
```bash
cd src/Cli
cargo install --path .
```

This installs `skynet-review` to your Cargo bin directory (usually `~/.cargo/bin`), which should already be in your PATH. You can now run the CLI from anywhere:
```bash
skynet-review health
skynet-review analyze MyCode.cs
```

### Option 2: Manual Installation

**Windows (Git Bash/PowerShell)**
```bash
# Build the release binary
cd src/Cli
cargo build --release

# Copy to a directory in your PATH
mkdir -p ~/bin
cp target/release/skynet-review.exe ~/bin/

# Add to PATH (add this line to ~/.bashrc or ~/.bash_profile)
export PATH="$HOME/bin:$PATH"

# Reload your shell
source ~/.bashrc
```

**macOS/Linux**
```bash
# Build the release binary
cd src/Cli
cargo build --release

# Copy to /usr/local/bin (requires sudo)
sudo cp target/release/skynet-review /usr/local/bin/

# Or to ~/bin without sudo
mkdir -p ~/bin
cp target/release/skynet-review ~/bin/
export PATH="$HOME/bin:$PATH"  # Add to ~/.bashrc or ~/.zshrc
```

### Option 3: Run Directly (No Installation)
```bash
cd src/Cli
cargo run --release -- health
cargo run --release -- analyze test.cs
```

### Verify Installation
```bash
skynet-review --help
skynet-review health
```
## 📖 Usage

### CLI Commands

**Health Check**
```bash
skynet-review health
```

**Analyze Files**
```bash
# Single file
skynet-review analyze MyCode.cs

# Multiple files
skynet-review analyze File1.cs File2.cs File3.cs

# All C# files in directory
skynet-review analyze *.cs
```

**Analyze Git Changes**
```bash
# Analyze all unstaged changes in working tree
skynet-review analyze --git-diff

# Analyze only staged changes (great for pre-commit checks)
skynet-review analyze --git-diff --staged

# Analyze changes since branching from main
skynet-review analyze --git-diff --commit main

# Analyze changes since a specific commit
skynet-review analyze --git-diff --commit abc1234

# Filter to specific file extensions
skynet-review analyze --git-diff --include-ext rs,cs

# Combine options
skynet-review analyze --git-diff --commit main --include-ext rs
```

**Custom Gateway URL**
```bash
# Via command line
skynet-review --gateway-url http://custom-host:5000 analyze MyCode.cs

# Via environment variable
export SKYNET_GATEWAY_URL=http://custom-host:5000
skynet-review analyze MyCode.cs
```

### API Endpoints

**Gateway (Port 5000)**

- `POST /api/analyze` - Analyze code with file contents in request body
```bash
  curl -X POST http://localhost:5000/api/analyze \
    -H "Content-Type: application/json" \
    -d '{
      "filePaths": ["test.cs"],
      "fileContents": {
        "test.cs": "public class Test { ... }"
      }
    }'
```

- `POST /api/analyze/upload` - Upload and analyze files
```bash
  curl -X POST http://localhost:5000/api/analyze/upload \
    -F "file=@MyCode.cs"
```

- `GET /api/health` - Health check
```bash
  curl http://localhost:5000/api/health
```

**Security Agent (Port 5001)**

- `POST /api/security/analyze` - Direct security analysis
- `GET /api/health` - Health check

**File Service (Port 5002)**

- `POST /api/files/upload` - Upload files
- `GET /api/files/{fileId}` - Retrieve file content
- `DELETE /api/files/{fileId}` - Delete file
- `GET /api/health` - Health check

## ⚙️ Configuration

### Security Rules Configuration

The Security Agent is highly configurable through the `security-rules.yml` file located in `src/SecurityAgent/`.

#### Configuration File Structure
```yaml
# AI Model configuration
# Available models: gpt-4o, gpt-5, claude-sonnet-4.5, claude-opus-4.5
model: "gpt-4o"

# System prompt sent to the AI
systemPrompt: "You are a security analysis expert. Analyze the following code for security vulnerabilities."

# Security rule categories
rules:
  - category: "SQL Injection"
    description: "SQL Injection vulnerabilities"
    ruleIdPrefixes: ["SQL"]
    keywords: ["sql", "injection", "query"]
    enabled: true
  
  - category: "Hardcoded Secrets"
    description: "Hardcoded secrets or credentials"
    ruleIdPrefixes: ["SEC", "SECRET"]
    keywords: ["secret", "password", "key", "token", "credential"]
    enabled: true

# Output format instructions for the AI
outputFormat: |
  Respond with ONLY a JSON array of findings...
```

#### Switching AI Models

Change the `model` field to use different AI models:
```yaml
# Fast and efficient (default)
model: "gpt-4o"

# Latest and most capable
model: "gpt-5"

# Anthropic Claude models
model: "claude-sonnet-4.5"
model: "claude-opus-4.5"

# Google Gemini models
model: "gemini-2.0-flash-exp"
```

Different models may produce different results - experiment to find which works best for your needs!

#### Enabling/Disabling Rules

Turn specific rule categories on or off:
```yaml
rules:
  - category: "SQL Injection"
    description: "SQL Injection vulnerabilities"
    ruleIdPrefixes: ["SQL"]
    keywords: ["sql", "injection", "query"]
    enabled: false  # <-- Disable SQL injection checks
```

**Use cases for disabling rules:**
- Reduce noise from false positives
- Focus on specific security concerns
- Speed up analysis by checking fewer categories
- Customize for different projects or teams

#### Customizing the System Prompt

Adjust the AI's behavior by modifying the system prompt:
```yaml
# More strict analysis
systemPrompt: "You are an expert security auditor with 20 years of experience. Analyze the following code with extreme scrutiny for security vulnerabilities. Be thorough and flag even minor potential issues."

# Focus on critical issues only
systemPrompt: "You are a security expert. Focus only on critical and high-severity security vulnerabilities that could lead to data breaches or system compromise."

# Industry-specific
systemPrompt: "You are a healthcare security expert. Analyze the following code for HIPAA compliance issues and healthcare-specific security vulnerabilities."
```

#### Adding Custom Rules

Add your own security rule categories:
```yaml
rules:
  - category: "PII Exposure"
    description: "Personally Identifiable Information exposure"
    ruleIdPrefixes: ["PII", "PRIVACY"]
    keywords: ["personal", "pii", "ssn", "email", "phone"]
    enabled: true
  
  - category: "Business Logic Flaws"
    description: "Business logic and workflow vulnerabilities"
    ruleIdPrefixes: ["LOGIC", "BUSINESS"]
    keywords: ["business logic", "workflow", "state"]
    enabled: true
```

**Rule Matching:**
- `ruleIdPrefixes`: Matches findings by their ID (e.g., "SQL-001" matches prefix "SQL")
- `keywords`: Matches findings by title or description content
- A finding must match *either* a prefix *or* a keyword to pass the filter

#### Configuration in Docker

When running with Docker, mount a custom config file:
```yaml
# docker-compose.yml
services:
  security-agent:
    volumes:
      - ./custom-security-rules.yml:/app/security-rules.yml
```

Or modify the default `src/SecurityAgent/security-rules.yml` before building:
```bash
# Edit the config
nano src/SecurityAgent/security-rules.yml

# Rebuild with new config
docker compose up --build
```

#### Configuration Best Practices

1. **Start with defaults** - The default configuration is balanced for general use
2. **Disable incrementally** - Turn off one rule at a time and test results
3. **Document changes** - Comment your config file explaining why rules are disabled
4. **Version control** - Track config changes alongside code changes
5. **Project-specific configs** - Consider different configs for different projects
6. **Test model changes** - Different AI models behave differently; validate results
7. **Review periodically** - Re-enable rules occasionally to catch new issues

#### Example: Security-Focused Configuration
```yaml
model: "claude-opus-4.5"  # Most capable model

systemPrompt: "You are a senior application security engineer conducting a thorough security audit. Identify all potential vulnerabilities with detailed explanations."

rules:
  - category: "SQL Injection"
    enabled: true
  - category: "Hardcoded Secrets"
    enabled: true
  - category: "Authentication & Authorization"
    enabled: true
  - category: "Input Validation"
    enabled: true
  - category: "Insecure Cryptography"
    enabled: true
  - category: "CORS Misconfiguration"
    enabled: true
  - category: "Sensitive Data Exposure"
    enabled: true
```

#### Example: Fast Development Scan
```yaml
model: "gpt-4o"  # Fast model

systemPrompt: "You are a security expert. Focus only on critical security vulnerabilities."

rules:
  - category: "SQL Injection"
    enabled: true
  - category: "Hardcoded Secrets"
    enabled: true
  - category: "Authentication & Authorization"
    enabled: false  # Skip for speed
  - category: "Input Validation"
    enabled: false  # Skip for speed
  - category: "Insecure Cryptography"
    enabled: true
  - category: "CORS Misconfiguration"
    enabled: false  # Skip for speed
  - category: "Sensitive Data Exposure"
    enabled: true
```

#### Troubleshooting Configuration

**Config file not loading:**
```bash
# Check logs for config path
docker compose logs security-agent | grep "security rules"

# Verify file exists in container
docker compose exec security-agent ls -la security-rules.yml
```

**Rules not filtering correctly:**
- Check that `ruleIdPrefixes` match the actual IDs returned by the AI (check logs)
- Add more `keywords` for better matching
- Use broad keywords like "sql", "secret", "auth" for reliable matching

**AI not following instructions:**
- Some models ignore certain prompts; try a different model
- Be more explicit in the system prompt
- Use the filtering mechanism rather than relying solely on the prompt

## 🛠️ Development

### Running Services Locally (without Docker)

**Terminal 1 - File Service**
```bash
cd src/FileService
dotnet run
```

**Terminal 2 - Security Agent**
```bash
cd src/SecurityAgent
# Make sure you have GitHub Copilot CLI installed
npm install -g @github/copilot
dotnet run
```

**Terminal 3 - Gateway**
```bash
cd src/Gateway
dotnet run
```

### Project Structure
```
skynet-review/
├── src/
│   ├── Gateway/              # API Gateway service
│   │   ├── Services/
│   │   │   ├── AgentOrchestrator.cs
│   │   │   └── ServiceEndpoints.cs
│   │   ├── Program.cs
│   │   └── Dockerfile
│   │
│   ├── SecurityAgent/        # AI security analysis service
│   │   ├── Services/
│   │   │   ├── SecurityAnalyzer.cs
│   │   │   └── ISecurityAnalyzer.cs
│   │   ├── Program.cs
│   │   └── Dockerfile
│   │
│   ├── FileService/          # File management service
│   │   ├── Services/
│   │   │   ├── FileManager.cs
│   │   │   └── FileStorageOptions.cs
│   │   ├── Program.cs
│   │   └── Dockerfile
│   │
│   ├── Shared/               # Shared models library
│   │   └── Models/
│   │       ├── AnalysisRequest.cs
│   │       ├── AnalysisResult.cs
│   │       ├── SecurityFinding.cs
│   │       ├── Severity.cs
│   │       └── UploadResult.cs
│   │
│   └── Cli/                  # Rust command-line interface
│       ├── src/
│       │   ├── main.rs
│       │   ├── api_client.rs
│       │   ├── git.rs          # Git integration for diff analysis
│       │   ├── models.rs
│       │   └── output.rs
│       └── Cargo.toml
│
├── docker-compose.yml
├── .env
├── .gitignore
└── README.md
```

### Adding a New Agent

1. Create a new service project:
```bash
   cd src
   dotnet new webapi -minimal -n SkynetReview.PerformanceAgent
   dotnet add reference ../Shared/SkynetReview.Shared.csproj
```

2. Implement your agent logic following the Security Agent pattern

3. Add the service to `docker-compose.yml`

4. Update Gateway's `AgentOrchestrator.cs` to call your new agent

5. Add configuration to `ServiceEndpoints.cs`

## 🔒 Security Vulnerabilities Detected

Skynet Review currently detects:

- **SQL Injection**: Unsafe string concatenation in database queries
- **Hardcoded Secrets**: API keys, passwords, and tokens in source code
- **Missing Authentication/Authorization**: Endpoints without access control
- **Input Validation Issues**: Unvalidated user input
- **Insecure Cryptography**: Weak or deprecated cryptographic algorithms
- **CORS Misconfigurations**: Overly permissive cross-origin settings
- **Sensitive Data Exposure**: Logging or displaying sensitive information

## 🧪 Testing

**Unit Tests** (TODO)
```bash
dotnet test
```

**Integration Tests** (TODO)
```bash
docker compose -f docker-compose.test.yml up --abort-on-container-exit
```

**Manual API Testing**
```bash
# Health checks
curl http://localhost:5000/api/health
curl http://localhost:5001/api/health
curl http://localhost:5002/api/health

# Analysis test
curl -X POST http://localhost:5000/api/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "filePaths": ["test.cs"],
    "fileContents": {
      "test.cs": "public class Test { string apiKey = \"secret\"; }"
    }
  }'
```

## 🐛 Troubleshooting

### Security Agent fails to start

**Problem**: Container exits with authentication errors

**Solution**: Ensure your GitHub token is properly set in `.env`:
```bash
# Get your token
gh auth token

# Add to .env
echo "GITHUB_TOKEN=your_token_here" > .env

# Restart services
docker compose down
docker compose up
```

### Analysis takes a long time

**Problem**: First analysis request takes 30-60 seconds

**Solution**: This is expected. The Copilot CLI initializes on first use. Subsequent requests are much faster (~5-10 seconds).

### CLI can't connect to services

**Problem**: `Error: error sending request`

**Solution**: Ensure Docker services are running:
```bash
docker compose ps

# If not running:
docker compose up -d
```

### Port already in use

**Problem**: `Error: bind: address already in use`

**Solution**: Change ports in `docker-compose.yml` or stop conflicting services:
```bash
# Check what's using port 5000
lsof -i :5000  # macOS/Linux
netstat -ano | findstr :5000  # Windows

# Use different ports
# Edit docker-compose.yml and change port mappings
```

## 📝 Configuration

### Environment Variables

- `GITHUB_TOKEN`: GitHub personal access token with Copilot access
- `SKYNET_GATEWAY_URL`: Gateway API URL (default: http://localhost:5000)
- `SKYNET_API_KEY`: Optional API key for authenticated gateway access
- `ASPNETCORE_ENVIRONMENT`: ASP.NET Core environment (Development/Docker/Production)

### Service Configuration Files

- `src/Gateway/appsettings.json` - Gateway configuration
- `src/Gateway/appsettings.Docker.json` - Docker-specific Gateway config
- `src/SecurityAgent/appsettings.json` - Security Agent configuration
- `src/FileService/appsettings.json` - File Service configuration

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- [GitHub Copilot](https://github.com/features/copilot) for AI-powered analysis
- [.NET](https://dotnet.microsoft.com/) for the microservices framework
- [Rust](https://www.rust-lang.org/) for the CLI implementation
- [Docker](https://www.docker.com/) for containerization

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/john-mckillip/skynet-review/issues)
- **Discussions**: [GitHub Discussions](https://github.com/john-mckillip/skynet-review/discussions)

## 🗺️ Roadmap

- [x] Git diff analysis for changed files
- [ ] Add Performance Analysis Agent
- [ ] Add Code Standards/Style Agent
- [ ] Implement caching for faster analysis
- [x] Add support for more languages (Python, JavaScript, Java)
- [ ] Web UI for analysis results
- [x] CI/CD integration (GitHub Actions, Azure DevOps)
- [ ] Historical analysis tracking
- [ ] Custom security rules configuration
- [ ] Report generation (PDF, HTML)
- [ ] IDE extensions (VS Code, Visual Studio)

## 📋 Changelog

### v1.2.0 (2026-02-03)

**CI/CD Pipeline, Security Agent & CLI Updates**

- ⚡ **Streaming Security Analysis**
  - Added a new /api/analyze/stream endpoint in src/Gateway/Program.cs that returns findings incrementally using Server-Sent Events (SSE), enabling clients to display security issues as soon as they're detected. The endpoint handles streaming, error events, and completion summaries.
  - Implemented the analyze_stream method in src/Cli/src/api_client.rs to consume the SSE stream, parse findings, and invoke a callback for each result.
  - Updated CLI logic in src/Cli/src/main.rs to support streaming mode (default), with a new --no-stream flag to revert to batch mode. Streaming findings are displayed live using a new output function.

- ⚡ **Developer Tooling and Build System**
  - Added VSCode launch configurations for debugging services and tests, and new build/test tasks for each component in .vscode/launch.json and .vscode/tasks.json.
  - Introduced a GitHub Actions workflow (.github/workflows/build-and-test.yml) to build all services and run/test SecurityAgent, including code coverage reporting and PR comments.

 - ⚡ **Miscellaneous Improvements**
  - Extended HTTP client timeouts in both Rust and C# to accommodate long-running analyses.
  - Updated dependencies in src/Cli/Cargo.toml to support logging, streaming and async features.
  - Registered the test project in the solution file for better IDE support.

### v1.1.0 (2026-01-27)

**Git Diff Analysis**

- 🔀 **Git integration** - Analyze only changed files in your repository
  - `--git-diff` flag to analyze uncommitted changes
  - `--staged` flag to analyze only staged changes (pre-commit workflow)
  - `--commit <REF>` flag to compare against a specific commit or branch
  - `--include-ext` flag to filter by file extensions
- 🔒 **Security hardening** in CLI
  - Command injection prevention in git reference validation
  - Path traversal protection for file operations
  - HTTPS enforcement for non-localhost connections
  - TLS 1.2 minimum version requirement
  - Optional API key authentication via `SKYNET_API_KEY`
- ⏱️ Extended request timeout to 120 seconds for AI analysis

### v1.0.0 (2026-01-25)

**Initial Release**

- ✨ Multi-agent microservices architecture
- 🤖 AI-powered security analysis using GitHub Copilot SDK
- ⚙️ **Configurable security rules via YAML**
  - Enable/disable specific security rule categories
  - Switch between AI models (GPT-4o, GPT-5, Claude Sonnet 4.5, Claude Opus 4.5, etc.)
  - Customize system prompts for different analysis styles
  - Add custom security rules with ID prefixes and keywords
  - Filter findings based on rule configuration
- 🔍 Security Agent detecting:
  - SQL injection vulnerabilities
  - Hardcoded secrets and API keys
  - Missing authentication/authorization
  - Input validation issues
  - Insecure cryptography
  - CORS misconfigurations
  - Sensitive data exposure
- 🚀 Gateway service for request orchestration
- 📁 File Service for upload and storage management
- 🦀 Rust CLI with beautiful colored output
- 🐳 Full Docker Compose setup for local development
- 📊 Multi-file batch analysis support
- 🔄 Two analysis workflows: direct content and file upload
- 📝 Detailed remediation guidance for each finding
- 🎯 Severity-based issue categorization (Critical, High, Medium, Low, Info)

---

![GIF](https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExcnBzaWcyOGFxbXd3OXo5M3QxOXllaXBzd2M5MHhqbDd0ZnV0eWVsOCZlcD12MV9naWZzX3NlYXJjaCZjdD1n/gFwZfXIqD0eNW/giphy.gif)

Built with 🔥 by John McKillip | Ice Nine Media
