use anyhow::{Context, Result};
use std::path::PathBuf;
use std::process::Command;

/// Represents the type of git diff to perform
pub enum DiffTarget {
    /// Unstaged changes in the working tree
    WorkingTree,
    /// Staged changes only
    Staged,
    /// Changes between HEAD and a specific commit/branch
    Commit(String),
}

/// Result of a git diff operation
#[derive(Debug)]
pub struct GitDiffResult {
    pub changed_files: Vec<PathBuf>,
    pub repository_root: PathBuf,
    pub description: String,
}

/// Check if current directory is inside a git repository
pub fn is_git_repository() -> Result<bool> {
    let output = Command::new("git")
        .args(["rev-parse", "--is-inside-work-tree"])
        .output()
        .context("Failed to execute git command")?;

    Ok(output.status.success())
}

/// Get the root directory of the git repository
pub fn get_repository_root() -> Result<PathBuf> {
    let output = Command::new("git")
        .args(["rev-parse", "--show-toplevel"])
        .output()
        .context("Failed to execute git command.")?;

    if !output.status.success() {
        anyhow::bail!("Not a git repository.");
    }

    let path_str = String::from_utf8(output.stdout)
        .context("Failed to parse git output.")?
        .trim()
        .to_string();

    Ok(PathBuf::from(path_str))
}

/// Validate a git reference to prevent command injection
fn validate_git_ref(reference: &str) -> Result<()> {
    // Reject refs starting with '-' to prevent flag injection
    if reference.starts_with('-') {
        anyhow::bail!("Invalid git reference: cannot start with '-'");
    }

    // Allow only safe characters in git refs
    let valid_pattern = regex::Regex::new(r"^[a-zA-Z0-9_./@^~-]+$").expect("Invalid regex pattern");

    if !valid_pattern.is_match(reference) {
        anyhow::bail!("Invalid git reference: contains invalid characters");
    }

    Ok(())
}

/// Validate that a path is within the repository root
fn validate_path_within_repo(path: &std::path::Path, repo_root: &std::path::Path) -> bool {
    // Skip validation if file doesn't exist (will be filtered later)
    if !path.exists() {
        return true;
    }

    match (path.canonicalize(), repo_root.canonicalize()) {
        (Ok(canonical_path), Ok(canonical_root)) => canonical_path.starts_with(&canonical_root),
        _ => false, // If canonicalization fails, reject the path
    }
}

/// Get a list of changed files based on diff target
pub fn get_changed_files(target: &DiffTarget) -> Result<GitDiffResult> {
    let repo_root = get_repository_root()?;

    // Build the git diff command based on target
    let (args, description) = match target {
        DiffTarget::WorkingTree => (vec!["diff", "--name-only"], "unstaged changes".to_string()),
        DiffTarget::Staged => (
            vec!["diff", "--staged", "--name-only"],
            "staged changes".to_string(),
        ),
        DiffTarget::Commit(commit) => {
            // Validate commit reference to prevent command injection
            validate_git_ref(commit)?;
            (
                vec!["diff", "--name-only", commit.as_str(), "HEAD"],
                format!("changes since {commit}"),
            )
        }
    };

    // Run git diff
    let output = Command::new("git")
        .args(&args)
        .current_dir(&repo_root)
        .output()
        .context("Failed to execute git diff.")?;

    if !output.status.success() {
        // Don't expose raw git stderr - provide generic error
        anyhow::bail!("Git diff failed. Verify the reference exists.");
    }

    // Parse output into file paths with path traversal validation
    let files: Vec<PathBuf> = String::from_utf8(output.stdout)
        .context("Failed to parse git diff output.")?
        .lines()
        .filter(|line| !line.trim().is_empty())
        .map(|line| repo_root.join(line))
        .filter(|path| validate_path_within_repo(path, &repo_root))
        .collect();

    Ok(GitDiffResult {
        changed_files: files,
        repository_root: repo_root,
        description,
    })
}

/// Filter files to only include analyzable source files
/// If `extensions` is None, uses default set of source file extensions
pub fn filter_analyzable_files(files: Vec<PathBuf>, extensions: Option<&[&str]>) -> Vec<PathBuf> {
    let default_extensions = [
        "cs", "rs", "py", "js", "ts", "jsx", "tsx", "java", "go", "rb", "php", "c", "cpp", "h",
        "hpp",
    ];

    let extensions = extensions.unwrap_or(&default_extensions);

    files
        .into_iter()
        .filter(|path| {
            path.extension()
                .and_then(|ext| ext.to_str())
                .map(|ext| extensions.iter().any(|e| e.eq_ignore_ascii_case(ext)))
                .unwrap_or(false)
        })
        .filter(|path| path.exists())
        .collect()
}
