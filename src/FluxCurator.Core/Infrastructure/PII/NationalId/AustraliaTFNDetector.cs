namespace FluxCurator.Core.Infrastructure.PII.NationalId;

/// <summary>
/// Detects Australia Tax File Numbers (TFN).
/// Format: XXX XXX XXX (9 digits)
/// Uses weighted sum modulo 11 for checksum validation.
/// </summary>
public sealed class AustraliaTFNDetector : NationalIdDetectorBase
{
    // Weights for TFN validation
    private static readonly int[] Weights = { 1, 4, 3, 7, 5, 8, 6, 9, 10 };

    /// <inheritdoc/>
    public override string LanguageCode => "en-AU";

    /// <inheritdoc/>
    public override string NationalIdType => "TFN";

    /// <inheritdoc/>
    public override string FormatDescription => "XXX XXX XXX (9 digits)";

    /// <inheritdoc/>
    public override string CountryName => "Australia";

    /// <inheritdoc/>
    public override string Name => "Australia TFN Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: 9 digits with optional spaces
        @"\d{3}[\s]?\d{3}[\s]?\d{3}";

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

        // Check for obviously fake patterns
        if (IsObviouslyFake(normalized))
        {
            confidence = 0.4f;
            return false;
        }

        // Validate using weighted sum mod 11
        if (!ValidateChecksum(normalized))
        {
            confidence = 0.5f;
            return false;
        }

        // Valid TFN format
        confidence = 0.95f;
        return true;
    }

    /// <summary>
    /// Validates the TFN using weighted sum modulo 11.
    /// </summary>
    private static bool ValidateChecksum(string number)
    {
        int sum = 0;

        for (int i = 0; i < 9; i++)
        {
            int digit = number[i] - '0';
            sum += digit * Weights[i];
        }

        return sum % 11 == 0;
    }

    /// <summary>
    /// Checks for obviously fake patterns.
    /// </summary>
    private static bool IsObviouslyFake(string tfn)
    {
        // Check for all same digits
        if (tfn.Distinct().Count() == 1)
            return true;

        // Check for sequential patterns
        if (tfn == "123456789" || tfn == "987654321")
            return true;

        return false;
    }
}
