namespace FluxCurator.Core.Infrastructure.PII;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects credit card numbers in text.
/// Validates using the Luhn algorithm.
/// </summary>
public sealed class CreditCardDetector : PIIDetectorBase
{
    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.CreditCard;

    /// <inheritdoc/>
    public override string Name => "Credit Card Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        @"(?:" +
            // Standard 16-digit with separators: 1234-5678-9012-3456
            @"\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}|" +
            // American Express format: 3xxx-xxxxxx-xxxxx (15 digits)
            @"3[47]\d{2}[-\s]?\d{6}[-\s]?\d{5}|" +
            // Continuous digits (13-19 digits)
            @"\d{13,19}" +
        @")";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = NormalizeValue(value);

        // Credit cards are typically 13-19 digits
        if (normalized.Length < 13 || normalized.Length > 19)
            return false;

        if (!normalized.All(char.IsDigit))
            return false;

        // Check if it matches known card network prefixes
        var cardType = IdentifyCardType(normalized);
        if (cardType == CardType.Unknown)
        {
            confidence = 0.5f;
            return false;
        }

        // Validate using Luhn algorithm
        if (!ValidateLuhn(normalized))
        {
            // Valid prefix but failed Luhn - could still be PII attempt
            confidence = 0.6f;
            return true;
        }

        // Passed all validations
        confidence = cardType switch
        {
            CardType.Visa or CardType.Mastercard or CardType.Amex => 0.98f,
            CardType.Discover or CardType.JCB => 0.95f,
            CardType.KoreanBC or CardType.KoreanShinhan or CardType.KoreanSamsung => 0.95f,
            _ => 0.9f
        };

        return true;
    }

    /// <summary>
    /// Validates a credit card number using the Luhn algorithm.
    /// </summary>
    private static bool ValidateLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            int digit = number[i] - '0';

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// Identifies the card type based on the number prefix.
    /// </summary>
    private static CardType IdentifyCardType(string number)
    {
        if (string.IsNullOrEmpty(number) || number.Length < 13)
            return CardType.Unknown;

        // Visa: starts with 4
        if (number.StartsWith('4') && (number.Length == 13 || number.Length == 16 || number.Length == 19))
            return CardType.Visa;

        // Mastercard: 51-55 or 2221-2720
        if (number.Length == 16)
        {
            if (int.TryParse(number[..2], out var prefix2) && prefix2 >= 51 && prefix2 <= 55)
                return CardType.Mastercard;

            if (int.TryParse(number[..4], out var prefix4) && prefix4 >= 2221 && prefix4 <= 2720)
                return CardType.Mastercard;
        }

        // American Express: 34 or 37
        if ((number.StartsWith("34", StringComparison.Ordinal) || number.StartsWith("37", StringComparison.Ordinal)) && number.Length == 15)
            return CardType.Amex;

        // Discover: 6011, 644-649, 65
        if (number.Length == 16)
        {
            if (number.StartsWith("6011", StringComparison.Ordinal) || number.StartsWith("65", StringComparison.Ordinal))
                return CardType.Discover;

            if (int.TryParse(number[..3], out var prefix3) && prefix3 >= 644 && prefix3 <= 649)
                return CardType.Discover;
        }

        // JCB: 3528-3589
        if (number.Length == 16)
        {
            if (int.TryParse(number[..4], out var prefix4) && prefix4 >= 3528 && prefix4 <= 3589)
                return CardType.JCB;
        }

        // Korean card prefixes (examples - actual prefixes may vary)
        // BC Card
        if (number.StartsWith("94", StringComparison.Ordinal) && number.Length == 16)
            return CardType.KoreanBC;

        // Samsung Card
        if (number.StartsWith('9') && number.Length == 16)
            return CardType.KoreanSamsung;

        // Generic valid-looking card (16 digits starting with valid digit)
        if (number.Length == 16 && number[0] >= '3' && number[0] <= '6')
            return CardType.Generic;

        return CardType.Unknown;
    }

    /// <summary>
    /// Card type enumeration.
    /// </summary>
    private enum CardType
    {
        Unknown,
        Generic,
        Visa,
        Mastercard,
        Amex,
        Discover,
        JCB,
        DinersClub,
        KoreanBC,
        KoreanShinhan,
        KoreanSamsung
    }
}
