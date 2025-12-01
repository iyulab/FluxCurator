namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects Spanish DNI (Documento Nacional de Identidad) and NIE (Número de Identidad de Extranjero).
/// DNI Format: 8 digits + letter (e.g., 12345678Z)
/// NIE Format: Letter + 7 digits + letter (e.g., X1234567L)
/// </summary>
public sealed class SpainDNIDetector : NationalIdDetectorBase
{
    // Check letter sequence (23 letters, excluding I, Ñ, O, U)
    private const string CheckLetters = "TRWAGMYFPDXBNJZSQVHLCKE";

    /// <inheritdoc/>
    public override string LanguageCode => "es";

    /// <inheritdoc/>
    public override string NationalIdType => "DNI/NIE";

    /// <inheritdoc/>
    public override string FormatDescription => "DNI: 8 digits + letter; NIE: X/Y/Z + 7 digits + letter";

    /// <inheritdoc/>
    public override string CountryName => "Spain";

    /// <inheritdoc/>
    public override string Name => "Spain DNI/NIE Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        @"(?:" +
            // DNI: 8 digits + letter
            @"\d{8}[A-Za-z]|" +
            // NIE: X/Y/Z + 7 digits + letter
            @"[XYZxyz]\d{7}[A-Za-z]" +
        @")";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = value.ToUpperInvariant().Replace(" ", "").Replace("-", "");

        if (normalized.Length != 9)
            return false;

        // Determine if DNI or NIE
        var firstChar = normalized[0];
        bool isNIE = firstChar == 'X' || firstChar == 'Y' || firstChar == 'Z';
        bool isDNI = char.IsDigit(firstChar);

        if (!isDNI && !isNIE)
        {
            confidence = 0.2f;
            return false;
        }

        // Validate check letter
        if (!ValidateCheckLetter(normalized, isNIE))
        {
            // Format valid but check letter wrong
            confidence = 0.6f;
            return true; // Still consider it PII
        }

        // All validations passed
        confidence = 0.95f;
        return true;
    }

    /// <summary>
    /// Validates the check letter.
    /// Algorithm: number mod 23 gives index into CheckLetters string
    /// </summary>
    private static bool ValidateCheckLetter(string id, bool isNIE)
    {
        if (id.Length != 9)
            return false;

        string numberPart;

        if (isNIE)
        {
            // For NIE, replace X=0, Y=1, Z=2
            var niePrefix = id[0] switch
            {
                'X' => "0",
                'Y' => "1",
                'Z' => "2",
                _ => ""
            };
            numberPart = niePrefix + id[1..8];
        }
        else
        {
            numberPart = id[..8];
        }

        if (!int.TryParse(numberPart, out var number))
            return false;

        var expectedLetter = CheckLetters[number % 23];
        var actualLetter = id[8];

        return expectedLetter == actualLetter;
    }

    /// <summary>
    /// Determines if the ID is a NIE (foreigner ID) or DNI (citizen ID).
    /// </summary>
    public static string GetIdType(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "Unknown";

        var normalized = id.ToUpperInvariant().Replace(" ", "");
        if (normalized.Length < 1)
            return "Unknown";

        var firstChar = normalized[0];
        return firstChar switch
        {
            'X' or 'Y' or 'Z' => "NIE",
            >= '0' and <= '9' => "DNI",
            _ => "Unknown"
        };
    }
}
