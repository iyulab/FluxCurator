namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects Chinese Resident Identity Card Numbers (身份证号码).
/// Format: 18 digits (RRRRRRYYYYMMDDSSSC)
/// - RRRRRR: Region code
/// - YYYYMMDD: Birth date
/// - SSS: Sequence number (odd=male, even=female)
/// - C: Check character (0-9 or X)
/// </summary>
public sealed class ChinaIdCardDetector : NationalIdDetectorBase
{
    // Weights for check digit calculation (ISO 7064 MOD 11-2)
    private static readonly int[] Weights = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];

    // Check characters corresponding to remainder 0-10
    private static readonly char[] CheckChars = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2'];

    /// <inheritdoc/>
    public override string LanguageCode => "zh-CN";

    /// <inheritdoc/>
    public override string NationalIdType => "ID Card";

    /// <inheritdoc/>
    public override string FormatDescription => "18 characters: RRRRRRYYYYMMDDSSSC (身份证)";

    /// <inheritdoc/>
    public override string CountryName => "China";

    /// <inheritdoc/>
    public override string Name => "China ID Card Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: 17 digits + check character (digit or X)
        @"\d{17}[\dXx]";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = value.ToUpperInvariant().Replace(" ", "");

        if (normalized.Length != 18)
            return false;

        // First 17 characters must be digits
        if (!normalized[..17].All(char.IsDigit))
            return false;

        // Last character must be digit or X
        var lastChar = normalized[17];
        if (!char.IsDigit(lastChar) && lastChar != 'X')
            return false;

        // Validate region code (first 6 digits)
        if (!IsValidRegionCode(normalized[..6]))
        {
            confidence = 0.4f;
            return false;
        }

        // Validate birth date (digits 7-14)
        if (!IsValidBirthDate(normalized[6..14]))
        {
            confidence = 0.3f;
            return false;
        }

        // Validate check character
        if (!ValidateCheckCharacter(normalized))
        {
            // Pattern and date valid but checksum fails
            confidence = 0.7f;
            return true; // Still consider it PII
        }

        // All validations passed
        confidence = 0.98f;
        return true;
    }

    /// <summary>
    /// Validates the region code.
    /// </summary>
    private static bool IsValidRegionCode(string regionCode)
    {
        if (!int.TryParse(regionCode, out var code))
            return false;

        // First two digits should be valid province code (11-82)
        var provinceCode = code / 10000;
        if (provinceCode < 11 || provinceCode > 82)
            return false;

        // Valid provinces: 11-15, 21-23, 31-37, 41-46, 50-54, 61-65, 71, 81-82
        var validPrefixes = new HashSet<int>
        {
            11, 12, 13, 14, 15, // Beijing, Tianjin, Hebei, Shanxi, Inner Mongolia
            21, 22, 23,         // Liaoning, Jilin, Heilongjiang
            31, 32, 33, 34, 35, 36, 37, // Shanghai, Jiangsu, Zhejiang, Anhui, Fujian, Jiangxi, Shandong
            41, 42, 43, 44, 45, 46,     // Henan, Hubei, Hunan, Guangdong, Guangxi, Hainan
            50, 51, 52, 53, 54,         // Chongqing, Sichuan, Guizhou, Yunnan, Tibet
            61, 62, 63, 64, 65,         // Shaanxi, Gansu, Qinghai, Ningxia, Xinjiang
            71,                          // Taiwan
            81, 82                       // Hong Kong, Macau
        };

        return validPrefixes.Contains(provinceCode);
    }

    /// <summary>
    /// Validates the birth date portion.
    /// </summary>
    private static bool IsValidBirthDate(string datePart)
    {
        if (!int.TryParse(datePart[..4], out var year) ||
            !int.TryParse(datePart[4..6], out var month) ||
            !int.TryParse(datePart[6..8], out var day))
        {
            return false;
        }

        // Year validation (reasonable range)
        if (year < 1900 || year > DateTime.Today.Year)
            return false;

        // Month validation
        if (month < 1 || month > 12)
            return false;

        // Day validation
        try
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            if (day < 1 || day > daysInMonth)
                return false;

            // Not in the future
            var birthDate = new DateTime(year, month, day);
            if (birthDate > DateTime.Today)
                return false;
        }
        catch
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the check character using ISO 7064 MOD 11-2.
    /// </summary>
    private static bool ValidateCheckCharacter(string id)
    {
        if (id.Length != 18)
            return false;

        int sum = 0;
        for (int i = 0; i < 17; i++)
        {
            sum += (id[i] - '0') * Weights[i];
        }

        var remainder = sum % 11;
        var expectedChar = CheckChars[remainder];
        var actualChar = id[17];

        return expectedChar == actualChar;
    }

    /// <summary>
    /// Gets the gender from the ID number.
    /// </summary>
    public static string? GetGender(string id)
    {
        if (id.Length != 18)
            return null;

        var sequenceDigit = id[16] - '0';
        return sequenceDigit % 2 == 1 ? "Male" : "Female";
    }
}
