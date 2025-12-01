namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects US Social Security Numbers (SSN).
/// Format: XXX-XX-XXXX (9 digits)
/// Validates area number ranges and excludes invalid patterns.
/// </summary>
public sealed class USSSNDetector : NationalIdDetectorBase
{
    /// <inheritdoc/>
    public override string LanguageCode => "en-US";

    /// <inheritdoc/>
    public override string NationalIdType => "SSN";

    /// <inheritdoc/>
    public override string FormatDescription => "XXX-XX-XXXX (9 digits)";

    /// <inheritdoc/>
    public override string CountryName => "United States";

    /// <inheritdoc/>
    public override string Name => "US SSN Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: XXX-XX-XXXX with optional separators
        @"\d{3}[-\s]?\d{2}[-\s]?\d{4}";

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

        var area = int.Parse(normalized[..3]);
        var group = int.Parse(normalized[3..5]);
        var serial = int.Parse(normalized[5..9]);

        // Area number validation
        // Cannot be 000, 666, or 900-999
        if (area == 0 || area == 666 || area >= 900)
        {
            confidence = 0.3f;
            return false;
        }

        // Group number cannot be 00
        if (group == 0)
        {
            confidence = 0.3f;
            return false;
        }

        // Serial number cannot be 0000
        if (serial == 0)
        {
            confidence = 0.3f;
            return false;
        }

        // Check for known invalid/test SSNs
        if (IsTestSSN(normalized))
        {
            confidence = 0.2f;
            return false;
        }

        // Check for obviously fake patterns
        if (IsObviouslyFake(normalized))
        {
            confidence = 0.4f;
            return false;
        }

        // Valid SSN format
        confidence = 0.95f;
        return true;
    }

    /// <summary>
    /// Checks if the SSN is a known test/sample SSN.
    /// </summary>
    private static bool IsTestSSN(string ssn)
    {
        // Well-known test SSNs
        var testSSNs = new HashSet<string>
        {
            "078051120", // Woolworth wallet card SSN
            "219099999", // Known test SSN
            "457555462"  // Apple's test SSN
        };

        return testSSNs.Contains(ssn);
    }

    /// <summary>
    /// Checks for obviously fake patterns like all same digits.
    /// </summary>
    private static bool IsObviouslyFake(string ssn)
    {
        // Check for all same digits
        if (ssn.Distinct().Count() == 1)
            return true;

        // Check for sequential patterns
        if (ssn == "123456789" || ssn == "987654321")
            return true;

        return false;
    }
}
