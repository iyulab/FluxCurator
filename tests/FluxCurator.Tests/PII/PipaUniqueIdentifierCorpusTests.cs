namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

/// <summary>
/// The Korean unique identifiers (PIPA Art. 24) every Korean PII detector in the ecosystem must find —
/// <c>pipa-unique-identifiers.tsv</c>, kept byte for byte in FluxGuard's tests too. A sample the masker misses means it leaves
/// a number in the text that the gate would block (or the other way round).
/// </summary>
public class PipaUniqueIdentifierCorpusTests
{
    private static readonly Dictionary<string, PIIType> TypeByKind = new(StringComparer.Ordinal)
    {
        ["RRN"] = PIIType.NationalId,
        ["Passport"] = PIIType.Passport,
        ["DriversLicense"] = PIIType.DriversLicense,
    };

    private static readonly PIIMasker Masker = new(new PIIMaskingOptions { LanguageCodes = ["ko"], TypesToMask = PIIType.All });

    public static TheoryData<string, string> Samples()
    {
        var data = new TheoryData<string, string>();
        foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, "PII", "pipa-unique-identifiers.tsv")))
        {
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var parts = line.Split('\t', 2);
            data.Add(parts[0], parts[1]);
        }

        return data;
    }

    [Fact]
    public void The_corpus_names_only_kinds_this_library_maps_and_has_samples_of_each()
    {
        var kinds = Samples().Select(row => row.Data.Item1).Distinct().Order(StringComparer.Ordinal).ToList();

        Assert.Equal(TypeByKind.Keys.Order(StringComparer.Ordinal), kinds);
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void Every_sample_is_found_as_its_type(string kind, string text)
    {
        var types = Masker.Detect(text).Select(m => m.Type).ToList();

        Assert.True(types.Contains(TypeByKind[kind]), $"expected {TypeByKind[kind]} in «{text}», found [{string.Join(", ", types)}]");
    }
}
