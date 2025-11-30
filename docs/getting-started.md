# Getting Started with FluxCurator

This guide walks you through setting up and using FluxCurator for text preprocessing in RAG pipelines.

## Installation

FluxCurator is available as two NuGet packages:

```bash
# Main package (includes LocalEmbedder for semantic chunking)
dotnet add package FluxCurator

# Core package only (zero dependencies)
dotnet add package FluxCurator.Core
```

### Package Comparison

| Feature | FluxCurator.Core | FluxCurator |
|---------|------------------|-------------|
| Basic Chunking (Sentence, Paragraph, Token) | Yes | Yes |
| Hierarchical Chunking | Yes | Yes |
| PII Masking | Yes | Yes |
| Content Filtering | Yes | Yes |
| Semantic Chunking | No | Yes |
| LocalEmbedder Integration | No | Yes |
| DI Extensions | No | Yes |
| External Dependencies | None | LocalEmbedder |

## Quick Start

### Basic Chunking

```csharp
using FluxCurator;
using FluxCurator.Core.Domain;

// Create curator with default options
var curator = new FluxCurator();

// Chunk text using sentence strategy
var chunks = await curator.ChunkAsync(text);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.Index + 1}/{chunk.TotalChunks}:");
    Console.WriteLine(chunk.Content);
    Console.WriteLine($"Tokens: ~{chunk.Metadata.EstimatedTokenCount}");
}
```

### Using ChunkerFactory Directly

For more control over chunking, use `IChunkerFactory`:

```csharp
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;
using FluxCurator.Infrastructure.Chunking;

// Create factory (no embedder = no semantic chunking)
var factory = new ChunkerFactory();

// Create specific chunker
var chunker = factory.CreateChunker(ChunkingStrategy.Sentence);

// Chunk with options
var options = new ChunkOptions
{
    TargetChunkSize = 512,
    MaxChunkSize = 1024,
    OverlapSize = 50
};

var chunks = await chunker.ChunkAsync(text, options);
```

### Korean Text Support

FluxCurator has first-class support for Korean text:

```csharp
var options = ChunkOptions.ForKorean;
var chunks = await chunker.ChunkAsync(koreanText, options);
```

## Chunk Options

### Preset Configurations

```csharp
// General purpose defaults
ChunkOptions.Default

// Optimized for RAG (512 target, semantic if available)
ChunkOptions.ForRAG

// Optimized for Korean (400 target, Korean sentence endings)
ChunkOptions.ForKorean

// Fixed size with overlap
ChunkOptions.FixedSize(256, 32)
```

### Custom Configuration

```csharp
var options = new ChunkOptions
{
    Strategy = ChunkingStrategy.Sentence,
    TargetChunkSize = 512,
    MinChunkSize = 100,
    MaxChunkSize = 1024,
    OverlapSize = 50,
    LanguageCode = "ko",          // null = auto-detect
    PreserveSentences = true,
    PreserveParagraphs = true,
    PreserveSectionHeaders = true,
    IncludeMetadata = true,
    TrimWhitespace = true
};
```

## DocumentChunk Structure

Each chunk contains:

```csharp
public class DocumentChunk
{
    public string Id { get; set; }
    public string Content { get; set; }
    public int Index { get; set; }
    public int TotalChunks { get; set; }
    public ChunkLocation Location { get; set; }
    public ChunkMetadata Metadata { get; set; }
}
```

### Location Information

```csharp
chunk.Location.StartPosition    // Character start position
chunk.Location.EndPosition      // Character end position
chunk.Location.StartLine        // Line number start
chunk.Location.EndLine          // Line number end
chunk.Location.SectionPath      // Hierarchical section path (e.g., "Chapter 1 > Section 1.1")
```

### Metadata

```csharp
chunk.Metadata.Strategy             // ChunkingStrategy used
chunk.Metadata.LanguageCode         // Detected/specified language
chunk.Metadata.EstimatedTokenCount  // Approximate token count
chunk.Metadata.QualityScore         // Content quality score
chunk.Metadata.DensityScore         // Information density
chunk.Metadata.Custom               // Custom key-value pairs
```

## PII Masking

Protect sensitive information:

```csharp
var curator = new FluxCurator()
    .WithPIIMasking();

var result = curator.MaskPII("Email: test@example.com, Phone: 010-1234-5678");
Console.WriteLine(result.MaskedText);
// Output: "Email: [EMAIL], Phone: [PHONE]"

// Access detection details
foreach (var detection in result.Detections)
{
    Console.WriteLine($"{detection.Type}: {detection.OriginalValue}");
}
```

### Korean-Specific PII

```csharp
var curator = new FluxCurator()
    .WithPIIMasking(PIIMaskingOptions.ForKorean);

// Detects and validates Korean RRN (Resident Registration Number)
var result = curator.MaskPII("RRN: 901231-1234567");
// Uses Modulo-11 checksum validation
```

## Content Filtering

Filter harmful or unwanted content:

```csharp
var curator = new FluxCurator()
    .WithContentFiltering();

var result = curator.Filter(text);
if (result.WasFiltered)
{
    Console.WriteLine($"Filtered {result.FilteredCount} items");
}
```

## Pipeline Processing

Combine multiple preprocessing steps:

```csharp
var curator = new FluxCurator()
    .WithContentFiltering()
    .WithPIIMasking()
    .WithChunkingOptions(ChunkOptions.ForKorean);

// Process: Filter -> Mask PII -> Chunk
var result = await curator.PreprocessAsync(text);

Console.WriteLine(result.GetSummary());
// Output: "Produced 5 chunk(s). Filtered 2 content item(s). Masked 3 PII item(s)."
```

## Next Steps

- [Chunking Strategies](chunking-strategies.md) - Detailed guide for each strategy
- [Dependency Injection](di-integration.md) - DI configuration patterns
- [FileFlux Integration](fileflux-integration.md) - Integration with FileFlux
