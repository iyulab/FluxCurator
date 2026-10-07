namespace FluxCurator.Core.Infrastructure.PII;

using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects hardware (MAC) addresses: six hex pairs joined by one separator throughout (<c>00:1A:2B:3C:4D:5E</c>,
/// <c>00-1A-2B-3C-4D-5E</c>) and the dotted form (<c>001a.2b3c.4d5e</c>). A device address identifies a person's device
/// as an IP address does. The all-zero and broadcast addresses identify nothing and are not reported.
/// </summary>
public sealed class HardwareAddressDetector : PIIDetectorBase
{
    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.HardwareAddress;

    /// <inheritdoc/>
    public override string Name => "Hardware Address Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        @"(?<![0-9A-Fa-f]|[0-9A-Fa-f][:.\-])(?:" +
            @"[0-9A-Fa-f]{2}(?<sep>[:\-])(?:[0-9A-Fa-f]{2}\k<sep>){4}[0-9A-Fa-f]{2}" +
            @"|[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}\.[0-9A-Fa-f]{4}" +
        @")(?![0-9A-Fa-f]|[:.\-][0-9A-Fa-f])";

    /// <inheritdoc/>
    protected override RegexOptions RegexOptions => RegexOptions.Compiled;

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        var hex = new string(value.Where(char.IsAsciiHexDigit).ToArray());
        if (hex.All(c => c == '0') || hex.All(c => c is 'f' or 'F'))
        {
            confidence = 0f;
            return false;
        }

        confidence = 0.9f;
        return true;
    }
}
