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
    private static readonly Dictionary<string, string[]> KnownUnread = new()
    {
        ["FluxCurator.Core.Domain.ChunkOptions"] =
        [
            "IncludeMetadata", "PreserveSectionHeaders",
        ],
        ["FluxCurator.Core.Domain.ContentFilterOptions"] = ["IncludeMetadata"],
        ["FluxCurator.Core.Domain.PIIMaskingOptions"] =
        [
            "EnableParallelProcessing", "IncludeMetadata", "ParallelThreshold", "ValidatePatterns",
        ],
    };

    [Fact]
    public void EveryPublicOption_IsRead() =>
        OptionsReachability.Scan(Libraries, OptionsTypes.NamedWith("Options", "Config"))
            .ShouldMatchRoster(KnownUnread);
}
