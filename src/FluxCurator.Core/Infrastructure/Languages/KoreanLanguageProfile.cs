namespace FluxCurator.Core.Infrastructure.Languages;

using System.Text.RegularExpressions;

/// <summary>
/// Language profile for Korean text processing.
/// Supports formal (합니다체), informal (해요체), and casual (반말) speech styles.
/// </summary>
public sealed class KoreanLanguageProfile : LanguageProfileBase
{
    private static readonly HashSet<string> KoreanAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "씨.", "님.", "님께서.", "선생님.", "박사님.", "교수님.",
        "Dr.", "Mr.", "Mrs.", "Ms.", "Prof.",
        "등.", "외.", "예.", "cf.", "vs.",
        "(주)", "(유)", "(합)", "(재)"
    };

    /// <inheritdoc/>
    public override string LanguageCode => "ko";

    /// <inheritdoc/>
    public override string LanguageName => "Korean";

    /// <inheritdoc/>
    public override string SentenceEndPattern =>
        // Korean sentence endings: formal, informal, questions, exclamations
        @"(?<=[가-힣])(?:" +
            // Formal declarative: -습니다, -입니다, -됩니다, etc.
            @"습니다|입니다|됩니다|셨습니다|였습니다|" +
            // Formal interrogative: -습니까, -입니까
            @"습니까|입니까|" +
            // Semi-formal: -세요, -에요, -아요, -어요, -여요, -이에요, -예요
            @"세요|에요|아요|어요|여요|이에요|예요|" +
            // Informal: -어, -아, -지, -야, -냐, -네, -군, -는데
            @"어\.|아\.|지\.|야\.|냐\.|네\.|군\.|는데\.|" +
            // Connective endings that can end sentences
            @"다\.|라\.|까\.|나\.|" +
            // Past tense indicators
            @"었다|았다|였다|" +
            // Common ending combinations
            @"거든요|잖아요|던데요|" +
            // Question particles
            @"[가-힣]+\?" +
        @")[\.\?!]*(?=\s|$)|" +
        // Standard punctuation
        @"[\.!?。！？](?=\s|$)";

    /// <inheritdoc/>
    public override string SectionMarkerPattern =>
        // Korean section markers: bullet points, numbering, special characters
        @"^[\s]*(?:" +
            // Korean/CJK bullets: □, ■, ○, ●, ◎, ◇, ◆, ▶, ▷, ►, ☆, ★, ※, ㅇ
            @"[□■○●◎◇◆▶▷►☆★※ㅇ]|" +
            // Standard bullets: -, *, •
            @"[\-\*•]|" +
            // Numbering: 1., 1), (1), ①, ㉠, 가., 가), (가)
            @"\d+[\.\)]|" +
            @"\(\d+\)|" +
            @"[①-⑳]|" +
            @"[㉠-㉻]|" +
            @"[가-힣][\.\)]|" +
            @"\([가-힣]\)|" +
            // Roman numerals
            @"[IVXivx]+[\.\)]|" +
            // Korean title markers: 제1장, 제1절, 제1조
            @"제\s*\d+\s*[장절조항편부]|" +
            // Chapter markers: 1장, 1절
            @"\d+\s*[장절조항편부]" +
        @")[\s]*";

    /// <inheritdoc/>
    public override IReadOnlySet<string> Abbreviations => KoreanAbbreviations;

    /// <summary>
    /// Korean text has approximately 1.5-2 characters per token on average
    /// due to the Hangul syllable structure.
    /// </summary>
    protected override float CharsPerToken => 2.0f;

    /// <inheritdoc/>
    public override IReadOnlyList<int> FindSentenceBoundaries(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var boundaries = new List<int>();
        var sentenceEndRegex = new Regex(SentenceEndPattern, RegexOptions.Compiled | RegexOptions.Multiline);
        var matches = sentenceEndRegex.Matches(text);

        foreach (Match match in matches)
        {
            var endPos = match.Index + match.Length;

            // Skip if inside parentheses or quotes
            if (IsInsideQuotesOrParentheses(text, match.Index))
                continue;

            // Skip if this is an abbreviation
            if (IsAbbreviationEnding(text, match.Index))
                continue;

            boundaries.Add(endPos);
        }

        // Ensure we include the end of text
        if (boundaries.Count == 0 || boundaries[^1] != text.Length)
        {
            boundaries.Add(text.Length);
        }

        return boundaries;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<(int Start, int End, string Header)> FindSectionHeaders(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var headers = new List<(int Start, int End, string Header)>();

        // Find Korean chapter/section markers
        var chapterPattern = new Regex(
            @"^[\s]*(?:제\s*\d+\s*[장절조항편부]|\d+\s*[장절조항편부])[^\n]*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        foreach (Match match in chapterPattern.Matches(text))
        {
            headers.Add((match.Index, match.Index + match.Length, match.Value.Trim()));
        }

        // Find markdown-style headers
        var markdownPattern = new Regex(@"^#{1,6}\s+[^\n]+", RegexOptions.Compiled | RegexOptions.Multiline);
        foreach (Match match in markdownPattern.Matches(text))
        {
            headers.Add((match.Index, match.Index + match.Length, match.Value.Trim()));
        }

        // Sort by position
        headers.Sort((a, b) => a.Start.CompareTo(b.Start));

        return headers;
    }

    /// <inheritdoc/>
    public override int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int tokenCount = 0;

        // Count Hangul characters (roughly 1-2 tokens per character)
        int hangulCount = 0;
        int otherCount = 0;

        foreach (char c in text)
        {
            if (IsHangul(c))
                hangulCount++;
            else if (!char.IsWhiteSpace(c))
                otherCount++;
        }

        // Korean: ~2 chars per token, English/other: ~4 chars per token
        tokenCount = (int)Math.Ceiling(hangulCount / 1.5f) + (int)Math.Ceiling(otherCount / 4.0f);

        return Math.Max(1, tokenCount);
    }

    /// <summary>
    /// Determines if a character is a Hangul syllable or Jamo.
    /// </summary>
    private static bool IsHangul(char c)
    {
        // Hangul Syllables: U+AC00 - U+D7A3
        // Hangul Jamo: U+1100 - U+11FF
        // Hangul Compatibility Jamo: U+3130 - U+318F
        return (c >= '\uAC00' && c <= '\uD7A3') ||
               (c >= '\u1100' && c <= '\u11FF') ||
               (c >= '\u3130' && c <= '\u318F');
    }

    /// <summary>
    /// Checks if the position is inside quotes or parentheses.
    /// </summary>
    private static bool IsInsideQuotesOrParentheses(string text, int position)
    {
        int singleQuotes = 0;
        int doubleQuotes = 0;
        int koreanQuotes = 0;
        int parentheses = 0;

        for (int i = 0; i < position && i < text.Length; i++)
        {
            switch (text[i])
            {
                case '\'': singleQuotes++; break;
                case '"': doubleQuotes++; break;
                case '「': koreanQuotes++; break;
                case '」': koreanQuotes--; break;
                case '『': koreanQuotes++; break;
                case '』': koreanQuotes--; break;
                case '(': parentheses++; break;
                case ')': parentheses--; break;
            }
        }

        return (singleQuotes % 2 == 1) ||
               (doubleQuotes % 2 == 1) ||
               (koreanQuotes > 0) ||
               (parentheses > 0);
    }
}
