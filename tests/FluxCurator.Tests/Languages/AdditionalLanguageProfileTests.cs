namespace FluxCurator.Tests.Languages;

using global::FluxCurator.Core.Infrastructure.Languages;

#region Cross-Language Property Tests

public class AdditionalLanguageProfilePropertyTests
{
    [Theory]
    [InlineData(typeof(ChineseLanguageProfile), "zh", "Chinese")]
    [InlineData(typeof(JapaneseLanguageProfile), "ja", "Japanese")]
    [InlineData(typeof(SpanishLanguageProfile), "es", "Spanish")]
    [InlineData(typeof(FrenchLanguageProfile), "fr", "French")]
    [InlineData(typeof(GermanLanguageProfile), "de", "German")]
    [InlineData(typeof(ArabicLanguageProfile), "ar", "Arabic")]
    [InlineData(typeof(HindiLanguageProfile), "hi", "Hindi")]
    [InlineData(typeof(PortugueseLanguageProfile), "pt", "Portuguese")]
    [InlineData(typeof(VietnameseLanguageProfile), "vi", "Vietnamese")]
    [InlineData(typeof(ThaiLanguageProfile), "th", "Thai")]
    [InlineData(typeof(RussianLanguageProfile), "ru", "Russian")]
    public void Profile_LanguageCodeAndName_AreCorrect(Type profileType, string expectedCode, string expectedName)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.Equal(expectedCode, profile.LanguageCode);
        Assert.Equal(expectedName, profile.LanguageName);
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_SentenceEndPattern_IsNotEmpty(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.False(string.IsNullOrEmpty(profile.SentenceEndPattern));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_SectionMarkerPattern_IsNotEmpty(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.False(string.IsNullOrEmpty(profile.SectionMarkerPattern));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_Abbreviations_IsNotNull(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.NotNull(profile.Abbreviations);
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_FindSentenceBoundaries_NullInput_ReturnsEmpty(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.Empty(profile.FindSentenceBoundaries(null!));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_FindSentenceBoundaries_EmptyInput_ReturnsEmpty(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.Empty(profile.FindSentenceBoundaries(""));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_FindParagraphBoundaries_NullInput_ReturnsEmpty(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.Empty(profile.FindParagraphBoundaries(null!));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_FindSectionHeaders_NullInput_ReturnsEmpty(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.Empty(profile.FindSectionHeaders(null!));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_EstimateTokenCount_NullInput_ReturnsZero(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.Equal(0, profile.EstimateTokenCount(null!));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_EstimateTokenCount_EmptyInput_ReturnsZero(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;

        Assert.Equal(0, profile.EstimateTokenCount(""));
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_FindSentenceBoundaries_SingleSentence_EndsAtTextLength(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;
        var text = "No sentence ending punctuation here";

        var boundaries = profile.FindSentenceBoundaries(text);

        Assert.Contains(text.Length, boundaries);
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_FindParagraphBoundaries_MultipleParagraphs_FindsBoundaries(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;
        var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

        var boundaries = profile.FindParagraphBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Theory]
    [InlineData(typeof(ChineseLanguageProfile))]
    [InlineData(typeof(JapaneseLanguageProfile))]
    [InlineData(typeof(SpanishLanguageProfile))]
    [InlineData(typeof(FrenchLanguageProfile))]
    [InlineData(typeof(GermanLanguageProfile))]
    [InlineData(typeof(ArabicLanguageProfile))]
    [InlineData(typeof(HindiLanguageProfile))]
    [InlineData(typeof(PortugueseLanguageProfile))]
    [InlineData(typeof(VietnameseLanguageProfile))]
    [InlineData(typeof(ThaiLanguageProfile))]
    [InlineData(typeof(RussianLanguageProfile))]
    public void Profile_FindSectionHeaders_MarkdownHeaders_Detected(Type profileType)
    {
        var profile = (LanguageProfileBase)Activator.CreateInstance(profileType)!;
        var text = "# Title\nContent here\n## Subtitle\nMore content";

        var headers = profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 1);
    }
}

#endregion

#region Chinese Profile Tests

public class ChineseLanguageProfileTests
{
    private readonly ChineseLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_ChinesePunctuation_DetectsBoundaries()
    {
        var text = "你好世界。欢迎来到这里！这是一个测试？";

        var boundaries = _profile.FindSentenceBoundaries(text);

        // Should detect 。！？ as sentence endings
        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_ChineseSemicolon_DetectsBoundary()
    {
        var text = "第一部分；第二部分。";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 1);
    }

    [Fact]
    public void FindSectionHeaders_ChineseChapterMarkers_Detected()
    {
        var text = "第一章 序论\n内容\n第二章 方法\n更多内容";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_ChineseNumberList_Detected()
    {
        var text = "一、第一点\n内容\n二、第二点\n更多内容";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_ParenthesizedChineseNumbers_Detected()
    {
        var text = "（一）概述\n内容\n（二）背景\n更多内容";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_ChineseText_UsesCorrectRatio()
    {
        // Chinese: 1.5 chars per token
        // 15 chars / 1.5 = 10 tokens
        var text = "你好世界欢迎来到这里测试测试测";

        var count = _profile.EstimateTokenCount(text);

        Assert.Equal(10, count);
    }

    [Fact]
    public void Abbreviations_ContainsChineseAbbreviations()
    {
        Assert.Contains("等", _profile.Abbreviations);
        Assert.Contains("如", _profile.Abbreviations);
        Assert.Contains("即", _profile.Abbreviations);
        Assert.Contains("例", _profile.Abbreviations);
    }
}

#endregion

#region Japanese Profile Tests

public class JapaneseLanguageProfileTests
{
    private readonly JapaneseLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_JapanesePunctuation_DetectsBoundaries()
    {
        var text = "こんにちは。お元気ですか？はい、元気です！";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_PoliteEndings_DetectsBoundaries()
    {
        var text = "これはテストです。次の文もテストです。";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 1);
    }

    [Fact]
    public void FindSectionHeaders_JapaneseChapterMarkers_Detected()
    {
        var text = "第一章 序論\n内容\n第二章 方法\n更多的内容";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_JapaneseNumberList_Detected()
    {
        var text = "一、はじめに\n内容\n二、方法\n更なる内容";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_JapaneseText_UsesCorrectRatio()
    {
        // Japanese: 1.5 chars per token
        // 6 chars / 1.5 = 4 tokens
        var text = "日本語テスト文";

        var count = _profile.EstimateTokenCount(text);

        Assert.True(count >= 4);
    }

    [Fact]
    public void Abbreviations_ContainsJapaneseAbbreviations()
    {
        Assert.Contains("等", _profile.Abbreviations);
        Assert.Contains("例", _profile.Abbreviations);
        Assert.Contains("即", _profile.Abbreviations);
    }
}

#endregion

#region Spanish Profile Tests

public class SpanishLanguageProfileTests
{
    private readonly SpanishLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_SpanishPunctuation_DetectsBoundaries()
    {
        var text = "Hola mundo. Bienvenido aquí. Esto es una prueba.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_InvertedPunctuation_DetectsBoundaries()
    {
        var text = "¿Cómo estás? ¡Muy bien! Gracias.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_SpanishNamedSections_Detected()
    {
        var text = "Capítulo 1\nContenido\nSección 2\nMás contenido";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_BulletPoints_Detected()
    {
        var text = "- Primer punto\n- Segundo punto\n- Tercer punto";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_SpanishText_UsesCorrectRatio()
    {
        // Spanish: 4.5 chars per token
        // "Hola mundo" = 10 chars / 4.5 ≈ 3 tokens (Ceiling)
        var count = _profile.EstimateTokenCount("Hola mundo");

        Assert.True(count >= 2 && count <= 4);
    }

    [Fact]
    public void Abbreviations_ContainsSpanishTitles()
    {
        Assert.Contains("Dr.", _profile.Abbreviations);
        Assert.Contains("Dra.", _profile.Abbreviations);
        Assert.Contains("Sr.", _profile.Abbreviations);
        Assert.Contains("Sra.", _profile.Abbreviations);
        Assert.Contains("Prof.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsSpanishCommonAbbreviations()
    {
        Assert.Contains("etc.", _profile.Abbreviations);
        Assert.Contains("Av.", _profile.Abbreviations);
        Assert.Contains("S.A.", _profile.Abbreviations);
    }
}

#endregion

#region French Profile Tests

public class FrenchLanguageProfileTests
{
    private readonly FrenchLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_FrenchPunctuation_DetectsBoundaries()
    {
        var text = "Bonjour le monde. Bienvenue ici. Ceci est un test.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_FrenchQuestionExclamation_DetectsBoundaries()
    {
        var text = "Comment allez-vous ? Très bien ! Merci.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_FrenchNamedSections_Detected()
    {
        var text = "Chapitre 1\nContenu\nSection 2\nPlus de contenu";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_LetterSections_Detected()
    {
        var text = "a. Premier point\nb. Deuxième point\nc. Troisième point";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_FrenchText_UsesCorrectRatio()
    {
        // French: 4.5 chars per token
        // "Bonjour" = 7 chars / 4.5 ≈ 2 tokens
        var count = _profile.EstimateTokenCount("Bonjour");

        Assert.Equal(2, count);
    }

    [Fact]
    public void Abbreviations_ContainsFrenchTitles()
    {
        Assert.Contains("Dr.", _profile.Abbreviations);
        Assert.Contains("M.", _profile.Abbreviations);
        Assert.Contains("Mme.", _profile.Abbreviations);
        Assert.Contains("Mlle.", _profile.Abbreviations);
        Assert.Contains("Prof.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsFrenchCommonAbbreviations()
    {
        Assert.Contains("etc.", _profile.Abbreviations);
        Assert.Contains("cf.", _profile.Abbreviations);
        Assert.Contains("S.A.", _profile.Abbreviations);
    }
}

#endregion

#region German Profile Tests

public class GermanLanguageProfileTests
{
    private readonly GermanLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_GermanPunctuation_DetectsBoundaries()
    {
        var text = "Hallo Welt. Willkommen hier. Dies ist ein Test.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_GermanExclamationQuestion_DetectsBoundaries()
    {
        var text = "Wie geht es Ihnen? Sehr gut! Danke.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_GermanNamedSections_Detected()
    {
        var text = "Kapitel 1\nInhalt\nAbschnitt 2\nMehr Inhalt";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_GermanText_UsesCorrectRatio()
    {
        // German: 5.0 chars per token (compound words)
        // "Hallo" = 5 chars / 5.0 = 1 token
        var count = _profile.EstimateTokenCount("Hallo");

        Assert.Equal(1, count);
    }

    [Fact]
    public void EstimateTokenCount_GermanCompoundWord_HigherTokenCount()
    {
        // "Geschwindigkeitsbegrenzung" = 26 chars / 5.0 = 6 tokens (Ceiling)
        var count = _profile.EstimateTokenCount("Geschwindigkeitsbegrenzung");

        Assert.True(count >= 5 && count <= 7);
    }

    [Fact]
    public void Abbreviations_ContainsGermanTitles()
    {
        Assert.Contains("Dr.", _profile.Abbreviations);
        Assert.Contains("Hr.", _profile.Abbreviations);
        Assert.Contains("Fr.", _profile.Abbreviations);
        Assert.Contains("Prof.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsGermanCommonAbbreviations()
    {
        Assert.Contains("z.B.", _profile.Abbreviations);
        Assert.Contains("d.h.", _profile.Abbreviations);
        Assert.Contains("usw.", _profile.Abbreviations);
        Assert.Contains("etc.", _profile.Abbreviations);
        Assert.Contains("GmbH.", _profile.Abbreviations);
    }
}

#endregion

#region Arabic Profile Tests

public class ArabicLanguageProfileTests
{
    private readonly ArabicLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_ArabicPunctuation_DetectsBoundaries()
    {
        var text = "مرحبا بالعالم. أهلا وسهلا. هذا اختبار.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_ArabicQuestionMark_DetectsBoundary()
    {
        var text = "كيف حالك؟ أنا بخير.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 1);
    }

    [Fact]
    public void FindSectionHeaders_NumberedSections_Detected()
    {
        var text = "1. المقدمة\nالمحتوى\n2. المنهجية\nمزيد من المحتوى";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_ArabicText_UsesCorrectRatio()
    {
        // Arabic: 3.0 chars per token
        // 9 chars / 3.0 = 3 tokens
        var text = "مرحبا بالع";

        var count = _profile.EstimateTokenCount(text);

        Assert.True(count >= 3);
    }

    [Fact]
    public void Abbreviations_IsEmpty()
    {
        Assert.Empty(_profile.Abbreviations);
    }
}

#endregion

#region Hindi Profile Tests

public class HindiLanguageProfileTests
{
    private readonly HindiLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_HindiDanda_DetectsBoundaries()
    {
        // Devanagari Danda (।)
        var text = "नमस्ते दुनिया। स्वागत है। यह एक परीक्षा है।";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_HindiPurnaViram_DetectsBoundary()
    {
        // Using the Devanagari danda (।) — Hindi purna viram
        var text = "यह पहला वाक्य है। यह दूसरा है।";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 1);
    }

    [Fact]
    public void FindSentenceBoundaries_LatinPunctuation_AlsoDetected()
    {
        var text = "Hindi with Latin punctuation. Second sentence.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 1);
    }

    [Fact]
    public void FindSectionHeaders_NumberedSections_Detected()
    {
        var text = "1. परिचय\nसामग्री\n2. विधि\nअधिक सामग्री";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_HindiText_UsesCorrectRatio()
    {
        // Hindi: 3.0 chars per token
        // "नमस्ते" = 6 chars / 3.0 = 2 tokens
        var count = _profile.EstimateTokenCount("नमस्ते");

        Assert.Equal(2, count);
    }

    [Fact]
    public void Abbreviations_IsEmpty()
    {
        Assert.Empty(_profile.Abbreviations);
    }
}

#endregion

#region Portuguese Profile Tests

public class PortugueseLanguageProfileTests
{
    private readonly PortugueseLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_PortuguesePunctuation_DetectsBoundaries()
    {
        var text = "Olá mundo. Bem-vindo aqui. Isso é um teste.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_QuestionExclamation_DetectsBoundaries()
    {
        var text = "Como vai você? Muito bem! Obrigado.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_PortugueseNamedSections_Detected()
    {
        var text = "Capítulo 1\nConteúdo\nSeção 2\nMais conteúdo";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_BulletPoints_Detected()
    {
        var text = "- Primeiro ponto\n- Segundo ponto\n- Terceiro ponto";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_PortugueseText_UsesCorrectRatio()
    {
        // Portuguese: 4.5 chars per token
        // "Olá mundo" = 9 chars / 4.5 = 2 tokens
        var count = _profile.EstimateTokenCount("Olá mundo");

        Assert.Equal(2, count);
    }

    [Fact]
    public void Abbreviations_ContainsPortugueseTitles()
    {
        Assert.Contains("Dr.", _profile.Abbreviations);
        Assert.Contains("Dra.", _profile.Abbreviations);
        Assert.Contains("Sr.", _profile.Abbreviations);
        Assert.Contains("Sra.", _profile.Abbreviations);
        Assert.Contains("Prof.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsPortugueseCommonAbbreviations()
    {
        Assert.Contains("etc.", _profile.Abbreviations);
        Assert.Contains("Av.", _profile.Abbreviations);
        Assert.Contains("Ltda.", _profile.Abbreviations);
        Assert.Contains("S.A.", _profile.Abbreviations);
    }
}

#endregion

#region Vietnamese Profile Tests

public class VietnameseLanguageProfileTests
{
    private readonly VietnameseLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_VietnamesePunctuation_DetectsBoundaries()
    {
        var text = "Xin chào thế giới. Chào mừng bạn. Đây là bài kiểm tra.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_QuestionExclamation_DetectsBoundaries()
    {
        var text = "Bạn khỏe không? Rất tốt! Cảm ơn.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_VietnameseChapterMarkers_Detected()
    {
        var text = "Chương 1\nNội dung\nPhần 2\nThêm nội dung";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_VietnameseArticleMarkers_Detected()
    {
        var text = "Điều 1\nNội dung\nMục 2\nThêm nội dung";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_VietnameseText_UsesCorrectRatio()
    {
        // Vietnamese: 4.0 chars per token
        // "Xin chào" = 8 chars / 4.0 = 2 tokens
        var count = _profile.EstimateTokenCount("Xin chào");

        Assert.Equal(2, count);
    }

    [Fact]
    public void Abbreviations_ContainsVietnameseGeographic()
    {
        Assert.Contains("TP.", _profile.Abbreviations);
        Assert.Contains("Q.", _profile.Abbreviations);
        Assert.Contains("P.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsVietnameseAcademic()
    {
        Assert.Contains("Ths.", _profile.Abbreviations);
        Assert.Contains("TS.", _profile.Abbreviations);
        Assert.Contains("PGS.", _profile.Abbreviations);
        Assert.Contains("GS.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsVietnameseCommon()
    {
        Assert.Contains("v.v.", _profile.Abbreviations);
        Assert.Contains("NXB.", _profile.Abbreviations);
    }
}

#endregion

#region Thai Profile Tests

public class ThaiLanguageProfileTests
{
    private readonly ThaiLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_ThaiWithLatinPunctuation_DetectsBoundaries()
    {
        var text = "สวัสดีครับ. ยินดีต้อนรับ. นี่คือการทดสอบ.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_ThaiSectionMarkers_Detected()
    {
        var text = "บทที่ 1\nเนื้อหา\nบทที่ 2\nเนื้อหาเพิ่มเติม";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_ThaiClauseMarkers_Detected()
    {
        var text = "ข้อ 1\nเนื้อหา\nข้อ 2\nเนื้อหาเพิ่มเติม";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_ThaiNumerals_Detected()
    {
        var text = "๑. ข้อแรก\n๒. ข้อสอง\n๓. ข้อสาม";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_ThaiText_UsesCorrectRatio()
    {
        // Thai: 2.0 chars per token
        // "สวัสดี" = 6 chars / 2.0 = 3 tokens
        var count = _profile.EstimateTokenCount("สวัสดี");

        Assert.Equal(3, count);
    }

    [Fact]
    public void Abbreviations_IsEmpty()
    {
        Assert.Empty(_profile.Abbreviations);
    }
}

#endregion

#region Russian Profile Tests

public class RussianLanguageProfileTests
{
    private readonly RussianLanguageProfile _profile = new();

    [Fact]
    public void FindSentenceBoundaries_RussianPunctuation_DetectsBoundaries()
    {
        var text = "Привет мир. Добро пожаловать. Это тест.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSentenceBoundaries_RussianQuestionExclamation_DetectsBoundaries()
    {
        var text = "Как дела? Хорошо! Спасибо.";

        var boundaries = _profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_RussianNamedSections_Detected()
    {
        var text = "Глава 1\nСодержание\nРаздел 2\nБольше содержания";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void FindSectionHeaders_CyrillicLetterSections_Detected()
    {
        var text = "а. Первый пункт\nб. Второй пункт\nв. Третий пункт";

        var headers = _profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void EstimateTokenCount_RussianText_UsesCorrectRatio()
    {
        // Russian: 4.0 chars per token
        // "Привет" = 6 chars / 4.0 = 2 tokens (Ceiling)
        var count = _profile.EstimateTokenCount("Привет");

        Assert.Equal(2, count);
    }

    [Fact]
    public void Abbreviations_ContainsRussianCommonAbbreviations()
    {
        Assert.Contains("г.", _profile.Abbreviations);
        Assert.Contains("др.", _profile.Abbreviations);
        Assert.Contains("проф.", _profile.Abbreviations);
        Assert.Contains("т.д.", _profile.Abbreviations);
        Assert.Contains("т.е.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsRussianAddresses()
    {
        Assert.Contains("ул.", _profile.Abbreviations);
        Assert.Contains("д.", _profile.Abbreviations);
        Assert.Contains("корп.", _profile.Abbreviations);
    }

    [Fact]
    public void Abbreviations_ContainsRussianOrganizations()
    {
        Assert.Contains("ООО.", _profile.Abbreviations);
        Assert.Contains("ОАО.", _profile.Abbreviations);
        Assert.Contains("ЗАО.", _profile.Abbreviations);
    }
}

#endregion
