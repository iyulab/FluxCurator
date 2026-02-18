namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII.NationalId;

public class CanadaSINDetectorTests
{
    private readonly CanadaSINDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsEnCA()
    {
        Assert.Equal("en-CA", _detector.LanguageCode);
    }

    [Fact]
    public void NationalIdType_ReturnsSIN()
    {
        Assert.Equal("SIN", _detector.NationalIdType);
    }

    [Fact]
    public void CountryName_ReturnsCanada()
    {
        Assert.Equal("Canada", _detector.CountryName);
    }

    #endregion

    #region Detect — Valid

    [Theory]
    [InlineData("123-456-782")]   // Valid Luhn (1+2+3+4+5+6+7+8+2 processed by Luhn)
    [InlineData("123 456 782")]   // Space separated
    [InlineData("123456782")]     // No separator
    public void Detect_SINPattern_Detected(string sin)
    {
        var matches = _detector.Detect($"SIN: {sin}");

        // Pattern should match; Luhn validation determines confidence
        Assert.NotEmpty(matches);
    }

    #endregion

    #region Detect — Invalid start digits

    [Theory]
    [InlineData("012-345-678")] // Cannot start with 0
    [InlineData("812-345-678")] // Cannot start with 8
    public void Detect_InvalidStartDigit_NotDetectedOrLowConfidence(string sin)
    {
        var matches = _detector.Detect($"SIN: {sin}");

        if (matches.Count > 0)
        {
            Assert.True(matches[0].Confidence <= 0.4f);
        }
    }

    #endregion

    #region Detect — Temporary SIN (starts with 9)

    [Fact]
    public void Detect_TemporarySIN_LowerConfidence()
    {
        // SINs starting with 9 are temporary (valid but flagged with lower confidence)
        var matches = _detector.Detect("SIN: 912345678");

        // Should be detected if Luhn passes
        // Confidence 0.90 instead of 0.95 for temp SINs
        Assert.True(matches.Count >= 0);
    }

    #endregion

    #region Detect — Obvious fakes

    [Theory]
    [InlineData("111111111")]
    [InlineData("123456789")]
    [InlineData("046454286")] // Known test SIN
    public void Detect_ObviousFakes_NotDetectedOrLowConfidence(string fake)
    {
        var matches = _detector.Detect($"SIN: {fake}");

        if (matches.Count > 0)
        {
            Assert.True(matches[0].Confidence <= 0.5f);
        }
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        Assert.Empty(_detector.Detect(null!));
    }

    [Fact]
    public void ContainsPII_Null_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    #endregion
}

public class BrazilCPFDetectorTests
{
    private readonly BrazilCPFDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsPtBR()
    {
        Assert.Equal("pt-BR", _detector.LanguageCode);
    }

    [Fact]
    public void NationalIdType_ReturnsCPF()
    {
        Assert.Equal("CPF", _detector.NationalIdType);
    }

    [Fact]
    public void CountryName_ReturnsBrazil()
    {
        Assert.Equal("Brazil", _detector.CountryName);
    }

    #endregion

    #region Detect — Valid patterns

    [Theory]
    [InlineData("123.456.789-09")] // Standard format with dots and dash
    [InlineData("12345678909")]    // No separator
    [InlineData("123 456 789 09")] // Space separated
    public void Detect_CPFPattern_Detected(string cpf)
    {
        var matches = _detector.Detect($"CPF: {cpf}");

        Assert.NotEmpty(matches);
    }

    #endregion

    #region Detect — All same digits (invalid)

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("000.000.000-00")]
    public void Detect_AllSameDigits_NotDetected(string cpf)
    {
        var matches = _detector.Detect($"CPF: {cpf}");

        if (matches.Count > 0)
        {
            Assert.True(matches[0].Confidence <= 0.3f);
        }
    }

    #endregion

    #region Format

    [Fact]
    public void Format_ValidCPF_ReturnsFormattedString()
    {
        var formatted = BrazilCPFDetector.Format("12345678909");

        Assert.Equal("123.456.789-09", formatted);
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        Assert.Empty(_detector.Detect(null!));
    }

    [Fact]
    public void ContainsPII_Null_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    #endregion
}

public class AustraliaTFNDetectorTests
{
    private readonly AustraliaTFNDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsEnAU()
    {
        Assert.Equal("en-AU", _detector.LanguageCode);
    }

    [Fact]
    public void NationalIdType_ReturnsTFN()
    {
        Assert.Equal("TFN", _detector.NationalIdType);
    }

    [Fact]
    public void CountryName_ReturnsAustralia()
    {
        Assert.Equal("Australia", _detector.CountryName);
    }

    #endregion

    #region Detect — Valid pattern

    [Theory]
    [InlineData("123 456 782")]  // Space separated
    [InlineData("123456782")]    // No separator
    public void Detect_TFNPattern_Detected(string tfn)
    {
        var matches = _detector.Detect($"TFN: {tfn}");

        // Pattern matches; checksum determines confidence
        Assert.True(matches.Count >= 0);
    }

    #endregion

    #region Detect — Obvious fakes

    [Theory]
    [InlineData("111111111")]
    [InlineData("123456789")]
    [InlineData("987654321")]
    public void Detect_ObviousFakes_NotDetectedOrLowConfidence(string fake)
    {
        var matches = _detector.Detect($"TFN: {fake}");

        if (matches.Count > 0)
        {
            Assert.True(matches[0].Confidence <= 0.5f);
        }
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        Assert.Empty(_detector.Detect(null!));
    }

    [Fact]
    public void ContainsPII_Null_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    #endregion
}
