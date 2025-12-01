namespace FluxCurator.Core.Domain;

/// <summary>
/// Types of Personally Identifiable Information (PII).
/// </summary>
[Flags]
public enum PIIType
{
    /// <summary>
    /// No PII type.
    /// </summary>
    None = 0,

    // ========================================
    // Global PII Types (language-agnostic)
    // ========================================

    /// <summary>
    /// Email addresses.
    /// </summary>
    Email = 1 << 0,

    /// <summary>
    /// Phone numbers (domestic and international).
    /// </summary>
    Phone = 1 << 1,

    /// <summary>
    /// Credit card numbers.
    /// </summary>
    CreditCard = 1 << 2,

    /// <summary>
    /// Bank account numbers.
    /// </summary>
    BankAccount = 1 << 3,

    /// <summary>
    /// Passport numbers.
    /// </summary>
    Passport = 1 << 4,

    /// <summary>
    /// Driver's license numbers.
    /// </summary>
    DriversLicense = 1 << 5,

    /// <summary>
    /// IP addresses (IPv4 and IPv6).
    /// </summary>
    IPAddress = 1 << 6,

    /// <summary>
    /// URLs and web addresses.
    /// </summary>
    URL = 1 << 7,

    /// <summary>
    /// Person names.
    /// </summary>
    PersonName = 1 << 8,

    /// <summary>
    /// Physical addresses.
    /// </summary>
    Address = 1 << 9,

    // ========================================
    // National ID Types (language-specific)
    // ========================================

    /// <summary>
    /// National identification number (language-specific).
    /// Examples: SSN (US), NINO (UK), RRN (KR), My Number (JP), etc.
    /// Use with LanguageCodes in PIIMaskingOptions to specify target countries.
    /// </summary>
    NationalId = 1 << 10,

    /// <summary>
    /// Tax identification number (language-specific).
    /// Examples: TIN (US), Steuer-ID (DE), BRN (KR), etc.
    /// </summary>
    TaxId = 1 << 11,

    /// <summary>
    /// Social security/insurance number (language-specific).
    /// Examples: SSN (US), INSEE (FR), NINO (UK), etc.
    /// </summary>
    SocialSecurityNumber = 1 << 12,

    /// <summary>
    /// Custom/user-defined PII type.
    /// </summary>
    Custom = 1 << 30,

    // ========================================
    // Combination Types
    // ========================================

    /// <summary>
    /// All built-in PII types.
    /// </summary>
    All = Email | Phone | CreditCard | BankAccount | Passport |
          DriversLicense | IPAddress | URL | PersonName | Address |
          NationalId | TaxId | SocialSecurityNumber,

    /// <summary>
    /// Common PII types for general use.
    /// </summary>
    Common = Email | Phone | NationalId | CreditCard
}

/// <summary>
/// Represents a detected PII match in text.
/// </summary>
public sealed class PIIMatch
{
    /// <summary>
    /// Gets or sets the type of PII detected.
    /// </summary>
    public required PIIType Type { get; init; }

    /// <summary>
    /// Gets or sets the original matched text.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets or sets the start position in the original text.
    /// </summary>
    public required int StartIndex { get; init; }

    /// <summary>
    /// Gets or sets the end position in the original text.
    /// </summary>
    public required int EndIndex { get; init; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// Gets or sets the masked replacement value.
    /// </summary>
    public string? MaskedValue { get; set; }

    /// <summary>
    /// Gets the length of the matched text.
    /// </summary>
    public int Length => EndIndex - StartIndex;

    /// <summary>
    /// Creates a new PII match.
    /// </summary>
    public static PIIMatch Create(PIIType type, string value, int startIndex, float confidence = 1.0f)
    {
        return new PIIMatch
        {
            Type = type,
            Value = value,
            StartIndex = startIndex,
            EndIndex = startIndex + value.Length,
            Confidence = confidence
        };
    }
}
