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

        var match = CompiledPattern.Match(text);
        return match.Success && ValidateMatch(match.Value, out _);
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
