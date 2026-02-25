namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

public class PhoneDetectorTests
{
    private readonly PhoneDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsPhone()
    {
        Assert.Equal(PIIType.Phone, _detector.PIIType);
    }

    [Fact]
    public void Name_ReturnsPhoneDetector()
    {
        Assert.Equal("Phone Detector", _detector.Name);
    }

    #endregion

    #region Detect — Korean Mobile

    [Theory]
    [InlineData("010-1234-5678")]
    [InlineData("010.1234.5678")]
    [InlineData("01012345678")]
    [InlineData("010 1234 5678")]
    public void Detect_KoreanMobile010_Detected(string phone)
    {
        var text = $"Call me at {phone}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(PIIType.Phone, matches[0].Type);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    [Theory]
    [InlineData("011-123-4567")]
    [InlineData("016-123-4567")]
    [InlineData("019-123-4567")]
    public void Detect_KoreanMobileOther_Detected(string phone)
    {
        var text = $"Number: {phone}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    #endregion

    #region Detect — Korean Landline

    [Theory]
    [InlineData("02-1234-5678")]     // Seoul
    [InlineData("031-123-4567")]     // Gyeonggi
    [InlineData("051-123-4567")]     // Busan
    public void Detect_KoreanLandline_Detected(string phone)
    {
        var text = $"Office: {phone}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.85f);
    }

    #endregion

    #region Detect — Korean Special

    [Theory]
    [InlineData("1588-1234")]   // Customer service (8 digits)
    [InlineData("1544-9876")]   // Customer service (8 digits)
    [InlineData("1600-1234")]   // Customer service (8 digits)
    public void Detect_KoreanSpecial8Digit_Detected(string phone)
    {
        var text = $"Call {phone}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(PIIType.Phone, matches[0].Type);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    [Fact]
    public void Detect_KoreanTollFree080_DetectedAsGeneric()
    {
        // 080-1234-5678 = 11 digits after normalization
        // IsKoreanSpecial only accepts 8 or 12 digits, so falls through to generic
        var text = "Call 080-1234-5678";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.7f);
    }

    #endregion

    #region Detect — International

    [Theory]
    [InlineData("+82-10-1234-5678")]   // Korean international
    [InlineData("+1-234-567-8900")]    // US international
    [InlineData("+44-20-7946-0958")]   // UK international
    public void Detect_InternationalFormat_Detected(string phone)
    {
        var text = $"Call {phone}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.8f);
    }

    #endregion

    #region Detect — US Format

    [Theory]
    [InlineData("(234) 567-8900")]
    [InlineData("234-567-8900")]
    public void Detect_USFormat_Detected(string phone)
    {
        var text = $"Call {phone}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.7f);
    }

    #endregion

    #region Detect — Invalid

    [Theory]
    [InlineData("")]
    [InlineData("not a phone")]
    [InlineData("12345")]
    public void Detect_NoPhone_ReturnsEmpty(string text)
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
    public void ContainsPII_WithPhone_ReturnsTrue()
    {
        Assert.True(_detector.ContainsPII("Call 010-1234-5678"));
    }

    [Fact]
    public void ContainsPII_WithoutPhone_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII("No phone here"));
    }

    [Fact]
    public void ContainsPII_NullInput_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    #endregion

    #region Detect — Multiple Phones

    [Fact]
    public void Detect_MultiplePhones_ReturnsAll()
    {
        var text = "Call 010-1234-5678 or 02-1234-5678";

        var matches = _detector.Detect(text);

        Assert.True(matches.Count >= 2);
    }

    #endregion

    #region Detect — Position

    [Fact]
    public void Detect_MatchPosition_CorrectStartIndex()
    {
        var text = "Tel: 010-1234-5678";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(5, matches[0].StartIndex);
    }

    #endregion
}
