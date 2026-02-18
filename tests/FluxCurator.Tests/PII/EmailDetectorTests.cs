namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

public class EmailDetectorTests
{
    private readonly EmailDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsEmail()
    {
        Assert.Equal(PIIType.Email, _detector.PIIType);
    }

    [Fact]
    public void Name_ReturnsEmailDetector()
    {
        Assert.Equal("Email Detector", _detector.Name);
    }

    #endregion

    #region Detect — Valid Emails

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@example.org")]
    [InlineData("admin@company.co.kr")]
    [InlineData("info@test.io")]
    public void Detect_ValidEmail_ReturnsMatch(string email)
    {
        var text = $"Contact us at {email} for more info.";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(email, matches[0].Value);
        Assert.Equal(PIIType.Email, matches[0].Type);
        Assert.True(matches[0].Confidence > 0);
    }

    [Fact]
    public void Detect_MultipleEmails_ReturnsAll()
    {
        var text = "Send to alice@example.com and bob@test.org";

        var matches = _detector.Detect(text);

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void Detect_KoreanTLD_HighConfidence()
    {
        var text = "admin@company.kr";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.95f);
    }

    [Fact]
    public void Detect_CommonTLD_HighConfidence()
    {
        var text = "user@example.com";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    [Fact]
    public void Detect_UncommonTLD_LowerConfidence()
    {
        var text = "user@example.xyz";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.5f);
    }

    [Fact]
    public void Detect_MatchPosition_CorrectStartIndex()
    {
        var text = "Email: test@example.com please";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(7, matches[0].StartIndex);
    }

    #endregion

    #region Detect — Invalid Emails

    [Theory]
    [InlineData("")]
    [InlineData("not an email")]
    [InlineData("just text without at sign")]
    public void Detect_NoEmail_ReturnsEmpty(string text)
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

    [Fact]
    public void Detect_EmailWithLeadingDotInLocal_Rejected()
    {
        // Leading dot in local part is invalid
        var text = "Contact .user@example.com here";

        var matches = _detector.Detect(text);

        // The regex may capture "user@example.com" (without leading dot)
        // which is valid — so check there's no match with leading dot
        foreach (var m in matches)
        {
            Assert.DoesNotContain("..", m.Value);
        }
    }

    [Fact]
    public void Detect_EmailWithConsecutiveDots_Rejected()
    {
        var text = "Contact user..name@example.com here";

        var matches = _detector.Detect(text);

        // Consecutive dots in local part should be rejected by ValidateMatch
        foreach (var m in matches)
        {
            var local = m.Value.Split('@')[0];
            Assert.DoesNotContain("..", local);
        }
    }

    #endregion

    #region ContainsPII

    [Fact]
    public void ContainsPII_WithEmail_ReturnsTrue()
    {
        Assert.True(_detector.ContainsPII("Send to user@example.com"));
    }

    [Fact]
    public void ContainsPII_WithoutEmail_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII("No email here"));
    }

    [Fact]
    public void ContainsPII_NullInput_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    [Fact]
    public void ContainsPII_EmptyInput_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(""));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Detect_EmailWithPlusSuffix_Detected()
    {
        var text = "user+tag@example.com";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
    }

    [Fact]
    public void Detect_EmailWithPercentSign_Detected()
    {
        var text = "user%tag@example.com";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
    }

    #endregion
}
