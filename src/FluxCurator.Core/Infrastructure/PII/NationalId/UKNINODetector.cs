namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects UK National Insurance Numbers (NINO).
/// Format: AB 12 34 56 C (2 letters + 6 digits + 1 letter suffix)
/// </summary>
public sealed class UKNINODetector : NationalIdDetectorBase
{
    // Invalid prefix combinations
    private static readonly HashSet<string> InvalidPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BG", "GB", "NK", "KN", "TN", "NT", "ZZ"
    };

    // Invalid first letters
    private static readonly HashSet<char> InvalidFirstLetters = new()
    {
        'D', 'F', 'I', 'Q', 'U', 'V'
    };

    // Invalid second letters
    private static readonly HashSet<char> InvalidSecondLetters = new()
    {
        'D', 'F', 'I', 'O', 'Q', 'U', 'V'
    };

    // Valid suffix letters
    private static readonly HashSet<char> ValidSuffixes = new()
    {
        'A', 'B', 'C', 'D'
    };

    /// <inheritdoc/>
    public override string LanguageCode => "en-GB";

    /// <inheritdoc/>
    public override string NationalIdType => "NINO";

    /// <inheritdoc/>
    public override string FormatDescription => "AB 12 34 56 C (2 letters + 6 digits + suffix letter)";

    /// <inheritdoc/>
    public override string CountryName => "United Kingdom";

    /// <inheritdoc/>
    public override string Name => "UK NINO Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: AB 12 34 56 C or AB123456C with optional spaces
        @"[A-Za-z]{2}[\s]?\d{2}[\s]?\d{2}[\s]?\d{2}[\s]?[A-Da-d]";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        // Normalize: remove spaces and convert to uppercase
        var normalized = value.Replace(" ", "").ToUpperInvariant();

        if (normalized.Length != 9)
            return false;

        // Extract parts
        var prefix = normalized[..2];
        var digits = normalized[2..8];
        var suffix = normalized[8];

        // Validate digits
        if (!digits.All(char.IsDigit))
        {
            confidence = 0.2f;
            return false;
        }

        // Validate first letter
        if (InvalidFirstLetters.Contains(prefix[0]))
        {
            confidence = 0.3f;
            return false;
        }

        // Validate second letter
        if (InvalidSecondLetters.Contains(prefix[1]))
        {
            confidence = 0.3f;
            return false;
        }

        // Validate prefix combination
        if (InvalidPrefixes.Contains(prefix))
        {
            confidence = 0.3f;
            return false;
        }

        // Validate suffix
        if (!ValidSuffixes.Contains(suffix))
        {
            confidence = 0.4f;
            return false;
        }

        // All validations passed
        confidence = 0.95f;
        return true;
    }
}
