# Dependency Injection Integration

FluxCurator provides full support for .NET dependency injection with `IServiceCollection` extensions.

## Basic Setup

### Without Embedder (Core Features Only)

```csharp
// Program.cs or Startup.cs
using FluxCurator;

services.AddFluxCurator(options =>
{
    options.DefaultChunkOptions = ChunkOptions.Default;
    options.EnablePIIMasking = true;
    options.EnableContentFiltering = true;
});
```

### With LocalEmbedder (Semantic Chunking)

```csharp
using FluxCurator;

services.AddFluxCuratorWithLocalEmbedder(options =>
{
    options.DefaultChunkOptions = new ChunkOptions
    {
        Strategy = ChunkingStrategy.Semantic,
        TargetChunkSize = 512,
        SemanticSimilarityThreshold = 0.5f
    };
    options.EnablePIIMasking = true;
});
```

## Registered Services

After calling `AddFluxCurator`, the following services are available:

| Service | Lifetime | Description |
|---------|----------|-------------|
| `IChunkerFactory` | Singleton | Factory for creating chunkers |
| `IFluxCurator` | Scoped | Main API for text processing |
| `IPIIMasker` | Singleton | PII detection and masking |
| `IContentFilter` | Singleton | Content filtering |
| `IEmbedder` | Singleton | Embedder (if using LocalEmbedder) |

## Using IChunkerFactory

Inject `IChunkerFactory` for flexible chunker creation:

```csharp
public class DocumentProcessor
{
    private readonly IChunkerFactory _chunkerFactory;

    public DocumentProcessor(IChunkerFactory chunkerFactory)
    {
        _chunkerFactory = chunkerFactory;
    }

    public async Task<IReadOnlyList<DocumentChunk>> ProcessAsync(
        string text,
        ChunkingStrategy strategy)
    {
        var chunker = _chunkerFactory.CreateChunker(strategy);
        return await chunker.ChunkAsync(text, ChunkOptions.Default);
    }

    public async Task<IReadOnlyList<DocumentChunk>> ProcessSmartAsync(string text)
    {
        // Check if semantic chunking is available
        if (_chunkerFactory.IsStrategyAvailable(ChunkingStrategy.Semantic))
        {
            var chunker = _chunkerFactory.CreateChunker(ChunkingStrategy.Semantic);
            return await chunker.ChunkAsync(text, ChunkOptions.ForRAG);
        }

        // Fall back to sentence chunking
        var fallbackChunker = _chunkerFactory.CreateChunker(ChunkingStrategy.Sentence);
        return await fallbackChunker.ChunkAsync(text, ChunkOptions.Default);
    }
}
```

## Using IFluxCurator

Inject `IFluxCurator` for the complete preprocessing pipeline:

```csharp
public class RAGService
{
    private readonly IFluxCurator _curator;

    public RAGService(IFluxCurator curator)
    {
        _curator = curator;
    }

    public async Task<PreprocessResult> PrepareForRAGAsync(string document)
    {
        return await _curator
            .WithContentFiltering()
            .WithPIIMasking()
            .PreprocessAsync(document);
    }
}
```

## Configuration Options

### FluxCuratorOptions

```csharp
services.AddFluxCurator(options =>
{
    // Default chunking options
    options.DefaultChunkOptions = new ChunkOptions
    {
        Strategy = ChunkingStrategy.Sentence,
        TargetChunkSize = 512,
        MaxChunkSize = 1024,
        MinChunkSize = 100,
        OverlapSize = 50,
        LanguageCode = null,  // Auto-detect
        PreserveSentences = true,
        PreserveParagraphs = true
    };

    // Enable/disable features
    options.EnablePIIMasking = true;
    options.EnableContentFiltering = true;

    // PII masking configuration
    options.PIIMaskingOptions = new PIIMaskingOptions
    {
        MaskingStrategy = MaskingStrategy.Token,
        DetectEmail = true,
        DetectPhone = true,
        DetectKoreanRRN = true,
        DetectCreditCard = true
    };

    // Content filtering configuration
    options.ContentFilteringOptions = new ContentFilteringOptions
    {
        FilterProfanity = true,
        FilterPersonalInfo = true,
        CustomBlocklist = new[] { "blocked-word" }
    };
});
```

## Custom Embedder Registration

Register your own `IEmbedder` implementation:

