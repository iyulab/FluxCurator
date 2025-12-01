namespace FluxCurator.Core.Infrastructure.PII.NationalId;

/// <summary>
/// Detects India Aadhaar numbers.
/// Format: XXXX XXXX XXXX (12 digits)
/// Uses Verhoeff algorithm for checksum validation.
/// </summary>
public sealed class IndiaAadhaarDetector : NationalIdDetectorBase
{
    // Verhoeff multiplication table
    private static readonly int[,] MultiplicationTable =
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
        { 1, 2, 3, 4, 0, 6, 7, 8, 9, 5 },
        { 2, 3, 4, 0, 1, 7, 8, 9, 5, 6 },
        { 3, 4, 0, 1, 2, 8, 9, 5, 6, 7 },
        { 4, 0, 1, 2, 3, 9, 5, 6, 7, 8 },
        { 5, 9, 8, 7, 6, 0, 4, 3, 2, 1 },
        { 6, 5, 9, 8, 7, 1, 0, 4, 3, 2 },
        { 7, 6, 5, 9, 8, 2, 1, 0, 4, 3 },
        { 8, 7, 6, 5, 9, 3, 2, 1, 0, 4 },
        { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }
    };

    // Verhoeff permutation table
    private static readonly int[,] PermutationTable =
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
        { 1, 5, 7, 6, 2, 8, 3, 0, 9, 4 },
        { 5, 8, 0, 3, 7, 9, 6, 1, 4, 2 },
        { 8, 9, 1, 6, 0, 4, 3, 5, 2, 7 },
        { 9, 4, 5, 3, 1, 2, 6, 8, 7, 0 },
        { 4, 2, 8, 6, 5, 7, 3, 9, 0, 1 },
        { 2, 7, 9, 3, 8, 0, 6, 4, 1, 5 },
        { 7, 0, 4, 6, 9, 1, 3, 2, 5, 8 }
    };

    /// <inheritdoc/>
    public override string LanguageCode => "hi";

    /// <inheritdoc/>
    public override string NationalIdType => "Aadhaar";

    /// <inheritdoc/>
    public override string FormatDescription => "XXXX XXXX XXXX (12 digits)";

    /// <inheritdoc/>
    public override string CountryName => "India";

    /// <inheritdoc/>
    public override string Name => "India Aadhaar Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: 12 digits with optional spaces or hyphens
        @"\d{4}[\s-]?\d{4}[\s-]?\d{4}";

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

        // Aadhaar cannot start with 0 or 1
        if (normalized[0] == '0' || normalized[0] == '1')
        {
            confidence = 0.3f;
            return false;
        }

        // Check for obviously fake patterns
        if (IsObviouslyFake(normalized))
        {
            confidence = 0.4f;
            return false;
        }

        // Validate using Verhoeff algorithm
        if (!ValidateVerhoeff(normalized))
        {
            confidence = 0.5f;
            return false;
        }

        // Valid Aadhaar format
        confidence = 0.95f;
        return true;
    }

    /// <summary>
    /// Validates the Aadhaar number using the Verhoeff algorithm.
    /// </summary>
    private static bool ValidateVerhoeff(string number)
    {
        int c = 0;
        int len = number.Length;

        for (int i = 0; i < len; i++)
        {
            int digit = number[len - 1 - i] - '0';
            c = MultiplicationTable[c, PermutationTable[i % 8, digit]];
        }

        return c == 0;
    }

    /// <summary>
    /// Checks for obviously fake patterns like all same digits.
    /// </summary>
    private static bool IsObviouslyFake(string aadhaar)
    {
        // Check for all same digits
        if (aadhaar.Distinct().Count() == 1)
            return true;

        // Check for sequential patterns
        if (aadhaar == "123456789012" || aadhaar == "210987654321")
            return true;

        // Check for repeating patterns
        if (aadhaar == "123412341234" || aadhaar == "111122223333")
            return true;

        return false;
    }
}
