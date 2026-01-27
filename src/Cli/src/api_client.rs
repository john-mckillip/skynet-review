use anyhow::Result;
use reqwest::Client;
use crate::models::{AnalysisRequest, AnalysisResult, HealthResponse};

pub struct ApiClient {
    base_url: String,
    client: Client,
}

impl ApiClient {
    pub fn new(base_url: &str) -> Self {
        Self {
            base_url: base_url.to_string(),
            client: Client::new(),
        }
    }

    pub async fn analyze(&self, request: AnalysisRequest) -> Result<Vec<AnalysisResult>> {
        let url = format!("{}/api/analyze", self.base_url);
        
        let response = self.client
            .post(&url)
            .json(&request)
            .send()
            .await?;

        if !response.status().is_success() {
            let status = response.status();
            let error_text = response.text().await.unwrap_or_default();
            anyhow::bail!("API request failed with status {status}: {error_text}");
        }

        let results = response.json().await?;
        Ok(results)
    }

    pub async fn health_check(&self) -> Result<HealthResponse> {
        let url = format!("{}/api/health", self.base_url);

        let response = self.client
            .get(&url)
            .send()
            .await?;

        if !response.status().is_success() {
            anyhow::bail!("Health check failed with status {}", response.status());
        }

        let health: HealthResponse = response.json().await?;
        Ok(health)
    }
}