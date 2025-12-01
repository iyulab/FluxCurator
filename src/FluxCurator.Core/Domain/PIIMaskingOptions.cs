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
    /// Gets or sets which PII types to detect and mask.
    /// Default: Common (Email, Phone, NationalId, CreditCard).
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
    /// Gets or sets whether to validate detected patterns (e.g., checksum validation).
    /// Default: true.
    /// </summary>
    public bool ValidatePatterns { get; set; } = true;

    /// <summary>
    /// Gets or sets custom token formats for each PII type.
    /// </summary>
    public Dictionary<PIIType, string> CustomTokens { get; set; } = new();

    /// <summary>
    /// Gets or sets the character to use for asterisk masking.
    /// Default: '*'.
    /// </summary>
    public char MaskCharacter { get; set; } = '*';

    /// <summary>
    /// Gets or sets the number of characters to preserve in partial masking.
    /// Default: 2.
    /// </summary>
    public int PartialPreserveCount { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to include detection metadata in results.
    /// Default: true.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to process text in parallel for large inputs.
    /// Default: true.
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum text length for parallel processing.
    /// Default: 10000 characters.
    /// </summary>
    public int ParallelThreshold { get; set; } = 10000;

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
            PIIType.URL => "[URL]",
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
    /// Creates options for a specific language/region.
    /// </summary>
    /// <param name="languageCode">ISO 639-1 language code or IETF tag (e.g., "ko", "en-US", "ja")</param>
    /// <returns>Options configured for the specified language.</returns>
    public static PIIMaskingOptions ForLanguage(string languageCode) => new()
    {
        LanguageCodes = [languageCode],
        TypesToMask = PIIType.Common,
        Strategy = MaskingStrategy.Token,
        ValidatePatterns = true
    };

    /// <summary>
    /// Creates options for multiple languages.
    /// Useful for multilingual documents.
    /// </summary>
    /// <param name="languageCodes">ISO 639-1 language codes or IETF tags.</param>
    /// <returns>Options configured for the specified languages.</returns>
    public static PIIMaskingOptions ForLanguages(params string[] languageCodes) => new()
    {
        LanguageCodes = languageCodes,
        TypesToMask = PIIType.Common,
        Strategy = MaskingStrategy.Token,
        ValidatePatterns = true
    };

    /// <summary>
    /// Creates options for strict masking (all PII types, all languages).
    /// </summary>
    public static PIIMaskingOptions Strict => new()
    {
        LanguageCodes = ["auto"],
        TypesToMask = PIIType.All,
        Strategy = MaskingStrategy.Token,
        MinConfidence = 0.7f,
        ValidatePatterns = true
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
