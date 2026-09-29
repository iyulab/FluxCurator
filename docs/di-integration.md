# Dependency Injection Integration

FluxCurator provides .NET dependency injection support with `IServiceCollection` extensions.

## Basic Setup

### Without Embedder (Core Features Only)

```csharp
using FluxCurator;

services.AddFluxCurator(options =>
{
    options.DefaultChunkOptions = ChunkOptions.Default;
    options.EnablePIIMasking = true;
    options.EnableContentFiltering = true;
});
```

### With External Embedder (Semantic Chunking)

```csharp
using FluxCurator;

// Register your IEmbedder implementation first
services.AddSingleton<IEmbedder>(myEmbedder);

// Then add FluxCurator (it uses the registered IEmbedder)
services.AddFluxCurator(options =>
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
| `IChunkerFactory` | Singleton | Factory for creating chunkers; semantic chunking when an `IEmbedder` is registered |
| `IFluxCurator` | Transient | The preprocessing pipeline, configured from `FluxCuratorOptions` |

Both are added with `TryAdd`, so a registration you make first wins. `IEmbedder` is yours to register; FluxCurator
does not register PII maskers or content filters as separate services — they are configured on the curator.

## Using IChunkerFactory

Inject `IChunkerFactory` for flexible chunker creation:

```csharp
public class DocumentProcessor(IChunkerFactory chunkerFactory)
{
    public Task<IReadOnlyList<DocumentChunk>> ProcessAsync(string text, ChunkingStrategy strategy)
    {
        var chunker = chunkerFactory.CreateChunker(strategy);
        return chunker.ChunkAsync(text, ChunkOptions.Default);
    }

    public Task<IReadOnlyList<DocumentChunk>> ProcessSmartAsync(string text)
    {
        // Semantic chunking is available only when an IEmbedder is registered
        if (chunkerFactory.IsStrategyAvailable(ChunkingStrategy.Semantic))
        {
            var chunker = chunkerFactory.CreateChunker(ChunkingStrategy.Semantic);
            return chunker.ChunkAsync(text, ChunkOptions.ForRAG);
        }

        // Fall back to sentence chunking
        var fallbackChunker = chunkerFactory.CreateChunker(ChunkingStrategy.Sentence);
        return fallbackChunker.ChunkAsync(text, ChunkOptions.Default);
    }
}
```

## Using IFluxCurator

Inject `IFluxCurator` for the complete preprocessing pipeline. The steps it runs (refinement, filtering, PII masking,
chunking) are the ones `FluxCuratorOptions` enabled at registration:

```csharp
using FluxCurator.Core;

public class RagService(IFluxCurator curator)
{
    public Task<PreprocessingResult> PrepareForRagAsync(string document) =>
        curator.PreprocessAsync(document);
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
        Strategy = MaskingStrategy.Token,
        TypesToMask = PIIType.Email | PIIType.Phone | PIIType.NationalId | PIIType.CreditCard
    };

    // Content filtering configuration
    var filterOptions = new ContentFilterOptions { CategoriesToFilter = ContentCategory.All };
    filterOptions.CustomBlocklist.Add("blocked-word");   // the set is case-insensitive; add to it rather than replace it
    options.ContentFilterOptions = filterOptions;
});
```

## Custom Embedder Registration

Register your own `IEmbedder` implementation (an example implementation follows):

```csharp
// Register custom embedder
services.AddSingleton<IEmbedder, OpenAIEmbedder>();

// Then add FluxCurator (it uses the registered IEmbedder)
services.AddFluxCurator(options =>
{
    options.DefaultChunkOptions = ChunkOptions.ForRAG;
});
```

### Custom Embedder Example

```csharp
using System.Net.Http;
using System.Net.Http.Json;

public class OpenAIEmbedder(HttpClient httpClient) : IEmbedder
{
    public int EmbeddingDimension => 1536;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // Call the embeddings API
        var response = await httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings",
            new { input = text, model = "text-embedding-3-small" },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        return result!.Data[0].Embedding;
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

    private sealed record EmbeddingResponse(EmbeddingData[] Data);

    private sealed record EmbeddingData(float[] Embedding);
}
```

## ASP.NET Core Integration

### Minimal API Example

```csharp
using FluxCurator.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Register your embedder (optional, for semantic chunking)
builder.Services.AddSingleton<IEmbedder>(myEmbedder);

// Add FluxCurator
builder.Services.AddFluxCurator(options =>
{
    options.DefaultChunkOptions = ChunkOptions.ForRAG;
    options.EnablePIIMasking = true;
});

var app = builder.Build();

app.MapPost("/chunk", async (ChunkRequest request, IChunkerFactory factory) =>
{
    var chunker = factory.CreateChunker(request.Strategy);
    var chunks = await chunker.ChunkAsync(request.Text, request.Options ?? ChunkOptions.Default);
    return Results.Ok(chunks);
});

app.MapPost("/preprocess", (string text, IFluxCurator curator) => curator.PreprocessAsync(text));

app.Run();

public record ChunkRequest(string Text, ChunkingStrategy Strategy, ChunkOptions? Options);
```

## Testing with DI

### Mock IChunkerFactory

```csharp
using NSubstitute;
using Xunit;

public class DocumentProcessorTests
{
    [Fact]
    public async Task ProcessAsync_UsesTheRequestedStrategy()
    {
        // Arrange
        var chunker = Substitute.For<IChunker>();
        chunker
            .ChunkAsync(Arg.Any<string>(), Arg.Any<ChunkOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<DocumentChunk> { new() { Content = "Test" } });

        var factory = Substitute.For<IChunkerFactory>();
        factory.CreateChunker(ChunkingStrategy.Sentence).Returns(chunker);

        var processor = new DocumentProcessor(factory);

        // Act
        var result = await processor.ProcessAsync("Test text", ChunkingStrategy.Sentence);

        // Assert
        Assert.Single(result);
        factory.Received(1).CreateChunker(ChunkingStrategy.Sentence);
    }
}
```

## Service Lifetime Considerations

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IChunkerFactory` | Singleton | Stateless, thread-safe factory |
| `IEmbedder` | Your choice (Singleton recommended) | Usually expensive to create; must be thread-safe if Singleton |
| `IFluxCurator` | Transient | A curator holds its own configuration; each resolution gets a fresh one |
| `IChunker` | Transient | Created per use via the factory |

**Note**: If your embedder is not thread-safe, register it Scoped or Transient instead.
