using System.Text;
using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

namespace FluxCurator.Core.Infrastructure.Refining;

/// <summary>
/// Default implementation of text refinement operations.
/// Provides token optimization, noise reduction, and text normalization for RAG pipelines.
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

        // ========================================
        // Phase 1: Token Optimization (early, largest impact)
        // ========================================

        // Step 1: Remove Base64 data URIs (significant token saver for web content)
        if (options.RemoveBase64Data)
            result = RemoveBase64Data(result, options.Base64Placeholder);

        // Step 2: Remove ASCII art boxes (before separator normalization)
        if (options.RemoveAsciiArt)
            result = RemoveAsciiArt(result);

        // Step 3: Normalize separator lines
        if (options.NormalizeSeparators)
            result = NormalizeSeparators(result, options.SeparatorReplacement);

        // Step 4: Normalize repeated characters (after separators)
        if (options.NormalizeRepeatedCharacters)
            result = NormalizeRepeatedCharacters(result, options.MaxConsecutiveRepeats);

        // ========================================
        // Phase 2: Line Processing
        // ========================================

        // Step 5: Remove empty list items
        if (options.RemoveEmptyListItems)
            result = RemoveEmptyListItems(result);

        // Step 6: Process lines (trim, blank, duplicate, min length)
        if (options.TrimLines || options.RemoveBlankLines ||
            options.RemoveDuplicateLines || options.MinLineLength > 0 ||
            options.CollapseBlankLines)
        {
            result = ProcessLines(result, options);
        }

        // ========================================
        // Phase 3: Custom Patterns
        // ========================================

        // Step 7: Apply custom remove patterns
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

        // Step 8: Apply custom replace patterns
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

        // ========================================
        // Phase 4: Final Normalization
        // ========================================

        // Step 9: Normalize whitespace (last, as it affects structure)
        if (options.NormalizeWhitespace)
            result = NormalizeWhitespace(result);

        return result;
    }

    // ========================================
    // Token Optimization Methods
    // ========================================

    /// <summary>
    /// Removes inline Base64 data URIs and replaces with placeholder.
    /// </summary>
    private static string RemoveBase64Data(string text, string placeholder)
    {
        return Base64DataRegex().Replace(text, placeholder);
    }

    /// <summary>
    /// Removes ASCII art box drawing characters while preserving content.
    /// </summary>
    private static string RemoveAsciiArt(string text)
    {
        // Remove box drawing characters
        var result = BoxDrawingRegex().Replace(text, "");

        // Clean up any resulting empty lines or excessive whitespace on lines
        var lines = result.Split('\n');
        var cleanedLines = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Keep line if it has content after removing box characters
            if (!string.IsNullOrEmpty(trimmed))
            {
                cleanedLines.Add(trimmed);
            }
            else if (cleanedLines.Count > 0 && !string.IsNullOrWhiteSpace(cleanedLines[^1]))
            {
                // Preserve one blank line between content
                cleanedLines.Add("");
            }
        }

        // Remove trailing blank lines
        while (cleanedLines.Count > 0 && string.IsNullOrEmpty(cleanedLines[^1]))
            cleanedLines.RemoveAt(cleanedLines.Count - 1);

        return string.Join('\n', cleanedLines);
    }

    /// <summary>
    /// Normalizes decorative separator lines to a standard format.
    /// </summary>
    private static string NormalizeSeparators(string text, string replacement)
    {
        return SeparatorLineRegex().Replace(text, replacement);
    }

    /// <summary>
    /// Normalizes repeated special characters to a maximum count.
    /// CJK characters are preserved to maintain semantic meaning.
    /// </summary>
    private static string NormalizeRepeatedCharacters(string text, int maxRepeats)
    {
        if (maxRepeats < 1)
            maxRepeats = 1;

        return RepeatedCharRegex().Replace(text, match =>
        {
            var repeatedChar = match.Groups[1].Value;
            var count = Math.Min(match.Length, maxRepeats);
            return new string(repeatedChar[0], count);
        });
    }

    // ========================================
    // Line Processing Methods
    // ========================================

    /// <summary>
    /// Removes empty list markers from text.
    /// </summary>
    private static string RemoveEmptyListItems(string text)
    {
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

    // ========================================
    // Compiled Regex Patterns
    // ========================================

    /// <summary>
    /// Matches Base64 data URIs (images, fonts, etc.).
    /// Minimum 50 chars to avoid false positives on short base64 strings.
    /// </summary>
    [GeneratedRegex(
        @"data:[a-zA-Z0-9+/.-]+;base64,[A-Za-z0-9+/=]{50,}",
        RegexOptions.Compiled)]
    private static partial Regex Base64DataRegex();

    /// <summary>
    /// Matches Unicode box drawing characters used in ASCII art.
    /// Covers: Box Drawing (U+2500-257F), light/heavy lines, corners, junctions.
    /// </summary>
    [GeneratedRegex(
        @"[╔╗╚╝║═╠╣╬╦╩╪┌┐└┘│─├┤┬┴┼┏┓┗┛┃━┣┫┳┻╋╭╮╯╰]+",
        RegexOptions.Compiled)]
    private static partial Regex BoxDrawingRegex();

    /// <summary>
    /// Matches decorative separator lines including:
    /// - ASCII separators: ===, ---, ***, ~~~, ___, ###
    /// - Unicode box drawing lines: ─━═┄┅┈┉
    /// - Geometric symbol lines: ■□▪▫●○◆◇★☆
    /// </summary>
    [GeneratedRegex(
        @"^[\s]*(?:" +
        @"[-=_*#~]{4,}|" +                      // ASCII separators (4+ chars)
        @"[─━═┄┅┈┉╌╍]{3,}|" +                   // Box drawing lines
        @"[■□▪▫●○◆◇★☆▶▷◀◁△▽]{3,}" +            // Geometric symbols
        @")[\s]*$",
        RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex SeparatorLineRegex();

    /// <summary>
    /// Matches repeated special characters (4+ consecutive).
    /// Excludes: word chars (\w), whitespace (\s), and CJK characters to preserve semantic meaning.
    /// CJK ranges: U+4E00-9FFF (CJK Unified), U+AC00-D7AF (Korean Hangul), U+3040-30FF (Japanese Hiragana/Katakana)
    /// </summary>
    [GeneratedRegex(
        @"([^\w\s\u4e00-\u9fff\uac00-\ud7af\u3040-\u30ff])\1{3,}",
        RegexOptions.Compiled)]
    private static partial Regex RepeatedCharRegex();

    /// <summary>
    /// Matches empty list items including unordered, ordered, and Korean markers.
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
        RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex EmptyListItemRegex();
}
