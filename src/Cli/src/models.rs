use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Serialize)]
pub struct AnalysisRequest {
    #[serde(rename = "filePaths")]
    pub file_paths: Vec<String>,
    
    #[serde(rename = "fileContents")]
    pub file_contents: HashMap<String, String>,
    
    #[serde(rename = "repositoryContext")]
    pub repository_context: Option<String>,
}

#[derive(Debug, Deserialize)]
pub struct AnalysisResult {
    #[serde(rename = "agentType")]
    pub agent_type: String,
    
    pub findings: Vec<SecurityFinding>,
    
    pub duration: String,
    
    pub success: bool,
    
    #[serde(rename = "errorMessage")]
    pub error_message: Option<String>,
}

#[derive(Debug, Deserialize)]
pub struct SecurityFinding {
    pub id: String,  // Changed from rule_id
    
    pub title: String,
    
    pub description: String,
    
    #[serde(rename = "severityLevel")]
    pub severity_level: u8,
    
    #[serde(rename = "filePath")]
    pub file_path: String,
    
    #[serde(rename = "lineNumber")]
    pub line_number: Option<i32>,
    
    #[serde(rename = "codeSnippet")]
    pub code_snippet: Option<String>,
    
    pub remediation: String,
}

#[derive(Debug, Deserialize)]
pub struct HealthResponse {
    pub status: String,
    pub service: String,
}