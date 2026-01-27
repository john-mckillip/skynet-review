use crate::models::{AnalysisResult, SecurityFinding};
use colored::*;

pub fn display_results(results: &[AnalysisResult]) {
    for result in results {
        if !result.success {
            println!(
                "\n{} {} failed: {}",
                "✗".red().bold(),
                result.agent_type,
                result.error_message.as_deref().unwrap_or("Unknown error")
            );
            continue;
        }

        println!(
            "\n{} {} Analysis (completed in {})",
            "✓".green().bold(),
            result.agent_type,
            result.duration
        );

        if result.findings.is_empty() {
            println!("  {}", "No issues found!".green());
            continue;
        }

        println!("  Found {} issue(s):\n", result.findings.len());

        for (i, finding) in result.findings.iter().enumerate() {
            display_finding(i + 1, finding);
        }
    }
}

fn display_finding(number: usize, finding: &SecurityFinding) {
    let severity_display = match finding.severity_level {
        0 => "CRITICAL".red().bold(),
        1 => "HIGH".red(),
        2 => "MEDIUM".yellow(),
        3 => "LOW".blue(),
        _ => "INFO".white(),
    };

    println!(
        "  {}. {} [{}]",
        number,
        finding.title.bold(),
        severity_display
    );
    println!("     ID: {}", finding.id.dimmed()); // Changed from rule_id
    println!("     File: {}", finding.file_path);

    if let Some(line) = finding.line_number {
        println!("     Line: {line}");
    }

    println!("     {}", finding.description);

    if let Some(snippet) = &finding.code_snippet {
        println!("\n     Code:");
        println!("     {}", "─".repeat(60).dimmed());
        println!("     {}", snippet.dimmed());
        println!("     {}", "─".repeat(60).dimmed());
    }

    println!("\n     💡 Remediation:");
    println!("     {}", finding.remediation.green());
    println!();
}
