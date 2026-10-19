namespace FluxCurator.Core.Domain;

/// <summary>
/// Masking strategy for PII replacement.
/// </summary>
public enum MaskingStrategy
{
    /// <summary>
    /// Replace with type token: [EMAIL], [PHONE], etc.
    /// Best for RAG as it preserves semantic meaning.
    /// </summary>
    Token,

    /// <summary>
    /// Replace with asterisks: ****@****.com
    /// </summary>
    Asterisk,

    /// <summary>
    /// Replace with X characters: XXXX@XXXX.XXX
    /// </summary>
    Character,

    /// <summary>
    /// Completely redact with fixed placeholder: [REDACTED]
    /// </summary>
    Redact,

    /// <summary>
    /// Partial masking preserving some characters: jo**@ex****.com
    /// </summary>
    Partial,

    /// <summary>
    /// Hash the value: [HASH:a1b2c3d4]
    /// </summary>
    Hash,

    /// <summary>
    /// Remove completely without replacement.
    /// </summary>
    Remove
}

/// <summary>
/// Configuration options for PII masking operations.
/// Supports multilingual PII detection through language codes.
/// </summary>
public sealed class PIIMaskingOptions
{
    /// <summary>
    /// Gets or sets the target language codes for PII detection.
    /// Uses ISO 639-1 codes (e.g., "ko", "en", "ja") or IETF tags (e.g., "en-US", "zh-CN").
    /// Multiple languages can be specified for multilingual documents.
    /// Use "auto" to detect PII for all supported languages (default).
    /// </summary>
    public IReadOnlyList<string> LanguageCodes { get; set; } = ["auto"];

    /// <summary>
    /// Gets or sets which PII types the built-in detectors detect and mask.
    /// Default: Common (Email, Phone, NationalId, CreditCard). Detectors you register yourself always run.
    /// </summary>
    public PIIType TypesToMask { get; set; } = PIIType.Common;

    /// <summary>
    /// Gets or sets the masking strategy to use.
    /// Default: Token (best for RAG).
    /// </summary>
    public MaskingStrategy Strategy { get; set; } = MaskingStrategy.Token;

    /// <summary>
    /// Gets or sets the minimum confidence threshold for detection.
    /// Range: 0.0 to 1.0. Default: 0.8.
    /// </summary>
    public float MinConfidence { get; set; } = 0.8f;

