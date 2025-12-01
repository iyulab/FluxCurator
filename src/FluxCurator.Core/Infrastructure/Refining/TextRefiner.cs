using System.Text;
using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

namespace FluxCurator.Core.Infrastructure.Refining;

/// <summary>
/// Default implementation of text refinement operations.
/// </summary>
public sealed partial class TextRefiner : ITextRefiner
{
    /// <summary>
    /// Shared singleton instance for stateless operations.
    /// </summary>
    public static TextRefiner Instance { get; } = new();

    /// <inheritdoc/>
    public string Refine(string text)
    {
        return Refine(text, TextRefineOptions.Light);
    }

    /// <inheritdoc/>
    public string Refine(string text, TextRefineOptions options)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;

        // Step 1: Remove empty list items (before line processing)
        if (options.RemoveEmptyListItems)
            result = RemoveEmptyListItems(result);

        // Step 2: Process lines
        if (options.TrimLines || options.RemoveBlankLines ||
            options.RemoveDuplicateLines || options.MinLineLength > 0 ||
            options.CollapseBlankLines)
        {
            result = ProcessLines(result, options);
        }

        // Step 3: Apply custom remove patterns
        foreach (var pattern in options.RemovePatterns)
        {
            try
            {
                result = Regex.Replace(result, pattern, "", RegexOptions.Multiline);
            }
            catch (RegexParseException)
            {
                // Skip invalid patterns
            }
        }

        // Step 4: Apply custom replace patterns
        foreach (var (pattern, replacement) in options.ReplacePatterns)
        {
            try
            {
                result = Regex.Replace(result, pattern, replacement, RegexOptions.Multiline);
            }
            catch (RegexParseException)
            {
                // Skip invalid patterns
            }
        }

        // Step 5: Normalize whitespace (last, as it may affect structure)
        if (options.NormalizeWhitespace)
            result = NormalizeWhitespace(result);

        return result;
    }

    /// <summary>
    /// Removes empty list markers from text.
    /// </summary>
    private static string RemoveEmptyListItems(string text)
    {
        // Match empty unordered list items: -, *, •, +, ◦, ▪, ▸
        // Match empty ordered list items: 1., 1), a., a), i., i)
        // Match Korean list markers: ㅇ, ○, ●, □, ■, ◇, ◆
        return EmptyListItemRegex().Replace(text, "");
    }

    /// <summary>
    /// Processes text line by line applying various transformations.
    /// </summary>
    private static string ProcessLines(string text, TextRefineOptions options)
    {
        var lines = text.Split('\n');
        var result = new List<string>(lines.Length);
        string? previousLine = null;
        int consecutiveBlankCount = 0;

        foreach (var rawLine in lines)
        {
            var line = options.TrimLines ? rawLine.Trim() : rawLine;

            // Check blank line
            bool isBlank = string.IsNullOrWhiteSpace(line);

            if (isBlank)
            {
                if (options.RemoveBlankLines)
                    continue;

                if (options.CollapseBlankLines)
                {
                    consecutiveBlankCount++;
                    if (consecutiveBlankCount > 1)
                        continue;
                }
            }
            else
            {
                consecutiveBlankCount = 0;
            }

            // Check minimum length
            if (!isBlank && options.MinLineLength > 0)
            {
                var trimmedLine = options.TrimLines ? line : line.Trim();
                if (trimmedLine.Length < options.MinLineLength)
                    continue;
            }

            // Check duplicate
            if (options.RemoveDuplicateLines && !isBlank)
            {
                if (line == previousLine)
                    continue;
                previousLine = line;
            }

            result.Add(line);
        }

        // Remove trailing blank lines if we're collapsing
        if (options.CollapseBlankLines || options.RemoveBlankLines)
        {
            while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
                result.RemoveAt(result.Count - 1);
        }

        return string.Join('\n', result);
    }

    /// <summary>
    /// Normalizes whitespace by collapsing multiple spaces/newlines to single spaces.
    /// </summary>
    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length);
        bool lastWasWhitespace = false;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasWhitespace)
                {
                    result.Append(' ');
                    lastWasWhitespace = true;
                }
            }
            else
            {
                result.Append(c);
                lastWasWhitespace = false;
            }
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// Regex for matching empty list items.
    /// </summary>
    [GeneratedRegex(
        @"^[ \t]*(?:" +
        // Unordered markers
        @"[-*•+◦▪▸]" +
        // Korean markers
        @"|[ㅇ○●□■◇◆]" +
        // Ordered numeric: 1. or 1) or (1)
        @"|\d+[.)]" +
        @"|\(\d+\)" +
        // Ordered alpha: a. or a) or (a)
        @"|[a-zA-Z][.)]" +
        @"|\([a-zA-Z]\)" +
        // Roman numerals: i. or i) or (i)
        @"|[ivxIVX]+[.)]" +
        @"|\([ivxIVX]+\)" +
        // Korean section markers: 가. 나. 제1조
        @"|[가-힣][.]" +
        @"|제\d+[조항]?" +
        @")[ \t]*$",
        RegexOptions.Multiline)]
    private static partial Regex EmptyListItemRegex();
}
