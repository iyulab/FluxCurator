namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects Italian Codice Fiscale (Tax Code).
/// Format: 16 alphanumeric characters
/// Structure: SSSNNNYYMDDCCCCC
/// - SSS: Surname consonants (3 chars)
/// - NNN: Name consonants (3 chars)
/// - YY: Year of birth (2 digits)
/// - M: Month of birth (letter A-E,H,L,M,P,R,S,T)
/// - DD: Day of birth (01-31, females +40)
/// - CCCC: Municipality code (4 chars)
/// - C: Check character
/// </summary>
public sealed class ItalyCodiceFiscaleDetector : NationalIdDetectorBase
{
    // Month codes
    private static readonly Dictionary<char, int> MonthCodes = new()
    {
        ['A'] = 1, ['B'] = 2, ['C'] = 3, ['D'] = 4, ['E'] = 5,
        ['H'] = 6, ['L'] = 7, ['M'] = 8, ['P'] = 9, ['R'] = 10,
        ['S'] = 11, ['T'] = 12
    };

    // Odd position values for check character calculation
    private static readonly Dictionary<char, int> OddValues = new()
    {
        ['0'] = 1, ['1'] = 0, ['2'] = 5, ['3'] = 7, ['4'] = 9,
        ['5'] = 13, ['6'] = 15, ['7'] = 17, ['8'] = 19, ['9'] = 21,
        ['A'] = 1, ['B'] = 0, ['C'] = 5, ['D'] = 7, ['E'] = 9,
        ['F'] = 13, ['G'] = 15, ['H'] = 17, ['I'] = 19, ['J'] = 21,
        ['K'] = 2, ['L'] = 4, ['M'] = 18, ['N'] = 20, ['O'] = 11,
        ['P'] = 3, ['Q'] = 6, ['R'] = 8, ['S'] = 12, ['T'] = 14,
        ['U'] = 16, ['V'] = 10, ['W'] = 22, ['X'] = 25, ['Y'] = 24,
        ['Z'] = 23
    };

    // Even position values for check character calculation
    private static readonly Dictionary<char, int> EvenValues = new()
    {
        ['0'] = 0, ['1'] = 1, ['2'] = 2, ['3'] = 3, ['4'] = 4,
        ['5'] = 5, ['6'] = 6, ['7'] = 7, ['8'] = 8, ['9'] = 9,
        ['A'] = 0, ['B'] = 1, ['C'] = 2, ['D'] = 3, ['E'] = 4,
        ['F'] = 5, ['G'] = 6, ['H'] = 7, ['I'] = 8, ['J'] = 9,
        ['K'] = 10, ['L'] = 11, ['M'] = 12, ['N'] = 13, ['O'] = 14,
        ['P'] = 15, ['Q'] = 16, ['R'] = 17, ['S'] = 18, ['T'] = 19,
        ['U'] = 20, ['V'] = 21, ['W'] = 22, ['X'] = 23, ['Y'] = 24,
        ['Z'] = 25
    };

    /// <inheritdoc/>
    public override string LanguageCode => "it";

    /// <inheritdoc/>
    public override string NationalIdType => "Codice Fiscale";

    /// <inheritdoc/>
    public override string FormatDescription => "16 alphanumeric characters with check letter";

    /// <inheritdoc/>
    public override string CountryName => "Italy";

    /// <inheritdoc/>
    public override string Name => "Italy Codice Fiscale Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: 6 letters + 2 digits + letter + 2 digits + letter + 3 alphanumeric + letter
        @"[A-Za-z]{6}\d{2}[A-EHLMPRSTa-ehlmprst]\d{2}[A-Za-z]\d{3}[A-Za-z]";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = value.ToUpperInvariant().Replace(" ", "");

        if (normalized.Length != 16)
            return false;

        // Validate structure
        // Positions 1-6: letters (surname + name)
        for (int i = 0; i < 6; i++)
        {
            if (!char.IsLetter(normalized[i]))
            {
                confidence = 0.2f;
                return false;
            }
        }

        // Positions 7-8: year digits
        if (!char.IsDigit(normalized[6]) || !char.IsDigit(normalized[7]))
        {
            confidence = 0.3f;
            return false;
        }

        // Position 9: month letter
        var monthChar = normalized[8];
        if (!MonthCodes.ContainsKey(monthChar))
        {
            confidence = 0.4f;
            return false;
        }

        // Positions 10-11: day digits
        if (!int.TryParse(normalized[9..11], out var day))
        {
            confidence = 0.3f;
            return false;
        }

        // Day validation (1-31 for males, 41-71 for females)
        if (!((day >= 1 && day <= 31) || (day >= 41 && day <= 71)))
        {
            confidence = 0.4f;
            return false;
        }

        // Position 12: municipality letter
        if (!char.IsLetter(normalized[11]))
        {
            confidence = 0.3f;
            return false;
        }

        // Positions 13-15: municipality digits
        if (!normalized[12..15].All(char.IsDigit))
        {
            confidence = 0.3f;
            return false;
        }

        // Position 16: check character
        if (!char.IsLetter(normalized[15]))
        {
            confidence = 0.3f;
            return false;
        }

        // Validate check character
        if (!ValidateCheckCharacter(normalized))
        {
            confidence = 0.7f;
            return true; // Still consider it PII
        }

        // All validations passed
        confidence = 0.95f;
        return true;
    }

    /// <summary>
    /// Validates the check character.
    /// </summary>
    private static bool ValidateCheckCharacter(string codiceFiscale)
    {
        if (codiceFiscale.Length != 16)
            return false;

        int sum = 0;

        for (int i = 0; i < 15; i++)
        {
            var c = codiceFiscale[i];
            // Odd positions (1-indexed) use OddValues, even positions use EvenValues
            if ((i + 1) % 2 == 1) // Odd position (1, 3, 5, ...)
            {
                if (OddValues.TryGetValue(c, out var value))
                    sum += value;
            }
            else // Even position (2, 4, 6, ...)
            {
                if (EvenValues.TryGetValue(c, out var value))
                    sum += value;
            }
        }

        var expectedCheckChar = (char)('A' + (sum % 26));
        var actualCheckChar = codiceFiscale[15];

        return expectedCheckChar == actualCheckChar;
    }

    /// <summary>
    /// Gets the gender from the Codice Fiscale.
    /// </summary>
    public static string? GetGender(string codiceFiscale)
    {
        if (string.IsNullOrEmpty(codiceFiscale) || codiceFiscale.Length < 11)
            return null;

        var normalized = codiceFiscale.ToUpperInvariant();

        if (!int.TryParse(normalized[9..11], out var day))
            return null;

        return day > 40 ? "Female" : "Male";
    }
}
