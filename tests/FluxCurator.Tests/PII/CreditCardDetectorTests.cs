namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

public class CreditCardDetectorTests
{
    private readonly CreditCardDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsCreditCard()
    {
        Assert.Equal(PIIType.CreditCard, _detector.PIIType);
    }

    [Fact]
    public void Name_ReturnsCreditCardDetector()
    {
        Assert.Equal("Credit Card Detector", _detector.Name);
    }

    #endregion

    #region Detect — Visa

    [Theory]
    [InlineData("4111111111111111")]      // Visa test number (Luhn-valid)
    [InlineData("4111-1111-1111-1111")]   // Visa with dashes
    [InlineData("4111 1111 1111 1111")]   // Visa with spaces
    public void Detect_Visa_Detected(string cardNumber)
    {
        var text = $"Card: {cardNumber}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(PIIType.CreditCard, matches[0].Type);
        Assert.True(matches[0].Confidence >= 0.95f);
    }

    #endregion

    #region Detect — Mastercard

    [Theory]
    [InlineData("5500000000000004")]  // Mastercard test (prefix 55)
    [InlineData("5105105105105100")]  // Mastercard test (prefix 51)
    public void Detect_Mastercard_Detected(string cardNumber)
    {
        var text = $"Pay with {cardNumber}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    #endregion

    #region Detect — Amex

    [Theory]
    [InlineData("378282246310005")]   // Amex test number (15 digits)
    [InlineData("371449635398431")]   // Amex test number
    public void Detect_Amex_Detected(string cardNumber)
    {
        var text = $"Amex: {cardNumber}";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.95f);
    }

    #endregion

    #region Detect — Discover

    [Fact]
    public void Detect_Discover_Detected()
    {
        // Discover test number: 6011111111111117 (Luhn-valid)
        var text = "Card: 6011111111111117";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    #endregion

    #region Detect — Invalid

    [Theory]
    [InlineData("")]
    [InlineData("not a card")]
    [InlineData("12345")]
    public void Detect_NoCard_ReturnsEmpty(string text)
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
    public void Detect_TooShort_NoMatch()
    {
        var text = "Number: 123456789012"; // 12 digits — too short

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    #endregion

    #region Detect — Luhn Validation

    [Fact]
    public void Detect_LuhnValid_HighConfidence()
    {
        // 4111111111111111 passes Luhn
        var text = "Card 4111111111111111";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.95f);
    }

    [Fact]
    public void Detect_LuhnInvalid_StillDetectedButLowerConfidence()
    {
        // 4111111111111112 — Visa prefix but fails Luhn
        var text = "Card 4111111111111112";

        var matches = _detector.Detect(text);

        // Still detected (valid prefix) but with lower confidence
        Assert.Single(matches);
        Assert.True(matches[0].Confidence <= 0.7f);
    }

    #endregion

    #region ContainsPII

    [Fact]
    public void ContainsPII_WithCard_ReturnsTrue()
    {
        Assert.True(_detector.ContainsPII("Card: 4111111111111111"));
    }

    [Fact]
    public void ContainsPII_WithoutCard_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII("No card numbers here"));
    }

    [Fact]
    public void ContainsPII_NullInput_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    #endregion

    #region Detect — Separator Handling

    [Fact]
    public void Detect_DashSeparated_Detected()
    {
        var text = "Card: 4111-1111-1111-1111";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
    }

    [Fact]
    public void Detect_SpaceSeparated_Detected()
    {
        var text = "Card: 4111 1111 1111 1111";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
    }

    #endregion

    #region Detect — Multiple Cards

    [Fact]
    public void Detect_MultipleCards_ReturnsAll()
    {
        var text = "Cards: 4111111111111111 and 5500000000000004";

        var matches = _detector.Detect(text);

        Assert.Equal(2, matches.Count);
    }

    #endregion
}
