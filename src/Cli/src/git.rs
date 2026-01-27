use std::path::PathBuf;
use std::process::Command;
use anyhow::{Context, Result};

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
    pub description: String
}

/// Check if current directory is inside a git respository
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