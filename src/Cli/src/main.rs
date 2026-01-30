use clap::{Parser, Subcommand};
use colored::*;
use std::path::PathBuf;

mod api_client;
mod git;
mod models;
mod output;

use api_client::ApiClient;

#[derive(Parser)]
#[command(name = "skynet-review")]
#[command(about = "AI-powered code security analysis", long_about = None)]
struct Cli {
    #[command(subcommand)]
    command: Commands,

    /// Gateway API URL
    #[arg(
        long,
        default_value = "http://localhost:5000",
        env = "SKYNET_GATEWAY_URL"
    )]
    gateway_url: String,
}

#[derive(Subcommand)]
enum Commands {
    /// Analyze source code files for security vulnerabilities
    Analyze {
        /// Files to analyze (mutually exclusive with --git-diff)
        #[arg(conflicts_with_all = ["git_diff", "staged", "commit"])]
        files: Vec<PathBuf>,

        /// Analyze changed files from git diff
        #[arg(long)]
        git_diff: bool,

        /// Analyze only staged changes (requires --git-diff)
        #[arg(long, requires = "git_diff")]
        staged: bool,

        /// Compare against a specific commit or branch (requires --git-diff)
        #[arg(long, requires = "git_diff", value_name = "REF")]
        commit: Option<String>,

        /// Only include these file extensions (comma-separated)
        #[arg(long, value_delimiter = ',')]
        include_ext: Option<Vec<String>>,
    },

    /// Check if services are healthy
    Health,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let cli = Cli::parse();
    let client = ApiClient::new(&cli.gateway_url)?;

    match cli.command {
        Commands::Analyze {
            files,
            git_diff,
            staged,
            commit,
            include_ext,
        } => {
            // Determine which files to analyze
            let files_to_analyze = if git_diff {
                // Git diff mode
                get_git_diff_files(staged, commit, &include_ext)?
            } else if files.is_empty() {
                eprintln!(
                    "{}",
                    "Error: No files specified. Use file paths or --git-diff"
                        .red()
                        .bold()
                );
                std::process::exit(1);
            } else {
                // Direct file mode - apply extension filter if provided
                apply_extension_filter(files, &include_ext)
            };

            if files_to_analyze.is_empty() {
                println!("{}", "No files to analyze.".yellow());
                return Ok(());
            }

            println!("{}", "Analyzing files...".cyan().bold());
            for file in &files_to_analyze {
                println!("  - {}", file.display());
            }
            println!();

            match analyze_files(&client, files_to_analyze).await {
                Ok(_) => Ok(()),
                Err(e) => {
                    eprintln!("{} {}", "Error:".red().bold(), e);
                    std::process::exit(1);
                }
            }
        }

        Commands::Health => {
            println!("{}", "🏥 Checking service health...".cyan().bold());
            match check_health(&client).await {
                Ok(health) => {
                    println!(
                        "{} {} ({})",
                        "✓".green().bold(),
                        health.service,
                        health.status
                    );
                    Ok(())
                }
                Err(e) => {
                    eprintln!("{} {}", "Error:".red().bold(), e);
                    std::process::exit(1);
                }
            }
        }
    }
}

fn get_git_diff_files(
    staged: bool,
    commit: Option<String>,
    include_ext: &Option<Vec<String>>,
) -> anyhow::Result<Vec<PathBuf>> {
    // Check we're in a git repo
    if !git::is_git_repository()? {
        anyhow::bail!("Not inside a git repository. Use file paths instead of --git-diff");
    }

    // Determine diff target
    let target = if staged {
        git::DiffTarget::Staged
    } else if let Some(ref commit_ref) = commit {
        git::DiffTarget::Commit(commit_ref.clone())
    } else {
        git::DiffTarget::WorkingTree
    };

    // Get changed files
    let result = git::get_changed_files(&target)?;

    println!(
        "{} {} in {} ({} files)",
        "Git:".cyan().bold(),
        result.description,
        result.repository_root.display(),
        result.changed_files.len()
    );

    // Convert include_ext to the format filter_analyzable_files expects
    let ext_refs: Option<Vec<&str>> = include_ext
        .as_ref()
        .map(|exts| exts.iter().map(|s| s.as_str()).collect());

    let filtered = git::filter_analyzable_files(result.changed_files, ext_refs.as_deref());

    Ok(filtered)
}

fn apply_extension_filter(files: Vec<PathBuf>, include_ext: &Option<Vec<String>>) -> Vec<PathBuf> {
    match include_ext {
        Some(exts) => {
            let ext_refs: Vec<&str> = exts.iter().map(|s| s.as_str()).collect();
            git::filter_analyzable_files(files, Some(&ext_refs))
        }
        None => files,
    }
}

async fn analyze_files(client: &ApiClient, files: Vec<PathBuf>) -> anyhow::Result<()> {
    // Read file contents
    let mut file_contents = std::collections::HashMap::new();

    for file_path in &files {
        let content = std::fs::read_to_string(file_path)
            .map_err(|e| anyhow::anyhow!("Failed to read {}: {}", file_path.display(), e))?;

        let file_name = file_path
            .file_name()
            .and_then(|n| n.to_str())
            .unwrap_or("unknown")
            .to_string();

        file_contents.insert(file_name, content);
    }

    // Create analysis request
    let file_paths: Vec<String> = file_contents.keys().cloned().collect();
    let request = models::AnalysisRequest {
        file_paths,
        file_contents,
        repository_context: None,
    };

    // Reset counter and stream findings
    output::reset_finding_counter();
    println!("{}", "Security Analysis (streaming)".green().bold());
    println!();

    let mut finding_count = 0;
    client
        .analyze_stream(request, |finding| {
            finding_count += 1;
            output::display_finding_streaming(&finding);
        })
        .await?;

    if finding_count == 0 {
        println!("  {}", "No issues found!".green());
    } else {
        println!(
            "\n{} Found {} issue(s)",
            "Summary:".cyan().bold(),
            finding_count
        );
    }

    Ok(())
}

async fn check_health(client: &ApiClient) -> anyhow::Result<models::HealthResponse> {
    client.health_check().await
}
