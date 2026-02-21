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
    private readonly Dictionary<PIIType, List<IPIIDetector>> _detectors = new();
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

    /// <inheritdoc/>
    public void RegisterDetector(IPIIDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
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

        foreach (var (type, detectors) in _detectors)
        {
            // Skip if this type is not in the mask list
            if (!Options.TypesToMask.HasFlag(type))
                continue;

            foreach (var detector in detectors)
            {
                var matches = detector.Detect(text);

                // Filter by confidence threshold
                foreach (var match in matches)
                {
                    if (match.Confidence >= Options.MinConfidence)
                    {
                        allMatches.Add(match);
                    }
                }
            }
        }

        // Sort by position and remove overlapping matches
        return ResolveOverlaps(allMatches);
    }

    /// <inheritdoc/>
    public bool ContainsPII(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var (type, detectors) in _detectors)
        {
            if (!Options.TypesToMask.HasFlag(type))
                continue;

            foreach (var detector in detectors)
            {
                if (detector.ContainsPII(text))
                    return true;
            }
        }

        return false;
    }

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
        RegisterDetector(new EmailDetector());
        RegisterDetector(new PhoneDetector());
        RegisterDetector(new CreditCardDetector());

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
                RegisterDetector(detector);
            }
            return;
        }

        // Register detectors for specified languages only
        foreach (var languageCode in languageCodes)
        {
            var detectors = _nationalIdRegistry.GetDetectors([languageCode]);
            foreach (var detector in detectors)
            {
                RegisterDetector(detector);
            }
        }
    }

    /// <summary>
    /// Resolves overlapping matches by keeping the highest confidence match.
    /// </summary>
    private static List<PIIMatch> ResolveOverlaps(List<PIIMatch> matches)
    {
        if (matches.Count <= 1)
            return matches;

        // Sort by start position, then by length (prefer longer matches)
        matches.Sort((a, b) =>
        {
            var posCompare = a.StartIndex.CompareTo(b.StartIndex);
            if (posCompare != 0)
                return posCompare;
            return b.Length.CompareTo(a.Length); // Longer first
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
