namespace FluxCurator.Core.Infrastructure.PII;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects email addresses in text.
/// </summary>
public sealed class EmailDetector : PIIDetectorBase
{
    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.Email;

    /// <inheritdoc/>
    public override string Name => "Email Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        // Basic structure validation
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex >= value.Length - 1)
            return false;

        var local = value[..atIndex];
        var domain = value[(atIndex + 1)..];

        // Local part validation
        if (local.Length == 0 || local.Length > 64)
            return false;

        if (local.StartsWith('.') || local.EndsWith('.') || local.Contains(".."))
            return false;

        // Domain validation
        if (domain.Length == 0 || domain.Length > 255)
            return false;

        var lastDot = domain.LastIndexOf('.');
        if (lastDot <= 0 || lastDot >= domain.Length - 1)
            return false;

        var tld = domain[(lastDot + 1)..];
        if (tld.Length < 2)
            return false;

        // Calculate confidence based on TLD commonality
        confidence = GetTLDConfidence(tld);

        return confidence >= 0.5f;
    }

    /// <summary>
    /// Gets confidence score based on TLD.
    /// </summary>
    private static float GetTLDConfidence(string tld)
    {
        var loweredTld = tld.ToLowerInvariant();

        // Common TLDs get high confidence
        var commonTlds = new HashSet<string>
        {
            "com", "org", "net", "edu", "gov", "mil",
            "co", "io", "me", "us", "uk", "de", "fr", "jp",
            "kr", "cn", "au", "ca", "in", "br", "ru"
        };

        // Korean specific TLDs
        var koreanTlds = new HashSet<string>
        {
            "kr", "한국", "co.kr", "or.kr", "go.kr", "ac.kr", "ne.kr"
        };

        if (koreanTlds.Contains(loweredTld))
            return 1.0f;

        if (commonTlds.Contains(loweredTld))
            return 0.95f;

        // Valid but less common TLDs
        if (tld.Length >= 2 && tld.Length <= 6 && tld.All(char.IsLetter))
            return 0.8f;

        return 0.6f;
    }
}
