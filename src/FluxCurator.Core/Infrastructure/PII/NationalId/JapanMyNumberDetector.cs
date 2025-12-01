namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects Japanese My Number (Individual Number / マイナンバー).
/// Format: 12 digits with check digit validation.
/// </summary>
public sealed class JapanMyNumberDetector : NationalIdDetectorBase
{
    // Weights for check digit calculation
    private static readonly int[] Weights = [6, 5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    /// <inheritdoc/>
    public override string LanguageCode => "ja";

    /// <inheritdoc/>
    public override string NationalIdType => "My Number";

    /// <inheritdoc/>
    public override string FormatDescription => "12 digits (マイナンバー) with check digit";

    /// <inheritdoc/>
    public override string CountryName => "Japan";

    /// <inheritdoc/>
    public override string Name => "Japan My Number Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: 12 consecutive digits, optionally with spaces or hyphens
        @"\d{4}[-\s]?\d{4}[-\s]?\d{4}";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = NormalizeValue(value);

        if (normalized.Length != 12)
            return false;

        if (!normalized.All(char.IsDigit))
            return false;

        // Check for obviously invalid patterns
        if (IsObviouslyInvalid(normalized))
        {
            confidence = 0.3f;
            return false;
        }

        // Validate check digit
        if (!ValidateCheckDigit(normalized))
        {
            // Pattern valid but checksum fails
            confidence = 0.6f;
            return true; // Still consider it PII
        }

        // All validations passed
        confidence = 0.95f;
        return true;
    }

    /// <summary>
    /// Validates the check digit using the official algorithm.
    /// Check digit = 11 - (weighted sum mod 11), or 0 if result >= 10
    /// </summary>
    private static bool ValidateCheckDigit(string number)
    {
        if (number.Length != 12)
            return false;

        int sum = 0;
        for (int i = 0; i < 11; i++)
        {
            sum += (number[i] - '0') * Weights[i];
        }

        var remainder = sum % 11;
        var expectedCheckDigit = remainder <= 1 ? 0 : 11 - remainder;
        var actualCheckDigit = number[11] - '0';

        return expectedCheckDigit == actualCheckDigit;
    }

    /// <summary>
    /// Checks for obviously invalid patterns.
    /// </summary>
    private static bool IsObviouslyInvalid(string number)
    {
        // All same digits
        if (number.Distinct().Count() == 1)
            return true;

        // Sequential patterns
        if (number == "123456789012" || number == "000000000000")
            return true;

        return false;
    }
}
