namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII.NationalId;

public class USSSNDetectorTests
{
    private readonly USSSNDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void Name_ReturnsUSSSNDetector()
    {
        Assert.Equal("US SSN Detector", _detector.Name);
    }

    [Fact]
    public void LanguageCode_ReturnsEnUS()
    {
        Assert.Equal("en-US", _detector.LanguageCode);
    }

    [Fact]
    public void NationalIdType_ReturnsSSN()
    {
        Assert.Equal("SSN", _detector.NationalIdType);
    }

    [Fact]
    public void CountryName_ReturnsUnitedStates()
    {
        Assert.Equal("United States", _detector.CountryName);
    }

    #endregion

    #region Detect — Valid SSN

    [Theory]
    [InlineData("234-56-7890")]
    [InlineData("234 56 7890")]
    [InlineData("234567890")]
    public void Detect_ValidSSN_Detected(string ssn)
    {
        var text = $"SSN: {ssn}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(PIIType.NationalId, matches[0].Type);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    [Theory]
    [InlineData("001-01-0001")] // Area 001 — valid
    [InlineData("899-01-0001")] // Area 899 — valid (just below 900)
    [InlineData("500-50-5000")] // Mid-range
    public void Detect_ValidAreaRanges_Detected(string ssn)
    {
        var text = $"SSN: {ssn}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    #endregion

    #region Detect — Invalid Area Number

    [Fact]
    public void Detect_Area000_NotDetected()
    {
        var text = "SSN: 000-12-3456";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_Area666_NotDetected()
    {
        var text = "SSN: 666-12-3456";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Theory]
    [InlineData("900-12-3456")]
    [InlineData("999-12-3456")]
    public void Detect_Area900Plus_NotDetected(string ssn)
    {
        var text = $"SSN: {ssn}";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    #endregion

    #region Detect — Invalid Group/Serial

    [Fact]
    public void Detect_Group00_NotDetected()
    {
        var text = "SSN: 123-00-6789";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_Serial0000_NotDetected()
    {
        var text = "SSN: 123-45-0000";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    #endregion

    #region Detect — Test SSNs

    [Theory]
    [InlineData("078-05-1120")] // Woolworth wallet card SSN
    [InlineData("219-09-9999")] // Known test SSN
    [InlineData("457-55-5462")] // Apple's test SSN
    public void Detect_TestSSN_NotDetected(string ssn)
    {
        var text = $"SSN: {ssn}";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    #endregion

    #region Detect — Obviously Fake

    [Fact]
    public void Detect_AllSameDigits_NotDetected()
    {
        var text = "SSN: 111-11-1111";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_SequentialAscending_NotDetected()
    {
        // 123456789 is in the obviously fake list
        var text = "SSN: 123-45-6789";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_SequentialDescending_NotDetected()
    {
        var text = "SSN: 987-65-4321";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    #endregion

    #region Detect — Invalid / No Match

    [Theory]
    [InlineData("")]
    [InlineData("not an SSN")]
    [InlineData("12345")]
    public void Detect_NoSSN_ReturnsEmpty(string text)
    {
        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_NullInput_ReturnsEmpty()
    {
        var matches = _detector.Detect(null!);

        Assert.Empty(matches);
    }

    #endregion

    #region ContainsPII

    [Fact]
    public void ContainsPII_WithSSN_ReturnsTrue()
    {
        Assert.True(_detector.ContainsPII("SSN: 234-56-7890"));
    }

    [Fact]
    public void ContainsPII_WithoutSSN_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII("No SSN here"));
    }

    [Fact]
    public void ContainsPII_NullInput_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    #endregion

    #region Detect — Position

    [Fact]
    public void Detect_MatchPosition_CorrectStartIndex()
    {
        var text = "SSN: 234-56-7890";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(5, matches[0].StartIndex);
    }

    #endregion

    #region Detect — Multiple SSNs

    [Fact]
    public void Detect_MultipleSSNs_ReturnsAll()
    {
        var text = "SSN1: 234-56-7890, SSN2: 345-67-8901";

        var matches = _detector.Detect(text);

        Assert.Equal(2, matches.Count);
    }

    #endregion
}
