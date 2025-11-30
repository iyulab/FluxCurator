namespace FluxCurator.Core.Infrastructure.Languages;

using System.Text.RegularExpressions;
using FluxCurator.Core.Core;

/// <summary>
/// Base implementation for language profiles with common functionality.
/// </summary>
public abstract class LanguageProfileBase : ILanguageProfile
{
    private Regex? _sentenceEndRegex;
    private Regex? _sectionMarkerRegex;

    /// <inheritdoc/>
    public abstract string LanguageCode { get; }

    /// <inheritdoc/>
    public abstract string LanguageName { get; }

    /// <inheritdoc/>
    public abstract string SentenceEndPattern { get; }

    /// <inheritdoc/>
    public abstract string SectionMarkerPattern { get; }

    /// <inheritdoc/>
    public abstract IReadOnlySet<string> Abbreviations { get; }

    /// <summary>
    /// Gets the average characters per token for this language.
    /// Used for token count estimation.
    /// </summary>
    protected virtual float CharsPerToken => 4.0f;

    /// <summary>
    /// Gets the compiled sentence end regex.
    /// </summary>
    protected Regex SentenceEndRegex =>
        _sentenceEndRegex ??= new Regex(SentenceEndPattern, RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Gets the compiled section marker regex.
    /// </summary>
    protected Regex SectionMarkerRegex =>
        _sectionMarkerRegex ??= new Regex(SectionMarkerPattern, RegexOptions.Compiled | RegexOptions.Multiline);

    /// <inheritdoc/>
    public virtual IReadOnlyList<int> FindSentenceBoundaries(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var boundaries = new List<int>();
        var matches = SentenceEndRegex.Matches(text);

        foreach (Match match in matches)
        {
            var endPos = match.Index + match.Length;

            // Skip if this looks like an abbreviation
            if (IsAbbreviationEnding(text, match.Index))
                continue;

            boundaries.Add(endPos);
        }

        // Ensure we include the end of text if not already
        if (boundaries.Count == 0 || boundaries[^1] != text.Length)
        {
            boundaries.Add(text.Length);
        }

        return boundaries;
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<int> FindParagraphBoundaries(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var boundaries = new List<int>();
        var paragraphPattern = new Regex(@"\n\s*\n", RegexOptions.Compiled);
        var matches = paragraphPattern.Matches(text);

        foreach (Match match in matches)
        {
            boundaries.Add(match.Index + match.Length);
        }

        // Ensure we include the end of text
        if (boundaries.Count == 0 || boundaries[^1] != text.Length)
        {
            boundaries.Add(text.Length);
        }

        return boundaries;
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<(int Start, int End, string Header)> FindSectionHeaders(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var headers = new List<(int Start, int End, string Header)>();
        var matches = SectionMarkerRegex.Matches(text);

        foreach (Match match in matches)
        {
            headers.Add((match.Index, match.Index + match.Length, match.Value.Trim()));
        }

        return headers;
    }

    /// <inheritdoc/>
    public virtual int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Simple estimation based on character count and language-specific ratio
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    /// <summary>
    /// Checks if the period at the given position is likely an abbreviation ending.
    /// </summary>
    protected virtual bool IsAbbreviationEnding(string text, int position)
    {
        if (position < 1)
            return false;

        // Look backwards for a potential abbreviation
        var start = Math.Max(0, position - 10);
        var word = text[start..(position + 1)];
        var lastSpace = word.LastIndexOf(' ');
        if (lastSpace >= 0)
        {
            word = word[(lastSpace + 1)..];
        }

        return Abbreviations.Contains(word.Trim());
    }
}
