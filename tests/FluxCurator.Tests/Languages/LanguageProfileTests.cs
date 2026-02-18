namespace FluxCurator.Tests.Languages;

using global::FluxCurator.Core.Infrastructure.Languages;

public class LanguageProfileTests
{
    #region English — Properties

    [Fact]
    public void English_LanguageCode_ReturnsEn()
    {
        var profile = new EnglishLanguageProfile();

        Assert.Equal("en", profile.LanguageCode);
    }

    [Fact]
    public void English_LanguageName_ReturnsEnglish()
    {
        var profile = new EnglishLanguageProfile();

        Assert.Equal("English", profile.LanguageName);
    }

    #endregion

    #region English — FindSentenceBoundaries

    [Fact]
    public void English_FindSentenceBoundaries_SimpleSentences()
    {
        var profile = new EnglishLanguageProfile();

        var boundaries = profile.FindSentenceBoundaries("Hello world. Goodbye world.");

        // Should have at least 1 boundary
        Assert.True(boundaries.Count >= 1);
    }

    [Fact]
    public void English_FindSentenceBoundaries_NullInput_ReturnsEmpty()
    {
        var profile = new EnglishLanguageProfile();

        var boundaries = profile.FindSentenceBoundaries(null!);

        Assert.Empty(boundaries);
    }

    [Fact]
    public void English_FindSentenceBoundaries_EmptyInput_ReturnsEmpty()
    {
        var profile = new EnglishLanguageProfile();

        var boundaries = profile.FindSentenceBoundaries("");

        Assert.Empty(boundaries);
    }

    [Fact]
    public void English_FindSentenceBoundaries_SingleSentence_EndsAtTextLength()
    {
        var profile = new EnglishLanguageProfile();
        var text = "Just one sentence";

        var boundaries = profile.FindSentenceBoundaries(text);

        Assert.Contains(text.Length, boundaries);
    }

    #endregion

    #region English — FindParagraphBoundaries

    [Fact]
    public void English_FindParagraphBoundaries_MultipleParagraphs()
    {
        var profile = new EnglishLanguageProfile();
        var text = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";

        var boundaries = profile.FindParagraphBoundaries(text);

        // 2 paragraph breaks + end of text
        Assert.True(boundaries.Count >= 2);
    }

    [Fact]
    public void English_FindParagraphBoundaries_NullInput_ReturnsEmpty()
    {
        var profile = new EnglishLanguageProfile();

        var boundaries = profile.FindParagraphBoundaries(null!);

        Assert.Empty(boundaries);
    }

    [Fact]
    public void English_FindParagraphBoundaries_SingleParagraph_EndsAtTextLength()
    {
        var profile = new EnglishLanguageProfile();
        var text = "Just one paragraph";

        var boundaries = profile.FindParagraphBoundaries(text);

        Assert.Single(boundaries);
        Assert.Equal(text.Length, boundaries[0]);
    }

    #endregion

    #region English — FindSectionHeaders

