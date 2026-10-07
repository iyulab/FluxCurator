namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

/// <summary>
/// A phone-shaped or ID-shaped number is not PII when the text says it is a machine identifier: the value of an audit,
/// process or cursor key (<see cref="PIIMaskingOptions.NonPiiKeys"/>), or a segment of a compound identifier such as a
/// journald cursor. The same number next to a person-shaped key, or next to a hash in another field, is still masked.
/// </summary>
public class MachineIdentifierSuppressionTests
{
    private static PIIMasker AllTypes(Action<PIIMaskingOptions>? configure = null)
    {
        var options = new PIIMaskingOptions
        {
            LanguageCodes = ["auto"],
            TypesToMask = PIIType.All,
            Strategy = MaskingStrategy.Token,
            MinConfidence = 0.8f,
        };
        configure?.Invoke(options);
        return new PIIMasker(options);
    }

    private const string Cursor =
        "s=4bf92f3577b34da6a3ce929d0e0e4736;i=43b37;b=9d1e4c2a7f3b48e6a5c0d2f1e8b7a6c5;m=0161431588;t=65c9c58d7b0f1;x=8f3a2b1c4d5e6f70";

    [Theory]
    [InlineData("{\"NodeId\":\"56c99d83\",\"ID\":\"" + Cursor + "\",\"Message\":\"service started\"}")]
    [InlineData("cursor s=4bf92f3577b34da6a3ce929d0e0e4736;i=43b37;m=0312345678;t=65c9c58d7b0f1 done")]
    [InlineData("type=USER_LOGIN uid=0161431588 auid=4294967295 ses=4294967295 pid=0101234567 res=success")]
    [InlineData("{\"pid\": 0101234567, \"port\": 5432}")]
    [InlineData("conn sport = 0161431588 dport=443")]
    [InlineData("host Pitsdshdb1 started; node Ksvdbsrv02 joined")]
    public void MachineIdentifierValues_StayIntact(string line)
    {
        var result = AllTypes().Mask(line);

        Assert.Equal(line, result.MaskedText);
    }

    [Theory]
    [InlineData("{\"trace\":\"9d1e4c2a7f3b48e6a5c0d2f1e8b7a6c5\",\"user\":\"010-1234-5678\"}", "[PHONE]")]
    [InlineData("hash=9d1e4c2a7f3b48e6a5c0d2f1e8b7a6c5 phone=010-1234-5678", "[PHONE]")]
    [InlineData("hash 9d1e4c2a7f3b48e6a5c0d2f1e8b7a6c5 주민번호: 901231-1234567", "[NATIONAL_ID]")]
    [InlineData("caller_id=010-1234-5678 uid=1000", "[PHONE]")]
    [InlineData("user=kim;phone=010-1234-5678;sid=9d1e4c2a7f3b48e6", "[PHONE]")]   // one hex segment: a record, not a cursor
    public void PersonValues_NextToIdentifiers_AreStillMasked(string line, string token)
    {
        var result = AllTypes().Mask(line);

        Assert.Contains(token, result.MaskedText, StringComparison.Ordinal);
        if (line.Contains("9d1e4c2a7f3b48e6a5c0d2f1e8b7a6c5", StringComparison.Ordinal))
            Assert.Contains("9d1e4c2a7f3b48e6a5c0d2f1e8b7a6c5", result.MaskedText, StringComparison.Ordinal);
    }

    [Fact]
    public void NonPiiKeys_IsExtensible_AndClearable()
    {
        const string line = "order_no=010-1234-5678 pid=010-9876-5432";

        Assert.Equal("order_no=[PHONE] pid=010-9876-5432", AllTypes().Mask(line).MaskedText);
        Assert.Equal(line, AllTypes(o => o.NonPiiKeys.Add("order_no")).Mask(line).MaskedText);
        Assert.Equal("order_no=[PHONE] pid=[PHONE]", AllTypes(o => o.NonPiiKeys.Clear()).Mask(line).MaskedText);
    }

    [Fact]
    public void ContainsPII_AgreesWithMask() =>
        Assert.False(AllTypes().ContainsPII("uid=0161431588 auid=4294967295"));
}
