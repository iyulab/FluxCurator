namespace FluxCurator.Core.Infrastructure.Languages;

/// <summary>
/// Language profile for Chinese (Simplified/Traditional) text processing.
/// </summary>
public sealed class ChineseLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> ChineseAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "等", "如", "即", "例"
    };

    /// <inheritdoc/>
    public override string LanguageCode => "zh";

    /// <inheritdoc/>
    public override string LanguageName => "Chinese";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        @"[。！？；]+|[.!?;]+(?:\s|$)";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Chinese chapter markers: 第一章, 第1章, etc.
            @"第[一二三四五六七八九十百千\d]+[章节条款]\s*|" +
            // Chinese number list
            @"[一二三四五六七八九十]+[、.．]\s*|" +
            // Parenthesized numbers
            @"（[一二三四五六七八九十\d]+）\s*|" +
            // Standard numbered list
            @"\d+[\.\)]\s+|" +
            // Bullet markers
            @"[■□●○◆◇▶▷]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => ChineseAbbreviations;

    /// <summary>
    /// Chinese text has approximately 1.5 characters per token due to ideographic nature.
    /// </summary>
    protected override float CharsPerToken => 1.5f;
}

/// <summary>
/// Language profile for Japanese text processing.
/// </summary>
public sealed class JapaneseLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> JapaneseAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "等", "例", "即"
    };

    /// <inheritdoc/>
    public override string LanguageCode => "ja";

    /// <inheritdoc/>
    public override string LanguageName => "Japanese";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        @"[。！？]+|[.!?]+(?:\s|$)|(?:です|ます|でした|ました)(?:[。！？]|\s|$)";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Japanese chapter markers: 第一章, 第1章, etc.
            @"第[一二三四五六七八九十百千\d]+[章節条項]\s*|" +
            // Japanese number list
            @"[一二三四五六七八九十]+[、.．]\s*|" +
            // Parenthesized numbers
            @"（[一二三四五六七八九十\d]+）\s*|" +
            // Standard numbered list
            @"\d+[\.\)]\s+|" +
            // Bullet markers
            @"[■□●○◆◇▶▷]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => JapaneseAbbreviations;

    /// <summary>
    /// Japanese text has approximately 1.5 characters per token.
    /// </summary>
    protected override float CharsPerToken => 1.5f;
}

/// <summary>
/// Language profile for Spanish text processing.
/// </summary>
public sealed class SpanishLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> SpanishAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Titles
        "Dr.", "Dra.", "Sr.", "Sra.", "Srta.", "Prof.",
        // Common abbreviations
        "etc.", "Ej.", "pág.", "págs.", "núm.", "tel.",
        "Ud.", "Uds.", "Vd.", "Vds.",
        // Addresses
        "Av.", "Avda.", "C/", "Ctra.",
        // Organizations
        "S.A.", "S.L."
    };

    /// <inheritdoc/>
    public override string LanguageCode => "es";

    /// <inheritdoc/>
    public override string LanguageName => "Spanish";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        @"[.!?¡¿]+(?:\s|$)|[.!?]+(?=[""'\)\]}>])";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered sections
            @"\d+[\.\)]\s+|" +
            // Letter sections
            @"[a-zA-Z][\.\)]\s+|" +
            // Named sections
            @"(?:Capítulo|Sección|Parte)\s+\d+|" +
            // Bullet points
            @"[\-\*•]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => SpanishAbbreviations;

    /// <summary>
    /// Spanish text has approximately 4.5 characters per token.
    /// </summary>
    protected override float CharsPerToken => 4.5f;
}

/// <summary>
/// Language profile for French text processing.
/// </summary>
public sealed class FrenchLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> FrenchAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Titles
        "Dr.", "M.", "Mme.", "Mlle.", "Prof.",
        // Common abbreviations
        "etc.", "ex.", "p.", "pp.", "vol.",
        "cf.", "fig.", "chap.", "éd.",
        // Organizations
        "S.A.", "S.A.R.L."
    };

    /// <inheritdoc/>
    public override string LanguageCode => "fr";

    /// <inheritdoc/>
    public override string LanguageName => "French";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        // French uses space before ? ! ; :
        @"[.]+(?:\s|$)|(?:\s)?[!?]+(?:\s|$)|[.!?]+(?=[""'\)\]}>«»])";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered sections
            @"\d+[\.\)]\s+|" +
            // Letter sections
            @"[a-zA-Z][\.\)]\s+|" +
            // Named sections
            @"(?:Chapitre|Section|Partie)\s+\d+|" +
            // Bullet points
            @"[\-\*•]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => FrenchAbbreviations;

    /// <summary>
    /// French text has approximately 4.5 characters per token.
    /// </summary>
    protected override float CharsPerToken => 4.5f;
}

