namespace FluxCurator.Core.Domain;

/// <summary>
/// Options for text refinement operations.
/// Text refinement cleans and normalizes raw text before PII masking, filtering, and chunking.
/// </summary>
public sealed class TextRefineOptions
{
    // ========================================
    // Line Processing Options
    // ========================================

    /// <summary>
    /// Gets or sets whether to remove lines that contain only whitespace.
    /// Default: false.
    /// </summary>
    public bool RemoveBlankLines { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to remove consecutive duplicate lines.
    /// Useful for cleaning copy-paste artifacts and HTML conversion issues.
    /// Default: false.
    /// </summary>
    public bool RemoveDuplicateLines { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to remove empty list markers (-, *, •, +, numbered).
    /// Empty list items like "- " or "1. " with no content are removed.
    /// Default: false.
    /// </summary>
    public bool RemoveEmptyListItems { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to collapse multiple spaces and newlines to single spaces.
    /// Default: false.
    /// </summary>
    public bool NormalizeWhitespace { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to collapse multiple consecutive blank lines to a single blank line.
    /// Different from RemoveBlankLines which removes all blank lines.
    /// Default: false.
    /// </summary>
    public bool CollapseBlankLines { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to trim leading and trailing whitespace from each line.
    /// Default: false.
    /// </summary>
    public bool TrimLines { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum line length to keep.
    /// Lines shorter than this (after trimming) are removed.
    /// Set to 0 to keep all lines.
    /// Default: 0.
    /// </summary>
    public int MinLineLength { get; set; } = 0;

    // ========================================
    // Token Optimization Options
    // ========================================

    /// <summary>
    /// Gets or sets whether to normalize repeated special characters.
    /// Characters repeated more than <see cref="MaxConsecutiveRepeats"/> times are reduced.
    /// CJK characters (Korean, Chinese, Japanese) are preserved to maintain semantic meaning.
    /// Default: true (reduces noise and token count without information loss).
    /// </summary>
    public bool NormalizeRepeatedCharacters { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum allowed consecutive repetitions of the same character.
    /// Only applies when <see cref="NormalizeRepeatedCharacters"/> is true.
    /// Research suggests 3-4 is optimal for visual distinction while reducing tokens.
    /// Default: 4.
    /// </summary>
    public int MaxConsecutiveRepeats { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether to normalize decorative separator lines to a standard format.
    /// Converts ASCII art separators (====, ----), Unicode box drawing lines, and
    /// repeated geometric symbols to <see cref="SeparatorReplacement"/>.
    /// Default: true (improves consistency and reduces token count).
    /// </summary>
    public bool NormalizeSeparators { get; set; } = true;

    /// <summary>
    /// Gets or sets the replacement string for normalized separators.
    /// Use "---" for markdown horizontal rule, or "" to remove separators entirely.
    /// Default: "---".
    /// </summary>
    public string SeparatorReplacement { get; set; } = "---";

    /// <summary>
    /// Gets or sets whether to remove ASCII art boxes and decorative frames.
    /// Removes Unicode box drawing characters (╔═╗║╚╝┌─┐│└┘) while preserving content.
    /// Default: false.
    /// </summary>
    public bool RemoveAsciiArt { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to remove inline Base64 data URIs.
    /// Replaces embedded images, fonts, and other base64-encoded data with <see cref="Base64Placeholder"/>.
    /// Significant token saver for web-extracted content.
    /// Default: true (Base64 data is noise for text processing and consumes many tokens).
    /// </summary>
    public bool RemoveBase64Data { get; set; } = true;

    /// <summary>
    /// Gets or sets the placeholder text for removed Base64 data.
    /// Default: "[embedded-data]".
    /// </summary>
    public string Base64Placeholder { get; set; } = "[embedded-data]";

    // ========================================
    // Custom Patterns
    // ========================================

    /// <summary>
    /// Gets or sets custom regex patterns to remove from text.
    /// Patterns are applied in order after other refinement operations.
    /// </summary>
    public List<string> RemovePatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets custom regex patterns to replace with a specified value.
    /// Key: regex pattern, Value: replacement string.
    /// </summary>
    public Dictionary<string, string> ReplacePatterns { get; set; } = new();

    // ========================================
    // Factory Methods
    // ========================================

    /// <summary>
    /// Creates options with no refinement (pass-through).
    /// Explicitly disables all processing options.
    /// </summary>
    public static TextRefineOptions None => new()
    {
        // Explicitly disable all options for pass-through
        RemoveBlankLines = false,
        RemoveDuplicateLines = false,
        RemoveEmptyListItems = false,
        NormalizeWhitespace = false,
        CollapseBlankLines = false,
        TrimLines = false,
        MinLineLength = 0,
        NormalizeRepeatedCharacters = false,
        NormalizeSeparators = false,
        RemoveAsciiArt = false,
        RemoveBase64Data = false
    };

    /// <summary>
    /// Creates light cleanup options that preserve document structure.
    /// Removes empty list items and trims lines.
    /// Token optimization (repeated chars, separators, Base64) is enabled by default.
    /// </summary>
    public static TextRefineOptions Light => new()
    {
        RemoveEmptyListItems = true,
        TrimLines = true,
        CollapseBlankLines = true
    };

    /// <summary>
    /// Creates standard cleanup options for general text processing.
    /// Token optimization (repeated chars, separators, Base64) is enabled by default.
    /// </summary>
    public static TextRefineOptions Standard => new()
    {
        RemoveBlankLines = false,
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        TrimLines = true,
        CollapseBlankLines = true
    };

    /// <summary>
    /// Creates aggressive cleanup options for web-extracted content.
    /// Removes all noise patterns commonly found in web scraping results.
    /// Token optimization is enabled by default.
    /// </summary>
    public static TextRefineOptions ForWebContent => new()
    {
        RemoveBlankLines = true,
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        NormalizeWhitespace = true,
        TrimLines = true,
        MinLineLength = 2,
        RemoveAsciiArt = true           // Web content often has decorative boxes
    };

    /// <summary>
    /// Creates cleanup options optimized for Korean text.
    /// Token optimization (repeated chars, separators, Base64) is enabled by default.
    /// CJK characters are preserved during repeated character normalization.
    /// </summary>
    public static TextRefineOptions ForKorean => new()
    {
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        TrimLines = true,
        CollapseBlankLines = true,
        RemovePatterns =
        [
            @"^#\s*댓글\s*$",           // Comment section markers
            @"^#\s*관련\s*글\s*$",       // Related posts markers
            @"^\[광고\].*$",            // Ad markers
            @"^Copyright\s*©.*$"        // Copyright notices
        ]
    };

    /// <summary>
    /// Creates cleanup options for PDF-extracted content.
    /// Handles common PDF extraction artifacts including ASCII art boxes.
    /// Token optimization is enabled by default.
    /// </summary>
    public static TextRefineOptions ForPdfContent => new()
    {
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        TrimLines = true,
        CollapseBlankLines = true,
        MinLineLength = 1,
        RemoveAsciiArt = true,          // PDFs often have box drawing artifacts
        RemovePatterns =
        [
            @"^\d+\s*$",                // Page numbers only
            @"^-\s*\d+\s*-\s*$"         // Page number markers like "- 1 -"
        ]
    };

    /// <summary>
    /// Creates token optimization options for RAG pipelines.
    /// Combines token optimization defaults with standard cleanup.
    /// Research-backed settings targeting 15-25% token reduction.
    /// Note: Token optimization is now enabled by default in all presets.
    /// This preset provides explicit documentation of recommended settings.
    /// </summary>
    public static TextRefineOptions ForTokenOptimization => new()
    {
        // Token optimization (explicit for documentation)
        NormalizeRepeatedCharacters = true,
        MaxConsecutiveRepeats = 4,
        NormalizeSeparators = true,
        SeparatorReplacement = "---",
        RemoveBase64Data = true,

        // Standard cleanup
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        TrimLines = true,
        CollapseBlankLines = true
    };

    /// <summary>
    /// Creates aggressive token optimization options for maximum reduction.
    /// Targets 30-45% token reduction. Use with caution as it may remove some meaningful content.
    /// Ideal for web-scraped content, PDF conversions, and documents with heavy formatting.
    /// </summary>
    public static TextRefineOptions ForAggressiveTokenOptimization => new()
    {
        // Aggressive token optimization
        NormalizeRepeatedCharacters = true,
        MaxConsecutiveRepeats = 3,
        NormalizeSeparators = true,
        SeparatorReplacement = "",      // Remove separators entirely
        RemoveAsciiArt = true,
        RemoveBase64Data = true,
        Base64Placeholder = "[embedded-data]",

        // Aggressive cleanup
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        NormalizeWhitespace = true,
        TrimLines = true,
        MinLineLength = 2
    };
}
