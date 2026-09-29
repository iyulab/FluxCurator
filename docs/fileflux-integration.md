# FileFlux Integration

[FileFlux](https://github.com/iyulab/FileFlux) turns document files (PDF, DOCX, PPTX, XLSX, HWP, HTML, Markdown, ...)
into RAG-ready chunks. It extracts and structures the text itself and delegates the chunking step to FluxCurator.

This page describes how the two fit together. The FileFlux side of the API is documented in the
[FileFlux README](https://github.com/iyulab/FileFlux#readme) — the examples below name FileFlux types and are not
compiled with this repository.

## How FileFlux uses FluxCurator

- `services.AddFileFlux()` calls `services.AddFluxCurator()`, so FluxCurator's `IChunkerFactory` is in the container.
  Both are registered with `TryAdd`: an `IChunkerFactory` you register first is the one FileFlux uses.
- FileFlux chunks through `IChunkerFactory.CreateChunker(strategy)`. The strategy is FileFlux's
  `ChunkingOptions.Strategy` string, mapped case-insensitively to FluxCurator's `ChunkingStrategy`:

  | FileFlux `ChunkingOptions.Strategy` | FluxCurator `ChunkingStrategy` |
  |---|---|
  | `"auto"` (default, or unset) | `Auto` — resolved from the text (see [Chunking Strategies](chunking-strategies.md#auto-strategy)) |
  | `"sentence"` | `Sentence` |
  | `"paragraph"` | `Paragraph` |
  | `"token"` | `Token` |
  | `"semantic"` | `Semantic` — needs an `IEmbedder` in the container |
  | `"hierarchical"` | `Hierarchical` |

  Any other value throws `ArgumentException` naming the accepted values.

## Semantic chunking

Register a FluxCurator `IEmbedder` before `AddFileFlux()`; `IChunkerFactory` picks it up and `"semantic"` becomes
available:

```csharp
services.AddSingleton<IEmbedder>(myEmbedder);   // FluxCurator.Core.Core.IEmbedder
services.AddFileFlux();
```

Without an embedder, asking for `"semantic"` fails when the chunker is created.

## Converting chunks

FileFlux's `DocumentChunk` and FluxCurator's `DocumentChunk` are different types. `FluxCuratorChunkAdapter`
(namespace `FileFlux.Infrastructure.Adapters`) converts between them:

```csharp
using FileFlux.Infrastructure.Adapters;

IReadOnlyList<FileFlux.Core.DocumentChunk> fileFluxChunks = curatorChunks.ToFileFluxChunks();
IReadOnlyList<FluxCurator.Core.Domain.DocumentChunk> backAgain = fileFluxChunks.ToFluxCuratorChunks();
```

The conversion carries the content, the location (character range, lines, pages, section path) and the chunk metadata.

## Using FluxCurator directly on extracted text

If you extract text some other way, chunk it with FluxCurator itself — see [Getting Started](getting-started.md) and
[Large Document Chunking](large-document-chunking.md).
