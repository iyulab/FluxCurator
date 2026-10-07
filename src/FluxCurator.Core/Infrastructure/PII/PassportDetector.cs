namespace FluxCurator.Core.Infrastructure.PII;

using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects passport numbers that follow a passport word on the same line (<c>passport</c>, <c>여권</c>, <c>旅券</c>, …):
/// one or two capital letters and 6 to 8 digits (<c>M12345678</c>), the Korean 2021 format (<c>M123A4567</c>), or nine
/// digits. Passport numbers have no checksum and their shapes are shared with many other identifiers, so a value
/// without the word is not reported.
/// </summary>
public sealed class PassportDetector : PIIDetectorBase
{
    /// <summary>Words that say the next value is a passport number.</summary>
    private static readonly string[] ContextWords =
    [
        "passport", "여권", "旅券", "护照", "護照", "reisepass", "passeport", "pasaporte", "passaporto",
    ];

    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.Passport;

    /// <inheritdoc/>
    public override string Name => "Passport Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        TokenStart + @"(?:[A-Z]{1,2}[0-9]{6,8}|[A-Z][0-9]{3}[A-Z][0-9]{4}|[0-9]{9})" + TokenEnd;

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
            if (HasContextBefore(text, match.Index, ContextWords))
                matches.Add(PIIMatch.Create(PIIType, match.Value, match.Index, 0.9f));
        }

        return matches;
    }

    /// <inheritdoc/>
    public override bool ContainsPII(string text) => Detect(text).Count > 0;
}
