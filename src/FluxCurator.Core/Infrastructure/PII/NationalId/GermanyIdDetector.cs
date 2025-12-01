namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects German ID numbers including:
/// - Personalausweis (ID card): 10 characters (1 letter + 9 alphanumeric)
/// - Steuer-ID (Tax ID): 11 digits
/// </summary>
public sealed class GermanyIdDetector : NationalIdDetectorBase
{
    /// <inheritdoc/>
    public override string LanguageCode => "de";

    /// <inheritdoc/>
    public override string NationalIdType => "Personalausweis/Steuer-ID";

    /// <inheritdoc/>
    public override string FormatDescription => "ID Card: 10 alphanumeric; Tax ID: 11 digits";

    /// <inheritdoc/>
    public override string CountryName => "Germany";

    /// <inheritdoc/>
    public override string Name => "Germany ID Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        @"(?:" +
            // Personalausweis: 1 letter + 8 alphanumeric + 1 check digit
            @"[A-Z][A-Z0-9]{8}\d|" +
            // Steuer-ID: 11 digits
            @"\d{11}" +
        @")";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = value.ToUpperInvariant().Replace(" ", "");

        // Check if it's a Steuer-ID (11 digits)
        if (normalized.Length == 11 && normalized.All(char.IsDigit))
        {
            return ValidateSteuerID(normalized, out confidence);
        }

        // Check if it's a Personalausweis (10 characters)
        if (normalized.Length == 10 && char.IsLetter(normalized[0]))
        {
            return ValidatePersonalausweis(normalized, out confidence);
        }

        return false;
    }

    /// <summary>
    /// Validates German Tax ID (Steuer-Identifikationsnummer).
    /// Rules:
    /// - 11 digits
    /// - Cannot start with 0
    /// - Exactly one digit appears twice or three times, others appear once
    /// - Last digit is check digit
    /// </summary>
    private static bool ValidateSteuerID(string id, out float confidence)
    {
        confidence = 0.0f;

        // Cannot start with 0
        if (id[0] == '0')
        {
            confidence = 0.3f;
            return false;
        }

        // Check digit frequency in first 10 digits
        var first10 = id[..10];
        var digitCounts = first10.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());

        // One digit should appear 2 or 3 times, the rest should appear once
        var multipleCount = digitCounts.Values.Count(v => v >= 2);
        var singleCount = digitCounts.Values.Count(v => v == 1);

        if (multipleCount != 1)
        {
            confidence = 0.5f;
            return false;
        }

        // Validate check digit
        if (!ValidateSteuerIDCheckDigit(id))
        {
            confidence = 0.6f;
            return true; // Still consider it PII
        }

        confidence = 0.92f;
        return true;
    }

    /// <summary>
    /// Validates the Steuer-ID check digit.
    /// </summary>
    private static bool ValidateSteuerIDCheckDigit(string id)
    {
        // Simplified check digit validation
        // The actual algorithm is complex and involves modulo operations
        int product = 10;

        for (int i = 0; i < 10; i++)
        {
            int sum = ((id[i] - '0') + product) % 10;
            if (sum == 0) sum = 10;
            product = (sum * 2) % 11;
        }

        int checkDigit = 11 - product;
        if (checkDigit == 10) checkDigit = 0;

        return checkDigit == (id[10] - '0');
    }

    /// <summary>
    /// Validates German Personalausweis number.
    /// </summary>
    private static bool ValidatePersonalausweis(string id, out float confidence)
    {
        confidence = 0.0f;

        // First character must be a letter
        if (!char.IsLetter(id[0]))
        {
            confidence = 0.2f;
            return false;
        }

        // Characters 2-9 must be alphanumeric (letters or digits)
        for (int i = 1; i < 9; i++)
        {
            if (!char.IsLetterOrDigit(id[i]))
            {
                confidence = 0.3f;
                return false;
            }
        }

        // Last character must be a digit (check digit)
        if (!char.IsDigit(id[9]))
        {
            confidence = 0.3f;
            return false;
        }

        // Basic format is valid
        confidence = 0.88f;
        return true;
    }
}
