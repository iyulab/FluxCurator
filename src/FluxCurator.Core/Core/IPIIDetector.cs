namespace FluxCurator.Core.Core;

using FluxCurator.Core.Domain;

/// <summary>
/// Interface for detecting specific types of PII in text.
/// </summary>
public interface IPIIDetector
{
    /// <summary>
    /// Gets the type of PII this detector identifies.
    /// </summary>
    PIIType PIIType { get; }

    /// <summary>
    /// Gets the display name for this detector.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Detects all occurrences of this PII type in the text.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>A list of detected PII matches.</returns>
    IReadOnlyList<PIIMatch> Detect(string text);

    /// <summary>
    /// Checks if the text contains this type of PII.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if PII is found.</returns>
    bool ContainsPII(string text);
}

/// <summary>
/// Interface for masking PII in text.
/// </summary>
public interface IPIIMasker
{
    /// <summary>
    /// Gets the configured masking options.
    /// </summary>
    PIIMaskingOptions Options { get; }

    /// <summary>
    /// Masks all detected PII in the text.
    /// </summary>
    /// <param name="text">The text to mask.</param>
    /// <returns>The masked text result.</returns>
    PIIMaskingResult Mask(string text);

    /// <summary>
    /// Detects all PII in the text without masking.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>A list of all detected PII matches.</returns>
    IReadOnlyList<PIIMatch> Detect(string text);

    /// <summary>
    /// Checks if the text contains any PII.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if any PII is found.</returns>
    bool ContainsPII(string text);

    /// <summary>
    /// Registers a custom PII detector.
    /// </summary>
    /// <param name="detector">The detector to register.</param>
    void RegisterDetector(IPIIDetector detector);
}
