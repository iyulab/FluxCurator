namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Infrastructure.PII.NationalId;

public class NationalIdRegistryTests
{
    #region Constructor

    [Fact]
    public void Constructor_Default_RegistersAllDetectors()
    {
        var registry = new NationalIdRegistry();

        var languages = registry.GetSupportedLanguages().ToList();
        Assert.True(languages.Count >= 13); // 13 country detectors
    }

    [Fact]
    public void Constructor_NoDefaults_IsEmpty()
    {
        var registry = new NationalIdRegistry(registerDefaults: false);

        var languages = registry.GetSupportedLanguages().ToList();
        Assert.Empty(languages);
    }

    [Fact]
    public void Constructor_WithDefaults_RegistersKnownLanguages()
    {
        var registry = new NationalIdRegistry();

        Assert.True(registry.HasDetector("ko"));      // Korea
        Assert.True(registry.HasDetector("en-US"));    // US
        Assert.True(registry.HasDetector("en-GB"));    // UK
        Assert.True(registry.HasDetector("ja"));       // Japan
        Assert.True(registry.HasDetector("zh-CN"));    // China
        Assert.True(registry.HasDetector("de"));       // Germany
        Assert.True(registry.HasDetector("fr"));       // France
        Assert.True(registry.HasDetector("es"));       // Spain
        Assert.True(registry.HasDetector("pt-BR"));    // Brazil
        Assert.True(registry.HasDetector("it"));       // Italy
    }

    #endregion

    #region GetDetector

    [Fact]
    public void GetDetector_ExactMatch_ReturnsDetector()
    {
        var registry = new NationalIdRegistry();

        var detector = registry.GetDetector("ko");

        Assert.NotNull(detector);
        Assert.Equal("ko", detector!.LanguageCode);
    }

    [Fact]
    public void GetDetector_BaseLanguageFallback_ReturnsDetector()
    {
        var registry = new NationalIdRegistry();

        // "ko-KR" is not registered, but base "ko" is — should fall back to "ko"
        var detector = registry.GetDetector("ko-KR");

        Assert.NotNull(detector);
        Assert.Equal("ko", detector!.LanguageCode);
    }

    [Fact]
    public void GetDetector_UnknownLanguage_ReturnsNull()
    {
        var registry = new NationalIdRegistry();

        var detector = registry.GetDetector("xx");

        Assert.Null(detector);
    }

    [Fact]
    public void GetDetector_NullInput_ReturnsNull()
    {
        var registry = new NationalIdRegistry();

        var detector = registry.GetDetector(null!);

        Assert.Null(detector);
    }

    [Fact]
    public void GetDetector_EmptyInput_ReturnsNull()
    {
        var registry = new NationalIdRegistry();

        var detector = registry.GetDetector("");

        Assert.Null(detector);
    }

    #endregion

    #region GetDetectors

    [Fact]
    public void GetDetectors_SpecificLanguages_ReturnsMatching()
    {
        var registry = new NationalIdRegistry();

        var detectors = registry.GetDetectors(["ko", "en-US"]).ToList();

        Assert.Equal(2, detectors.Count);
    }

    [Fact]
    public void GetDetectors_AutoKeyword_ReturnsAll()
    {
        var registry = new NationalIdRegistry();

        var allDetectors = registry.GetAllDetectors().ToList();
        var autoDetectors = registry.GetDetectors(["auto"]).ToList();

        Assert.Equal(allDetectors.Count, autoDetectors.Count);
    }

    [Fact]
    public void GetDetectors_UnknownLanguage_ReturnsEmpty()
    {
        var registry = new NationalIdRegistry();

        var detectors = registry.GetDetectors(["xx"]).ToList();

        Assert.Empty(detectors);
    }

    #endregion

    #region HasDetector

    [Fact]
    public void HasDetector_KnownLanguage_ReturnsTrue()
    {
        var registry = new NationalIdRegistry();

        Assert.True(registry.HasDetector("ko"));
    }

    [Fact]
    public void HasDetector_UnknownLanguage_ReturnsFalse()
    {
        var registry = new NationalIdRegistry();

        Assert.False(registry.HasDetector("xx"));
    }

    #endregion

    #region Register

    [Fact]
    public void Register_CustomDetector_OverridesExisting()
    {
        var registry = new NationalIdRegistry();
        var customDetector = new KoreaRRNDetector();

        registry.Register(customDetector);

        Assert.True(registry.HasDetector("ko"));
    }

    [Fact]
    public void Register_NullDetector_ThrowsArgumentNull()
    {
        var registry = new NationalIdRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    #endregion

    #region GetSupportedLanguages

    [Fact]
    public void GetSupportedLanguages_Default_ContainsExpectedCodes()
    {
        var registry = new NationalIdRegistry();

        var languages = registry.GetSupportedLanguages().ToList();

        Assert.Contains("ko", languages);
        Assert.Contains("en-US", languages);
        Assert.Contains("ja", languages);
    }

    #endregion

    #region CaseInsensitivity

    [Fact]
    public void GetDetector_CaseInsensitive_ReturnsDetector()
    {
        var registry = new NationalIdRegistry();

        var detector = registry.GetDetector("KO");

        Assert.NotNull(detector);
    }

    #endregion
}
