# FluxCurator

Clean, protect, and chunk your text for RAG pipelines — no dependencies required.

[![NuGet](https://img.shields.io/nuget/v/FluxCurator.svg)](https://www.nuget.org/packages/FluxCurator)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Overview

FluxCurator is a text preprocessing library for RAG (Retrieval-Augmented Generation) pipelines. It provides PII masking, content filtering, and intelligent text chunking with first-class Korean language support.

**Zero Dependencies Philosophy**: Core functionality (`FluxCurator.Core`) works standalone with no external dependencies. The main package (`FluxCurator`) adds optional LocalEmbedder integration for semantic chunking.

## Features

- **PII Masking** - Auto-detect and mask emails, phone numbers, Korean RRN, credit cards
- **Content Filtering** - Filter harmful content with customizable rules and blocklists
- **Smart Chunking** - Rule-based chunking (sentence, paragraph, token)
- **Semantic Chunking** - Embedding-based chunking for semantic boundaries
- **Hierarchical Chunking** - Document structure-aware chunking with parent-child relationships
- **Korean-First Design** - Optimized for Korean text (습니다체, 해요체, sentence endings)
- **Multi-Language Support** - 11 languages including Korean, English, Japanese, Chinese
- **Pipeline Processing** - Combine filtering, masking, and chunking in one call
- **Dependency Injection** - Full DI support with `IServiceCollection` extensions
- **FileFlux Integration** - Seamless integration with FileFlux document processing

## Installation

```bash
# Main package (includes LocalEmbedder for semantic chunking)
dotnet add package FluxCurator

# Core package only (zero dependencies)
dotnet add package FluxCurator.Core
```

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

### Dependency Injection

```csharp
// Program.cs or Startup.cs
services.AddFluxCurator(options =>
{
    options.DefaultChunkOptions = ChunkOptions.ForRAG;
    options.EnablePIIMasking = true;
    options.EnableContentFiltering = true;
});

// Or with LocalEmbedder for semantic chunking
services.AddFluxCuratorWithLocalEmbedder(options =>
{
    options.DefaultChunkOptions = new ChunkOptions
    {
        Strategy = ChunkingStrategy.Semantic,
        TargetChunkSize = 512
    };
});
```

### Using IChunkerFactory

```csharp
// Inject IChunkerFactory for flexible chunker creation
public class MyService
{
    private readonly IChunkerFactory _chunkerFactory;

    public MyService(IChunkerFactory chunkerFactory)
    {
        _chunkerFactory = chunkerFactory;
    }

    public async Task<IReadOnlyList<DocumentChunk>> ProcessAsync(string text)
    {
        // Create specific chunker
        var chunker = _chunkerFactory.CreateChunker(ChunkingStrategy.Hierarchical);
        return await chunker.ChunkAsync(text, ChunkOptions.Default);
    }
}
```

### PII Masking

```csharp
// Enable PII masking
var curator = new FluxCurator()
    .WithPIIMasking();

// Mask PII in text
var result = curator.MaskPII("Contact: 010-1234-5678, Email: test@example.com");
Console.WriteLine(result.MaskedText);
// Output: "Contact: [PHONE], Email: [EMAIL]"
```

### Korean RRN Detection

```csharp
var curator = new FluxCurator()
    .WithPIIMasking(PIIMaskingOptions.ForKorean);

var result = curator.MaskPII("RRN: 901231-1234567");
// Output: "RRN: [RRN]"
// Validates using Modulo-11 checksum algorithm
```

### Hierarchical Chunking

```csharp
var curator = new FluxCurator()
    .WithChunkingOptions(opt =>
    {
        opt.Strategy = ChunkingStrategy.Hierarchical;
        opt.MaxChunkSize = 1024;
    });

var chunks = await curator.ChunkAsync(markdownText);

foreach (var chunk in chunks)
{
    // Access hierarchy information
    var level = chunk.Metadata.Custom?["HierarchyLevel"];
    var parentId = chunk.Metadata.Custom?["ParentId"];
    var sectionPath = chunk.Location.SectionPath;

    Console.WriteLine($"[Level {level}] {sectionPath}");
    Console.WriteLine(chunk.Content);
}
```

### Full Pipeline Processing

```csharp
// Complete preprocessing pipeline
var curator = new FluxCurator()
    .WithContentFiltering()
    .WithPIIMasking()
    .WithChunkingOptions(ChunkOptions.ForKorean);

// Process: Filter → Mask PII → Chunk
var result = await curator.PreprocessAsync(text);

Console.WriteLine(result.GetSummary());
// Output: "Produced 5 chunk(s). Filtered 2 content item(s). Masked 3 PII item(s)."
```

### Semantic Chunking

```csharp
// With LocalEmbedder integration (auto-loaded via DI)
var curator = new FluxCurator()
    .UseEmbedder(myEmbedder)
    .WithChunkingOptions(opt =>
    {
        opt.Strategy = ChunkingStrategy.Semantic;
        opt.SemanticSimilarityThreshold = 0.5f;
    });

var chunks = await curator.ChunkAsync(text);
// Chunks at natural semantic boundaries
```

## Chunking Strategies

| Strategy | Description | Embedder Required | Best For |
|----------|-------------|-------------------|----------|
| `Auto` | Automatically select best strategy | No | General use |
| `Sentence` | Split by sentence boundaries | No | Conversational text |
| `Paragraph` | Split by paragraph boundaries | No | Structured documents |
| `Token` | Split by token count | No | Consistent chunk sizes |
| `Semantic` | Split by semantic similarity | **Yes** | RAG applications |
| `Hierarchical` | Preserve document structure with parent-child relationships | No | Technical docs, Markdown |

## Supported Languages

FluxCurator includes language profiles for accurate sentence detection and token estimation:

| Language | Code | Features |
|----------|------|----------|
| Korean | `ko` | 습니다체/해요체 endings, Korean sentence markers |
| English | `en` | Standard sentence boundaries |
| Japanese | `ja` | Japanese sentence endings (。、！？) |
| Chinese (Simplified) | `zh` | Chinese punctuation |
| Chinese (Traditional) | `zh-TW` | Traditional Chinese support |
| Spanish | `es` | Spanish punctuation |
| French | `fr` | French punctuation |
| German | `de` | German punctuation |
| Portuguese | `pt` | Portuguese punctuation |
| Russian | `ru` | Cyrillic support |
| Arabic | `ar` | RTL and Arabic punctuation |

## PII Types Supported

| Type | Description | Validation |
|------|-------------|------------|
| `Email` | Email addresses | TLD validation |
| `Phone` | Phone numbers (KR/US/International) | Format validation |
| `KoreanRRN` | Korean Resident Registration Number | Modulo-11 checksum |
| `CreditCard` | Credit card numbers | Luhn algorithm |
| `KoreanBRN` | Korean Business Registration Number | Format validation |

## Configuration Options

### ChunkOptions

```csharp
var options = new ChunkOptions
{
    Strategy = ChunkingStrategy.Sentence,
    TargetChunkSize = 512,
    MinChunkSize = 100,
    MaxChunkSize = 1024,
    OverlapSize = 50,
    LanguageCode = "ko",  // null = auto-detect
    PreserveSentences = true,
    PreserveParagraphs = true,
    SemanticSimilarityThreshold = 0.5f
};

// Preset configurations
ChunkOptions.Default       // General purpose
ChunkOptions.ForRAG        // Optimized for RAG (512 target, semantic)
ChunkOptions.ForKorean     // Optimized for Korean (400 target)
ChunkOptions.FixedSize(256, 32)  // Fixed token size with overlap
```

### Masking Strategies

| Strategy | Example Output |
|----------|----------------|
| `Token` | `[EMAIL]`, `[PHONE]` |
| `Asterisk` | `****@****.com` |
| `Redact` | `[REDACTED]` |
| `Partial` | `jo**@ex****.com` |
| `Hash` | `[HASH:a1b2c3d4]` |
| `Remove` | *(empty)* |

## Integration with Iyulab Ecosystem

FluxCurator is part of the Iyulab open-source RAG ecosystem:

```
┌─────────────────────────────────────────────────────────────┐
│                    Foundation Layer                          │
├─────────────────────────────────────────────────────────────┤
│  LocalEmbedder    LocalReranker    FluxCurator  FluxImprover│
│  (Embeddings)     (Reranking)      (Chunking)   (LLM-based) │
└───────────┬───────────────────────────┬─────────────────────┘
            │                           │
            ▼                           ▼
┌───────────────────────────────────────────────────────────────┐
│                    Processing Layer                           │
├───────────────────────────────────────────────────────────────┤
│        FileFlux (Document Processing)    WebFlux (Web)        │
└───────────────────────────┬───────────────────────────────────┘
                            │
                            ▼
┌───────────────────────────────────────────────────────────────┐
│                    Storage Layer                              │
├───────────────────────────────────────────────────────────────┤
│                    FluxIndex (Vector DB)                      │
└───────────────────────────┬───────────────────────────────────┘
                            │
                            ▼
┌───────────────────────────────────────────────────────────────┐
│                    Application Layer                          │
├───────────────────────────────────────────────────────────────┤
│                        Filer (App)                            │
└───────────────────────────────────────────────────────────────┘
```

### FileFlux Integration

```csharp
using FileFlux.Infrastructure.Strategies;
using FileFlux.Infrastructure.Adapters;

// Use FluxCurator chunking in FileFlux
var chunkerFactory = new ChunkerFactory(embedder);
var strategy = new FluxCuratorChunkingStrategy(
    chunkerFactory,
    ChunkingStrategy.Hierarchical);

var chunks = await strategy.ChunkAsync(documentContent, options);

// Convert between chunk types
var fileFluxChunks = fluxCuratorChunks.ToFileFluxChunks();
var curatorChunks = fileFluxChunks.ToFluxCuratorChunks();
```

## Project Structure

```
FluxCurator/
├── src/
│   ├── FluxCurator.Core/              # Zero-dependency core
│   │   ├── Core/                      # Interfaces
│   │   │   ├── IChunker.cs
│   │   │   ├── IChunkerFactory.cs
│   │   │   ├── IEmbedder.cs
│   │   │   └── ILanguageProfile.cs
│   │   ├── Domain/                    # Models
│   │   │   ├── ChunkOptions.cs
│   │   │   ├── DocumentChunk.cs
│   │   │   ├── ChunkingStrategy.cs
│   │   │   └── PIIMaskingOptions.cs
│   │   └── Infrastructure/            # Implementations
│   │       ├── Chunking/
│   │       │   ├── ChunkerBase.cs
│   │       │   ├── SentenceChunker.cs
│   │       │   ├── ParagraphChunker.cs
│   │       │   ├── TokenChunker.cs
│   │       │   ├── SemanticChunker.cs
│   │       │   └── HierarchicalChunker.cs
│   │       └── Languages/
│   │           ├── LanguageProfileRegistry.cs
│   │           ├── KoreanLanguageProfile.cs
│   │           └── EnglishLanguageProfile.cs
│   │
│   └── FluxCurator/                   # Main package
│       ├── Infrastructure/
│       │   └── Chunking/
│       │       └── ChunkerFactory.cs  # Factory with all strategies
│       ├── ServiceCollectionExtensions.cs
│       └── FluxCurator.cs             # Main API
│
└── docs/                              # Documentation
    ├── getting-started.md
    ├── chunking-strategies.md
    ├── di-integration.md
    └── fileflux-integration.md
```

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and basic usage
- [Chunking Strategies](docs/chunking-strategies.md) - Detailed guide for each strategy
- [Dependency Injection](docs/di-integration.md) - DI configuration and patterns
- [FileFlux Integration](docs/fileflux-integration.md) - Integration with FileFlux

## Roadmap

- [x] Core chunking strategies (Sentence, Paragraph, Token)
- [x] Korean language profile with 11 language support
- [x] Language detection
- [x] Batch processing
- [x] PII masking (Korean RRN, phone, email, credit card)
- [x] Content filtering
- [x] Semantic chunking
- [x] Hierarchical chunking
- [x] Dependency Injection support
- [x] FileFlux integration
- [ ] Additional language profiles (Vietnamese, Thai)
- [ ] Custom detector registration
- [ ] Streaming chunk support

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.

---

**Part of the [Iyulab](https://github.com/iyulab) Open Source Ecosystem**
