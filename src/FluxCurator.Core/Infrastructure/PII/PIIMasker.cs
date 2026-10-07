namespace FluxCurator.Core.Infrastructure.PII;

using System.Security.Cryptography;
using System.Text;
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.PII.NationalId;

/// <summary>
/// Main PII masker that coordinates detection and masking operations.
/// Supports multilingual PII detection through language codes.
/// </summary>
public sealed class PIIMasker : IPIIMasker
{
    // Built-in detectors, selected by Options.TypesToMask.
    private readonly Dictionary<PIIType, List<IPIIDetector>> _detectors = new();

    // Detectors the caller registered. Registering one is the opt-in, so they run whatever TypesToMask says -
    // a custom detector usually reports PIIType.Custom, which no TypesToMask preset (not even All) includes.
    private readonly List<IPIIDetector> _registeredDetectors = [];
    private readonly INationalIdRegistry _nationalIdRegistry;

    /// <summary>
    /// Creates a new PIIMasker with default options.
    /// </summary>
    public PIIMasker() : this(PIIMaskingOptions.Default)
    {
    }

    /// <summary>
    /// Creates a new PIIMasker with specified options.
    /// </summary>
    public PIIMasker(PIIMaskingOptions options) : this(options, new NationalIdRegistry())
    {
    }

