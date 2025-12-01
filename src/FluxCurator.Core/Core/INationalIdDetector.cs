namespace FluxCurator.Core.Core;

/// <summary>
/// Interface for national ID detectors that are language/country-specific.
/// Extends IPIIDetector with language awareness for country-specific ID validation.
/// </summary>
public interface INationalIdDetector : IPIIDetector
{
    /// <summary>
    /// Gets the ISO 639-1 language code or IETF language tag this detector supports.
    /// Examples: "ko", "en-US", "en-GB", "ja", "zh-CN", "de", "fr", "es", "pt-BR", "it"
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// Gets the specific type of national ID (e.g., "RRN", "SSN", "NINO", "My Number").
    /// </summary>
    string NationalIdType { get; }

    /// <summary>
    /// Gets a human-readable description of the ID format.
    /// </summary>
    string FormatDescription { get; }

    /// <summary>
    /// Gets the country name associated with this detector.
    /// </summary>
    string CountryName { get; }
}
