namespace FluxCurator.Core.Domain;

/// <summary>
/// Configuration options for content filtering operations.
/// </summary>
public sealed class ContentFilterOptions
{
    /// <summary>
    /// Gets or sets which content categories to filter.
    /// Default: Common (Profanity, HateSpeech, Violence, Adult).
    /// </summary>
    public ContentCategory CategoriesToFilter { get; set; } = ContentCategory.Common;

    /// <summary>
    /// Gets or sets the action to take when filtered content is detected.
    /// Default: Replace.
    /// </summary>
    public FilterAction DefaultAction { get; set; } = FilterAction.Replace;

    /// <summary>
    /// Gets or sets the minimum confidence threshold for filtering.
    /// Range: 0.0 to 1.0. Default: 0.8.
    /// </summary>
    public float MinConfidence { get; set; } = 0.8f;

    /// <summary>
    /// Gets or sets the default replacement text for filtered content.
    /// Default: "[FILTERED]".
    /// </summary>
    public string ReplacementText { get; set; } = "[FILTERED]";

    /// <summary>
    /// Gets or sets custom replacement texts for each category.
    /// </summary>
    public Dictionary<ContentCategory, string> CustomReplacements { get; set; } = new();

    /// <summary>
    /// Gets or sets custom actions for each category.
    /// </summary>
    public Dictionary<ContentCategory, FilterAction> CustomActions { get; set; } = new();

    /// <summary>
    /// Gets or sets the character to use for redaction.
    /// Default: '*'.
    /// </summary>
    public char RedactCharacter { get; set; } = '*';

    /// <summary>
    /// Gets or sets whether to perform case-insensitive matching.
    /// Default: true.
    /// </summary>
    public bool CaseInsensitive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to match whole words only.
    /// Default: true.
    /// </summary>
    public bool WholeWordOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets custom blocklist words.
    /// </summary>
    public HashSet<string> CustomBlocklist { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets custom allowlist words (exceptions to blocklist).
    /// </summary>
    public HashSet<string> CustomAllowlist { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets custom regex patterns for filtering.
    /// Key: pattern name, Value: regex pattern.
    /// </summary>
    public Dictionary<string, string> CustomPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to include detection metadata in results.
    /// Default: true.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets the replacement text for a specific category.
    /// </summary>
    public string GetReplacementText(ContentCategory category)
    {
        if (CustomReplacements.TryGetValue(category, out var custom))
            return custom;

        return category switch
        {
            ContentCategory.Profanity => "[PROFANITY]",
            ContentCategory.HateSpeech => "[HATE_SPEECH]",
            ContentCategory.Violence => "[VIOLENCE]",
            ContentCategory.Adult => "[ADULT]",
            ContentCategory.Spam => "[SPAM]",
            ContentCategory.Misinformation => "[MISINFO]",
            ContentCategory.SelfHarm => "[SELF_HARM]",
            ContentCategory.Drugs => "[DRUGS]",
            ContentCategory.Custom => "[FILTERED]",
            _ => ReplacementText
        };
    }

    /// <summary>
    /// Gets the filter action for a specific category.
    /// </summary>
    public FilterAction GetAction(ContentCategory category)
    {
        if (CustomActions.TryGetValue(category, out var action))
            return action;
        return DefaultAction;
    }

    /// <summary>
    /// Creates default filtering options.
    /// </summary>
    public static ContentFilterOptions Default => new();

    /// <summary>
    /// Creates strict filtering options (all categories, block action).
    /// </summary>
    public static ContentFilterOptions Strict => new()
    {
        CategoriesToFilter = ContentCategory.All,
        DefaultAction = FilterAction.Block,
        MinConfidence = 0.7f
    };

    /// <summary>
    /// Creates lenient filtering options (common categories, replace action).
    /// </summary>
    public static ContentFilterOptions Lenient => new()
    {
        CategoriesToFilter = ContentCategory.Profanity | ContentCategory.HateSpeech,
        DefaultAction = FilterAction.Redact,
        MinConfidence = 0.9f
    };

    /// <summary>
    /// Creates flag-only options (detect but don't modify).
    /// </summary>
    public static ContentFilterOptions FlagOnly => new()
    {
        CategoriesToFilter = ContentCategory.Common,
        DefaultAction = FilterAction.Flag,
        MinConfidence = 0.8f
    };
}
