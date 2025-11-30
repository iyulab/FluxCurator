# FileFlux Integration

FluxCurator integrates seamlessly with FileFlux for document processing pipelines.

## Overview

FileFlux handles document parsing (PDF, DOCX, etc.) while FluxCurator provides advanced text chunking. Together, they form a complete document processing solution for RAG pipelines.

```
Document → FileFlux (Parse) → FluxCurator (Chunk) → Vector DB
```

## Integration Components

FluxCurator provides two integration components:

1. **FluxCuratorChunkAdapter** - Converts between chunk types
2. **FluxCuratorChunkingStrategy** - FileFlux strategy implementation

## Using FluxCuratorChunkingStrategy

Implement FileFlux's `IChunkingStrategy` with FluxCurator:

```csharp
using FileFlux.Infrastructure.Strategies;
using FluxCurator.Core.Core;
using FluxCurator.Infrastructure.Chunking;

// Create chunker factory
var embedder = new LocalEmbedder();  // Or null for non-semantic
var chunkerFactory = new ChunkerFactory(embedder);

// Create FluxCurator strategy for FileFlux
var strategy = new FluxCuratorChunkingStrategy(
    chunkerFactory,
    ChunkingStrategy.Hierarchical);

// Use with FileFlux
var fileFluxChunks = await strategy.ChunkAsync(documentContent, options);
```

### Available Strategy Factories

```csharp
// Extension methods for common strategies
var sentenceStrategy = chunkerFactory.CreateSentenceStrategy();
var paragraphStrategy = chunkerFactory.CreateParagraphStrategy();
var tokenStrategy = chunkerFactory.CreateTokenStrategy();
var semanticStrategy = chunkerFactory.CreateSemanticStrategy();
var hierarchicalStrategy = chunkerFactory.CreateHierarchicalStrategy();
```

## Converting Between Chunk Types

### FluxCurator to FileFlux

```csharp
using FileFlux.Infrastructure.Adapters;

// Single chunk
var fileFluxChunk = fluxCuratorChunk.ToFileFluxChunk(parsedId, rawId);

// Multiple chunks
var fileFluxChunks = fluxCuratorChunks.ToFileFluxChunks(parsedId, rawId);
```

### FileFlux to FluxCurator

```csharp
// Single chunk
var fluxCuratorChunk = fileFluxChunk.ToFluxCuratorChunk();

// Multiple chunks
var fluxCuratorChunks = fileFluxChunks.ToFluxCuratorChunks();
```

## Complete Pipeline Example

```csharp
using FileFlux;
using FileFlux.Infrastructure.Strategies;
using FluxCurator.Infrastructure.Chunking;

public class DocumentPipeline
{
    private readonly IDocumentParser _parser;
    private readonly FluxCuratorChunkingStrategy _chunkingStrategy;
    private readonly IVectorStore _vectorStore;

    public DocumentPipeline(
        IDocumentParser parser,
        IChunkerFactory chunkerFactory,
        IVectorStore vectorStore)
    {
        _parser = parser;
        _chunkingStrategy = new FluxCuratorChunkingStrategy(
            chunkerFactory,
            ChunkingStrategy.Hierarchical);
        _vectorStore = vectorStore;
    }

    public async Task ProcessDocumentAsync(string filePath)
    {
        // 1. Parse document with FileFlux
        var content = await _parser.ParseAsync(filePath);

        // 2. Configure chunking options
        var options = new ChunkingOptions
        {
            MaxChunkSize = 1024,
            MinChunkSize = 100,
            OverlapSize = 50,
            PreserveParagraphs = true
        };

        // 3. Chunk with FluxCurator strategy
        var chunks = await _chunkingStrategy.ChunkAsync(content, options);

        // 4. Store in vector database
        foreach (var chunk in chunks)
        {
            await _vectorStore.AddAsync(chunk);
        }
    }
}
```

## Dependency Injection Setup

```csharp
// Program.cs
services.AddFileFlux();
services.AddFluxCuratorWithLocalEmbedder(options =>
{
    options.DefaultChunkOptions = ChunkOptions.ForRAG;
});

// Register FluxCurator chunking strategy for FileFlux
services.AddSingleton<IChunkingStrategy>(sp =>
{
    var factory = sp.GetRequiredService<IChunkerFactory>();
    return new FluxCuratorChunkingStrategy(factory, ChunkingStrategy.Hierarchical);
});
```

## Property Mapping

### FluxCurator → FileFlux Mapping

