namespace FluxCurator.Core.Domain;

/// <summary>
/// Result of a PII masking operation.
/// </summary>
public sealed class PIIMaskingResult
{
    /// <summary>
    /// Gets or sets the original input text.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Gets or sets the masked output text.
    /// </summary>
    public required string MaskedText { get; init; }

    /// <summary>
    /// Gets or sets the list of detected PII matches.
    /// </summary>
    public required IReadOnlyList<PIIMatch> Matches { get; init; }

    /// <summary>
    /// Gets or sets the masking options used.
    /// </summary>
    public required PIIMaskingOptions Options { get; init; }

    /// <summary>
    /// Gets whether any PII was detected.
    /// </summary>
    public bool HasPII => Matches.Count > 0;

    /// <summary>
    /// Gets the count of PII matches found.
    /// </summary>
    public int PIICount => Matches.Count;

    /// <summary>
    /// Gets the count of matches by PII type.
    /// </summary>
    public IReadOnlyDictionary<PIIType, int> CountByType =>
        Matches.GroupBy(m => m.Type)
               .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Gets all unique PII types detected.
    /// </summary>
    public IReadOnlySet<PIIType> DetectedTypes =>
        Matches.Select(m => m.Type).ToHashSet();

    /// <summary>
    /// Gets the processing timestamp.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a result indicating no PII was found.
    /// </summary>
    public static PIIMaskingResult NoPII(string text, PIIMaskingOptions options) => new()
    {
        OriginalText = text,
        MaskedText = text,
        Matches = [],
        Options = options
    };

    /// <summary>
    /// Gets a summary of the masking operation.
    /// </summary>
    public string GetSummary()
    {
        if (!HasPII)
            return "No PII detected.";

        var summary = $"Detected {PIICount} PII item(s): ";
        var parts = CountByType.Select(kvp => $"{kvp.Value} {kvp.Key}");
        return summary + string.Join(", ", parts);
    }
}
