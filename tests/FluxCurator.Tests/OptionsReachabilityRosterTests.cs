using System.Reflection;
using Iyu.Conventions.Testing;
using Xunit;

namespace FluxCurator.Tests;

/// <summary>
/// Every public option in this library is read by the library. An option nothing reads is a promise it does not keep:
/// a caller sets it, and nothing changes and nothing is reported. The roster fails both ways - a new unread option,
/// and a listed one that has since been wired - so each change is recorded on purpose.
/// </summary>
public class OptionsReachabilityRosterTests
{
    private static readonly Assembly[] Libraries =
    [
        Assembly.Load("FluxCurator"),
        Assembly.Load("FluxCurator.Core"),
    ];

    /// <summary>
    /// Options accepted as unread today. Shrink this list; never grow it silently.
    /// <para>
    /// Opening baseline (2026-09-20): 7 unread public options across 3 types, recorded as found rather than
    /// as judged - none has been investigated, so none carries a reason of its own. Recording them is what makes
    /// the gate start green and makes the *next* unread option a failure instead of silently joining a crowd.
    /// </para>
    /// <para>
    /// The assembly list above must cover every assembly this repository ships. Scanning only the main one
    /// reports options that a sibling assembly reads as unread - this repository first reported zero because only FluxCurator was scanned, and the seven
    /// below live in FluxCurator.Core.
    /// </para>
    /// </summary>
    /// <para>
    /// Now empty, and the seven closed in three different ways.
    /// </para>
    /// <para>
    /// Wired: <c>ChunkOptions.PreserveSectionHeaders</c>. The header was carried into a section's
    /// first chunk unconditionally, so the option could not turn it off - and the wiring exposed a
    /// second defect, because the header was also prepended a second time to the first chunk of a
    /// split section. It now enters once, where sections are parsed.
    /// </para>
    /// <para>
    /// Removed as a switch over something the library always does and callers cannot opt out of:
    /// the three <c>IncludeMetadata</c> copies (chunk metadata is not optional - the chunker itself
    /// reads <c>EstimatedTokenCount</c> to merge and split; neither <c>PIIMaskingResult</c> nor
    /// <c>ContentFilterResult</c> has a metadata field to gate) and
    /// <c>PIIMaskingOptions.ValidatePatterns</c>. That last one is worth a sentence: checksum
    /// validation is real and thorough - Luhn for cards, ISO 7064, Modulo-97 and a dozen national
    /// schemes - but every detector runs it unconditionally and none of them takes an options
    /// object, so the switch had nowhere to land. Detection quality is not a knob here.
    /// </para>
    /// <para>
    /// Removed as a feature that does not exist: <c>EnableParallelProcessing</c> and
    /// <c>ParallelThreshold</c>. There is no parallelism in the PII path at all - no
    /// <c>Parallel.*</c>, no <c>AsParallel</c>, no <c>Task.WhenAll</c> anywhere in
    /// <c>FluxCurator.Core</c>; masking is a sequential loop that never looks at the text length.
    /// </para>
    private static readonly Dictionary<string, string[]> KnownUnread = new();

    [Fact]
    public void EveryPublicOption_IsRead() =>
        OptionsReachability.Scan(Libraries, OptionsTypes.NamedWith("Options", "Config"))
            .ShouldMatchRoster(KnownUnread);
}
