namespace SkynetReview.Shared.Models;
/// <summary>
/// Represents a security rule used for code analysis.
/// </summary>
public class SecurityRule
{
    /// <summary>
    /// The category of the security rule.
    /// </summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>
    /// The description of the security rule.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Indicates whether the security rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}