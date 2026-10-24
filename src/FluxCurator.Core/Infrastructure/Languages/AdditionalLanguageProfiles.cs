namespace FluxCurator.Core.Infrastructure.Languages;

using TokenEstimation;

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

    /// <inheritdoc/>
    /// <remarks>Fitted to o200k_base on Spanish Wikipedia paragraphs (2026-10-24).</remarks>
    private protected override CharTokenRatios TokenRatios { get; } = CharTokenRatios.O200k with { Latin = 5.2 };
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

    /// <inheritdoc/>
    /// <remarks>Fitted to o200k_base on German Wikipedia paragraphs (2026-10-24).</remarks>
    private protected override CharTokenRatios TokenRatios { get; } = CharTokenRatios.O200k with { Latin = 5.0 };
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
}

/// <summary>
/// Language profile for Vietnamese text processing.
/// Vietnamese uses Latin script with many diacritical marks (tonal and modification marks).
/// White spaces separate syllables, not necessarily words.
/// </summary>
public sealed class VietnameseLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> VietnameseAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common Vietnamese abbreviations
        "TP.", "Q.", "P.", "TX.", "TT.",  // Geographic: City, District, Ward, Town, Center
        "Ths.", "TS.", "PGS.", "GS.",     // Academic: Master, Doctor, Associate Prof, Professor
        "ông", "bà", "anh", "chị",        // Titles
        "v.v.", "vv.",                     // etc.
        "tr.", "NXB."                      // Page, Publisher
    };

    /// <inheritdoc/>
    public override string LanguageCode => "vi";

    /// <inheritdoc/>
    public override string LanguageName => "Vietnamese";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        // Vietnamese uses standard punctuation for sentence endings
        @"[.!?]+(?:\s|$)|[.!?]+(?=[""'\)\]}>])";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Vietnamese chapter markers: Chương I, Phần 1, Mục 2
            @"(?:Chương|Phần|Mục|Bài|Điều)\s+[\dIVXLCDM]+[.:]?\s*|" +
            // Numbered sections
            @"\d+[\.\)]\s+|" +
            // Letter sections
            @"[a-zA-Z][\.\)]\s+|" +
            // Bullet points
            @"[\-\*•]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => VietnameseAbbreviations;

    /// <inheritdoc/>
    /// <remarks>Fitted to o200k_base on Vietnamese Wikipedia paragraphs (2026-10-24).</remarks>
    private protected override CharTokenRatios TokenRatios { get; } = CharTokenRatios.O200k with { Latin = 4.0, Other = 4.0 };
}

/// <summary>
/// Language profile for Thai text processing.
/// Thai script does not use spaces between words, only between clauses/sentences.
/// Sentence boundary detection is challenging.
/// </summary>
public sealed class ThaiLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> ThaiAbbreviations = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override string LanguageCode => "th";

    /// <inheritdoc/>
    public override string LanguageName => "Thai";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        // Thai uses ฯ (Paiyannoi) as abbreviation, ๆ (Maiyamok) for repetition
        // Sentences often end with space or Thai-specific punctuation
        // Kho Lak (ฯลฯ) means "etc.", Pai-yan-noi (ฯ) is abbreviation marker
        @"[.!?]+(?:\s|$)|(?:\s){2,}|[。！？]+";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        @"^[\s]*(?:" +
            // Markdown headers
            @"#{1,6}\s+|" +
            // Thai numbered sections: ข้อ 1, บทที่ 1, ตอนที่ 1
            @"(?:ข้อ|บทที่|ตอนที่|ส่วนที่|หมวด)\s*\d+[.:]?\s*|" +
            // Standard numbered list
            @"\d+[\.\)]\s+|" +
            // Thai number list using Thai numerals (๑, ๒, ๓...)
            @"[๐-๙]+[\.\)]\s+|" +
            // Bullet markers
            @"[■□●○◆◇]\s+" +
        @")";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => ThaiAbbreviations;

    /// <inheritdoc/>
    /// <remarks>Fitted to o200k_base on Thai Wikipedia paragraphs (2026-10-24).</remarks>
    private protected override CharTokenRatios TokenRatios { get; } = CharTokenRatios.O200k with { Other = 2.3 };
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

    /// <inheritdoc/>
    /// <remarks>Fitted to o200k_base on Russian Wikipedia paragraphs (2026-10-24).</remarks>
    private protected override CharTokenRatios TokenRatios { get; } = CharTokenRatios.O200k with { Other = 4.0 };
}
