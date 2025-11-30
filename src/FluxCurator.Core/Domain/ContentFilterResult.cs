namespace FluxCurator.Core.Domain;

/// <summary>
/// Result of a content filtering operation.
/// </summary>
public sealed class ContentFilterResult
{
    /// <summary>
    /// Gets or sets the original input text.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Gets or sets the filtered output text.
    /// </summary>
    public required string FilteredText { get; init; }

    /// <summary>
    /// Gets or sets the list of content matches.
    /// </summary>
    public required IReadOnlyList<ContentMatch> Matches { get; init; }

    /// <summary>
    /// Gets or sets the filtering options used.
    /// </summary>
    public required ContentFilterOptions Options { get; init; }

    /// <summary>
    /// Gets or sets whether the content was blocked entirely.
    /// </summary>
    public bool IsBlocked { get; init; }

    /// <summary>
    /// Gets whether any filtered content was found.
    /// </summary>
    public bool HasFilteredContent => Matches.Count > 0;

    /// <summary>
    /// Gets the count of filtered matches.
    /// </summary>
    public int MatchCount => Matches.Count;

    /// <summary>
    /// Gets the count of matches by category.
    /// </summary>
    public IReadOnlyDictionary<ContentCategory, int> CountByCategory =>
        Matches.GroupBy(m => m.Category)
               .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Gets all unique categories detected.
    /// </summary>
    public IReadOnlySet<ContentCategory> DetectedCategories =>
        Matches.Select(m => m.Category).ToHashSet();

    /// <summary>
    /// Gets the processing timestamp.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a result indicating no filtered content was found.
    /// </summary>
    public static ContentFilterResult NoMatch(string text, ContentFilterOptions options) => new()
    {
        OriginalText = text,
        FilteredText = text,
        Matches = [],
        Options = options,
        IsBlocked = false
    };

    /// <summary>
    /// Creates a result indicating the content was blocked.
    /// </summary>
    public static ContentFilterResult Blocked(
        string text,
        IReadOnlyList<ContentMatch> matches,
        ContentFilterOptions options) => new()
    {
        OriginalText = text,
        FilteredText = string.Empty,
        Matches = matches,
        Options = options,
        IsBlocked = true
    };

    /// <summary>
    /// Gets a summary of the filtering operation.
    /// </summary>
    public string GetSummary()
    {
        if (IsBlocked)
            return $"Content blocked. Found {MatchCount} violation(s).";

        if (!HasFilteredContent)
            return "No filtered content detected.";

        var summary = $"Filtered {MatchCount} item(s): ";
        var parts = CountByCategory.Select(kvp => $"{kvp.Value} {kvp.Key}");
        return summary + string.Join(", ", parts);
    }
}
