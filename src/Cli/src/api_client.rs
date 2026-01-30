use crate::models::{AnalysisRequest, AnalysisResult, HealthResponse, SecurityFinding};
use anyhow::{Context, Result};
use futures::StreamExt;
use reqwest::Client;
use std::time::Duration;

pub struct ApiClient {
    base_url: String,
    client: Client,
    api_key: Option<String>,
}

impl ApiClient {
    pub fn new(base_url: &str) -> Result<Self> {
        // Validate URL format
        let url = url::Url::parse(base_url).context("Invalid gateway URL format")?;

        // Enforce HTTPS for non-localhost connections
        let host = url.host_str().unwrap_or("");
        let is_localhost = host == "localhost" || host == "127.0.0.1" || host == "::1";

        if url.scheme() == "http" && !is_localhost {
            anyhow::bail!(
                "HTTPS is required for non-localhost connections. \
                Use https:// or connect to localhost for development."
            );
        }

        // Get optional API key from environment
        let api_key = std::env::var("SKYNET_API_KEY").ok();

        // Build client with timeout and explicit TLS config
        // Use longer timeout for AI analysis which can take time
        let client = Client::builder()
            .timeout(Duration::from_secs(120)) // 2 minutes for analysis
            .connect_timeout(Duration::from_secs(10))
            .min_tls_version(reqwest::tls::Version::TLS_1_2)
            .build()
            .context("Failed to create HTTP client")?;

        Ok(Self {
            base_url: base_url.to_string(),
            client,
            api_key,
        })
    }

    pub async fn analyze(&self, request: AnalysisRequest) -> Result<Vec<AnalysisResult>> {
        let url = format!("{}/api/analyze", self.base_url);

        let mut req = self.client.post(&url).json(&request);

        // Add API key header if configured
        if let Some(ref key) = self.api_key {
            req = req.header("Authorization", format!("Bearer {key}"));
        }

        let response = req
            .send()
            .await
            .context("Analysis request failed (timeout or connection issue)")?;

        if !response.status().is_success() {
            let status = response.status();
            // Don't expose raw API error details - just the status code
            anyhow::bail!("API request failed with status {status}");
        }

        let results = response
            .json()
            .await
            .context("Failed to parse API response")?;
        Ok(results)
    }

    pub async fn health_check(&self) -> Result<HealthResponse> {
        let url = format!("{}/api/health", self.base_url);

        let mut req = self.client.get(&url);

        // Add API key header if configured
        if let Some(ref key) = self.api_key {
            req = req.header("Authorization", format!("Bearer {key}"));
        }

        let response = req
            .send()
            .await
            .context("Health check failed (service may not be running)")?;

        if !response.status().is_success() {
            anyhow::bail!("Health check failed with status {}", response.status());
        }

        let health: HealthResponse = response
            .json()
            .await
            .context("Failed to parse health response")?;
        Ok(health)
    }

    pub async fn analyze_stream<F>(&self, request: AnalysisRequest, mut on_finding: F) -> Result<()>
    where
        F: FnMut(SecurityFinding),
    {
        let url = format!("{}/api/analyze/stream", self.base_url);

        let mut req = self.client.post(&url).json(&request);

        // Add API key header if configured
        if let Some(ref key) = self.api_key {
            req = req.header("Authorization", format!("Bearer {key}"));
        }

        let response = req
            .send()
            .await
            .context("Streaming analysis request failed (timeout or connection issue)")?;

        if !response.status().is_success() {
            let status = response.status();
            anyhow::bail!("API request failed with status {status}");
        }

        let mut stream = response.bytes_stream();
        let mut buffer = String::new();
        let mut event_type: Option<String> = None;

        while let Some(chunk) = stream.next().await {
            let chunk = chunk.context("Error reading stream")?;
            let text = String::from_utf8_lossy(&chunk);
            buffer.push_str(&text);

            // Process complete lines
            while let Some(newline_pos) = buffer.find('\n') {
                let line = buffer[..newline_pos].to_string();
                buffer = buffer[newline_pos + 1..].to_string();

                let line = line.trim();

                if line.is_empty() {
                    event_type = None;
                    continue;
                }

                if let Some(event) = line.strip_prefix("event: ") {
                    event_type = Some(event.to_string());
                } else if let Some(data) = line.strip_prefix("data: ") {
                    match event_type.as_deref() {
                        Some("finding") => {
                            if let Ok(finding) = serde_json::from_str::<SecurityFinding>(data) {
                                on_finding(finding);
                            }
                        }
                        Some("error") => {
                            anyhow::bail!("Server error: {data}");
                        }
                        Some("complete") => {
                            // Stream completed successfully
                        }
                        _ => {}
                    }
                }
            }
        }

        Ok(())
    }
}
