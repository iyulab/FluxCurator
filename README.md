# FluxCurator

Clean, protect, and chunk your text for RAG pipelines — no dependencies required.

## Overview

FluxCurator is a text preprocessing library for RAG (Retrieval-Augmented Generation) pipelines. It provides PII masking, content filtering, and intelligent text chunking with first-class Korean language support.

**Zero Dependencies Philosophy**: Core functionality works standalone. Optional embedder integration enables semantic chunking.

## Features

- **PII Masking** - Auto-detect and mask emails, phone numbers, Korean RRN, credit cards
- **Content Filtering** - Filter harmful content with customizable rules and blocklists
- **Smart Chunking** - Rule-based chunking (sentence, paragraph, token)
- **Semantic Chunking** - Embedding-based chunking for semantic boundaries
- **Korean-First Design** - Optimized for Korean text (습니다체, 해요체, sentence endings)
- **Multi-Language Support** - English, Korean with extensible language profiles
- **Pipeline Processing** - Combine filtering, masking, and chunking in one call

## Installation

```bash
dotnet add package FluxCurator
```

## Quick Start

### Basic Chunking

```csharp
using FluxCurator;
using FluxCurator.Domain;

// Create curator with default options
var curator = new FluxCurator();

// Chunk text using auto-detected strategy
var chunks = await curator.ChunkAsync(text);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.Index + 1}/{chunk.TotalChunks}:");
    Console.WriteLine(chunk.Content);
    Console.WriteLine($"Tokens: ~{chunk.Metadata.EstimatedTokenCount}");
}
```

### PII Masking

```csharp
// Enable PII masking
var curator = new FluxCurator()
    .WithPIIMasking();

// Mask PII in text
var result = curator.MaskPII("연락처: 010-1234-5678, 이메일: test@example.com");
Console.WriteLine(result.MaskedText);
// Output: "연락처: [PHONE], 이메일: [EMAIL]"

// Check PII count
Console.WriteLine($"Found {result.PIICount} PII items");
```

### Korean RRN Detection

```csharp
var curator = new FluxCurator()
    .WithPIIMasking(PIIMaskingOptions.ForKorean);

var result = curator.MaskPII("주민번호: 901231-1234567");
// Output: "주민번호: [RRN]"
// Validates using Modulo-11 checksum algorithm
```

### Content Filtering

```csharp
// Enable content filtering with custom blocklist
var curator = new FluxCurator()
    .WithContentFiltering(opt =>
    {
        opt.CustomBlocklist.Add("spam");
        opt.CustomBlocklist.Add("inappropriate");
        opt.DefaultAction = FilterAction.Replace;
    });

var result = curator.FilterContent(text);
Console.WriteLine(result.FilteredText);
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

### Semantic Chunking (Requires Embedder)

```csharp
// With LocalEmbedder integration
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

| Strategy | Description | Embedder Required |
|----------|-------------|-------------------|
| `Auto` | Automatically select best strategy | No |
| `Sentence` | Split by sentence boundaries | No |
| `Paragraph` | Split by paragraph boundaries | No |
| `Token` | Split by token count | No |
| `Semantic` | Split by semantic similarity | **Yes** |
| `Hierarchical` | Preserve document structure | No |

## PII Types Supported

| Type | Description | Validation |
|------|-------------|------------|
| `Email` | Email addresses | TLD validation |
| `Phone` | Phone numbers (KR/US/International) | Format validation |
| `KoreanRRN` | Korean Resident Registration Number | Modulo-11 checksum |
| `CreditCard` | Credit card numbers | Luhn algorithm |
| `KoreanBRN` | Korean Business Registration Number | Format validation |

## Content Filter Categories

