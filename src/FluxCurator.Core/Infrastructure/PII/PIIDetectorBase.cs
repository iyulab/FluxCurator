namespace FluxCurator.Core.Infrastructure.PII;

using System.Text.RegularExpressions;
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Base class for PII detectors with common regex-based detection.
/// </summary>
public abstract class PIIDetectorBase : IPIIDetector
{
    private Regex? _compiledPattern;

    /// <summary>
    /// Lookbehind asserting that a match does not start in the middle of a run of ASCII letters or digits.
    /// Place it at the start of <see cref="Pattern"/> so a detector never reports a slice of a longer token
    /// such as an identifier, a hash or a timestamp.
    /// </summary>
    /// <remarks>
    /// Unlike <c>\b</c>, which treats every Unicode letter as a word character, this only looks at ASCII
    /// letters and digits, so a value glued to text in a script written without spaces
    /// (for example <c>연락처010-1234-5678로</c>) still matches.
    /// </remarks>
    protected const string TokenStart = "(?<![0-9A-Za-z])";

    /// <summary>
    /// Lookahead asserting that a match does not end in the middle of a run of ASCII letters or digits.
    /// The counterpart of <see cref="TokenStart"/>; place it at the end of <see cref="Pattern"/>.
    /// </summary>
    protected const string TokenEnd = "(?![0-9A-Za-z])";

    /// <summary>
    /// <see cref="TokenStart"/>, and additionally not a group of a longer hyphenated number: a match does not start
    /// right after a digit and a hyphen. Use it for numeric identifiers, so the last group of
    /// <c>550e8400-e29b-41d4-a716-446655440000</c> is not read as a twelve-digit ID on its own.
    /// </summary>
    protected const string NumberStart = TokenStart + "(?<![0-9]-)";

    /// <summary>
    /// The counterpart of <see cref="NumberStart"/>: <see cref="TokenEnd"/>, and a match does not end right before
    /// a hyphen and a digit.
    /// </summary>
    protected const string NumberEnd = "(?!-[0-9])" + TokenEnd;

    /// <inheritdoc/>
    public abstract PIIType PIIType { get; }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the regex pattern for detection.
    /// </summary>
    protected abstract string Pattern { get; }

    /// <summary>
    /// Gets the regex options for pattern matching.
    /// </summary>
    protected virtual RegexOptions RegexOptions => RegexOptions.Compiled | RegexOptions.IgnoreCase;

    /// <summary>
    /// Gets the compiled regex pattern.
    /// </summary>
    protected Regex CompiledPattern => _compiledPattern ??= new Regex(Pattern, RegexOptions);

    /// <inheritdoc/>
    public virtual IReadOnlyList<PIIMatch> Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var matches = new List<PIIMatch>();
        var regexMatches = CompiledPattern.Matches(text);

        foreach (Match match in regexMatches)
        {
            if (ValidateMatch(match.Value, out var confidence))
            {
                matches.Add(PIIMatch.Create(
                    PIIType,
                    match.Value,
                    match.Index,
                    confidence));
            }
        }

        return matches;
    }

    /// <inheritdoc/>
    public virtual bool ContainsPII(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (Match match in CompiledPattern.Matches(text))
        {
            if (ValidateMatch(match.Value, out _))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates a matched value and returns a confidence score.
    /// Override to add custom validation logic.
    /// </summary>
    /// <param name="value">The matched value to validate.</param>
    /// <param name="confidence">The confidence score (0.0 to 1.0).</param>
    /// <returns>True if the match is valid.</returns>
    protected virtual bool ValidateMatch(string value, out float confidence)
    {
        confidence = 1.0f;
        return true;
    }

    /// <summary>
    /// Whether one of <paramref name="words"/> appears on the same line within <paramref name="window"/> characters
    /// before <paramref name="index"/>, compared case-insensitively. For values whose shape alone is too common to call
    /// PII - a run of hyphenated digits is an account number only when the text says so.
    /// </summary>
    protected static bool HasContextBefore(string text, int index, IReadOnlyList<string> words, int window = 40)
    {
        if (index <= 0)
            return false;

        var start = Math.Max(0, index - window);
        var lineStart = text.LastIndexOf('\n', index - 1);
        if (lineStart >= start)
            start = lineStart + 1;

        var before = text.AsSpan(start, index - start);
        foreach (var word in words)
        {
            if (before.Contains(word, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Normalizes a value by removing common separators.
    /// </summary>
    protected static string NormalizeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("-", "")
                    .Replace(" ", "")
                    .Replace(".", "")
                    .Replace("(", "")
                    .Replace(")", "");
    }
}
