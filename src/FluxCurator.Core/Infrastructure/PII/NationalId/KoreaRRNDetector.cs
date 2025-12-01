namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects Korean Resident Registration Numbers (RRN / 주민등록번호).
/// Validates using the Modulo-11 checksum algorithm.
/// Format: YYMMDD-GNNNNNN (13 digits)
/// </summary>
public sealed class KoreaRRNDetector : NationalIdDetectorBase
{
    // Weights for Modulo-11 checksum: 2,3,4,5,6,7,8,9,2,3,4,5
    private static readonly int[] ChecksumWeights = [2, 3, 4, 5, 6, 7, 8, 9, 2, 3, 4, 5];

    /// <inheritdoc/>
    public override string LanguageCode => "ko";

    /// <inheritdoc/>
    public override string NationalIdType => "RRN";

    /// <inheritdoc/>
    public override string FormatDescription => "YYMMDD-GNNNNNN (13 digits with Modulo-11 checksum)";

    /// <inheritdoc/>
    public override string CountryName => "South Korea";

    /// <inheritdoc/>
    public override string Name => "Korea RRN Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: YYMMDD-GNNNNNN or YYMMDDGNNNNNN
        @"\d{6}[-\s]?[1-8]\d{6}";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = NormalizeValue(value);

        if (normalized.Length != 13)
            return false;

        if (!normalized.All(char.IsDigit))
            return false;

        // Validate birth date portion
        if (!IsValidBirthDate(normalized[..6], normalized[6]))
        {
            confidence = 0.3f; // Pattern match but invalid date
            return false;
        }

        // Validate gender digit (7th digit)
        // 1,2: 1900s Korean, 3,4: 2000s Korean, 5,6: 1900s Foreign, 7,8: 2000s Foreign
        var genderDigit = normalized[6] - '0';
        if (genderDigit < 1 || genderDigit > 8)
        {
            confidence = 0.4f;
            return false;
        }

        // Validate checksum using Modulo-11
        if (!ValidateChecksum(normalized))
        {
            // Pattern and date valid but checksum fails
            // Could be a typo or fake number - still flag it
            confidence = 0.7f;
            return true; // Still consider it PII even with invalid checksum
        }

        // All validations passed
        confidence = 0.99f;
        return true;
    }

    /// <summary>
    /// Validates the birth date portion of the RRN.
    /// </summary>
    private static bool IsValidBirthDate(string datePart, char genderDigit)
    {
        if (datePart.Length != 6)
            return false;

        if (!int.TryParse(datePart[..2], out var year) ||
            !int.TryParse(datePart[2..4], out var month) ||
            !int.TryParse(datePart[4..6], out var day))
        {
            return false;
        }

        // Determine century based on gender digit
        // 1,2,5,6: 1900s, 3,4,7,8: 2000s
        var gender = genderDigit - '0';
        var century = (gender <= 2 || gender == 5 || gender == 6) ? 1900 : 2000;
        var fullYear = century + year;

        // Validate month
        if (month < 1 || month > 12)
            return false;

        // Validate day based on month
        try
        {
            var daysInMonth = DateTime.DaysInMonth(fullYear, month);
            if (day < 1 || day > daysInMonth)
                return false;
        }
        catch
        {
            return false;
        }

        // Check if date is not in the future
        var birthDate = new DateTime(fullYear, month, day);
        if (birthDate > DateTime.Today)
            return false;

        // Check reasonable age (not before 1900, not more than 150 years old)
        if (fullYear < 1900 || fullYear > DateTime.Today.Year)
            return false;

        return true;
    }

    /// <summary>
    /// Validates the RRN checksum using the Modulo-11 algorithm.
    /// </summary>
    private static bool ValidateChecksum(string rrn)
    {
        if (rrn.Length != 13)
            return false;

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (rrn[i] - '0') * ChecksumWeights[i];
        }

        var expectedCheckDigit = (11 - (sum % 11)) % 10;
        var actualCheckDigit = rrn[12] - '0';

        return expectedCheckDigit == actualCheckDigit;
    }

    /// <summary>
    /// Gets the gender from an RRN (for informational purposes).
    /// </summary>
    public static string? GetGender(string rrn)
    {
        var normalized = NormalizeValue(rrn);
        if (normalized.Length != 13)
            return null;

        var genderDigit = normalized[6] - '0';
        return genderDigit switch
        {
            1 or 3 or 5 or 7 => "Male",
            2 or 4 or 6 or 8 => "Female",
            _ => null
        };
    }

    /// <summary>
    /// Gets the approximate birth year from an RRN (for informational purposes).
    /// </summary>
    public static int? GetBirthYear(string rrn)
    {
        var normalized = NormalizeValue(rrn);
        if (normalized.Length != 13)
            return null;

        if (!int.TryParse(normalized[..2], out var year))
            return null;

        var genderDigit = normalized[6] - '0';
        var century = (genderDigit <= 2 || genderDigit == 5 || genderDigit == 6) ? 1900 : 2000;

        return century + year;
    }
}
