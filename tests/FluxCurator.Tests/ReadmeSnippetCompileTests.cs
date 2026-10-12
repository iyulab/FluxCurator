using System.Reflection;
using System.Text.RegularExpressions;
using Iyu.Conventions.Testing.Snippets;
using Xunit;

namespace FluxCurator.Tests;

/// <summary>
/// Compiles every C# block in the README(s) and the docs/ guides against the current assemblies. A compiler checks the
/// receiver, the arguments, the return types a block goes on to use, and the namespaces it needs.
/// </summary>
/// <remarks>
/// Each block is compiled as a top-level program by <see cref="SnippetCompiler"/>: its own usings, the common usings and
/// the ones its document assumes, and the stand-ins below when the block uses the name without declaring it — values and
/// types a reader already has from the surrounding text (a loaded model, an input file, a class an earlier block
/// declared), not part of what the block shows.
/// </remarks>
public partial class ReadmeSnippetCompileTests
{
    // A block that is deliberately not a program (a signature sketch, pseudocode), by the heading it sits under, with the
    // reason. Shrink this, never grow it silently.
    private static readonly SnippetRoster Fragments = new(new Dictionary<string, string>(StringComparer.Ordinal));

    // The implicit usings of an SDK project, plus dependency injection: every guide registers through IServiceCollection.
    private static readonly string[] CommonUsings =
    [
        "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Threading", "System.Threading.Tasks",
        "Microsoft.Extensions.DependencyInjection",
    ];

    // The repository README reads as one guide: its Quick Start states these three usings once, and every later block
    // assumes them. ReadmeUsings_AreTheOnesTheQuickStartStates keeps this list and the README's statement the same.
    private static readonly string[] ReadmeUsings = ["FluxCurator", "FluxCurator.Core.Core", "FluxCurator.Core.Domain"];

    private static readonly Dictionary<string, string> StandIns = new(StringComparer.Ordinal)
    {
        ["services"] = "Microsoft.Extensions.DependencyInjection.IServiceCollection services = null!;",
        ["text"] = "string text = \"\";",
        ["largeText"] = "string largeText = \"\";",
        ["rawText"] = "string rawText = \"\";",
        ["markdownText"] = "string markdownText = \"\";",
        ["largeDocument"] = "string largeDocument = \"\";",
        ["extractedText"] = "string extractedText = \"\";",
        ["koreanText"] = "string koreanText = \"\";",
        ["pdfText"] = "string pdfText = \"\";",
        ["chunks"] = "IReadOnlyList<FluxCurator.Core.Domain.DocumentChunk> chunks = [];",
        ["curator"] = "FluxCurator.Curator curator = new();",
        // Option snippets that are the body of a WithChunkingOptions(opt => { ... }) callback.
        ["opt"] = "FluxCurator.Core.Domain.ChunkOptions opt = new();",
        ["myEmbedder"] = "FluxCurator.Core.Core.IEmbedder myEmbedder = null!;",

        // Types an earlier block declares and a later block uses.
        ["SingaporeNricDetector"] = """
            public class SingaporeNricDetector : FluxCurator.Core.Infrastructure.PII.NationalId.NationalIdDetectorBase
            {
                public override string LanguageCode => "en-SG";
                public override string NationalIdType => "NRIC";
                public override string FormatDescription => "";
                public override string CountryName => "Singapore";
                public override string Name => "";
                protected override string Pattern => "x";
                protected override bool ValidateMatch(string value, out float confidence) { confidence = 1; return true; }
            }
            """,
        ["OpenAIEmbedder"] = """
            public class OpenAIEmbedder : FluxCurator.Core.Core.IEmbedder
            {
                public int EmbeddingDimension => 1536;
                public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) => Task.FromResult(new float[1536]);
                public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<float[]>>([]);
                public float CalculateSimilarity(float[] embedding1, float[] embedding2) => 0;
            }
            """,
        ["DocumentProcessor"] = """
            public class DocumentProcessor(FluxCurator.Core.Core.IChunkerFactory chunkerFactory)
            {
                public Task<IReadOnlyList<FluxCurator.Core.Domain.DocumentChunk>> ProcessAsync(string text, FluxCurator.Core.Domain.ChunkingStrategy strategy) =>
                    chunkerFactory.CreateChunker(strategy).ChunkAsync(text, FluxCurator.Core.Domain.ChunkOptions.Default);
            }
            """,
    };

