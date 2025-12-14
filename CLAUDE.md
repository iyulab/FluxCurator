# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~SentenceChunkerTests"

# Run single test method
dotnet test --filter "FullyQualifiedName~SentenceChunkerTests.Should_Chunk_Korean_Sentences"

# Pack NuGet packages
dotnet pack
```

## Architecture Overview

FluxCurator is a text preprocessing library for RAG pipelines with two packages:
- **FluxCurator.Core**: Zero-dependency core (rule-based chunking, PII masking, filtering)
- **FluxCurator**: Main package with LocalEmbedder integration for semantic chunking

### Processing Pipeline

```
Raw Text → Refine → Filter Content → Mask PII → Chunk
```

Each step is optional and configurable via fluent builder pattern:
```csharp
var curator = FluxCurator.Create()
    .WithTextRefinement(TextRefineOptions.Standard)
    .WithContentFiltering()
    .WithPIIMasking(PIIMaskingOptions.ForLanguages("en", "ko"))
    .WithChunkingOptions(ChunkOptions.ForRAG)
    .Build();
```

### Core Interfaces

| Interface | Purpose | Location |
|-----------|---------|----------|
| `IFluxCurator` | Main API facade | Core/Core/ |
| `IChunker` | Chunking strategy contract | Core/Core/ |
| `IChunkerFactory` | Strategy factory for DI | Core/Core/ |
| `IPIIDetector` | PII detection contract | Core/Core/ |
| `INationalIdDetector` | Country-specific ID detection | Core/Core/ |
| `ILanguageProfile` | Language-specific text processing | Core/Core/ |

### Chunking Strategies

All chunkers extend `ChunkerBase` and implement `IChunker`:

| Strategy | Class | Location | Requires Embedder |
|----------|-------|----------|-------------------|
| Sentence | `SentenceChunker` | Core/Infrastructure/Chunking/ | No |
| Paragraph | `ParagraphChunker` | Core/Infrastructure/Chunking/ | No |
| Token | `TokenChunker` | Core/Infrastructure/Chunking/ | No |
| Hierarchical | `HierarchicalChunker` | Core/Infrastructure/Chunking/ | No |
| Semantic | `SemanticChunker` | FluxCurator/Infrastructure/Chunking/ | **Yes** |

### PII Detection Architecture

National ID detectors extend `NationalIdDetectorBase` with checksum validation:
- Located in `Core/Infrastructure/PII/NationalId/`
- Each detector implements country-specific validation (Modulo-11, Luhn, Verhoeff, etc.)
- `NationalIdRegistry` manages detector lookup by language code

### Language Profiles

Profiles define sentence boundaries and token estimation per language:
- Base class: `LanguageProfileBase`
- Registry: `LanguageProfileRegistry.Instance` (singleton with auto-detection)
- 14 languages supported (Korean, English, Japanese, Chinese, Vietnamese, Thai, etc.)

## Key Design Patterns

1. **Singleton Registries**: `LanguageProfileRegistry.Instance`, `NationalIdRegistry`, `ChunkBalancer.Instance`
2. **Fluent Builder**: `FluxCurator.Create()...Build()`
3. **Strategy Pattern**: Chunkers selected via `ChunkingStrategy` enum
4. **Factory Pattern**: `IChunkerFactory` for DI scenarios

## Central Package Management

Uses `Directory.Packages.props` for version management. When adding dependencies:
1. Add `<PackageVersion>` to `Directory.Packages.props`
2. Reference in `.csproj` without version attribute

### Key Dependencies
- **LocalAI.Embedder**: Semantic chunking (replaces deprecated LocalEmbedder)
- **Microsoft.Extensions.DependencyInjection.Abstractions**: DI support

## Code Style

- C# 13 / .NET 10.0 target
- Nullable enabled (`<Nullable>enable</Nullable>`)
- XML documentation required (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`)
- Warnings as errors for nullable only
