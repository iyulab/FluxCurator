namespace FluxCurator.Core.Infrastructure.PII;

using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects IPv4 and IPv6 addresses in text.
/// </summary>
public sealed class IPAddressDetector : PIIDetectorBase
{
    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.IPAddress;

    /// <inheritdoc/>
    public override string Name => "IP Address Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        @"(?:" +
            // IPv4: 1.2.3.4 through 255.255.255.255
            @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b" +
            @"|" +
            // IPv6: broad capture, validated by IPAddress.TryParse()
            // Matches hex groups with colons, including :: compressed forms
            @"(?:[0-9a-fA-F]{0,4}:){2,7}[0-9a-fA-F]{0,4}" +
            @"|::(?:[0-9a-fA-F]{1,4}(?::[0-9a-fA-F]{1,4}){0,6})?" +
        @")";

    /// <inheritdoc/>
    protected override RegexOptions RegexOptions => RegexOptions.Compiled;

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        // Use System.Net.IPAddress for authoritative validation
        if (!IPAddress.TryParse(value, out var parsed))
            return false;

        if (parsed.AddressFamily == AddressFamily.InterNetwork)
        {
            // IPv4 validation
            confidence = GetIPv4Confidence(parsed);
            return confidence >= 0.5f;
        }

        if (parsed.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // IPv6 — high confidence by nature (unlikely false positive)
            confidence = 0.95f;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates confidence for an IPv4 address based on plausibility.
    /// </summary>
    private static float GetIPv4Confidence(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var first = bytes[0];

        // Reject 0.0.0.0 — ambiguous, not PII
        if (bytes.All(b => b == 0))
            return 0.0f;

        // Loopback (127.x.x.x) — lower confidence, usually not PII
        if (first == 127)
            return 0.6f;

        // Link-local (169.254.x.x) — auto-assigned, not PII
        if (first == 169 && bytes[1] == 254)
            return 0.5f;

        // Private ranges — real internal addresses, genuine PII in logs
        // 10.0.0.0/8
        if (first == 10)
            return 0.9f;

        // 172.16.0.0/12
        if (first == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return 0.9f;

        // 192.168.0.0/16
        if (first == 192 && bytes[1] == 168)
            return 0.9f;

        // Public IP — highest confidence
        return 0.95f;
    }
}
