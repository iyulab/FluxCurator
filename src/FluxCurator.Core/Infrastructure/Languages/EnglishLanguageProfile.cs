namespace FluxCurator.Core.Infrastructure.Languages;

/// <summary>
/// Language profile for English text processing.
/// </summary>
public sealed class EnglishLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> EnglishAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Titles
        "Mr.", "Mrs.", "Ms.", "Dr.", "Prof.", "Rev.", "Gen.", "Col.", "Lt.", "Sgt.",
        "Jr.", "Sr.", "Ph.D.", "M.D.", "B.A.", "M.A.", "B.S.", "M.S.",
        // Common abbreviations
        "etc.", "e.g.", "i.e.", "vs.", "viz.", "cf.", "et al.",
        "Inc.", "Corp.", "Ltd.", "Co.", "LLC.",
        "Jan.", "Feb.", "Mar.", "Apr.", "Jun.", "Jul.", "Aug.", "Sep.", "Sept.", "Oct.", "Nov.", "Dec.",
        "Mon.", "Tue.", "Wed.", "Thu.", "Fri.", "Sat.", "Sun.",
        "St.", "Ave.", "Blvd.", "Rd.", "Dr.", "Ln.", "Ct.",
        "No.", "Vol.", "pp.", "p.", "ed.", "eds.",
        "approx.", "est.", "min.", "max.", "avg.",
        "U.S.", "U.K.", "E.U.", "U.N.",
    };

    /// <inheritdoc/>
    public override string LanguageCode => "en";

    /// <inheritdoc/>
    public override string LanguageName => "English";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        @"(?<=[a-zA-Z0-9])[\.!?]+(?=\s+[A-Z]|\s*$|\s*\n)";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered lists
            @"\d+[\.\)]\s+|" +
            // Lettered lists
            @"[a-zA-Z][\.\)]\s+|" +
            // Bullet points
            @"[\-\*•]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => EnglishAbbreviations;

    /// <summary>
    /// English text has approximately 4 characters per token on average.
    /// </summary>
    protected override float CharsPerToken => 4.0f;
}
