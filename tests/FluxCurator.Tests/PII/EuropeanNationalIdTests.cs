namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII.NationalId;

public class UKNINODetectorTests
{
    private readonly UKNINODetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsEnGB()
    {
        Assert.Equal("en-GB", _detector.LanguageCode);
    }

    [Fact]
    public void NationalIdType_ReturnsNINO()
    {
        Assert.Equal("NINO", _detector.NationalIdType);
    }

    [Fact]
    public void CountryName_ReturnsUnitedKingdom()
    {
        Assert.Equal("United Kingdom", _detector.CountryName);
    }

    #endregion

    #region Detect — Valid

    [Theory]
    [InlineData("AB123456C")]
    [InlineData("AB 12 34 56 C")]
    [InlineData("CE123456D")]
    public void Detect_ValidNINO_Detected(string nino)
    {
        var matches = _detector.Detect($"NINO: {nino}");

        Assert.NotEmpty(matches);
        Assert.True(matches[0].Confidence > 0.5f);
    }

    #endregion

    #region Detect — Invalid prefixes

    [Theory]
    [InlineData("DA123456C")] // D invalid first letter
    [InlineData("FA123456C")] // F invalid first letter
    [InlineData("QA123456C")] // Q invalid first letter
    [InlineData("AD123456C")] // D invalid second letter
    [InlineData("BG123456C")] // BG invalid combo
    [InlineData("GB123456C")] // GB invalid combo
    public void Detect_InvalidPrefix_NotDetectedOrLowConfidence(string nino)
    {
        var matches = _detector.Detect($"NINO: {nino}");

        // Invalid prefix should fail validation
        if (matches.Count > 0)
        {
            Assert.True(matches[0].Confidence < 0.9f);
        }
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        var matches = _detector.Detect(null!);
        Assert.Empty(matches);
    }

    [Fact]
    public void ContainsPII_Null_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII(null!));
    }

    #endregion
}

public class GermanyIdDetectorTests
{
    private readonly GermanyIdDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsDe()
    {
        Assert.Equal("de", _detector.LanguageCode);
    }

    #endregion

    #region Detect — Steuer-ID (11 digits)

    [Fact]
    public void Detect_SteuerIdPattern_Detected()
    {
        // 11-digit number, one digit repeating twice (frequency rule)
        var text = "Steuer-ID: 11234567890";
        var matches = _detector.Detect(text);

        Assert.NotEmpty(matches);
    }

    [Theory]
    [InlineData("00000000000")] // starts with 0
    public void Detect_InvalidSteuerIdPrefix_NotDetectedOrLow(string id)
    {
        var matches = _detector.Detect($"ID: {id}");

        if (matches.Count > 0)
        {
            Assert.True(matches[0].Confidence < 0.9f);
        }
    }

    #endregion

    #region Detect — Personalausweis (10 alphanumeric)

    [Fact]
    public void Detect_PersonalausweisPattern_Detected()
    {
        var text = "Ausweis: A123456780";
        var matches = _detector.Detect(text);

        Assert.NotEmpty(matches);
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        Assert.Empty(_detector.Detect(null!));
    }

    #endregion
}

public class FranceINSEEDetectorTests
{
    private readonly FranceINSEEDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsFr()
    {
        Assert.Equal("fr", _detector.LanguageCode);
    }

    #endregion

    #region Detect — Valid

    [Theory]
    [InlineData("1 85 01 75 012 123 45")]  // Male born Jan 1985 in Paris
    [InlineData("2 90 12 13 456 789 01")]  // Female born Dec 1990 in dept 13
    public void Detect_ValidINSEEPattern_Detected(string insee)
    {
        var matches = _detector.Detect($"Numéro: {insee}");

        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Detect_InvalidSexDigit_NotDetected()
    {
        // Must start with 1 or 2
        var matches = _detector.Detect("ID: 3 85 01 75 012 123 45");

        Assert.Empty(matches);
    }

    #endregion

    #region GetGender

    [Fact]
    public void GetGender_Male_ReturnsMale()
    {
        Assert.Equal("Male", FranceINSEEDetector.GetGender("185017501212345"));
    }

    [Fact]
    public void GetGender_Female_ReturnsFemale()
    {
        Assert.Equal("Female", FranceINSEEDetector.GetGender("285017501212345"));
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        Assert.Empty(_detector.Detect(null!));
    }

    #endregion
}

public class SpainDNIDetectorTests
{
    private readonly SpainDNIDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsEs()
    {
        Assert.Equal("es", _detector.LanguageCode);
    }

    #endregion

    #region Detect — DNI

    [Theory]
    [InlineData("12345678Z")] // 12345678 % 23 = 14 → 'Z'
    public void Detect_ValidDNI_Detected(string dni)
    {
        var matches = _detector.Detect($"DNI: {dni}");

        Assert.NotEmpty(matches);
        Assert.True(matches[0].Confidence >= 0.9f);
    }

    #endregion

    #region Detect — NIE

    [Theory]
    [InlineData("X1234567L")] // NIE starting with X (X→0, 01234567 % 23 = ?)
    [InlineData("Y1234567X")] // NIE starting with Y
    public void Detect_ValidNIE_Detected(string nie)
    {
        var matches = _detector.Detect($"NIE: {nie}");

        Assert.NotEmpty(matches);
    }

    #endregion

    #region GetIdType

    [Fact]
    public void GetIdType_DNI_ReturnsDNI()
    {
        Assert.Equal("DNI", SpainDNIDetector.GetIdType("12345678Z"));
    }

    [Fact]
    public void GetIdType_NIE_ReturnsNIE()
    {
        Assert.Equal("NIE", SpainDNIDetector.GetIdType("X1234567L"));
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        Assert.Empty(_detector.Detect(null!));
    }

    #endregion
}

public class ItalyCodiceFiscaleDetectorTests
{
    private readonly ItalyCodiceFiscaleDetector _detector = new();

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void LanguageCode_ReturnsIt()
    {
        Assert.Equal("it", _detector.LanguageCode);
    }

    #endregion

    #region Detect — Valid

    [Theory]
    [InlineData("RSSMRA85A01H501Z")] // Pattern-matching format
    [InlineData("BNCLRA92E45F205X")] // Another format
    public void Detect_ValidCodiceFiscale_Detected(string cf)
    {
        var matches = _detector.Detect($"CF: {cf}");

        Assert.NotEmpty(matches);
    }

    #endregion

    #region GetGender

    [Fact]
    public void GetGender_MaleDay_ReturnsMale()
    {
        // Day <= 31 = Male (position 9-10 in code)
        Assert.Equal("Male", ItalyCodiceFiscaleDetector.GetGender("RSSMRA85A01H501Z"));
    }

    [Fact]
    public void GetGender_FemaleDay_ReturnsFemale()
    {
        // Day > 40 = Female (e.g., 45 = day 5)
        Assert.Equal("Female", ItalyCodiceFiscaleDetector.GetGender("BNCLRA92E45F205X"));
    }

    #endregion

    #region Null handling

    [Fact]
    public void Detect_Null_ReturnsEmpty()
    {
        Assert.Empty(_detector.Detect(null!));
    }

    #endregion
}
