using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace FluxCurator.Tests;

/// <summary>
/// Compiles every <c>```csharp</c> block in the README(s) against the current assemblies. A compiler checks the receiver,
/// the arguments, the return types a block goes on to use, and the namespaces it needs.
/// </summary>
/// <remarks>
/// A block is compiled as a top-level program: its <c>using</c> lines are hoisted, the common usings below are added, and
/// the stand-ins below are declared when the block uses the name without declaring it — values a reader already has
/// from the surrounding text (a loaded model, an input file), not part of what the block shows.
/// </remarks>
public class ReadmeSnippetCompileTests
{
    // A block that is deliberately not a program (a signature sketch, pseudocode) is listed here by the heading it sits
    // under, with the reason. Shrink this, never grow it silently.
    private static readonly Dictionary<string, string> Fragments = new(StringComparer.Ordinal);

    private const string CommonUsings = """
        using System;
        using System.Collections.Generic;
        using System.IO;
        using System.Linq;
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        """;

    // The repository README reads as one guide: its Quick Start states these three usings once, and every later block
    // assumes them. ReadmeUsings_AreTheOnesTheQuickStartStates keeps this list and the README's statement the same.
    private static readonly string[] ReadmeUsings = ["FluxCurator", "FluxCurator.Core.Core", "FluxCurator.Core.Domain"];

    private static readonly (string Name, string Declaration)[] StandIns =
    [
        ("services", "IServiceCollection services = null!;"),
        ("text", "string text = \"\";"),
        ("largeText", "string largeText = \"\";"),
        ("rawText", "string rawText = \"\";"),
        ("markdownText", "string markdownText = \"\";"),
        ("largeDocument", "string largeDocument = \"\";"),
        ("extractedText", "string extractedText = \"\";"),
        ("koreanText", "string koreanText = \"\";"),
        ("pdfText", "string pdfText = \"\";"),
        ("chunks", "IReadOnlyList<FluxCurator.Core.Domain.DocumentChunk> chunks = [];"),
        ("curator", "FluxCurator.Curator curator = new();"),
        // Option snippets that are the body of a WithChunkingOptions(opt => { ... }) callback.
        ("opt", "FluxCurator.Core.Domain.ChunkOptions opt = new();"),
        ("myEmbedder", "FluxCurator.Core.Core.IEmbedder myEmbedder = null!;"),
    ];

    // Types an earlier block declares and a later block uses. Appended after the body: a top-level program declares its
    // types after its statements.
    private static readonly (string Name, string Declaration)[] TypeStandIns =
    [
        ("SingaporeNricDetector", """
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
            """),
        ("OpenAIEmbedder", """
            public class OpenAIEmbedder : FluxCurator.Core.Core.IEmbedder
            {
                public int EmbeddingDimension => 1536;
                public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) => Task.FromResult(new float[1536]);
                public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<float[]>>([]);
                public float CalculateSimilarity(float[] embedding1, float[] embedding2) => 0;
            }
            """),
        ("DocumentProcessor", """
            public class DocumentProcessor(FluxCurator.Core.Core.IChunkerFactory chunkerFactory)
            {
                public Task<IReadOnlyList<FluxCurator.Core.Domain.DocumentChunk>> ProcessAsync(string text, FluxCurator.Core.Domain.ChunkingStrategy strategy) =>
                    chunkerFactory.CreateChunker(strategy).ChunkAsync(text, FluxCurator.Core.Domain.ChunkOptions.Default);
            }
            """),
    ];

    private static readonly Dictionary<string, (string Name, string Declaration)[]> DocumentStandIns = new(StringComparer.Ordinal)
    {
        // chunking-strategies.md creates its factory once, in the first example.
        ["docs/chunking-strategies.md"] = [("factory", "FluxCurator.Infrastructure.Chunking.ChunkerFactory factory = new();")],
    };

    private static readonly string[] AssembliesToLoad =
    [
        "FluxCurator.Core", "FluxCurator",
        "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.DependencyInjection.Abstractions",
    ];

    public static TheoryData<string> Blocks()
    {
        var data = new TheoryData<string>();
        foreach (var block in ReadBlocks())
            data.Add(block.Key);
        return data;
    }

    [Theory]
    [MemberData(nameof(Blocks))]
    public void ReadmeBlock_Compiles(string key)
    {
        var block = ReadBlocks().Single(b => b.Key == key);
        if (Fragments.ContainsKey(block.Heading))
            return;

        var errors = Compile(block.Code, block.Document);

        Assert.True(errors.IsEmpty,
            $"README block {key} does not compile against the current API:\n" +
            string.Join("\n", errors.Select(e => e.ToString())) + "\n--- source ---\n" + Program(block.Code, block.Document));
    }

    [Fact]
    public void EveryReadmeBlock_IsFoundAndFragmentsNameRealHeadings()
    {
        var blocks = ReadBlocks();
        Assert.True(blocks.Count >= 15, $"expected the README's C# blocks, found {blocks.Count}");
        Assert.All(Fragments.Keys, heading => Assert.Contains(blocks, b => b.Heading == heading));
    }

    [Fact]
    public void ReadmeUsings_AreTheOnesTheQuickStartStates()
    {
        var quickStart = ReadBlocks().First(b => b.Document == "README.md");
        var stated = quickStart.Code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Select(l => Regex.Match(l, @"^using ([\w.]+);"))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value);

