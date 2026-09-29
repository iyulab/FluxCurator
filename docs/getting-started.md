# Getting Started with FluxCurator

This guide walks you through setting up and using FluxCurator for text preprocessing in RAG pipelines.

## Installation

FluxCurator is available as two NuGet packages:

```bash
# Main package (DI support and semantic chunking)
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
| Semantic Chunking | No | Yes (with IEmbedder) |
| DI Extensions | No | Yes |
| External Dependencies | None | None (IEmbedder injected) |

## Quick Start

### Basic Chunking

```csharp
using FluxCurator;
using FluxCurator.Core.Domain;

// Create curator with default options
var curator = new Curator();

// Chunk text using sentence strategy
var chunks = await curator.ChunkAsync(text);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex + 1}/{chunk.TotalChunks}:");
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
var curator = new Curator().WithChunkingOptions(ChunkOptions.ForKorean);
var chunks = await curator.ChunkAsync(koreanText);
```

## Chunk Options

### Preset Configurations

| Preset | Use |
|--------|-----|
| `ChunkOptions.Default` | General purpose defaults |
| `ChunkOptions.ForRAG` | Optimized for RAG (512 target, semantic) |
| `ChunkOptions.ForKorean` | Optimized for Korean (400 target, Korean sentence endings) |
| `ChunkOptions.FixedSize(256, 32)` | Fixed size with overlap |

The full preset table (including `ForLargeDocument` and the context-size presets) is in the
[README](../README.md#preset-comparison-by-embedding-model).

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
    TrimWhitespace = true
};
```

## DocumentChunk Structure

Each `DocumentChunk` carries:

| Property | Meaning |
|----------|---------|
| `Id` | Chunk identifier |
| `Content` | The chunk text |
| `ChunkIndex` / `TotalChunks` | Position in the result (0-based) and the result size |
| `Location` | Where the chunk came from (below) |
| `Metadata` | What the chunker knows about it (below) |

### Location Information

| Property | Meaning |
|----------|---------|
| `Location.StartPosition` / `EndPosition` | Character positions in the source text |
| `Location.StartLine` / `EndLine` | Line numbers |
| `Location.StartPage` / `EndPage` | Page numbers, when the source had pages |
| `Location.SectionPath` | Hierarchical section path (e.g., "Chapter 1 > Section 1.1") |

### Metadata

| Property | Meaning |
|----------|---------|
| `Metadata.Strategy` | The `ChunkingStrategy` used |
| `Metadata.LanguageCode` | Detected or specified language |
| `Metadata.EstimatedTokenCount` | Approximate token count |
| `Metadata.QualityScore` / `DensityScore` | Content quality and information density |
| `Metadata.Custom` | Strategy-specific key-value pairs (e.g., `HierarchyLevel`) |

## PII Masking

Protect sensitive information:

```csharp
var curator = new Curator()
    .WithPIIMasking();

var result = curator.MaskPII("Email: test@example.com, Phone: 010-1234-5678");
Console.WriteLine(result.MaskedText);
// Output: "Email: [EMAIL], Phone: [PHONE]"

// Access detection details
foreach (var match in result.Matches)
{
    Console.WriteLine($"{match.Type}: {match.Value} -> {match.MaskedValue}");
}
```

### Korean-Specific PII

```csharp
var curator = new Curator()
    .WithPIIMasking(PIIMaskingOptions.ForLanguage("ko"));

// Detects and validates Korean RRN (Resident Registration Number)
var result = curator.MaskPII("RRN: 901231-1234567");
// Uses Modulo-11 checksum validation
```

## Content Filtering

Filter harmful or unwanted content:

```csharp
var curator = new Curator()
    .WithContentFiltering();

var result = curator.FilterContent(text);
if (result.HasFilteredContent)
{
    Console.WriteLine($"Filtered {result.MatchCount} items");
}
```

## Pipeline Processing

Combine multiple preprocessing steps:

```csharp
var curator = new Curator()
    .WithContentFiltering()
    .WithPIIMasking()
    .WithChunkingOptions(ChunkOptions.ForKorean);

// Process: Filter -> Mask PII -> Chunk
var result = await curator.PreprocessAsync(text);

Console.WriteLine(result.GetSummary());
// e.g. "Produced 5 chunk(s). Filtered 2 content item(s). Masked 3 PII item(s)."
```

## Next Steps

- [Chunking Strategies](chunking-strategies.md) - Detailed guide for each strategy
- [Dependency Injection](di-integration.md) - DI configuration patterns
- [FileFlux Integration](fileflux-integration.md) - Integration with FileFlux
