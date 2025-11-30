namespace FluxCurator.Core.Domain;

/// <summary>
/// Categories of content that can be filtered.
/// </summary>
[Flags]
public enum ContentCategory
{
    /// <summary>
    /// No category.
    /// </summary>
    None = 0,

    /// <summary>
    /// Profanity and offensive language.
    /// </summary>
    Profanity = 1 << 0,

    /// <summary>
    /// Hate speech and discrimination.
    /// </summary>
    HateSpeech = 1 << 1,

    /// <summary>
    /// Violence and threats.
    /// </summary>
    Violence = 1 << 2,

    /// <summary>
    /// Adult/sexual content.
    /// </summary>
    Adult = 1 << 3,

    /// <summary>
    /// Spam and promotional content.
    /// </summary>
    Spam = 1 << 4,

    /// <summary>
    /// Misinformation and fake news.
    /// </summary>
    Misinformation = 1 << 5,

    /// <summary>
    /// Self-harm and suicide related content.
    /// </summary>
    SelfHarm = 1 << 6,

    /// <summary>
    /// Drug and substance abuse related content.
    /// </summary>
    Drugs = 1 << 7,

    /// <summary>
    /// Custom/user-defined category.
    /// </summary>
    Custom = 1 << 30,

    /// <summary>
    /// All built-in categories.
    /// </summary>
    All = Profanity | HateSpeech | Violence | Adult | Spam |
          Misinformation | SelfHarm | Drugs,

    /// <summary>
    /// Common categories for general filtering.
    /// </summary>
    Common = Profanity | HateSpeech | Violence | Adult
}

/// <summary>
/// Actions to take when filtered content is detected.
/// </summary>
public enum FilterAction
{
    /// <summary>
    /// Remove the matched content completely.
    /// </summary>
    Remove,

    /// <summary>
    /// Replace the matched content with a placeholder.
    /// </summary>
    Replace,

    /// <summary>
    /// Redact the matched content with asterisks.
    /// </summary>
    Redact,

    /// <summary>
    /// Flag the content but don't modify it.
    /// </summary>
    Flag,

    /// <summary>
    /// Block the entire text from processing.
    /// </summary>
    Block
}

/// <summary>
/// Represents a match found by content filtering.
/// </summary>
public sealed class ContentMatch
{
    /// <summary>
    /// Gets or sets the category of filtered content.
    /// </summary>
    public required ContentCategory Category { get; init; }

    /// <summary>
    /// Gets or sets the matched text.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets or sets the start position in the original text.
    /// </summary>
    public required int StartIndex { get; init; }

    /// <summary>
    /// Gets or sets the end position in the original text.
    /// </summary>
    public required int EndIndex { get; init; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// Gets or sets the rule or pattern that triggered the match.
    /// </summary>
    public string? MatchedRule { get; init; }

    /// <summary>
    /// Gets or sets the replacement value after filtering.
    /// </summary>
    public string? ReplacementValue { get; set; }

    /// <summary>
    /// Gets the length of the matched text.
    /// </summary>
    public int Length => EndIndex - StartIndex;

    /// <summary>
    /// Creates a new content match.
    /// </summary>
    public static ContentMatch Create(
        ContentCategory category,
        string value,
        int startIndex,
        string? rule = null,
        float confidence = 1.0f)
    {
        return new ContentMatch
        {
            Category = category,
            Value = value,
            StartIndex = startIndex,
            EndIndex = startIndex + value.Length,
            MatchedRule = rule,
            Confidence = confidence
        };
    }
}
