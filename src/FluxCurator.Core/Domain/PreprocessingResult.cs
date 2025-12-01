namespace FluxCurator.Core.Domain;

/// <summary>
/// Result of a complete preprocessing operation including PII masking and chunking.
/// </summary>
public sealed class PreprocessingResult
{
    /// <summary>
    /// Gets or sets the original input text.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Gets or sets the processed text (after PII masking if enabled).
    /// </summary>
    public required string ProcessedText { get; init; }

    /// <summary>
    /// Gets or sets the resulting chunks.
    /// </summary>
    public required IReadOnlyList<DocumentChunk> Chunks { get; init; }

    /// <summary>
    /// Gets or sets the PII masking result (null if PII masking was not applied).
    /// </summary>
    public PIIMaskingResult? PIIMaskingResult { get; init; }

    /// <summary>
    /// Gets or sets the content filtering result (null if filtering was not applied).
    /// </summary>
    public ContentFilterResult? ContentFilterResult { get; init; }

    /// <summary>
    /// Gets or sets the refined text (null if text refinement was not applied).
    /// This is the text after noise removal and normalization, before PII masking.
    /// </summary>
    public string? RefinedText { get; init; }

    /// <summary>
    /// Gets whether text refinement was applied.
    /// </summary>
    public bool HasRefinedText => RefinedText is not null;

    /// <summary>
    /// Gets whether PII was detected and masked.
    /// </summary>
    public bool HasMaskedPII => PIIMaskingResult?.HasPII ?? false;

    /// <summary>
    /// Gets whether content was filtered.
    /// </summary>
    public bool HasFilteredContent => ContentFilterResult?.HasFilteredContent ?? false;

    /// <summary>
    /// Gets whether content was blocked by filtering.
    /// </summary>
    public bool IsBlocked => ContentFilterResult?.IsBlocked ?? false;

    /// <summary>
    /// Gets the total number of chunks produced.
    /// </summary>
    public int ChunkCount => Chunks.Count;

    /// <summary>
    /// Gets the count of PII items masked.
    /// </summary>
    public int PIICount => PIIMaskingResult?.PIICount ?? 0;

    /// <summary>
    /// Gets the processing timestamp.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the count of filtered content items.
    /// </summary>
    public int FilteredContentCount => ContentFilterResult?.MatchCount ?? 0;

    /// <summary>
    /// Gets a summary of the preprocessing operation.
    /// </summary>
    public string GetSummary()
    {
        if (IsBlocked)
        {
            return $"Content blocked. Found {FilteredContentCount} violation(s).";
        }

        var parts = new List<string>
        {
            $"Produced {ChunkCount} chunk(s)"
        };

        if (HasRefinedText)
        {
            parts.Add("Text refined");
        }

        if (ContentFilterResult is not null)
        {
            if (ContentFilterResult.HasFilteredContent)
            {
                parts.Add($"Filtered {FilteredContentCount} content item(s)");
            }
        }

        if (PIIMaskingResult is not null)
        {
            if (PIIMaskingResult.HasPII)
            {
                parts.Add($"Masked {PIICount} PII item(s)");
            }
        }

        return string.Join(". ", parts) + ".";
    }
}