    /// <summary>
    /// Keys whose values are machine identifiers, never PII: a value written as <c>key=value</c>, <c>key: value</c> or
    /// <c>"key": value</c> under one of these keys is not reported, whatever its shape (<c>pid=0101234567</c>,
    /// <c>"port": 5432</c>). Compared case-insensitively. The default covers audit, process, network and journald
    /// cursor fields (<see cref="DefaultNonPiiKeys"/>); add your own, or clear it to judge every value by shape alone.
    /// </summary>
    /// <remarks>
    /// Independently of this set, a value that is one segment of a compound identifier - a token of
    /// <c>;</c>-separated <c>k=v</c> segments, at least two of the others hex ids of 8 or more characters, such as a journald
    /// cursor <c>s=4bf9…;i=43b37;m=0161431588;t=65c9…</c> - is not reported either.
    /// </remarks>
    public ISet<string> NonPiiKeys { get; set; } = new HashSet<string>(DefaultNonPiiKeys, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The keys <see cref="NonPiiKeys"/> starts with: Linux audit ids (<c>uid</c>, <c>auid</c>, <c>ses</c>, …), process and
    /// thread ids, ports, sizes, sequence numbers, timestamps and offsets, and the journald cursor fields.
    /// </summary>
    public static IReadOnlyList<string> DefaultNonPiiKeys { get; } =
    [
        "uid", "auid", "euid", "suid", "fsuid", "ouid", "gid", "egid", "sgid", "fsgid", "ogid", "ses", "session_id",
        "pid", "ppid", "tid", "port", "sport", "dport", "src_port", "dst_port", "srcport", "dstport", "lport", "rport",
        "bytes", "len", "length", "size", "seq", "seqno", "ack", "ts", "timestamp", "time", "epoch", "offset", "inode",
        "ino", "dev", "exit", "errno", "code", "status", "count",
        "__cursor", "__realtime_timestamp", "__monotonic_timestamp", "_pid", "_uid", "_gid", "_boot_id", "_machine_id",
    ];

    /// <summary>
    /// Gets or sets custom token formats for each PII type.
    /// </summary>
    public Dictionary<PIIType, string> CustomTokens { get; set; } = new();

    /// <summary>
    /// Gets or sets the character <see cref="MaskingStrategy.Partial"/> uses for the part of a value it hides.
    /// Default: '*'. <see cref="MaskingStrategy.Asterisk"/> always uses '*' and <see cref="MaskingStrategy.Character"/>
    /// always uses 'X'; this setting does not change them.
    /// </summary>
    public char MaskCharacter { get; set; } = '*';

    /// <summary>
    /// Gets or sets the number of characters to preserve in partial masking.
    /// Default: 2.
    /// </summary>
    public int PartialPreserveCount { get; set; } = 2;

    /// <summary>
    /// Gets the token format for a specific PII type.
    /// </summary>
    public string GetToken(PIIType type)
    {
        if (CustomTokens.TryGetValue(type, out var custom))
            return custom;

        return type switch
        {
            PIIType.Email => "[EMAIL]",
            PIIType.Phone => "[PHONE]",
            PIIType.NationalId => "[NATIONAL_ID]",
            PIIType.TaxId => "[TAX_ID]",
            PIIType.SocialSecurityNumber => "[SSN]",
            PIIType.CreditCard => "[CARD]",
            PIIType.BankAccount => "[ACCOUNT]",
            PIIType.Passport => "[PASSPORT]",
            PIIType.DriversLicense => "[LICENSE]",
            PIIType.IPAddress => "[IP]",
            PIIType.UrlCredential => "[CREDENTIAL]",
            PIIType.HardwareAddress => "[MAC]",
            PIIType.PersonName => "[NAME]",
            PIIType.Address => "[ADDRESS]",
            PIIType.Custom => "[PII]",
            _ => "[REDACTED]"
        };
    }

    // ========================================
    // Factory Methods - Language-Agnostic (Recommended)
    // ========================================

    /// <summary>
    /// Creates default masking options optimized for RAG.
    /// Detects PII for all supported languages.
    /// </summary>
    public static PIIMaskingOptions Default => new();

    /// <summary>
    /// The identifiers a region's law singles out, masked by the region presets on top of <see cref="PIIType.Common"/>.
    /// Keyed by the primary language subtag.
    /// </summary>
    /// <remarks>
    /// <c>ko</c>: the unique identifiers of the Personal Information Protection Act, Art. 24 — resident registration
    /// numbers (foreigner registration numbers included, both <see cref="PIIType.NationalId"/>), passport numbers and
    /// driver's license numbers.
    /// </remarks>
    private static readonly Dictionary<string, PIIType> StatutoryIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ko"] = PIIType.NationalId | PIIType.Passport | PIIType.DriversLicense,
    };

    private static PIIType RegionTypes(IEnumerable<string> languageCodes)
    {
        var types = PIIType.Common;
        foreach (var code in languageCodes)
        {
            var primary = code.Split('-', '_')[0];
            if (StatutoryIdentifiers.TryGetValue(primary, out var statutory))
            {
                types |= statutory;
            }
        }

        return types;
    }

    /// <summary>
    /// Creates options for a specific language/region: <see cref="PIIType.Common"/> plus the identifiers that region's
    /// law singles out — for Korean (<c>ko</c>), passport and driver's license numbers too (PIPA Art. 24).
    /// </summary>
    /// <param name="languageCode">ISO 639-1 language code or IETF tag (e.g., "ko", "en-US", "ja")</param>
    /// <returns>Options configured for the specified language.</returns>
    public static PIIMaskingOptions ForLanguage(string languageCode) => new()
    {
        LanguageCodes = [languageCode],
        TypesToMask = RegionTypes([languageCode]),
        Strategy = MaskingStrategy.Token
    };

    /// <summary>
    /// Creates options for multiple languages: <see cref="PIIType.Common"/> plus the identifiers each region's law singles
    /// out (see <see cref="ForLanguage"/>). Useful for multilingual documents.
    /// </summary>
    /// <param name="languageCodes">ISO 639-1 language codes or IETF tags.</param>
    /// <returns>Options configured for the specified languages.</returns>
    public static PIIMaskingOptions ForLanguages(params string[] languageCodes) => new()
    {
        LanguageCodes = languageCodes,
        TypesToMask = RegionTypes(languageCodes),
        Strategy = MaskingStrategy.Token
    };

    /// <summary>
    /// Creates options for strict masking (all PII types, all languages).
    /// </summary>
    public static PIIMaskingOptions Strict => new()
    {
        LanguageCodes = ["auto"],
        TypesToMask = PIIType.All,
        Strategy = MaskingStrategy.Token,
        MinConfidence = 0.7f
    };

    /// <summary>
    /// Creates options for partial masking (preserving some characters).
    /// </summary>
    public static PIIMaskingOptions Partial => new()
    {
        LanguageCodes = ["auto"],
        TypesToMask = PIIType.Common,
        Strategy = MaskingStrategy.Partial,
        PartialPreserveCount = 3
    };
}
