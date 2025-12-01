namespace FluxCurator.Core.Core;

/// <summary>
/// Registry for national ID detectors, allowing language-specific detector lookup.
/// </summary>
public interface INationalIdRegistry
{
    /// <summary>
    /// Registers a national ID detector.
    /// </summary>
    /// <param name="detector">The detector to register.</param>
    void Register(INationalIdDetector detector);

    /// <summary>
    /// Gets a detector for the specified language code.
    /// </summary>
    /// <param name="languageCode">ISO 639-1 language code or IETF language tag.</param>
    /// <returns>The detector if found, null otherwise.</returns>
    INationalIdDetector? GetDetector(string languageCode);

    /// <summary>
    /// Gets all registered detectors.
    /// </summary>
    /// <returns>All registered national ID detectors.</returns>
    IEnumerable<INationalIdDetector> GetAllDetectors();

    /// <summary>
    /// Gets detectors for the specified language codes.
    /// </summary>
    /// <param name="languageCodes">Collection of language codes.</param>
    /// <returns>Matching detectors.</returns>
    IEnumerable<INationalIdDetector> GetDetectors(IEnumerable<string> languageCodes);

    /// <summary>
    /// Gets all supported language codes.
    /// </summary>
    /// <returns>Collection of supported language codes.</returns>
    IEnumerable<string> GetSupportedLanguages();

    /// <summary>
    /// Checks if a detector exists for the specified language code.
    /// </summary>
    /// <param name="languageCode">The language code to check.</param>
    /// <returns>True if a detector is registered for this language.</returns>
    bool HasDetector(string languageCode);
}
