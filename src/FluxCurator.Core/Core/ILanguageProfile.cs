namespace FluxCurator.Core.Core;

/// <summary>
/// Interface for language-specific text processing profiles.
/// Supports multi-language chunking with proper sentence boundary detection.
/// </summary>
public interface ILanguageProfile
{
    /// <summary>
    /// Gets the ISO 639-1 language code (e.g., "ko", "en", "ja").
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// Gets the display name of the language.
    /// </summary>
    string LanguageName { get; }

    /// <summary>
    /// Gets the regex pattern for detecting sentence endings.
    /// </summary>
    string SentenceEndPattern { get; }

    /// <summary>
    /// Gets the regex pattern for detecting section markers and headings.
    /// </summary>
    string SectionMarkerPattern { get; }

    /// <summary>
    /// Gets common abbreviations that should not be treated as sentence endings.
    /// </summary>
    IReadOnlySet<string> Abbreviations { get; }

    /// <summary>
    /// Finds sentence boundaries in the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>A list of indices where sentences end.</returns>
    IReadOnlyList<int> FindSentenceBoundaries(string text);

    /// <summary>
    /// Finds paragraph boundaries in the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>A list of indices where paragraphs end.</returns>
    IReadOnlyList<int> FindParagraphBoundaries(string text);

    /// <summary>
    /// Detects potential section headers in the text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>A list of (startIndex, endIndex, headerText) tuples.</returns>
    IReadOnlyList<(int Start, int End, string Header)> FindSectionHeaders(string text);

    /// <summary>
    /// Estimates the average token count for the given text.
    /// Different languages have different character-to-token ratios.
    /// </summary>
    /// <param name="text">The text to estimate.</param>
    /// <returns>Estimated token count.</returns>
    int EstimateTokenCount(string text);
}
