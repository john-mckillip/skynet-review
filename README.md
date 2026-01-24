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
  - File upload workflow for larger codebases
  - Multi-file batch analysis support
- **Docker-First Design**: Fully containerized for consistent deployment and easy local setup
- **Developer-Friendly**: Designed for integration into local development workflows and CI/CD pipelines

## 💡 Use Cases

- **Pre-Commit Security Checks**: Scan your code before committing to catch vulnerabilities early
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

- [ ] Add Performance Analysis Agent
- [ ] Add Code Standards/Style Agent
- [ ] Implement caching for faster analysis
- [ ] Add support for more languages (Python, JavaScript, Java)
- [ ] Web UI for analysis results
- [ ] CI/CD integration (GitHub Actions, Azure DevOps)
- [ ] Historical analysis tracking
- [ ] Custom security rules configuration
- [ ] Report generation (PDF, HTML)
- [ ] IDE extensions (VS Code, Visual Studio)

---

Built with 🔥 by John McKillip | Ice Nine Media