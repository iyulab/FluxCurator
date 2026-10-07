namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

/// <summary>
/// The account, passport, URL-credential and hardware-address types are found by built-in detectors, so a masker with
/// <see cref="PIIType.All"/> leaves none of them in log text - while the shapes they share with ordinary log values
/// (order numbers, timestamps, ports, hashes) stay untouched. <see cref="PIIMasker.CoveredTypes"/> says which categories
/// the masker can actually find.
/// </summary>
public class DeclaredTypeDetectorTests
{
    private static PIIMasker AllTypes() => new(new PIIMaskingOptions
    {
        LanguageCodes = ["auto"],
        TypesToMask = PIIType.All,
        Strategy = MaskingStrategy.Token,
        MinConfidence = 0.8f,
    });

    [Theory]
    [InlineData("DHCP lease assigned to MAC 00:1A:2B:3C:4D:5E", "DHCP lease assigned to MAC [MAC]")]
    [InlineData("iface eth0 hw 00-1a-2b-3c-4d-5e up", "iface eth0 hw [MAC] up")]
    [InlineData("cisco port learned 001a.2b3c.4d5e.", "cisco port learned [MAC].")]
    [InlineData("Passport M12345678 attached to visa application record", "Passport [PASSPORT] attached to visa application record")]
    [InlineData("여권번호 M123A4567 확인", "여권번호 [PASSPORT] 확인")]
    [InlineData("Settlement account 110-234-567890 transfer rejected", "Settlement account [ACCOUNT] transfer rejected")]
    [InlineData("입금 계좌: 1002345678901", "입금 계좌: [ACCOUNT]")]
    [InlineData("wire to DE89 3704 0044 0532 0130 00 failed", "wire to [ACCOUNT] failed")]
    [InlineData("API call https://api.example.com/v1/users?token=abcd1234efgh returned 401",
        "API call https://api.example.com/v1/users?token=[CREDENTIAL] returned 401")]
    [InlineData("GET https://h.example/x?page=2&api_key=K9z.Q-1&sort=asc",
        "GET https://h.example/x?page=2&api_key=[CREDENTIAL]&sort=asc")]
    [InlineData("connect postgres://app:s3cr3t@db.internal:5432/main",
        "connect postgres://app:[CREDENTIAL]@db.internal:5432/main")]
    public void Mask_RemovesTheValue_AndKeepsTheLine(string line, string expected) =>
        Assert.Equal(expected, AllTypes().Mask(line).MaskedText);

    [Theory]
    [InlineData("order 2023-1001-55555 shipped")]                       // hyphenated digits, no account word
    [InlineData("ts=1728299999123 pid=48213 port=5432 uid=1000")]       // epoch ms, pid, port, uid
    [InlineData("auid=4294967295 ses=4294967295")]                       // audit sentinels
    [InlineData("sha 9d1e4c2a7f3b48e6a5c0d2f1e8b7a6c5 ok")]             // hash
    [InlineData("host web-01.example.com responded")]                   // hostname
    [InlineData("build M12345678 published")]                           // passport shape, no passport word
    [InlineData("MAC 00:00:00:00:00:00 and ff:ff:ff:ff:ff:ff")]         // all-zero and broadcast
    [InlineData("see https://example.com/docs?page=2&sort=asc")]        // URL without a credential parameter
    [InlineData("fe80::1a2b:3c4d:5e6f:7a8b on link")]                    // an IPv6 address is not a MAC
    public void Mask_LeavesOrdinaryLogValuesAlone(string line)
    {
        var result = AllTypes().Mask(line);
        Assert.DoesNotContain(result.Matches, m =>
            m.Type is PIIType.BankAccount or PIIType.Passport or PIIType.UrlCredential or PIIType.HardwareAddress);
    }

    [Fact]
    public void CoveredTypes_NamesWhatTheMaskerCanFind_NotEveryDeclaredCategory()
    {
        var covered = AllTypes().CoveredTypes;

        foreach (var type in new[]
                 {
                     PIIType.Email, PIIType.Phone, PIIType.CreditCard, PIIType.IPAddress, PIIType.NationalId,
                     PIIType.BankAccount, PIIType.Passport, PIIType.UrlCredential, PIIType.HardwareAddress,
                 })
        {
            Assert.True(covered.HasFlag(type), $"{type} should be covered");
        }

        Assert.False(covered.HasFlag(PIIType.PersonName));
        Assert.False(covered.HasFlag(PIIType.Address));
        Assert.False(covered.HasFlag(PIIType.DriversLicense));
    }

    [Fact]
    public void CoveredTypes_FollowsTypesToMask_AndRegisteredDetectors()
    {
        var masker = new PIIMasker(new PIIMaskingOptions { TypesToMask = PIIType.Email });
        Assert.Equal(PIIType.Email, masker.CoveredTypes);

        masker.RegisterDetector(new HardwareAddressDetector());
        Assert.Equal(PIIType.Email | PIIType.HardwareAddress, masker.CoveredTypes);
    }
}
