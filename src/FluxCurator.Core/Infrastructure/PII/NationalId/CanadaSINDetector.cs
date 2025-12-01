namespace FluxCurator.Core.Infrastructure.PII.NationalId;

/// <summary>
/// Detects Canada Social Insurance Numbers (SIN).
/// Format: XXX-XXX-XXX (9 digits)
/// Uses Luhn algorithm for checksum validation.
/// </summary>
public sealed class CanadaSINDetector : NationalIdDetectorBase
{
    /// <inheritdoc/>
    public override string LanguageCode => "en-CA";

    /// <inheritdoc/>
    public override string NationalIdType => "SIN";

    /// <inheritdoc/>
    public override string FormatDescription => "XXX-XXX-XXX (9 digits)";

    /// <inheritdoc/>
    public override string CountryName => "Canada";

    /// <inheritdoc/>
    public override string Name => "Canada SIN Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: 9 digits with optional spaces or hyphens
        @"\d{3}[\s-]?\d{3}[\s-]?\d{3}";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = NormalizeValue(value);

        if (normalized.Length != 9)
            return false;

        if (!normalized.All(char.IsDigit))
            return false;

        // SIN cannot start with 0 or 8
        char firstDigit = normalized[0];
        if (firstDigit == '0' || firstDigit == '8')
        {
            confidence = 0.3f;
            return false;
        }

        // SINs starting with 9 are temporary SINs for non-residents
        bool isTemporary = firstDigit == '9';

        // Check for obviously fake patterns
        if (IsObviouslyFake(normalized))
        {
            confidence = 0.4f;
            return false;
        }

        // Validate using Luhn algorithm
        if (!ValidateLuhn(normalized))
        {
            confidence = 0.5f;
            return false;
        }

        // Valid SIN format
        confidence = isTemporary ? 0.90f : 0.95f;
        return true;
    }

    /// <summary>
    /// Validates the SIN using the Luhn algorithm.
    /// </summary>
    private static bool ValidateLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            int digit = number[i] - '0';

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit = (digit / 10) + (digit % 10);
            }

            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// Checks for obviously fake patterns.
    /// </summary>
    private static bool IsObviouslyFake(string sin)
    {
        // Check for all same digits
        if (sin.Distinct().Count() == 1)
            return true;

        // Check for sequential patterns
        if (sin == "123456789" || sin == "987654321")
            return true;

        // Known test SIN
        if (sin == "046454286")
            return true;

        return false;
    }
}
