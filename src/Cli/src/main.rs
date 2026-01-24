use clap::{Parser, Subcommand};
use colored::*;
use std::path::PathBuf;

mod api_client;
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
    #[arg(long, default_value = "http://localhost:5000", env = "SKYNET_GATEWAY_URL")]
    gateway_url: String,
}

#[derive(Subcommand)]
enum Commands {
    /// Analyze source code files for security vulnerabilities
    Analyze {
        /// Files to analyze
        files: Vec<PathBuf>,
    },
    
    /// Check if services are healthy
    Health,
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let cli = Cli::parse();
    let client = ApiClient::new(&cli.gateway_url);

    match cli.command {
        Commands::Analyze { files } => {
            if files.is_empty() {
                eprintln!("{}", "Error: No files specified".red().bold());
                std::process::exit(1);
            }

            println!("{}", "🔍 Analyzing files...".cyan().bold());
            
            for file in &files {
                println!("  • {}", file.display());
            }
            println!();

            match analyze_files(&client, files).await {
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
                Ok(_) => {
                    println!("{}", "✓ All services are healthy".green().bold());
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

    // Call API
    let results = client.analyze(request).await?;

    // Display results
    output::display_results(&results);

    Ok(())
}

async fn check_health(client: &ApiClient) -> anyhow::Result<()> {
    client.health_check().await
}