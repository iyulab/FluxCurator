namespace FluxCurator.Core.Domain;

/// <summary>
/// Options for text refinement operations.
/// Text refinement cleans and normalizes raw text before PII masking, filtering, and chunking.
/// </summary>
public sealed class TextRefineOptions
{
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
    /// </summary>
    public static TextRefineOptions None => new();

    /// <summary>
    /// Creates light cleanup options that preserve document structure.
    /// Removes empty list items and normalizes whitespace.
    /// </summary>
    public static TextRefineOptions Light => new()
    {
        RemoveEmptyListItems = true,
        TrimLines = true,
        CollapseBlankLines = true
    };

    /// <summary>
    /// Creates standard cleanup options for general text processing.
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
    /// </summary>
    public static TextRefineOptions ForWebContent => new()
    {
        RemoveBlankLines = true,
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        NormalizeWhitespace = true,
        TrimLines = true,
        MinLineLength = 2
    };

    /// <summary>
    /// Creates cleanup options optimized for Korean text.
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
    /// Handles common PDF extraction artifacts.
    /// </summary>
    public static TextRefineOptions ForPdfContent => new()
    {
        RemoveDuplicateLines = true,
        RemoveEmptyListItems = true,
        TrimLines = true,
        CollapseBlankLines = true,
        MinLineLength = 1,
        RemovePatterns =
        [
            @"^\d+\s*$",                // Page numbers only
            @"^-\s*\d+\s*-\s*$"         // Page number markers like "- 1 -"
        ]
    };
}
