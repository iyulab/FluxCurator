namespace FluxCurator.Core.Infrastructure.Languages;

using System.Collections.Concurrent;
using FluxCurator.Core.Core;

/// <summary>
/// Registry for language profiles with automatic language detection.
/// </summary>
public sealed class LanguageProfileRegistry
{
    private static readonly Lazy<LanguageProfileRegistry> _instance = new(() => new LanguageProfileRegistry());
    private readonly ConcurrentDictionary<string, ILanguageProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILanguageProfile _defaultProfile;

    /// <summary>
    /// Gets the singleton instance of the language profile registry.
    /// </summary>
    public static LanguageProfileRegistry Instance => _instance.Value;

    private LanguageProfileRegistry()
    {
        // Register built-in profiles
        _defaultProfile = new EnglishLanguageProfile();
        Register(_defaultProfile);
        Register(new KoreanLanguageProfile());
        Register(new ChineseLanguageProfile());
        Register(new JapaneseLanguageProfile());
        Register(new SpanishLanguageProfile());
        Register(new FrenchLanguageProfile());
        Register(new GermanLanguageProfile());
        Register(new ArabicLanguageProfile());
        Register(new HindiLanguageProfile());
        Register(new PortugueseLanguageProfile());
        Register(new RussianLanguageProfile());
    }

    /// <summary>
    /// Registers a language profile.
    /// </summary>
    public void Register(ILanguageProfile profile)
    {
        _profiles[profile.LanguageCode] = profile;
    }

    /// <summary>
    /// Gets a language profile by language code.
    /// </summary>
    /// <param name="languageCode">ISO 639-1 language code.</param>
    /// <returns>The language profile, or default (English) if not found.</returns>
    public ILanguageProfile GetProfile(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return _defaultProfile;

        return _profiles.TryGetValue(languageCode, out var profile) ? profile : _defaultProfile;
    }

    /// <summary>
    /// Detects the language of the given text and returns the appropriate profile.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>The detected language profile.</returns>
    public ILanguageProfile DetectProfile(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return _defaultProfile;

        var languageCode = DetectLanguage(text);
        return GetProfile(languageCode);
    }

    /// <summary>
    /// Detects the primary language of the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>ISO 639-1 language code.</returns>
    public string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en";

        // Count character types
        int koreanChars = 0;
        int latinChars = 0;
        int japaneseChars = 0;
        int chineseChars = 0;
        int cyrillicChars = 0;
        int arabicChars = 0;
        int devanagariChars = 0;
        int totalChars = 0;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                continue;

            totalChars++;

            if (IsKorean(c))
                koreanChars++;
            else if (IsJapanese(c))
                japaneseChars++;
            else if (IsChinese(c))
                chineseChars++;
            else if (IsCyrillic(c))
                cyrillicChars++;
            else if (IsArabic(c))
                arabicChars++;
            else if (IsDevanagari(c))
                devanagariChars++;
            else if (IsLatin(c))
                latinChars++;
        }

        if (totalChars == 0)
            return "en";

        // Determine dominant language based on script
        float koreanRatio = (float)koreanChars / totalChars;
        float japaneseRatio = (float)japaneseChars / totalChars;
        float chineseRatio = (float)chineseChars / totalChars;
        float cyrillicRatio = (float)cyrillicChars / totalChars;
        float arabicRatio = (float)arabicChars / totalChars;
        float devanagariRatio = (float)devanagariChars / totalChars;

        // Korean threshold: if more than 30% Korean characters
        if (koreanRatio > 0.3f)
            return "ko";

        // Japanese threshold (including Hiragana/Katakana)
        if (japaneseRatio > 0.3f)
            return "ja";

        // Chinese threshold
        if (chineseRatio > 0.3f)
            return "zh";

        // Cyrillic threshold (Russian)
        if (cyrillicRatio > 0.3f)
            return "ru";

        // Arabic threshold
        if (arabicRatio > 0.3f)
            return "ar";

        // Devanagari threshold (Hindi)
        if (devanagariRatio > 0.3f)
            return "hi";

        // Default to English for Latin-based text
        // Note: Detecting specific Latin-based languages (Spanish, French, German, Portuguese)
        // requires more sophisticated analysis beyond character detection
        return "en";
    }

    /// <summary>
    /// Gets all registered language codes.
    /// </summary>
    public IReadOnlyCollection<string> RegisteredLanguages => _profiles.Keys.ToList().AsReadOnly();

    private static bool IsKorean(char c) =>
        (c >= '\uAC00' && c <= '\uD7A3') ||  // Hangul Syllables
        (c >= '\u1100' && c <= '\u11FF') ||  // Hangul Jamo
        (c >= '\u3130' && c <= '\u318F');    // Hangul Compatibility Jamo

    private static bool IsJapanese(char c) =>
        (c >= '\u3040' && c <= '\u309F') ||  // Hiragana
        (c >= '\u30A0' && c <= '\u30FF');    // Katakana

    private static bool IsChinese(char c) =>
        (c >= '\u4E00' && c <= '\u9FFF') ||  // CJK Unified Ideographs
        (c >= '\u3400' && c <= '\u4DBF');    // CJK Extension A

    private static bool IsLatin(char c) =>
        (c >= 'A' && c <= 'Z') ||
        (c >= 'a' && c <= 'z') ||
        (c >= '\u00C0' && c <= '\u00FF');    // Latin Extended-A

    private static bool IsCyrillic(char c) =>
        (c >= '\u0400' && c <= '\u04FF');    // Cyrillic

    private static bool IsArabic(char c) =>
        (c >= '\u0600' && c <= '\u06FF') ||  // Arabic
        (c >= '\u0750' && c <= '\u077F');    // Arabic Supplement

    private static bool IsDevanagari(char c) =>
        (c >= '\u0900' && c <= '\u097F');    // Devanagari
}