```csharp
// Register custom embedder
services.AddSingleton<IEmbedder, MyCustomEmbedder>();

// Then add FluxCurator (will use registered IEmbedder)
services.AddFluxCurator(options =>
{
    options.DefaultChunkOptions = ChunkOptions.ForRAG;
});
```

### Custom Embedder Example

```csharp
public class OpenAIEmbedder : IEmbedder
{
    private readonly HttpClient _httpClient;

    public int EmbeddingDimension => 1536;

    public OpenAIEmbedder(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        // Call OpenAI API
        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings",
            new { input = text, model = "text-embedding-ada-002" },
            cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return result.Data[0].Embedding;
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            results.Add(await GenerateEmbeddingAsync(text, cancellationToken));
        }
        return results;
    }

    public float CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        // Cosine similarity
        var dotProduct = embedding1.Zip(embedding2, (a, b) => a * b).Sum();
        var magnitude1 = Math.Sqrt(embedding1.Sum(x => x * x));
        var magnitude2 = Math.Sqrt(embedding2.Sum(x => x * x));
        return (float)(dotProduct / (magnitude1 * magnitude2));
    }
}
```

## ASP.NET Core Integration

### Minimal API Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add FluxCurator
builder.Services.AddFluxCuratorWithLocalEmbedder(options =>
{
    options.DefaultChunkOptions = ChunkOptions.ForRAG;
    options.EnablePIIMasking = true;
});

var app = builder.Build();

app.MapPost("/chunk", async (
    ChunkRequest request,
    IChunkerFactory factory) =>
{
    var chunker = factory.CreateChunker(request.Strategy);
    var chunks = await chunker.ChunkAsync(request.Text, request.Options);
    return Results.Ok(chunks);
});

app.Run();
```

### Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IFluxCurator _curator;
    private readonly IChunkerFactory _chunkerFactory;

    public DocumentController(
        IFluxCurator curator,
        IChunkerFactory chunkerFactory)
    {
        _curator = curator;
        _chunkerFactory = chunkerFactory;
    }

    [HttpPost("preprocess")]
    public async Task<ActionResult<PreprocessResult>> Preprocess(
        [FromBody] PreprocessRequest request)
    {
        var result = await _curator
            .WithContentFiltering()
            .WithPIIMasking()
            .PreprocessAsync(request.Text);

        return Ok(result);
    }

    [HttpPost("chunk")]
    public async Task<ActionResult<IReadOnlyList<DocumentChunk>>> Chunk(
        [FromBody] ChunkRequest request)
    {
        var chunker = _chunkerFactory.CreateChunker(request.Strategy);
        var options = request.Options ?? ChunkOptions.Default;
        var chunks = await chunker.ChunkAsync(request.Text, options);

        return Ok(chunks);
    }
}
```

## Testing with DI

### Mock IChunkerFactory

```csharp
using Moq;

[Fact]
public async Task ProcessDocument_UsesCorrectStrategy()
{
    // Arrange
    var mockChunker = new Mock<IChunker>();
    mockChunker
        .Setup(c => c.ChunkAsync(It.IsAny<string>(), It.IsAny<ChunkOptions>(), default))
        .ReturnsAsync(new List<DocumentChunk> { new() { Content = "Test" } });

    var mockFactory = new Mock<IChunkerFactory>();
    mockFactory
        .Setup(f => f.CreateChunker(ChunkingStrategy.Sentence))
        .Returns(mockChunker.Object);

    var processor = new DocumentProcessor(mockFactory.Object);

    // Act
    var result = await processor.ProcessAsync("Test text", ChunkingStrategy.Sentence);

    // Assert
    Assert.Single(result);
    mockFactory.Verify(f => f.CreateChunker(ChunkingStrategy.Sentence), Times.Once);
}
```

### Integration Testing

```csharp
public class ChunkingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChunkingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChunkEndpoint_ReturnsChunks()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new ChunkRequest
        {
            Text = "Hello world. This is a test.",
            Strategy = ChunkingStrategy.Sentence
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/document/chunk", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var chunks = await response.Content.ReadFromJsonAsync<List<DocumentChunk>>();
        Assert.NotEmpty(chunks);
    }
}
```

## Service Lifetime Considerations

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IChunkerFactory` | Singleton | Stateless, thread-safe factory |
| `IEmbedder` | Singleton | Expensive to create, thread-safe |
| `IFluxCurator` | Scoped | May hold request-specific state |
| `IChunker` | Transient | Created per use via factory |

**Note**: If your embedder is not thread-safe, consider using a factory pattern or scoped lifetime.