        Assert.Equal(ReadmeUsings, stated);
    }

    /// <summary>Positive control: the compiler rejects a call the library does not have.</summary>
    [Fact]
    public void Compile_RejectsAMethodTheLibraryDoesNotHave()
    {
        var errors = Compile("""
            services.AddFluxCuratorThatDoesNotExist();
            """);

        Assert.NotEmpty(errors);
    }

    private sealed record Block(string Key, string Document, string Heading, string Code);

    // The repository README and every package README: the package READMEs are what nuget.org shows each package's readers.
    private static IEnumerable<string> Documents()
    {
        var root = RepoRoot();
        yield return Path.Combine(root, "README.md");
        foreach (var readme in Directory.GetDirectories(Path.Combine(root, "src")).Order(StringComparer.Ordinal)
                     .Select(d => Path.Combine(d, "README.md")).Where(File.Exists))
            yield return readme;

        // The guides under docs/ are read the same way. fileflux-integration.md shows FileFlux's side of the integration:
        // its types live in the FileFlux package, which depends on this one, so this test project cannot reference it.
        foreach (var doc in Directory.GetFiles(Path.Combine(root, "docs"), "*.md").Order(StringComparer.Ordinal)
                     .Where(d => Path.GetFileName(d) != "fileflux-integration.md"))
            yield return doc;
    }

    private static List<Block> ReadBlocks()
    {
        var root = RepoRoot();
        return Documents().SelectMany(path => ReadBlocks(path, Path.GetRelativePath(root, path).Replace('\\', '/'))).ToList();
    }

    private static List<Block> ReadBlocks(string path, string document)
    {
        var lines = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var blocks = new List<Block>();
        var heading = "(top)";
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith('#'))
                heading = lines[i].TrimStart('#').Trim();
            if (lines[i].Trim() != "```csharp")
                continue;

            var start = i + 1;
            var code = new StringBuilder();
            for (i++; i < lines.Length && lines[i].Trim() != "```"; i++)
                code.AppendLine(lines[i]);
            blocks.Add(new Block($"{document} line {start}: {heading}", document, heading, code.ToString()));
        }

        return blocks;
    }

    private static string Program(string code, string document = "README.md")
    {
        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        // "using X;   // what it is for" is a directive too: the README annotates its usings.
        static string Code(string l) => l.Split("//", 2)[0].TrimEnd();
        bool IsUsingDirective(string l) =>
            l.StartsWith("using ", StringComparison.Ordinal) && Code(l).EndsWith(';') && !l.StartsWith("using var ", StringComparison.Ordinal);

        var body = string.Join("\n", lines.Where(l => !IsUsingDirective(l)));
        var standIns = StandIns.Concat(DocumentStandIns.GetValueOrDefault(document, []))
            .Where(s => Regex.IsMatch(body, $@"\b{s.Name}\b")
                        && !Regex.IsMatch(body, $@"\b(var|[A-Z][\w<>?,\s]*)\s+{s.Name}\s*[=;]"))
            .Select(s => s.Declaration);

        var typeStandIns = TypeStandIns
            .Where(s => Regex.IsMatch(body, $@"\b{s.Name}\b") && !Regex.IsMatch(body, $@"\bclass\s+{s.Name}\b"))
            .Select(s => s.Declaration);

        // A block that states a using the README already assumes must not get it twice (CS0105 is a warning, but keep
        // the program as a reader would write it).
        var stated = lines.Where(IsUsingDirective).Select(Code).ToHashSet(StringComparer.Ordinal);
        var assumed = (IsGuide(document) ? ReadmeUsings : []).Select(n => $"using {n};").Where(u => !stated.Contains(u));

        return string.Join("\n", stated) + "\n" + CommonUsings + "\n"
               + string.Join("\n", assumed) + "\n"
               + string.Join("\n", PackageNamespaces(document).Select(n => $"using {n};")) + "\n"
               + string.Join("\n", standIns) + "\n" + body + "\n" + string.Join("\n", typeStandIns);
    }

    // The repository README and the docs/ guides assume the three usings the README's Quick Start states.
    private static bool IsGuide(string document) =>
        document == "README.md" || document.StartsWith("docs/", StringComparison.Ordinal);

    // A package README is read with that package's root namespace in scope (FluxCurator.Core for FluxCurator.Core):
    // the namespaces of its public types with the fewest segments.
    private static IEnumerable<string> PackageNamespaces(string document)
    {
        var match = Regex.Match(document, @"^src/(?<package>[^/]+)/README\.md$");
        if (!match.Success)
            return [];

        var namespaces = Assembly.Load(match.Groups["package"].Value).GetExportedTypes()
            .Select(t => t.Namespace).OfType<string>().Distinct().ToList();
        var fewest = namespaces.Min(n => n.Count(c => c == '.'));
        return namespaces.Where(n => n.Count(c => c == '.') == fewest);
    }

    private static ImmutableArray<Diagnostic> Compile(string code, string document = "README.md")
    {
        var tree = CSharpSyntaxTree.ParseText(Program(code, document), new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "ReadmeSnippet", [tree], References(),
            new CSharpCompilationOptions(
                // A block that only declares types (a test class, a service) is a library, not a program.
                tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.GlobalStatementSyntax>().Any()
                    ? OutputKind.ConsoleApplication
                    : OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        return compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
    }

    private static List<MetadataReference> References()
    {
        foreach (var name in AssembliesToLoad)
            Assembly.Load(name);

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
            paths.UnionWith(trusted.Split(Path.PathSeparator).Where(p => p.Length > 0));
        paths.UnionWith(AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.Location.Length > 0)
            .Select(a => a.Location));
        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FluxCurator.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("FluxCurator.slnx not found above the test output directory");
    }
}
