namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using System.Collections.Concurrent;
using FluxCurator.Core.Core;

/// <summary>
/// Registry for national ID detectors, allowing language-specific detector lookup.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public sealed class NationalIdRegistry : INationalIdRegistry
{
    private readonly ConcurrentDictionary<string, INationalIdDetector> _detectors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new NationalIdRegistry with default detectors registered.
    /// </summary>
    public NationalIdRegistry()
    {
        RegisterDefaultDetectors();
    }

    /// <summary>
    /// Creates a NationalIdRegistry without default detectors.
    /// </summary>
    /// <param name="registerDefaults">Whether to register default detectors.</param>
    public NationalIdRegistry(bool registerDefaults)
    {
        if (registerDefaults)
        {
            RegisterDefaultDetectors();
        }
    }

    /// <inheritdoc/>
    public void Register(INationalIdDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
        _detectors[detector.LanguageCode] = detector;
    }

    /// <inheritdoc/>
    public INationalIdDetector? GetDetector(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return null;

        // Try exact match first
        if (_detectors.TryGetValue(languageCode, out var detector))
            return detector;

        // Try base language (e.g., "en" for "en-US")
        var baseLang = GetBaseLanguage(languageCode);
        if (baseLang != languageCode && _detectors.TryGetValue(baseLang, out detector))
            return detector;

        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<INationalIdDetector> GetAllDetectors()
    {
        return _detectors.Values;
    }

    /// <inheritdoc/>
    public IEnumerable<INationalIdDetector> GetDetectors(IEnumerable<string> languageCodes)
    {
        ArgumentNullException.ThrowIfNull(languageCodes);

        var codes = languageCodes.ToList();

        // If "auto" is specified, return all detectors
        if (codes.Contains("auto", StringComparer.OrdinalIgnoreCase))
        {
            return GetAllDetectors();
        }

        var result = new List<INationalIdDetector>();
        foreach (var code in codes)
        {
            var detector = GetDetector(code);
            if (detector != null)
            {
                result.Add(detector);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetSupportedLanguages()
    {
        return _detectors.Keys;
    }

    /// <inheritdoc/>
    public bool HasDetector(string languageCode)
    {
        return GetDetector(languageCode) != null;
    }

    /// <summary>
    /// Registers all default national ID detectors.
    /// </summary>
    private void RegisterDefaultDetectors()
    {
        // Korean RRN
        Register(new KoreaRRNDetector());

        // US SSN
        Register(new USSSNDetector());

        // UK NINO
        Register(new UKNINODetector());

        // Japan My Number
        Register(new JapanMyNumberDetector());

        // China ID Card
        Register(new ChinaIdCardDetector());

        // Germany ID
        Register(new GermanyIdDetector());

        // France INSEE
        Register(new FranceINSEEDetector());

        // Spain DNI/NIE
        Register(new SpainDNIDetector());

        // Brazil CPF
        Register(new BrazilCPFDetector());

        // Italy Codice Fiscale
        Register(new ItalyCodiceFiscaleDetector());
    }

    /// <summary>
    /// Gets the base language code from an IETF language tag.
    /// </summary>
    private static string GetBaseLanguage(string languageCode)
    {
        var dashIndex = languageCode.IndexOf('-');
        return dashIndex > 0 ? languageCode[..dashIndex] : languageCode;
    }
}
