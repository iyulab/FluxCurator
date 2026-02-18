namespace FluxCurator.Tests.Languages;

using global::FluxCurator.Core.Infrastructure.Languages;

public class LanguageProfileRegistryTests
{
    #region Singleton

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = LanguageProfileRegistry.Instance;
        var instance2 = LanguageProfileRegistry.Instance;

        Assert.Same(instance1, instance2);
    }

    #endregion

    #region RegisteredLanguages

    [Fact]
    public void RegisteredLanguages_Contains13Languages()
    {
        var registry = LanguageProfileRegistry.Instance;

        Assert.True(registry.RegisteredLanguages.Count >= 13);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ko")]
    [InlineData("zh")]
    [InlineData("ja")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("ar")]
    [InlineData("hi")]
    [InlineData("pt")]
    [InlineData("ru")]
    [InlineData("vi")]
    [InlineData("th")]
    public void RegisteredLanguages_ContainsExpected(string code)
    {
        var registry = LanguageProfileRegistry.Instance;

        Assert.Contains(code, registry.RegisteredLanguages, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region GetProfile

    [Fact]
    public void GetProfile_English_ReturnsEnglishProfile()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.GetProfile("en");

        Assert.Equal("en", profile.LanguageCode);
        Assert.Equal("English", profile.LanguageName);
    }

    [Fact]
    public void GetProfile_Korean_ReturnsKoreanProfile()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.GetProfile("ko");

        Assert.Equal("ko", profile.LanguageCode);
        Assert.Equal("Korean", profile.LanguageName);
    }

    [Fact]
    public void GetProfile_Null_ReturnsDefault()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.GetProfile(null);

        Assert.Equal("en", profile.LanguageCode);
    }

    [Fact]
    public void GetProfile_Empty_ReturnsDefault()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.GetProfile("");

        Assert.Equal("en", profile.LanguageCode);
    }

    [Fact]
    public void GetProfile_Unknown_ReturnsDefault()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.GetProfile("xx");

        Assert.Equal("en", profile.LanguageCode);
    }

    #endregion

    #region DetectLanguage — Script-based Detection

    [Fact]
    public void DetectLanguage_Korean_ReturnsKo()
    {
        var result = LanguageProfileRegistry.DetectLanguage("안녕하세요 반갑습니다 테스트입니다");

        Assert.Equal("ko", result);
    }

    [Fact]
    public void DetectLanguage_Japanese_ReturnsJa()
    {
        // Hiragana + Katakana
        var result = LanguageProfileRegistry.DetectLanguage("こんにちは テスト です");

        Assert.Equal("ja", result);
    }

    [Fact]
    public void DetectLanguage_Chinese_ReturnsZh()
    {
        var result = LanguageProfileRegistry.DetectLanguage("你好世界欢迎来到这里");

        Assert.Equal("zh", result);
    }

    [Fact]
    public void DetectLanguage_Russian_ReturnsRu()
    {
        var result = LanguageProfileRegistry.DetectLanguage("Привет мир добро пожаловать");

        Assert.Equal("ru", result);
    }

    [Fact]
    public void DetectLanguage_Arabic_ReturnsAr()
    {
        var result = LanguageProfileRegistry.DetectLanguage("مرحبا بالعالم أهلا وسهلا");

        Assert.Equal("ar", result);
    }

    [Fact]
    public void DetectLanguage_Hindi_ReturnsHi()
    {
        var result = LanguageProfileRegistry.DetectLanguage("नमस्ते दुनिया स्वागत है");

        Assert.Equal("hi", result);
    }

    [Fact]
    public void DetectLanguage_Thai_ReturnsTh()
    {
        var result = LanguageProfileRegistry.DetectLanguage("สวัสดีครับ ยินดีต้อนรับ");

        Assert.Equal("th", result);
    }

    [Fact]
    public void DetectLanguage_Vietnamese_ReturnsVi()
    {
        // Vietnamese has unique diacritics: ă, ơ, ư, đ
        var result = LanguageProfileRegistry.DetectLanguage("Xin chào đây là Việt Nam ơi ư");

        Assert.Equal("vi", result);
    }

    [Fact]
    public void DetectLanguage_English_ReturnsEn()
    {
        var result = LanguageProfileRegistry.DetectLanguage("Hello world this is a test");

        Assert.Equal("en", result);
    }

    #endregion

    #region DetectLanguage — Edge Cases

    [Fact]
    public void DetectLanguage_Null_ReturnsEn()
    {
        var result = LanguageProfileRegistry.DetectLanguage(null!);

        Assert.Equal("en", result);
    }

    [Fact]
    public void DetectLanguage_Empty_ReturnsEn()
    {
        var result = LanguageProfileRegistry.DetectLanguage("");

        Assert.Equal("en", result);
    }

    [Fact]
    public void DetectLanguage_WhitespaceOnly_ReturnsEn()
    {
        var result = LanguageProfileRegistry.DetectLanguage("   \n\t  ");

        Assert.Equal("en", result);
    }

    [Fact]
    public void DetectLanguage_NumbersOnly_ReturnsEn()
    {
        // Numbers are not counted — 0 totalChars → default "en"
        var result = LanguageProfileRegistry.DetectLanguage("12345");

        Assert.Equal("en", result);
    }

    #endregion

    #region DetectProfile

    [Fact]
    public void DetectProfile_Korean_ReturnsKoreanProfile()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.DetectProfile("안녕하세요 반갑습니다");

        Assert.Equal("ko", profile.LanguageCode);
    }

    [Fact]
    public void DetectProfile_NullInput_ReturnsDefault()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.DetectProfile(null!);

        Assert.Equal("en", profile.LanguageCode);
    }

    [Fact]
    public void DetectProfile_EmptyInput_ReturnsDefault()
    {
        var registry = LanguageProfileRegistry.Instance;

        var profile = registry.DetectProfile("");

        Assert.Equal("en", profile.LanguageCode);
    }

    #endregion
}
