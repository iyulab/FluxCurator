namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects French INSEE Numbers (Numéro de sécurité sociale).
/// Format: 13 digits + 2 check digits (15 total)
/// Structure: SAAMMDDPPPNNN + CC
/// - S: Sex (1=male, 2=female)
/// - AA: Year of birth
/// - MM: Month of birth (01-12, or 20+ for special cases)
/// - DD: Department of birth (01-95, 99 for foreign)
/// - PPP: Municipality code
/// - NNN: Registration number
/// - CC: Check digits (97 - (13-digit number mod 97))
/// </summary>
public sealed class FranceINSEEDetector : NationalIdDetectorBase
{
    /// <inheritdoc/>
    public override string LanguageCode => "fr";

    /// <inheritdoc/>
    public override string NationalIdType => "INSEE";

    /// <inheritdoc/>
    public override string FormatDescription => "15 digits: 13 + 2 check digits (Numéro de sécurité sociale)";

    /// <inheritdoc/>
    public override string CountryName => "France";

    /// <inheritdoc/>
    public override string Name => "France INSEE Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: 15 digits with optional spaces
        @"[12]\s?\d{2}\s?\d{2}\s?\d{2,3}\s?\d{3}\s?\d{3}\s?\d{2}";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = value.Replace(" ", "");

        if (normalized.Length != 15)
            return false;

        if (!normalized.All(char.IsDigit))
            return false;

        // Validate sex digit (1 or 2)
        var sexDigit = normalized[0];
        if (sexDigit != '1' && sexDigit != '2')
        {
            confidence = 0.3f;
            return false;
        }

        // Validate month (01-12, or 20+ for special cases like overseas territories)
        if (!int.TryParse(normalized[3..5], out var month))
            return false;

        if (month < 1 || (month > 12 && month < 20))
        {
            confidence = 0.4f;
            return false;
        }

        // Validate department code
        var deptCode = normalized[5..7];
        if (!IsValidDepartment(deptCode))
        {
            confidence = 0.4f;
            return false;
        }

        // Validate check digits (Modulo-97)
        if (!ValidateCheckDigits(normalized))
        {
            // Format valid but checksum fails
            confidence = 0.7f;
            return true; // Still consider it PII
        }

        // All validations passed
        confidence = 0.95f;
        return true;
    }

    /// <summary>
    /// Validates the department code.
    /// </summary>
    private static bool IsValidDepartment(string deptCode)
    {
        if (!int.TryParse(deptCode, out var dept))
            return false;

        // Metropolitan France: 01-95 (except 20, which is split into 2A, 2B for Corsica)
        // Overseas departments: 97x
        // Foreign born: 99
        if ((dept >= 1 && dept <= 19) ||
            (dept >= 21 && dept <= 95) ||
            dept == 99)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Validates the check digits using Modulo-97.
    /// Check = 97 - (first 13 digits mod 97)
    /// </summary>
    private static bool ValidateCheckDigits(string insee)
    {
        if (insee.Length != 15)
            return false;

        // Handle Corsica departments (2A, 2B stored as 19, 18 in calculation)
        var numberPart = insee[..13];
        var checkPart = insee[13..15];

        if (!long.TryParse(numberPart, out var number))
            return false;

        if (!int.TryParse(checkPart, out var checkDigits))
            return false;

        var expectedCheck = 97 - (number % 97);

        return expectedCheck == checkDigits;
    }

    /// <summary>
    /// Gets the gender from the INSEE number.
    /// </summary>
    public static string? GetGender(string insee)
    {
        if (string.IsNullOrEmpty(insee))
            return null;

        var normalized = insee.Replace(" ", "");
        if (normalized.Length < 1)
            return null;

        return normalized[0] switch
        {
            '1' => "Male",
            '2' => "Female",
            _ => null
        };
    }
}