    // Stand-ins one document adds to the common ones.
    private static readonly Dictionary<string, Dictionary<string, string>> DocumentStandIns = new(StringComparer.Ordinal)
    {
        // chunking-strategies.md creates its factory once, in the first example.
        ["docs/chunking-strategies.md"] = new(StringComparer.Ordinal)
        {
            ["factory"] = "FluxCurator.Infrastructure.Chunking.ChunkerFactory factory = new();",
        },
    };

    private static readonly IReadOnlyList<Microsoft.CodeAnalysis.MetadataReference> References = SnippetCompiler.ReferencesFor(
        [.. new[]
        {
            "FluxCurator.Core", "FluxCurator",
            "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.DependencyInjection.Abstractions",
        }.Select(Assembly.Load)]);

    private static readonly IReadOnlyList<DocSnippet> Snippets = [.. Documents().SelectMany(d => DocSnippets.Read(d.Path, d.Name))];

    public static TheoryData<string> Blocks() => new(Snippets.Select(s => s.Key));

    [Theory]
    [MemberData(nameof(Blocks))]
    public void ReadmeBlock_Compiles(string key)
    {
        var snippet = Snippets.Single(s => s.Key == key);
        if (!Fragments.IsFragment(snippet))
            CompilerFor(snippet.File).ShouldCompile(snippet);
    }

    [Fact]
    public void EveryReadmeBlock_IsFoundAndFragmentsNameRealHeadings() => Fragments.ShouldMatch([.. Snippets], atLeast: 15);

    [Fact]
    public void ReadmeUsings_AreTheOnesTheQuickStartStates()
    {
        var quickStart = Snippets.First(s => s.File == "README.md");
        var stated = quickStart.Code.Split('\n')
            .Select(l => UsingDirective().Match(l))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value);

        Assert.Equal(ReadmeUsings, stated);
    }

    /// <summary>Positive control: the compiler rejects a call the library does not have.</summary>
    [Fact]
    public void Compile_RejectsAMethodTheLibraryDoesNotHave()
    {
        var errors = CompilerFor("README.md").Compile("""
            services.AddFluxCuratorThatDoesNotExist();
            """);

        Assert.NotEmpty(errors);
    }

    private static SnippetCompiler CompilerFor(string document) => new(References)
    {
        Usings = [.. CommonUsings, .. IsGuide(document) ? ReadmeUsings : [], .. PackageNamespaces(document)],
        StandIns = DocumentStandIns.TryGetValue(document, out var extra)
            ? new Dictionary<string, string>(StandIns.Concat(extra), StringComparer.Ordinal)
            : StandIns,
    };

    // The repository README and every package README: the package READMEs are what nuget.org shows each package's readers.
    // The guides under docs/ are read the same way. fileflux-integration.md shows FileFlux's side of the integration: its
    // types live in the FileFlux package, which depends on this one, so this test project cannot reference it.
    private static IEnumerable<(string Path, string Name)> Documents()
    {
        var root = RepoRoot();
        yield return (Path.Combine(root, "README.md"), "README.md");
        foreach (var readme in Directory.GetDirectories(Path.Combine(root, "src")).Order(StringComparer.Ordinal)
                     .Select(d => Path.Combine(d, "README.md")).Where(File.Exists))
            yield return (readme, Path.GetRelativePath(root, readme).Replace('\\', '/'));

        foreach (var doc in Directory.GetFiles(Path.Combine(root, "docs"), "*.md").Order(StringComparer.Ordinal)
                     .Where(d => Path.GetFileName(d) != "fileflux-integration.md"))
            yield return (doc, Path.GetRelativePath(root, doc).Replace('\\', '/'));
    }

    // The repository README and the docs/ guides assume the three usings the README's Quick Start states.
    private static bool IsGuide(string document) =>
        document == "README.md" || document.StartsWith("docs/", StringComparison.Ordinal);

    // A package README is read with that package's root namespace in scope (FluxCurator.Core for FluxCurator.Core):
    // the namespaces of its public types with the fewest segments.
    private static IEnumerable<string> PackageNamespaces(string document)
    {
        var match = PackageReadme().Match(document);
        if (!match.Success)
            return [];

        var namespaces = Assembly.Load(match.Groups["package"].Value).GetExportedTypes()
            .Select(t => t.Namespace).OfType<string>().Distinct().ToList();
        var fewest = namespaces.Min(n => n.Count(c => c == '.'));
        return namespaces.Where(n => n.Count(c => c == '.') == fewest);
    }

    [GeneratedRegex(@"^src/(?<package>[^/]+)/README\.md$")]
    private static partial Regex PackageReadme();

    [GeneratedRegex(@"^using ([\w.]+);")]
    private static partial Regex UsingDirective();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FluxCurator.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("FluxCurator.slnx not found above the test output directory");
    }
}