/// <summary>
/// Language profile for German text processing.
/// </summary>
public sealed class GermanLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> GermanAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Titles
        "Dr.", "Hr.", "Fr.", "Prof.",
        // Common abbreviations
        "z.B.", "d.h.", "u.a.", "usw.", "etc.",
        "Nr.", "Bd.", "S.", "Aufl.",
        "bzw.", "ca.", "evtl.", "ggf.",
        // Organizations
        "GmbH.", "AG.", "e.V."
    };

    /// <inheritdoc/>
    public override string LanguageCode => "de";

    /// <inheritdoc/>
    public override string LanguageName => "German";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        @"[.!?]+(?:\s|$)|[.!?]+(?=[""'\)\]}>])";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered sections
            @"\d+[\.\)]\s+|" +
            // Letter sections
            @"[a-zA-Z][\.\)]\s+|" +
            // Named sections
            @"(?:Kapitel|Abschnitt|Teil)\s+\d+|" +
            // Bullet points
            @"[\-\*\u2022]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => GermanAbbreviations;

    /// <summary>
    /// German text has approximately 5 characters per token due to compound words.
    /// </summary>
    protected override float CharsPerToken => 5.0f;
}

/// <summary>
/// Language profile for Arabic text processing.
/// </summary>
public sealed class ArabicLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> ArabicAbbreviations = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override string LanguageCode => "ar";

    /// <inheritdoc/>
    public override string LanguageName => "Arabic";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        // Arabic/Persian full stop (۔), Arabic question mark (؟)
        @"[.。۔؟!！？]+(?:\s|$)";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered sections (Arabic uses both Arabic and Western numerals)
            @"\d+[.．\)）]\s*|" +
            // Bullet markers
            @"[■□●○◆◇]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => ArabicAbbreviations;

    /// <summary>
    /// Arabic text has approximately 3 characters per token.
    /// </summary>
    protected override float CharsPerToken => 3.0f;
}

/// <summary>
/// Language profile for Hindi text processing.
/// </summary>
public sealed class HindiLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> HindiAbbreviations = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override string LanguageCode => "hi";

    /// <inheritdoc/>
    public override string LanguageName => "Hindi";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        // Hindi uses Devanagari Danda (।) and Double Danda (॥) as sentence terminators
        @"[।॥]+(?:\s|$)|[.!?]+(?:\s|$)";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered sections
            @"\d+[.．\)）]\s*|" +
            // Hindi letter list (Devanagari consonants)
            @"[क-ह][.．\)）]\s*|" +
            // Bullet markers
            @"[■□●○◆◇]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => HindiAbbreviations;

    /// <summary>
    /// Hindi text has approximately 3 characters per token.
    /// </summary>
    protected override float CharsPerToken => 3.0f;
}

/// <summary>
/// Language profile for Portuguese text processing.
/// </summary>
public sealed class PortugueseLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> PortugueseAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Titles
        "Dr.", "Dra.", "Sr.", "Sra.", "Srta.", "Prof.",
        // Common abbreviations
        "etc.", "ex.", "pág.", "págs.", "núm.", "tel.",
        "V.Ex.", "V.S.",
        // Addresses
        "Av.", "R.",
        // Organizations
        "Ltda.", "S.A."
    };

    /// <inheritdoc/>
    public override string LanguageCode => "pt";

    /// <inheritdoc/>
    public override string LanguageName => "Portuguese";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        @"[.!?]+(?:\s|$)|[.!?]+(?=[""'\)\]}>])";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered sections
            @"\d+[\.\)]\s+|" +
            // Letter sections
            @"[a-zA-Z][\.\)]\s+|" +
            // Named sections
            @"(?:Capítulo|Seção|Parte)\s+\d+|" +
            // Bullet points
            @"[\-\*•]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => PortugueseAbbreviations;

    /// <summary>
    /// Portuguese text has approximately 4.5 characters per token.
    /// </summary>
    protected override float CharsPerToken => 4.5f;
}

/// <summary>
/// Language profile for Russian text processing.
/// </summary>
public sealed class RussianLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> RussianAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common abbreviations
        "г.", "гг.", "др.", "проф.",
        "т.д.", "т.е.", "т.п.",
        "и т.д.", "и т.п.", "и др.",
        "см.", "ср.", "напр.",
        // Addresses and organizations
        "ул.", "пр.", "д.", "корп.", "кв.",
        "ООО.", "ОАО.", "ЗАО."
    };

    /// <inheritdoc/>
    public override string LanguageCode => "ru";

    /// <inheritdoc/>
    public override string LanguageName => "Russian";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        @"[.!?]+(?:\s|$)|[.!?]+(?=[""'\)\]}>«»])";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Numbered sections
            @"\d+[\.\)]\s+|" +
            // Cyrillic letter sections
            @"[А-Яа-я][\.\)]\s+|" +
            // Named sections
            @"(?:Глава|Раздел|Часть)\s+\d+|" +
            // Bullet points
            @"[\-\*•]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => RussianAbbreviations;

    /// <summary>
    /// Russian text has approximately 4 characters per token.
    /// </summary>
    protected override float CharsPerToken => 4.0f;
}