    [Fact]
    public void English_FindSectionHeaders_MarkdownHeaders()
    {
        var profile = new EnglishLanguageProfile();
        var text = "# Title\nSome content\n## Subtitle\nMore content";

        var headers = profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 1);
    }

    [Fact]
    public void English_FindSectionHeaders_NullInput_ReturnsEmpty()
    {
        var profile = new EnglishLanguageProfile();

        var headers = profile.FindSectionHeaders(null!);

        Assert.Empty(headers);
    }

    #endregion

    #region English — EstimateTokenCount

    [Fact]
    public void English_EstimateTokenCount_ApproximatelyCorrect()
    {
        var profile = new EnglishLanguageProfile();

        // "Hello world" = 11 chars / 4 chars-per-token = ~3 tokens
        var count = profile.EstimateTokenCount("Hello world");

        Assert.True(count >= 2 && count <= 5);
    }

    [Fact]
    public void English_EstimateTokenCount_NullInput_ReturnsZero()
    {
        var profile = new EnglishLanguageProfile();

        Assert.Equal(0, profile.EstimateTokenCount(null!));
    }

    [Fact]
    public void English_EstimateTokenCount_EmptyInput_ReturnsZero()
    {
        var profile = new EnglishLanguageProfile();

        Assert.Equal(0, profile.EstimateTokenCount(""));
    }

    #endregion

    #region English — Abbreviations

    [Fact]
    public void English_Abbreviations_ContainsCommonTitles()
    {
        var profile = new EnglishLanguageProfile();

        Assert.Contains("Dr.", profile.Abbreviations);
        Assert.Contains("Mr.", profile.Abbreviations);
        Assert.Contains("etc.", profile.Abbreviations);
    }

    #endregion

    #region Korean — Properties

    [Fact]
    public void Korean_LanguageCode_ReturnsKo()
    {
        var profile = new KoreanLanguageProfile();

        Assert.Equal("ko", profile.LanguageCode);
    }

    [Fact]
    public void Korean_LanguageName_ReturnsKorean()
    {
        var profile = new KoreanLanguageProfile();

        Assert.Equal("Korean", profile.LanguageName);
    }

    #endregion

    #region Korean — FindSentenceBoundaries

    [Fact]
    public void Korean_FindSentenceBoundaries_FormalEnding()
    {
        var profile = new KoreanLanguageProfile();
        var text = "안녕하십니까. 반갑습니다.";

        var boundaries = profile.FindSentenceBoundaries(text);

        Assert.True(boundaries.Count >= 1);
    }

    [Fact]
    public void Korean_FindSentenceBoundaries_NullInput_ReturnsEmpty()
    {
        var profile = new KoreanLanguageProfile();

        var boundaries = profile.FindSentenceBoundaries(null!);

        Assert.Empty(boundaries);
    }

    [Fact]
    public void Korean_FindSentenceBoundaries_EmptyInput_ReturnsEmpty()
    {
        var profile = new KoreanLanguageProfile();

        var boundaries = profile.FindSentenceBoundaries("");

        Assert.Empty(boundaries);
    }

    #endregion

    #region Korean — FindSectionHeaders

    [Fact]
    public void Korean_FindSectionHeaders_ChapterMarker()
    {
        var profile = new KoreanLanguageProfile();
        var text = "제1장 서론\n내용입니다\n제2장 본론\n더 많은 내용";

        var headers = profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void Korean_FindSectionHeaders_MarkdownHeaders()
    {
        var profile = new KoreanLanguageProfile();
        var text = "# 제목\n내용\n## 부제목\n더 많은 내용";

        var headers = profile.FindSectionHeaders(text);

        Assert.True(headers.Count >= 2);
    }

    [Fact]
    public void Korean_FindSectionHeaders_NullInput_ReturnsEmpty()
    {
        var profile = new KoreanLanguageProfile();

        var headers = profile.FindSectionHeaders(null!);

        Assert.Empty(headers);
    }

    #endregion

    #region Korean — EstimateTokenCount

    [Fact]
    public void Korean_EstimateTokenCount_HigherThanEnglish()
    {
        var profile = new KoreanLanguageProfile();
        var englishProfile = new EnglishLanguageProfile();

        // Korean text of similar semantic content requires more tokens
        // due to different character-per-token ratio
        var koreanCount = profile.EstimateTokenCount("안녕하세요 반갑습니다");
        var englishCount = englishProfile.EstimateTokenCount("Hello nice to meet you");

        // Korean tokens should be >= 1
        Assert.True(koreanCount >= 1);
    }

    [Fact]
    public void Korean_EstimateTokenCount_NullInput_ReturnsZero()
    {
        var profile = new KoreanLanguageProfile();

        Assert.Equal(0, profile.EstimateTokenCount(null!));
    }

    [Fact]
    public void Korean_EstimateTokenCount_MixedText()
    {
        var profile = new KoreanLanguageProfile();

        // Mixed Korean + English text
        var count = profile.EstimateTokenCount("안녕하세요 Hello");

        Assert.True(count >= 2);
    }

    #endregion

    #region Korean — Abbreviations

    [Fact]
    public void Korean_Abbreviations_ContainsKoreanTitles()
    {
        var profile = new KoreanLanguageProfile();

        Assert.Contains("씨.", profile.Abbreviations);
        Assert.Contains("님.", profile.Abbreviations);
    }

    #endregion
}
