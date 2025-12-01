namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using FluxCurator.Core.Domain;

/// <summary>
/// Detects Brazilian CPF (Cadastro de Pessoas Físicas).
/// Format: XXX.XXX.XXX-XX (11 digits)
/// Validation uses two Modulo-11 check digits.
/// </summary>
public sealed class BrazilCPFDetector : NationalIdDetectorBase
{
    /// <inheritdoc/>
    public override string LanguageCode => "pt-BR";

    /// <inheritdoc/>
    public override string NationalIdType => "CPF";

    /// <inheritdoc/>
    public override string FormatDescription => "XXX.XXX.XXX-XX (11 digits with 2 check digits)";

    /// <inheritdoc/>
    public override string CountryName => "Brazil";

    /// <inheritdoc/>
    public override string Name => "Brazil CPF Detector";

    /// <inheritdoc/>
    protected override string Pattern =>
        // Format: XXX.XXX.XXX-XX with optional separators
        @"\d{3}[.\s]?\d{3}[.\s]?\d{3}[-.\s]?\d{2}";

    /// <inheritdoc/>
    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.0f;

        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = NormalizeValue(value);

        if (normalized.Length != 11)
            return false;

        if (!normalized.All(char.IsDigit))
            return false;

        // Check for obviously invalid patterns (all same digit)
        if (normalized.Distinct().Count() == 1)
        {
            confidence = 0.2f;
            return false;
        }

        // Validate first check digit
        var firstCheckDigit = CalculateFirstCheckDigit(normalized);
        if (firstCheckDigit != (normalized[9] - '0'))
        {
            confidence = 0.6f;
            return true; // Still consider it PII
        }

        // Validate second check digit
        var secondCheckDigit = CalculateSecondCheckDigit(normalized);
        if (secondCheckDigit != (normalized[10] - '0'))
        {
            confidence = 0.7f;
            return true; // Still consider it PII
        }

        // All validations passed
        confidence = 0.98f;
        return true;
    }

    /// <summary>
    /// Calculates the first check digit using Modulo-11.
    /// Weights: 10, 9, 8, 7, 6, 5, 4, 3, 2
    /// </summary>
    private static int CalculateFirstCheckDigit(string cpf)
    {
        int[] weights = [10, 9, 8, 7, 6, 5, 4, 3, 2];

        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (cpf[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>
    /// Calculates the second check digit using Modulo-11.
    /// Weights: 11, 10, 9, 8, 7, 6, 5, 4, 3, 2
    /// </summary>
    private static int CalculateSecondCheckDigit(string cpf)
    {
        int[] weights = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            sum += (cpf[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>
    /// Formats a CPF number with standard punctuation.
    /// </summary>
    public static string Format(string cpf)
    {
        var normalized = cpf.Replace(".", "").Replace("-", "").Replace(" ", "");
        if (normalized.Length != 11)
            return cpf;

        return $"{normalized[..3]}.{normalized[3..6]}.{normalized[6..9]}-{normalized[9..11]}";
    }
}
