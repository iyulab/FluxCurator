namespace FluxCurator.Core.Infrastructure.PII;

using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects Korean driver's license numbers (운전면허번호): the written form <c>11-22-123456-78</c> (region, year, serial,
/// check) on its own, and the same twelve digits written together or with spaces when a license word comes before them on
/// the same line (<c>운전면허</c>, <c>driver's license</c>, <c>driving licence</c>, <c>免許</c>). Twelve bare digits are too common
/// (order numbers, timestamps) to report without the word.
/// </summary>
public sealed partial class DriversLicenseDetector : PIIDetectorBase
{
    /// <summary>Words that say the next value is a driver's license number.</summary>
    private static readonly string[] ContextWords =
    [
        "운전면허", "면허번호", "driver's license", "drivers license", "driver license", "driving licence", "driving license", "免許",
    ];

    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.DriversLicense;

    /// <inheritdoc/>
    public override string Name => "Driver's License Detector";

    /// <inheritdoc/>
    protected override string Pattern => PatternText;

    private const string PatternText =
        NumberStart + @"(?<written>\d{2}-\d{2}-\d{6}-\d{2})" + NumberEnd + "|" + NumberStart + @"\d{2}\s?\d{2}\s?\d{6}\s?\d{2}" + NumberEnd;

    [GeneratedRegex(PatternText)]
    private static partial Regex GeneratedPattern();

    /// <inheritdoc/>
    protected override Regex CreateRegex() => GeneratedPattern();

    /// <inheritdoc/>
    protected override RegexOptions RegexOptions => RegexOptions.Compiled;

    /// <inheritdoc/>
    public override IReadOnlyList<PIIMatch> Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var matches = new List<PIIMatch>();
        foreach (Match match in CompiledPattern.Matches(text))
        {
            if (match.Groups["written"].Success || HasContextBefore(text, match.Index, ContextWords))
                matches.Add(PIIMatch.Create(PIIType, match.Value, match.Index, 0.9f));
        }

        return matches;
    }

    /// <inheritdoc/>
    public override bool ContainsPII(string text) => Detect(text).Count > 0;
}