| FluxCurator Property | FileFlux Property |
|---------------------|-------------------|
| `Id` | `Id` (parsed as Guid) |
| `Content` | `Content` |
| `Index` | `Index` |
| `TotalChunks` | `SourceInfo.ChunkCount` |
| `Location.StartPosition` | `Location.StartChar` |
| `Location.EndPosition` | `Location.EndChar` |
| `Location.SectionPath` | `Location.HeadingPath` (split by " > ") |
| `Metadata.LanguageCode` | `SourceInfo.Language` |
| `Metadata.EstimatedTokenCount` | `Tokens` |
| `Metadata.Strategy` | `Strategy` |
| `Metadata.QualityScore` | `Quality` |
| `Metadata.DensityScore` | `Density` |
| `Metadata.Custom` | `Props` |

### Hierarchy Metadata

For hierarchical chunking, FluxCurator's hierarchy metadata maps to FileFlux:

| FluxCurator Custom Key | Description |
|-----------------------|-------------|
| `HierarchyLevel` | Document tree depth |
| `ParentId` | Parent chunk ID |
| `ChildIds` | Child chunk IDs |
| `SectionTitle` | Section heading text |

These are preserved in `Props` when converting to FileFlux.

## Document Context Enrichment

The strategy enriches chunks with document context from FileFlux:

### Page Information

```csharp
// If DocumentContent has page ranges, chunks get page info
chunk.Location.StartPage  // Starting page number
chunk.Location.EndPage    // Ending page number
```

### Heading Path

```csharp
// If DocumentContent has sections, chunks get heading path
chunk.Location.HeadingPath = ["Chapter 1", "Section 1.1", "Subsection"];
```

## Options Conversion

FileFlux `ChunkingOptions` automatically convert to FluxCurator `ChunkOptions`:

| FileFlux Option | FluxCurator Option |
|-----------------|-------------------|
| `MaxChunkSize` | `MaxChunkSize` |
| `MinChunkSize` | `MinChunkSize` |
| `OverlapSize` | `OverlapSize` |
| `LanguageCode` | `LanguageCode` ("auto" → null) |
| `PreserveParagraphs` | `PreserveParagraphs` |
| `PreserveSentences` | `PreserveSentences` |

Additional FluxCurator options are set to sensible defaults:

```csharp
TargetChunkSize = MaxChunkSize / 2
PreserveSectionHeaders = true
IncludeMetadata = true
TrimWhitespace = true
```

## Chunk Count Estimation

```csharp
// Estimate without actually chunking
var estimate = strategy.EstimateChunkCount(content, options);
Console.WriteLine($"Estimated {estimate} chunks");
```

## Strategy Selection

Use different FluxCurator strategies based on document type:

```csharp
public FluxCuratorChunkingStrategy GetStrategy(
    IChunkerFactory factory,
    string contentType)
{
    var strategy = contentType switch
    {
        "text/markdown" => ChunkingStrategy.Hierarchical,
        "application/pdf" => ChunkingStrategy.Semantic,
        "text/plain" => ChunkingStrategy.Sentence,
        _ => ChunkingStrategy.Auto
    };

    return new FluxCuratorChunkingStrategy(factory, strategy);
}
```

## Best Practices

### 1. Match Strategy to Content

```csharp
// Technical docs with headers → Hierarchical
var techStrategy = factory.CreateHierarchicalStrategy();

// RAG with semantic boundaries → Semantic
var ragStrategy = factory.CreateSemanticStrategy();

// Simple text → Sentence
var textStrategy = factory.CreateSentenceStrategy();
```

### 2. Preserve Document Structure

```csharp
var options = new ChunkingOptions
{
    PreserveParagraphs = true,
    PreserveSentences = true
};
```

### 3. Use Appropriate Chunk Sizes

| Use Case | Target Size | Max Size |
|----------|-------------|----------|
| Embedding models | 256-512 | 1024 |
| LLM context | 1024-2048 | 4096 |
| Search indexes | 512-1024 | 2048 |

### 4. Handle Large Documents

```csharp
// For very large documents, use streaming if available
// or process in batches with FluxCurator

var pageChunks = new List<DocumentChunk>();
foreach (var page in document.Pages)
{
    var content = new DocumentContent { Text = page.Text };
    var chunks = await strategy.ChunkAsync(content, options);
    pageChunks.AddRange(chunks);
}
```

## Troubleshooting

### Common Issues

1. **Missing hierarchy metadata**: Ensure input has markdown headers for hierarchical chunking
2. **Large chunk sizes**: Increase `MaxChunkSize` or use `Token` strategy
3. **Lost document context**: Check that `DocumentContent.PageRanges` and `Sections` are populated

### Debug Logging

```csharp
// Log chunk details
foreach (var chunk in chunks)
{
    _logger.LogDebug(
        "Chunk {Index}: {Length} chars, pages {Start}-{End}, path: {Path}",
        chunk.Index,
        chunk.Content.Length,
        chunk.Location.StartPage,
        chunk.Location.EndPage,
        string.Join(" > ", chunk.Location.HeadingPath));
}
```
