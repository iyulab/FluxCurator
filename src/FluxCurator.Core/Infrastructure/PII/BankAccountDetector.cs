namespace FluxCurator.Core.Infrastructure.PII;

using System.Numerics;
using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects bank account numbers: an IBAN by its shape and check digits, and a domestic account number - a run of 10 to 16
/// digits, plain or in hyphenated groups (<c>110-234-567890</c>) - only when an account word precedes it on the same line
/// (<c>account</c>, <c>acct</c>, <c>계좌</c>, <c>이체</c>, …). Without that word the digits are as likely an order number
/// or a timestamp, so they are left alone.
/// </summary>
public sealed class BankAccountDetector : PIIDetectorBase
{
    /// <summary>Words that say the next number is an account number.</summary>
    private static readonly string[] ContextWords =
    [
        "account", "acct", "a/c", "bank", "iban",
        "계좌", "통장", "은행", "입금", "송금", "이체", "예금",
        "口座", "账户", "帳戶", "konto", "compte", "cuenta", "conto",
    ];

    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.BankAccount;

    /// <inheritdoc/>
    public override string Name => "Bank Account Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        TokenStart + @"(?:" +
            @"(?<iban>[A-Z]{2}[0-9]{2}(?: ?[A-Z0-9]{4}){2,7}(?: ?[A-Z0-9]{1,3})?)" +
            @"|" +
            NumberStart + @"(?<domestic>[0-9]{2,6}(?:-[0-9]{1,8}){1,4}|[0-9]{10,16})" + NumberEnd +
        @")" + TokenEnd;

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
            if (match.Groups["iban"].Success)
            {
                if (IsValidIban(match.Value))
                    matches.Add(PIIMatch.Create(PIIType, match.Value, match.Index, 0.95f));
                continue;
            }

            var digits = match.Value.Count(char.IsAsciiDigit);
            if (digits is >= 10 and <= 16 && HasContextBefore(text, match.Index, ContextWords))
                matches.Add(PIIMatch.Create(PIIType, match.Value, match.Index, 0.9f));
        }

        return matches;
    }

    /// <inheritdoc/>
    public override bool ContainsPII(string text) => Detect(text).Count > 0;

    /// <summary>ISO 13616 check: move the first four characters to the end, letters to numbers, mod 97 == 1.</summary>
    private static bool IsValidIban(string value)
    {
        var compact = value.Replace(" ", "", StringComparison.Ordinal);
        if (compact.Length is < 15 or > 34)
            return false;

        var rearranged = compact[4..] + compact[..4];
        var numeric = new System.Text.StringBuilder(rearranged.Length * 2);
        foreach (var c in rearranged)
        {
            if (char.IsAsciiDigit(c))
                numeric.Append(c);
            else if (char.IsAsciiLetterUpper(c))
                numeric.Append(c - 'A' + 10);
            else
                return false;
        }

        return BigInteger.Parse(numeric.ToString(), System.Globalization.CultureInfo.InvariantCulture) % 97 == 1;
    }
}