    /// <summary>
    /// Creates a new PIIMasker with specified options and national ID registry.
    /// </summary>
    public PIIMasker(PIIMaskingOptions options, INationalIdRegistry nationalIdRegistry)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _nationalIdRegistry = nationalIdRegistry ?? throw new ArgumentNullException(nameof(nationalIdRegistry));
        RegisterDefaultDetectors();
    }

    /// <inheritdoc/>
    public PIIMaskingOptions Options { get; }

    /// <summary>
    /// The types this masker can actually find: the built-in detectors selected by
    /// <see cref="PIIMaskingOptions.TypesToMask"/> and <see cref="PIIMaskingOptions.LanguageCodes"/>, plus every
    /// registered detector. A category in <see cref="PIIType.All"/> that no detector reports (for example
    /// <see cref="PIIType.PersonName"/>) is not in it - assert on this when a masking policy requires a type.
    /// </summary>
    public PIIType CoveredTypes =>
        ActiveDetectors().Aggregate(PIIType.None, (covered, detector) => covered | detector.PIIType);

    /// <inheritdoc/>
    public void RegisterDetector(IPIIDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
        _registeredDetectors.Add(detector);
    }

    private void AddBuiltInDetector(IPIIDetector detector)
    {
        if (!_detectors.TryGetValue(detector.PIIType, out var list))
        {
            list = [];
            _detectors[detector.PIIType] = list;
        }
        list.Add(detector);
    }

    /// <inheritdoc/>
    public IReadOnlyList<PIIMatch> Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var allMatches = new List<PIIMatch>();
        foreach (var detector in ActiveDetectors())
        {
            foreach (var match in detector.Detect(text))
            {
                if (!IsMachineIdentifierValue(text, match))
                    allMatches.Add(match);
            }
        }

        // Resolve overlaps FIRST (prefer longer/more specific matches),
        // then filter by confidence. This prevents shorter partial matches
        // (e.g. Phone) from leaking digits when a longer match (e.g. NationalId)
        // covers the same region.
        var resolved = ResolveOverlaps(allMatches);
        return resolved.Where(m => m.Confidence >= Options.MinConfidence).ToList();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Answers exactly what <see cref="Mask"/> would act on — the same overlap resolution and
    /// <see cref="PIIMaskingOptions.MinConfidence"/> — so a text reported as containing PII is never
    /// returned unchanged by <see cref="Mask"/>.
    /// </remarks>
    public bool ContainsPII(string text) => Detect(text).Count > 0;

    // Built-in detectors of the types in TypesToMask, then every registered detector.
    private IEnumerable<IPIIDetector> ActiveDetectors() =>
        _detectors.Where(entry => Options.TypesToMask.HasFlag(entry.Key))
            .SelectMany(entry => entry.Value)
            .Concat(_registeredDetectors);

    /// <inheritdoc/>
    public PIIMaskingResult Mask(string text)
    {
        if (string.IsNullOrEmpty(text))
            return PIIMaskingResult.NoPII(text ?? string.Empty, Options);

        var matches = Detect(text);

        if (matches.Count == 0)
            return PIIMaskingResult.NoPII(text, Options);

        // Apply masking
        var maskedText = ApplyMasking(text, matches);

        return new PIIMaskingResult
        {
            OriginalText = text,
            MaskedText = maskedText,
            Matches = matches,
            Options = Options
        };
    }

    /// <summary>
    /// Registers default PII detectors.
    /// </summary>
    private void RegisterDefaultDetectors()
    {
        // Global detectors (language-agnostic)
        AddBuiltInDetector(new EmailDetector());
        AddBuiltInDetector(new PhoneDetector());
        AddBuiltInDetector(new CreditCardDetector());
        AddBuiltInDetector(new IPAddressDetector());
        AddBuiltInDetector(new BankAccountDetector());
        AddBuiltInDetector(new PassportDetector());
        AddBuiltInDetector(new UrlCredentialDetector());
        AddBuiltInDetector(new HardwareAddressDetector());

        // Register national ID detectors based on language codes
        RegisterNationalIdDetectors();
    }

    /// <summary>
    /// Registers national ID detectors based on configured language codes.
    /// </summary>
    private void RegisterNationalIdDetectors()
    {
        var languageCodes = Options.LanguageCodes;

        // If "auto" is specified, register all available detectors
        if (languageCodes.Contains("auto", StringComparer.OrdinalIgnoreCase))
        {
            foreach (var detector in _nationalIdRegistry.GetAllDetectors())
            {
                AddBuiltInDetector(detector);
            }
            return;
        }

        // Register detectors for specified languages only
        foreach (var languageCode in languageCodes)
        {
            var detectors = _nationalIdRegistry.GetDetectors([languageCode]);
            foreach (var detector in detectors)
            {
                AddBuiltInDetector(detector);
            }
        }
    }

    /// <summary>
    /// Whether a match is the value of a machine identifier rather than PII: the value of a key in
    /// <see cref="PIIMaskingOptions.NonPiiKeys"/>, or one segment of a compound identifier token
    /// (<c>k=v;k=v</c> with a hex id segment). The digits of <c>pid=0161431588</c> are phone-shaped; the key says what they are.
    /// </summary>
    private bool IsMachineIdentifierValue(string text, PIIMatch match)
    {
        var key = KeyBefore(text, match.StartIndex);
        if (key is not null && Options.NonPiiKeys.Contains(key))
            return true;

        return key is not null && IsCompoundIdentifierSegment(text, match);
    }

    /// <summary>The key a value at <paramref name="index"/> is assigned to: <c>key=</c>, <c>key:</c> or <c>"key":</c>, or null.</summary>
    private static string? KeyBefore(string text, int index)
    {
        var i = index - 1;
        while (i >= 0 && text[i] is '"' or '\'' or ' ' or '\t')
            i--;
        if (i < 0 || text[i] is not ('=' or ':'))
            return null;

        i--;
        while (i >= 0 && text[i] is ' ' or '\t' or '"' or '\'')
            i--;

        var end = i + 1;
        while (i >= 0 && (char.IsAsciiLetterOrDigit(text[i]) || text[i] is '_' or '-' or '.'))
            i--;

        return end > i + 1 ? text[(i + 1)..end] : null;
    }

    /// <summary>
    /// Whether the match is one <c>k=v</c> segment of a token of <c>;</c>-separated segments, at least two others of which
    /// carry a hex id of 8 or more characters - a journald cursor, a session descriptor. One hex segment is not enough:
    /// <c>user=kim;phone=010-1234-5678;sid=9d1e…</c> is a record that happens to carry a session id, and its phone is PII.
    /// </summary>
    private static bool IsCompoundIdentifierSegment(string text, PIIMatch match)
    {
        var start = match.StartIndex;
        while (start > 0 && !IsTokenBoundary(text[start - 1]))
            start--;
        var end = match.EndIndex;
        while (end < text.Length && !IsTokenBoundary(text[end]))
            end++;

        var segments = text[start..end].Split(';');
        if (segments.Length < 3)
            return false;

        var own = text.AsSpan(start, match.StartIndex - start).Count(';');
        var hexSegments = 0;
        for (var s = 0; s < segments.Length; s++)
        {
            if (s == own)
                continue;
            var eq = segments[s].IndexOf('=');
            if (eq > 0 && segments[s].Length - eq - 1 >= 8 && segments[s].AsSpan(eq + 1).ContainsAnyExcept(HexDigits) is false)
                hexSegments++;
        }

        return hexSegments >= 2;
    }

    private static readonly System.Buffers.SearchValues<char> HexDigits =
        System.Buffers.SearchValues.Create("0123456789abcdefABCDEF");

    private static bool IsTokenBoundary(char c) => char.IsWhiteSpace(c) || c is '"' or '\'' or ',' or '{' or '}' or '[' or ']';

    /// <summary>
    /// Resolves overlapping matches by keeping the highest confidence match.
    /// </summary>
    private static List<PIIMatch> ResolveOverlaps(List<PIIMatch> matches)
    {
        if (matches.Count <= 1)
            return matches;

        // Sort by start position, then by length (prefer longer matches), then by confidence, then by type - so two
        // detectors reporting the same span (a phone number after the word "bank") always resolve the same way.
        matches.Sort((a, b) =>
        {
            var posCompare = a.StartIndex.CompareTo(b.StartIndex);
            if (posCompare != 0)
                return posCompare;
            var lengthCompare = b.Length.CompareTo(a.Length); // Longer first
            if (lengthCompare != 0)
                return lengthCompare;
            var confidenceCompare = b.Confidence.CompareTo(a.Confidence); // More confident first
            return confidenceCompare != 0 ? confidenceCompare : a.Type.CompareTo(b.Type);
        });

        var result = new List<PIIMatch>();
        int lastEnd = -1;

        foreach (var match in matches)
        {
            // Skip if this match overlaps with a previous one
            if (match.StartIndex < lastEnd)
                continue;

            result.Add(match);
            lastEnd = match.EndIndex;
        }

        return result;
    }

    /// <summary>
    /// Applies masking to the text based on detected matches.
    /// </summary>
    private string ApplyMasking(string text, IReadOnlyList<PIIMatch> matches)
    {
        if (matches.Count == 0)
            return text;

        var sb = new StringBuilder(text.Length);
        int currentPos = 0;

        foreach (var match in matches.OrderBy(m => m.StartIndex))
        {
            // Add text before this match
            if (match.StartIndex > currentPos)
            {
                sb.Append(text[currentPos..match.StartIndex]);
            }

            // Apply masking strategy
            var maskedValue = GetMaskedValue(match);
            match.MaskedValue = maskedValue;
            sb.Append(maskedValue);

            currentPos = match.EndIndex;
        }

        // Add remaining text
        if (currentPos < text.Length)
        {
            sb.Append(text[currentPos..]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the masked value based on the masking strategy.
    /// </summary>
    private string GetMaskedValue(PIIMatch match)
    {
        return Options.Strategy switch
        {
            MaskingStrategy.Token => Options.GetToken(match.Type),
            MaskingStrategy.Asterisk => MaskWithCharacter(match.Value, '*'),
            MaskingStrategy.Character => MaskWithCharacter(match.Value, 'X'),
            MaskingStrategy.Redact => "[REDACTED]",
            MaskingStrategy.Partial => MaskPartial(match.Value, match.Type),
            MaskingStrategy.Hash => MaskWithHash(match.Value),
            MaskingStrategy.Remove => string.Empty,
            _ => Options.GetToken(match.Type)
        };
    }

    /// <summary>
    /// Masks a value with a specific character.
    /// </summary>
    private static string MaskWithCharacter(string value, char maskChar)
    {
        return new string(maskChar, value.Length);
    }

    /// <summary>
    /// Partially masks a value, preserving some characters.
    /// </summary>
    private string MaskPartial(string value, PIIType type)
    {
        if (value.Length <= Options.PartialPreserveCount * 2)
            return new string(Options.MaskCharacter, value.Length);

        var preserveStart = Options.PartialPreserveCount;
        var preserveEnd = Options.PartialPreserveCount;

        // Adjust based on PII type
        switch (type)
        {
            case PIIType.Email:
                // Show first 2 chars and domain: jo**@ex****.com
                var atIndex = value.IndexOf('@');
                if (atIndex > 0)
                {
                    var local = value[..atIndex];
                    var domain = value[(atIndex + 1)..];
                    var maskedLocal = local.Length > 2
                        ? local[..2] + new string('*', local.Length - 2)
                        : local;
                    var dotIndex = domain.LastIndexOf('.');
                    var maskedDomain = dotIndex > 2
                        ? domain[..2] + new string('*', dotIndex - 2) + domain[dotIndex..]
                        : domain;
                    return maskedLocal + "@" + maskedDomain;
                }
                break;

            case PIIType.Phone:
                // Show last 4 digits: ***-****-5678
                if (value.Length >= 4)
                {
                    return new string('*', value.Length - 4) + value[^4..];
                }
                break;

            case PIIType.CreditCard:
                // Show last 4 digits: ****-****-****-3456
                if (value.Length >= 4)
                {
                    return new string('*', value.Length - 4) + value[^4..];
                }
                break;

            case PIIType.NationalId:
                // Show first 6 characters for national IDs: 901231-*******
                if (value.Length >= 7)
                {
                    var normalized = value.Replace("-", "").Replace(" ", "");
                    if (normalized.Length >= 6)
                    {
                        var maskLength = Math.Max(normalized.Length - 6, 0);
                        return normalized[..6] + new string('*', maskLength);
                    }
                }
                break;

            case PIIType.IPAddress:
                // Show first octet only: 192.***.***.***
                var dotIndex2 = value.IndexOf('.');
                var colonIndex = value.IndexOf(':');
                if (dotIndex2 > 0)
                {
                    // IPv4: show first octet
                    var firstOctet = value[..dotIndex2];
                    return firstOctet + ".***.***.***";
                }
                if (colonIndex >= 0)
                {
                    // IPv6: show first group
                    var firstGroup = value[..colonIndex];
                    return firstGroup + ":****:****:****";
                }
                break;
        }

        // Default partial masking
        var start = value[..preserveStart];
        var end = value[^preserveEnd..];
        var middleLength = value.Length - preserveStart - preserveEnd;

        return start + new string(Options.MaskCharacter, middleLength) + end;
    }

    /// <summary>
    /// Masks a value with a hash.
    /// </summary>
    private static string MaskWithHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        var hashString = Convert.ToHexString(hash)[..8].ToLowerInvariant();
        return $"[HASH:{hashString}]";
    }
}
