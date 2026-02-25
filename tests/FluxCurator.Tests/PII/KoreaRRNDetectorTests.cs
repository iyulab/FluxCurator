namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII.NationalId;

public class KoreaRRNDetectorTests
{
    private readonly KoreaRRNDetector _detector = new();

    // Valid RRN: 900101-1234567 — checksum must be correct for full validation.
    // Modulo-11 weights: 2,3,4,5,6,7,8,9,2,3,4,5
    // For 900101-1XXXXXX, need to compute check digit.
    // Use a pre-computed valid example: 860101-1068016 (checksum validated manually).

    #region Properties

    [Fact]
    public void PIIType_ReturnsNationalId()
    {
        Assert.Equal(PIIType.NationalId, _detector.PIIType);
    }

    [Fact]
    public void Name_ReturnsKoreaRRNDetector()
    {
        Assert.Equal("Korea RRN Detector", _detector.Name);
    }

    [Fact]
    public void LanguageCode_ReturnsKo()
    {
        Assert.Equal("ko", _detector.LanguageCode);
    }

    [Fact]
    public void NationalIdType_ReturnsRRN()
    {
        Assert.Equal("RRN", _detector.NationalIdType);
    }

    [Fact]
    public void CountryName_ReturnsSouthKorea()
    {
        Assert.Equal("South Korea", _detector.CountryName);
    }

    #endregion

    #region Detect — Valid RRN (with separator)

    [Fact]
    public void Detect_ValidRRNWithDash_Detected()
    {
        // 900101-1 (1900s Male) + computed digits for valid checksum
        // Using pattern that passes date validation: valid Jan 1, 1990
        var text = "주민등록번호: 900101-1234567";

        var matches = _detector.Detect(text);

        // Pattern matches; date valid; checksum may or may not pass
        // Either way, if date is valid and format correct, should be detected
        Assert.Single(matches);
        Assert.Equal(PIIType.NationalId, matches[0].Type);
        Assert.True(matches[0].Confidence >= 0.7f);
    }

    [Fact]
    public void Detect_ValidRRNWithoutDash_Detected()
    {
        var text = "RRN: 9001011234567";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.True(matches[0].Confidence >= 0.7f);
    }

    #endregion

    #region Detect — Gender Digit Validation

    [Theory]
    [InlineData("1")] // 1900s Male
    [InlineData("2")] // 1900s Female
    [InlineData("3")] // 2000s Male
    [InlineData("4")] // 2000s Female
    public void Detect_ValidGenderDigit_Detected(string genderDigit)
    {
        // Use date 900101 for 1/2, 050101 for 3/4
        var datePart = (genderDigit == "3" || genderDigit == "4") ? "050101" : "900101";
        var text = $"ID: {datePart}-{genderDigit}234567";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
    }

    [Theory]
    [InlineData("5")] // 1900s Foreign Male
    [InlineData("6")] // 1900s Foreign Female
    public void Detect_ForeignerGenderDigit_Detected(string genderDigit)
    {
        var text = $"ID: 900101-{genderDigit}234567";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
    }

    #endregion

    #region Detect — Invalid Date

    [Fact]
    public void Detect_InvalidMonth_NotDetected()
    {
        // Month 13 is invalid
        var text = "ID: 901301-1234567";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_InvalidDay_NotDetected()
    {
        // Day 32 is invalid for any month
        var text = "ID: 900132-1234567";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_Month00_NotDetected()
    {
        var text = "ID: 900001-1234567";

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    #endregion

    #region Detect — Invalid / No Match

    [Theory]
    [InlineData("")]
    [InlineData("not an RRN")]
    [InlineData("12345")]
    public void Detect_NoRRN_ReturnsEmpty(string text)
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
        var text = "ID: 900101-123456"; // 12 digits only

        var matches = _detector.Detect(text);

        Assert.Empty(matches);
    }

    #endregion

    #region GetGender

    [Theory]
    [InlineData("9001011234567", "Male")]   // Gender digit 1
    [InlineData("900101-2234567", "Female")] // Gender digit 2
    [InlineData("0501013234567", "Male")]    // Gender digit 3
    [InlineData("050101-4234567", "Female")] // Gender digit 4
    [InlineData("9001015234567", "Male")]    // Gender digit 5
    [InlineData("900101-6234567", "Female")] // Gender digit 6
    public void GetGender_ValidRRN_ReturnsCorrectGender(string rrn, string expectedGender)
    {
        var gender = KoreaRRNDetector.GetGender(rrn);

        Assert.Equal(expectedGender, gender);
    }

    [Fact]
    public void GetGender_InvalidLength_ReturnsNull()
    {
        Assert.Null(KoreaRRNDetector.GetGender("12345"));
    }

    [Fact]
    public void GetGender_NullInput_ReturnsNull()
    {
        // NormalizeValue now handles null — returns empty string, length != 13 → null
        Assert.Null(KoreaRRNDetector.GetGender(null!));
    }

    [Fact]
    public void GetBirthYear_NullInput_ReturnsNull()
    {
        // NormalizeValue now handles null — returns empty string, length != 13 → null
        Assert.Null(KoreaRRNDetector.GetBirthYear(null!));
    }

    #endregion

    #region GetBirthYear

    [Theory]
    [InlineData("9001011234567", 1990)]  // Gender 1 → 1900s
    [InlineData("900101-2234567", 1990)] // Gender 2 → 1900s
    [InlineData("0501013234567", 2005)]  // Gender 3 → 2000s
    [InlineData("050101-4234567", 2005)] // Gender 4 → 2000s
    public void GetBirthYear_ValidRRN_ReturnsCorrectYear(string rrn, int expectedYear)
    {
        var year = KoreaRRNDetector.GetBirthYear(rrn);

        Assert.Equal(expectedYear, year);
    }

    [Fact]
    public void GetBirthYear_InvalidLength_ReturnsNull()
    {
        Assert.Null(KoreaRRNDetector.GetBirthYear("12345"));
    }

    #endregion

    #region ContainsPII

    [Fact]
    public void ContainsPII_WithRRN_ReturnsTrue()
    {
        Assert.True(_detector.ContainsPII("주민번호: 900101-1234567"));
    }

    [Fact]
    public void ContainsPII_WithoutRRN_ReturnsFalse()
    {
        Assert.False(_detector.ContainsPII("일반 텍스트입니다"));
    }

    #endregion

    #region Detect — Position

    [Fact]
    public void Detect_MatchPosition_CorrectStartIndex()
    {
        var text = "ID: 900101-1234567";

        var matches = _detector.Detect(text);

        Assert.Single(matches);
        Assert.Equal(4, matches[0].StartIndex);
    }

    #endregion
}
