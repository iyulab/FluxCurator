# FluxCurator

Clean, protect, and chunk your text for RAG pipelines — no dependencies required.

[![NuGet](https://img.shields.io/nuget/v/FluxCurator.svg)](https://www.nuget.org/packages/FluxCurator)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Overview

FluxCurator is a text preprocessing library for RAG (Retrieval-Augmented Generation) pipelines. It provides multilingual PII masking, content filtering, and intelligent text chunking with support for 14 languages and 13 countries' national IDs.

**Zero Dependencies Philosophy**: Core functionality (`FluxCurator.Core`) works standalone with no external dependencies. The main package (`FluxCurator`) adds optional [LocalAI.Embedder](https://github.com/iyulab/local-ai) integration for semantic chunking.

## Features

- **Text Refinement** - Clean noisy text by removing blank lines, duplicates, empty list markers, and custom patterns
- **Multilingual PII Masking** - Auto-detect and mask emails, phones, national IDs, credit cards across 14 languages
- **Content Filtering** - Filter harmful content with customizable rules and blocklists
- **Smart Chunking** - Rule-based chunking (sentence, paragraph, token)
- **Semantic Chunking** - Embedding-based chunking for semantic boundaries
- **Hierarchical Chunking** - Document structure-aware chunking with parent-child relationships
- **Multi-Language Support** - 14 languages including Korean, English, Japanese, Chinese, Vietnamese, Thai
- **National ID Validation** - Checksum validation for 13 countries including SSN (US), RRN (Korea), Aadhaar (India), SIN (Canada)
- **Streaming Support** - Memory-efficient streaming chunk generation via `ChunkStreamAsync`
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

### Streaming Chunks

```csharp
// Memory-efficient streaming for large texts
var curator = new FluxCurator();

await foreach (var chunk in curator.ChunkStreamAsync(largeText))
{
    // Process chunks as they are generated
    Console.WriteLine($"Chunk {chunk.Index}: {chunk.Content.Length} chars");
    await ProcessChunkAsync(chunk);
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

// Or with LocalAI.Embedder for semantic chunking
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

### Text Refinement

```csharp
// Clean noisy text before processing
var curator = new FluxCurator()
    .WithTextRefinement(TextRefineOptions.Standard);

var result = await curator.PreprocessAsync(rawText);
// Pipeline: Refine → Filter → Mask → Chunk

// Use presets for specific content types
TextRefineOptions.Light        // Minimal: empty list markers, trim, collapse blanks
TextRefineOptions.Standard     // Default: + remove duplicates
TextRefineOptions.ForWebContent  // Web-optimized: aggressive cleaning
TextRefineOptions.ForKorean    // Korean: removes 댓글 sections, copyright
TextRefineOptions.ForPdfContent  // PDF: removes page numbers

// Custom patterns
var options = new TextRefineOptions
{
    RemoveBlankLines = true,
    RemoveDuplicateLines = true,
    RemoveEmptyListItems = true,  // Supports Korean markers: ㅇ, ○, ●, □, ■
    TrimLines = true,
    RemovePatterns = [@"^#\s*댓글\s*$", @"^\[광고\].*$"]
};
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

### Multilingual National ID Detection

```csharp
// Auto-detect PII for all supported languages
var curator = new FluxCurator()
    .WithPIIMasking(PIIMaskingOptions.Default);

var result = curator.MaskPII("SSN: 123-45-6789, RRN: 901231-1234567");
// Output: "SSN: [NATIONAL_ID], RRN: [NATIONAL_ID]"

// Detect for specific language
var koreanCurator = new FluxCurator()
    .WithPIIMasking(PIIMaskingOptions.ForLanguage("ko"));

var krResult = koreanCurator.MaskPII("주민등록번호: 901231-1234567");
// Output: "주민등록번호: [NATIONAL_ID]"
// Validates using Modulo-11 checksum algorithm

// Detect for multiple languages
var multiCurator = new FluxCurator()
    .WithPIIMasking(PIIMaskingOptions.ForLanguages("en-US", "ko", "pt-BR"));
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
    .WithTextRefinement(TextRefineOptions.Standard)
    .WithContentFiltering()
    .WithPIIMasking(PIIMaskingOptions.ForLanguages("en", "ko", "ja"))
    .WithChunkingOptions(ChunkOptions.ForRAG);

// Process: Refine → Filter → Mask PII → Chunk
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

### Large Document Processing

For documents with 50K+ tokens, use hierarchical chunking with the `ForLargeDocument` preset:

```csharp
// Use the preset for large documents
var curator = new FluxCurator()
    .WithChunkingOptions(ChunkOptions.ForLargeDocument);

var chunks = await curator.ChunkAsync(largeDocument);

// Access hierarchy metadata
foreach (var chunk in chunks)
{
    var level = chunk.Metadata.Custom?["HierarchyLevel"];
    var sectionPath = chunk.Location.SectionPath;
    Console.WriteLine($"[Level {level}] {sectionPath}: {chunk.Content.Length} chars");
}
```

See [Large Document Chunking Guide](docs/large-document-chunking.md) for detailed configuration options.

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
| Hindi | `hi` | Devanagari script support |
| Vietnamese | `vi` | Latin with Vietnamese diacritics |
| Thai | `th` | Thai script (no word spaces) |

## PII Types Supported

### Global PII Types

| Type | Description | Validation |
|------|-------------|------------|
| `Email` | Email addresses | TLD validation |
| `Phone` | Phone numbers (International) | E.164 format validation |
| `CreditCard` | Credit card numbers | Luhn algorithm |
| `BankAccount` | Bank account numbers | Format validation |
| `IPAddress` | IPv4 and IPv6 addresses | Format validation |
| `URL` | URLs and web addresses | Format validation |

### National ID Types by Country

| Country | Language Code | ID Type | Validation |
|---------|---------------|---------|------------|
| Korea | `ko` | Resident Registration Number (RRN) | Modulo-11 checksum |
| USA | `en-US` | Social Security Number (SSN) | Area/Group validation |
| UK | `en-GB` | National Insurance Number (NINO) | Prefix/Suffix validation |
| Japan | `ja` | My Number | Check digit validation |
| China | `zh-CN` | ID Card Number | ISO 7064 MOD 11-2 |
| Germany | `de` | Personalausweis / Steuer-ID | Check digit validation |
| France | `fr` | INSEE Number | Modulo-97 validation |
| Spain | `es` | DNI / NIE | Check letter validation |
| Brazil | `pt-BR` | CPF | Dual Modulo-11 |
| Italy | `it` | Codice Fiscale | Check character validation |
| India | `hi` | Aadhaar | Verhoeff checksum |
| Canada | `en-CA` | Social Insurance Number (SIN) | Luhn algorithm |
| Australia | `en-AU` | Tax File Number (TFN) | Weighted sum mod 11 |

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
ChunkOptions.Default           // General purpose
ChunkOptions.ForRAG            // Optimized for RAG (512 target, semantic)
ChunkOptions.ForKorean         // Optimized for Korean text
ChunkOptions.ForLargeDocument  // Large docs (50K+ tokens, hierarchical)
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

## Extensibility

FluxCurator is designed for extensibility. You can add custom PII detectors for your specific needs.

### Custom PII Detector

Implement `IPIIDetector` or extend `PIIDetectorBase` for pattern-based detection:

```csharp
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.PII;

public class EmployeeIdDetector : PIIDetectorBase
{
    public override PIIType PIIType => PIIType.Custom;
    public override string Name => "Employee ID Detector";

    // Pattern: EMP-123456
    protected override string Pattern => @"EMP-\d{6}";

    protected override bool ValidateMatch(string value, out float confidence)
    {
        confidence = 0.95f;
        return true;
    }
}

// Register and use via PIIMasker
var masker = new PIIMasker(PIIMaskingOptions.Default);
masker.RegisterDetector(new EmployeeIdDetector());

var result = masker.Mask("Contact employee EMP-123456 for details.");
// Output: "Contact employee [PII] for details."

// Or register directly via FluxCurator
var curator = new FluxCurator()
    .WithPIIMasking()
    .RegisterPIIDetector(new EmployeeIdDetector());

var curatorResult = curator.MaskPII("Contact employee EMP-123456 for details.");
// Output: "Contact employee [PII] for details."
```

### Custom National ID Detector

Extend `NationalIdDetectorBase` to add support for additional countries:

```csharp
using FluxCurator.Core.Core;
using FluxCurator.Core.Infrastructure.PII.NationalId;

public class IndiaAadhaarDetector : NationalIdDetectorBase
{
    public override string LanguageCode => "hi";
    public override string NationalIdType => "Aadhaar";
    public override string FormatDescription => "12 digits with optional spaces";
    public override string CountryName => "India";
    public override string Name => "India Aadhaar Detector";

    // Pattern: 1234 5678 9012 or 123456789012
    protected override string Pattern => @"\d{4}\s?\d{4}\s?\d{4}";

    protected override bool ValidateMatch(string value, out float confidence)
    {
        var normalized = NormalizeValue(value);

        if (normalized.Length != 12 || !normalized.All(char.IsDigit))
        {
            confidence = 0.0f;
            return false;
        }

        // Implement Verhoeff checksum validation
        if (!ValidateVerhoeffChecksum(normalized))
        {
            confidence = 0.6f;
            return true; // Still flag as PII
        }

        confidence = 0.98f;
        return true;
    }

    private static bool ValidateVerhoeffChecksum(string number)
    {
        // Verhoeff algorithm implementation
        // ...
        return true;
    }
}

// Register with the national ID registry
var registry = new NationalIdRegistry();
registry.Register(new IndiaAadhaarDetector());

var masker = new PIIMasker(
    PIIMaskingOptions.ForLanguage("hi"),
    registry);
```

### Dependency Injection with Custom Detectors

```csharp
// Register custom registry with additional detectors
services.AddSingleton<INationalIdRegistry>(sp =>
{
    var registry = new NationalIdRegistry();
    registry.Register(new IndiaAadhaarDetector());
    registry.Register(new CanadaSINDetector());
    registry.Register(new AustraliaTFNDetector());
    return registry;
});

// Register PIIMasker with custom registry
services.AddScoped<IPIIMasker>(sp =>
{
    var registry = sp.GetRequiredService<INationalIdRegistry>();
    var options = PIIMaskingOptions.ForLanguages("en", "hi");
    return new PIIMasker(options, registry);
});
```

### Extension Points Summary

| Interface | Base Class | Purpose |
|-----------|------------|---------|
| `IPIIDetector` | `PIIDetectorBase` | General PII detection (email, phone, custom) |
| `INationalIdDetector` | `NationalIdDetectorBase` | Country-specific national ID detection |
| `INationalIdRegistry` | `NationalIdRegistry` | Manage and lookup national ID detectors |
| `IPIIMasker` | `PIIMasker` | Coordinate detection and masking |

## Integration with Iyulab Ecosystem

FluxCurator is part of the Iyulab open-source RAG ecosystem:

```
┌─────────────────────────────────────────────────────────────┐
│                    Foundation Layer                          │
├─────────────────────────────────────────────────────────────┤
│  LocalAI          LocalAI          FluxCurator  FluxImprover│
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
- [Large Document Chunking](docs/large-document-chunking.md) - Processing 50K+ token documents
- [Dependency Injection](docs/di-integration.md) - DI configuration and patterns
- [FileFlux Integration](docs/fileflux-integration.md) - Integration with FileFlux

## Roadmap

- [x] Core chunking strategies (Sentence, Paragraph, Token)
- [x] 11 language profiles for text processing
- [x] Language detection
- [x] Batch processing
- [x] Multilingual PII masking (10 countries)
- [x] Content filtering
- [x] Semantic chunking
- [x] Hierarchical chunking
- [x] Dependency Injection support
- [x] FileFlux integration
- [x] Text refinement with Korean support
- [x] Additional national ID detectors (India Aadhaar, Canada SIN, Australia TFN)
- [x] Additional language profiles (Vietnamese, Thai)
- [x] Custom detector registration via `RegisterPIIDetector`
- [x] Streaming chunk support via `ChunkStreamAsync`

## FAQ

**Q: How do I process large documents (100K+ tokens)?**

Use `ChunkingStrategy.Hierarchical` with the `ForLargeDocument` preset. It recognizes document structure (#, ##, ###) and chunks at section boundaries while preserving context.

```csharp
var curator = new FluxCurator()
    .WithChunkingOptions(ChunkOptions.ForLargeDocument);
```

**Q: My chunks are too small/too large. How do I fix this?**

Enable chunk balancing and adjust size limits:

```csharp
var options = new ChunkOptions
{
    MinChunkSize = 200,        // Merge chunks smaller than this
    MaxChunkSize = 1024,       // Split chunks larger than this
    EnableChunkBalancing = true
};
```

**Q: How do I preserve context across chunk boundaries?**

Increase the overlap size. For technical documents, 15-20% overlap is recommended:

```csharp
var options = new ChunkOptions
{
    TargetChunkSize = 512,
    OverlapSize = 100  // ~20% overlap
};
```

**Q: Does FluxCurator support document structure from FileFlux?**

Yes. When documents are processed through FileFlux, structure hints (headings, sections) are passed to FluxCurator for intelligent boundary detection. Use `ChunkingStrategy.Hierarchical` for best results.

**Q: How do I process Korean documents (DOCX, PPTX, HWP)?**

FluxCurator processes text, not document files directly. Use FileFlux to extract text first, then chunk with FluxCurator:

```csharp
// 1. Extract text from document using FileFlux
var document = await fileFlux.ProcessAsync("보고서.docx");

// 2. Chunk the extracted Korean text
var curator = new FluxCurator()
    .WithTextRefinement(TextRefineOptions.ForKorean)
    .WithChunkingOptions(opt =>
    {
        opt.Strategy = ChunkingStrategy.Hierarchical;
        opt.LanguageCode = "ko";  // Use Korean language profile
        opt.EnableChunkBalancing = true;
    });

var chunks = await curator.ChunkAsync(document.Text);
```

See [Large Document Chunking Guide](docs/large-document-chunking.md#korean-document-processing) for detailed Korean document processing examples.

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.

---

**Part of the [Iyulab](https://github.com/iyulab) Open Source Ecosystem**
