namespace FluxCurator.Core.Infrastructure.PII;

using System.Text.RegularExpressions;
using FluxCurator.Core.Domain;

/// <summary>
/// Detects secrets carried in a URL and reports the secret only, so masking keeps the URL readable:
/// the value of a query, fragment or matrix parameter named like a credential (<c>token</c>, <c>access_token</c>,
/// <c>api_key</c>, <c>key</c>, <c>sig</c>, <c>password</c>, <c>secret</c>, <c>code</c>, …) and the password of a
/// <c>scheme://user:password@host</c> URL.
/// </summary>
/// <remarks>
/// <c>https://api.example.com/v1/users?token=abcd1234&amp;page=2</c> masks to
/// <c>https://api.example.com/v1/users?token=[CREDENTIAL]&amp;page=2</c>.
/// </remarks>
public sealed partial class UrlCredentialDetector : PIIDetectorBase
{
    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.UrlCredential;

    /// <inheritdoc/>
    public override string Name => "URL Credential Detector";

    /// <inheritdoc/>
    protected override string Pattern => @"(?<![A-Za-z0-9+.\-])[A-Za-z][A-Za-z0-9+.\-]*://[^\s""'<>`]+";

    /// <inheritdoc/>
    protected override RegexOptions RegexOptions => RegexOptions.Compiled;

    /// <inheritdoc/>
    public override IReadOnlyList<PIIMatch> Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var matches = new List<PIIMatch>();
        foreach (Match url in CompiledPattern.Matches(text))
        {
            var userInfo = UserInfoPassword().Match(url.Value);
            if (userInfo.Success)
            {
                var password = userInfo.Groups["password"];
                matches.Add(PIIMatch.Create(PIIType, password.Value, url.Index + password.Index, 0.95f));
            }

            foreach (Match parameter in CredentialParameter().Matches(url.Value))
            {
                var value = parameter.Groups["value"];
                matches.Add(PIIMatch.Create(PIIType, value.Value, url.Index + value.Index, 0.9f));
            }
        }

        return matches;
    }

    /// <inheritdoc/>
    public override bool ContainsPII(string text) => Detect(text).Count > 0;

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9+.\-]*://[^/?#@:]+:(?<password>[^/?#@]+)@")]
    private static partial Regex UserInfoPassword();

    [GeneratedRegex(
        @"[?&#;](?:access_token|refresh_token|id_token|auth_token|token|api_key|apikey|api-key|key|client_secret|secret|" +
        @"password|passwd|pwd|sig|signature|x-amz-signature|x-amz-credential|x-amz-security-token|code|session|sessionid|auth)" +
        @"=(?<value>[^&#;\s]+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex CredentialParameter();
}
