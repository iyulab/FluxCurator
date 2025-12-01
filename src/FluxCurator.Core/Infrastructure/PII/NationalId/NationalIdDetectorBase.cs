namespace FluxCurator.Core.Infrastructure.PII.NationalId;

using System.Text.RegularExpressions;
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Base class for national ID detectors with language/country awareness.
/// </summary>
public abstract class NationalIdDetectorBase : PIIDetectorBase, INationalIdDetector
{
    /// <inheritdoc/>
    public override PIIType PIIType => PIIType.NationalId;

    /// <inheritdoc/>
    public abstract string LanguageCode { get; }

    /// <inheritdoc/>
    public abstract string NationalIdType { get; }

    /// <inheritdoc/>
    public abstract string FormatDescription { get; }

    /// <inheritdoc/>
    public abstract string CountryName { get; }
}
