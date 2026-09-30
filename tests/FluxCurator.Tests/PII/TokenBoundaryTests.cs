namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

/// <summary>
/// A PII value is a whole token: a detector must not report a slice of a longer run of letters or digits
/// (an identifier, a hash, a timestamp), while text in scripts without spaces glued to the value
/// (e.g. Hangul particles) must not stop a real value from matching.
/// </summary>
public class TokenBoundaryTests
{
    private static PIIMasker AllTypesAllCountries(float minConfidence = 0.8f) => new(new PIIMaskingOptions
    {
        LanguageCodes = ["auto"],
        TypesToMask = PIIType.All,
        Strategy = MaskingStrategy.Token,
        MinConfidence = minConfidence,
    });

    private static string Describe(IReadOnlyList<PIIMatch> matches) =>
        string.Join(", ", matches.Select(m => $"{m.Type}:{m.Value} ({m.Confidence:0.00})"));

    #region Slices of longer tokens are not PII

    [Theory]
    [InlineData("evt_id=1790721636242")]                      // 13-digit epoch milliseconds
    [InlineData("order 2026093012345")]                       // 13-digit order number
    [InlineData("amount=1234567890123")]                      // 13-digit amount
    [InlineData("trace=4bf92f3577b34da6a3ce929d0e0e4736")]    // 32-char hex trace id
    [InlineData("id 550e8400-e29b-41d4-a716-446655440000")]   // UUID
    [InlineData("bytes=9876543210")]                          // bare 10-digit count
    [InlineData("sha1 da39a3ee5e6b4b0d3255bfef95601890afd80709")]
    [InlineData("build 20260930123456789")]
    [InlineData("amount=15000000")]                           // bare 8 digits with a service-number prefix
    [InlineData("size=16777216")]
    [InlineData("id 18001234")]
    public void Mask_MachineToken_ReportsNothing(string text)
    {
        var result = AllTypesAllCountries().Mask(text);

        Assert.True(result.Matches.Count == 0, Describe(result.Matches));
        Assert.Equal(text, result.MaskedText);
    }

    [Theory]
    [InlineData("evt_id=1790721636242")]
    [InlineData("order 2026093012345")]
    [InlineData("amount=1234567890123")]
    [InlineData("trace=4bf92f3577b34da6a3ce929d0e0e4736")]
    [InlineData("id 550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("bytes=9876543210")]
    public void Detect_MachineToken_NoSliceAtAnyConfidence(string text)
    {
        var matches = AllTypesAllCountries(minConfidence: 0f).Detect(text);

        Assert.True(matches.Count == 0, Describe(matches));
    }

    [Theory]
    [InlineData("x010-1234-5678")]
    [InlineData("010-1234-5678x")]
    [InlineData("A901231-1234567")]
    [InlineData("ref4111111111111111")]
    [InlineData("v192.168.1.10")]
    public void Detect_ValueGluedToAsciiAlphanumeric_NotReported(string text)
    {
        var matches = AllTypesAllCountries(minConfidence: 0f).Detect(text);

        Assert.True(matches.Count == 0, Describe(matches));
    }

    [Fact]
    public void ContainsPII_MachineTokenOnly_ReturnsFalse()
    {
        Assert.False(AllTypesAllCountries().ContainsPII("evt_id=1790721636242 trace=4bf92f3577b34da6a3ce929d0e0e4736"));
    }

    [Theory]
    [InlineData("1.2.3.4.5")]
    [InlineData("1:2:3:4:5:6:7:8:9")]
    [InlineData("2026-09-30-901231-1234567")]
    [InlineData("901231-1234567-01")]
    public void Detect_SegmentOfLongerDottedOrHyphenatedNumber_NotReported(string text)
    {
        var matches = AllTypesAllCountries(minConfidence: 0f).Detect(text);

        Assert.True(matches.Count == 0, Describe(matches));
    }

    [Theory]
    [InlineData("ID-901231-1234567", "901231-1234567")]
    [InlineData("range 10.0.0.1-10.0.0.254", "10.0.0.1")]
    [InlineData("range 10.0.0.1-10.0.0.254", "10.0.0.254")]
    [InlineData("call (234) 567-8900", "(234) 567-8900")]
    public void Detect_ValueAfterLabelOrInRange_StillReported(string text, string expected)
    {
        var matches = AllTypesAllCountries().Detect(text);

        Assert.Contains(matches, m => m.Value == expected);
    }

    [Fact]
    public void DetectorContainsPII_FirstCandidateInvalidLaterValid_ReturnsTrue()
    {
        // The first candidate has no known card prefix and fails validation; the second is a valid card number.
        var detector = new CreditCardDetector();

        Assert.True(detector.ContainsPII("ref 1234567890123, card 4111111111111111"));
    }

    #endregion

    #region Values glued to Hangul still match

    [Theory]
    [InlineData("연락처010-1234-5678로", "010-1234-5678")]
    [InlineData("전화는02-555-1234입니다", "02-555-1234")]
    [InlineData("주민번호901231-1234567이고", "901231-1234567")]
    [InlineData("서버192.168.1.10에", "192.168.1.10")]
    public void Detect_ValueGluedToHangul_Reported(string text, string expected)
    {
        var matches = AllTypesAllCountries().Detect(text);

        Assert.Contains(matches, m => m.Value == expected);
    }

    #endregion

    #region Parenthesised area codes

    [Theory]
    [InlineData("fax (02) 555-1234", "(02) 555-1234")]
    [InlineData("tel (02) 1234-5678", "(02) 1234-5678")]
    [InlineData("tel (031) 123-4567", "(031) 123-4567")]
    [InlineData("tel (051)1234-5678", "(051)1234-5678")]
    public void Detect_ParenthesisedAreaCode_ReportedAsPhone(string text, string expected)
    {
        var matches = AllTypesAllCountries().Detect(text);

        var match = Assert.Single(matches);
        Assert.Equal(PIIType.Phone, match.Type);
        Assert.Equal(expected, match.Value);
    }

    [Fact]
    public void Mask_ParenthesisedAreaCode_Masked()
    {
        var result = AllTypesAllCountries().Mask("fax (02) 555-1234");

        Assert.Equal("fax [PHONE]", result.MaskedText);
    }

    #endregion

    #region Internationalised email addresses

    [Theory]
    [InlineData("메일: 홍길동@회사.kr", "홍길동@회사.kr")]
    [InlineData("정수민@example.kr 로 보내 주세요", "정수민@example.kr")]
    [InlineData("contact: user@회사.한국", "user@회사.한국")]
    [InlineData("müller@example.de", "müller@example.de")]
    public void Detect_NonAsciiEmail_ReportedAsEmail(string text, string expected)
    {
        var matches = AllTypesAllCountries().Detect(text);

        var match = Assert.Single(matches);
        Assert.Equal(PIIType.Email, match.Type);
        Assert.Equal(expected, match.Value);
    }

    [Fact]
    public void Detect_EmailFollowedByHangulParticle_ParticleNotIncluded()
    {
        var matches = AllTypesAllCountries().Detect("홍길동@회사.kr로 연락 주세요");

        var match = Assert.Single(matches);
        Assert.Equal("홍길동@회사.kr", match.Value);
    }

    [Fact]
    public void Mask_NonAsciiEmail_Masked()
    {
        var result = AllTypesAllCountries().Mask("메일: 홍길동@회사.kr");

        Assert.Equal("메일: [EMAIL]", result.MaskedText);
    }

    #endregion
}
