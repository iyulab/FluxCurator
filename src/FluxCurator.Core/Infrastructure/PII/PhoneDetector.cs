namespace FluxCurator.Core.Infrastructure.PII;

using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects phone numbers in text.
/// Supports Korean, US, and international formats.
/// </summary>
public sealed class PhoneDetector : PIIDetectorBase
{
    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.Phone;

    /// <inheritdoc/>
    public override string Name => "Phone Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        @"(?:" +
            // Korean mobile: 010-1234-5678, 010.1234.5678, 01012345678
            @"01[016789][-.\s]?\d{3,4}[-.\s]?\d{4}|" +
            // Korean landline: 02-1234-5678, 031-123-4567
            @"0[2-6][1-5]?[-.\s]?\d{3,4}[-.\s]?\d{4}|" +
            // Korean toll-free/special: 1588-1234, 1544-1234, 080-123-4567
            @"1(?:5[0-9]{2}|6[0-9]{2}|8[0-9]{2})[-.\s]?\d{4}|" +
            @"080[-.\s]?\d{3,4}[-.\s]?\d{4}|" +
            // International format with country code: +82-10-1234-5678, +1-234-567-8900
            @"\+\d{1,3}[-.\s]?\d{1,4}[-.\s]?\d{1,4}[-.\s]?\d{1,4}(?:[-.\s]?\d{1,4})?|" +
            // US format: (123) 456-7890, 123-456-7890
            @"\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}" +
        @")";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = NormalizeValue(value);

        // Check minimum length (Korean mobile minimum: 10 digits)
        if (normalized.Length < 9)
            return false;

        // Check maximum length
        if (normalized.Length > 15)
            return false;

        // Korean mobile validation
        if (IsKoreanMobile(normalized))
        {
            confidence = 0.95f;
            return true;
        }

        // Korean landline validation
        if (IsKoreanLandline(normalized))
        {
            confidence = 0.9f;
            return true;
        }

        // Korean special numbers (toll-free, etc.)
        if (IsKoreanSpecial(normalized))
        {
            confidence = 0.9f;
            return true;
        }

        // International format
        if (value.StartsWith('+'))
        {
            confidence = 0.85f;
            return true;
        }

        // US format
        if (IsUSFormat(normalized))
        {
            confidence = 0.85f;
            return true;
        }

        // Generic phone number pattern
        if (normalized.All(char.IsDigit) && normalized.Length >= 10)
        {
            confidence = 0.7f;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the number is a Korean mobile number.
    /// Korean mobile: 010, 011, 016, 017, 018, 019
    /// </summary>
    private static bool IsKoreanMobile(string normalized)
    {
        if (normalized.Length < 10 || normalized.Length > 11)
            return false;

        if (!normalized.StartsWith("01"))
            return false;

        var prefix = normalized[..3];
        var validPrefixes = new[] { "010", "011", "016", "017", "018", "019" };

        return validPrefixes.Contains(prefix);
    }

    /// <summary>
    /// Checks if the number is a Korean landline.
    /// Seoul: 02, Other regions: 031-064
    /// </summary>
    private static bool IsKoreanLandline(string normalized)
    {
        if (normalized.Length < 9 || normalized.Length > 11)
            return false;

        // Seoul (02)
        if (normalized.StartsWith("02"))
            return true;

        // Other regions (031-064)
        if (normalized.StartsWith("0") && normalized.Length >= 2)
        {
            var areaCode = normalized[1..3];
            if (int.TryParse(areaCode, out var code))
            {
                return code >= 31 && code <= 64;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the number is a Korean special number.
    /// </summary>
    private static bool IsKoreanSpecial(string normalized)
    {
        // Toll-free and special service numbers
        if (normalized.Length == 8 || normalized.Length == 12)
        {
            if (normalized.StartsWith("15") || normalized.StartsWith("16") ||
                normalized.StartsWith("18") || normalized.StartsWith("080"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the number matches US format.
    /// </summary>
    private static bool IsUSFormat(string normalized)
    {
        if (normalized.Length != 10)
            return false;

        // US numbers don't start with 0 or 1
        var firstDigit = normalized[0];
        return firstDigit >= '2' && firstDigit <= '9';
    }
}