| Category | Description |
|----------|-------------|
| `Profanity` | Offensive language |
| `HateSpeech` | Discrimination content |
| `Violence` | Violence and threats |
| `Adult` | Adult/sexual content |
| `Spam` | Promotional content |
| `Custom` | User-defined patterns |

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
ChunkOptions.ForRAG        // Optimized for RAG
ChunkOptions.ForKorean     // Optimized for Korean
```

### PIIMaskingOptions

```csharp
var options = new PIIMaskingOptions
{
    TypesToMask = PIIType.Common,
    Strategy = MaskingStrategy.Token,
    MinConfidence = 0.8f,
    ValidatePatterns = true
};

// Preset configurations
PIIMaskingOptions.Default   // Token replacement
PIIMaskingOptions.ForKorean // Korean PII focus
PIIMaskingOptions.Strict    // All types
PIIMaskingOptions.Partial   // Partial masking
```

### ContentFilterOptions

```csharp
var options = new ContentFilterOptions
{
    CategoriesToFilter = ContentCategory.Common,
    DefaultAction = FilterAction.Replace,
    MinConfidence = 0.8f,
    CustomBlocklist = new() { "spam", "unwanted" }
};

// Preset configurations
ContentFilterOptions.Default   // Replace action
ContentFilterOptions.Strict    // Block action
ContentFilterOptions.Lenient   // Redact action
ContentFilterOptions.FlagOnly  // Detection only
```

## Masking Strategies

| Strategy | Example Output |
|----------|----------------|
| `Token` | `[EMAIL]`, `[PHONE]` |
| `Asterisk` | `****@****.com` |
| `Redact` | `[REDACTED]` |
| `Partial` | `jo**@ex****.com` |
| `Hash` | `[HASH:a1b2c3d4]` |
| `Remove` | *(empty)* |

## Integration with Iyulab Ecosystem

```csharp
// FileFlux integration
var fileFlux = new FileFlux()
    .UseCurator(new FluxCurator()
        .WithPIIMasking()
        .WithContentFiltering());

// Full pipeline
var pipeline = new FileFlux()
    .UseCurator(new FluxCurator())
    .UseEmbedder(new LocalEmbedder())
    .UseImprover(new FluxImprover());
```

### Iyulab Project Dependencies

```
LocalEmbedder ─────┐
LocalReranker ─────┤
FluxCurator ───────┼── FileFlux ──┐
FluxImprover ──────┘              │
                                  ├── FluxIndex ── Filer
WebFlux ──────────────────────────┘
```

## Project Structure

```
src/FluxCurator/
├── Core/                    # Interfaces
│   ├── IChunker.cs
│   ├── IEmbedder.cs
│   ├── ILanguageProfile.cs
│   ├── IPIIDetector.cs
│   └── IContentFilter.cs
├── Domain/                  # Models
│   ├── ChunkOptions.cs
│   ├── DocumentChunk.cs
│   ├── PIITypes.cs
│   ├── PIIMaskingOptions.cs
│   ├── ContentFilterTypes.cs
│   └── PreprocessingResult.cs
├── Infrastructure/          # Implementations
│   ├── Chunking/
│   │   ├── SentenceChunker.cs
│   │   ├── ParagraphChunker.cs
│   │   ├── TokenChunker.cs
│   │   └── SemanticChunker.cs
│   ├── Languages/
│   │   ├── KoreanLanguageProfile.cs
│   │   └── EnglishLanguageProfile.cs
│   ├── PII/
│   │   ├── EmailDetector.cs
│   │   ├── PhoneDetector.cs
│   │   ├── KoreanRRNDetector.cs
│   │   └── CreditCardDetector.cs
│   └── Filtering/
│       ├── RuleBasedFilter.cs
│       └── ContentFilterManager.cs
└── FluxCurator.cs           # Main API
```

## Roadmap

- [x] Core chunking strategies (Sentence, Paragraph, Token)
- [x] Korean language profile
- [x] Language detection
- [x] Batch processing
- [x] PII masking (Korean RRN, phone, email, credit card)
- [x] Content filtering
- [x] Semantic chunking
- [ ] Hierarchical chunking
- [ ] Additional language profiles (Japanese, Chinese)
- [ ] Custom detector registration

## License

MIT
