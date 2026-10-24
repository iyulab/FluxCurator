namespace FluxCurator.Core.Infrastructure.Languages;

using System.Text.RegularExpressions;
using FluxCurator.Core.Core;
using TokenEstimation;

/// <summary>
/// Base implementation for language profiles with common functionality.
/// </summary>
public abstract partial class LanguageProfileBase : ILanguageProfile
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
        var matches = ParagraphBreak().Matches(text);

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

    /// <summary>
    /// Characters per token for each kind of character. A profile whose language spends more tokens on a kind than
    /// English text does (German compounds, Spanish inflection, Thai or Vietnamese script) overrides the ratio of that kind,
    /// fitted to o200k_base on that language's text; the other kinds keep the shared values, so mixed text stays consistent.
    /// </summary>
    private protected virtual CharTokenRatios TokenRatios => CharTokenRatios.O200k;

    /// <inheritdoc/>
    /// <remarks>
    /// Charged per kind of character (Latin, Hangul, kana, Han, digits, punctuation, whitespace runs, other scripts) at
    /// ratios fitted to o200k_base (<see cref="TokenRatios"/>).
    /// </remarks>
    public virtual int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return Math.Max(1, CharTokenEstimator.Count(text, TokenRatios));
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

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex ParagraphBreak();
}
