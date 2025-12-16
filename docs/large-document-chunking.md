# Large Document Chunking Guide

This guide explains how to effectively chunk large documents (500+ pages, 100K+ tokens) using FluxCurator for optimal RAG retrieval quality.

## Overview

Large documents present unique challenges for RAG pipelines:
- Fixed-size chunking loses semantic coherence
- Context fragmentation across related content
- Increased chunk count without quality improvement

FluxCurator addresses these challenges with **Hierarchical Chunking** and **Chunk Balancing**.

## Quick Start

```csharp
// Recommended settings for large documents
var curator = new FluxCurator()
    .WithChunkingOptions(opt =>
    {
        opt.Strategy = ChunkingStrategy.Hierarchical;
        opt.TargetChunkSize = 768;      // Larger chunks for context
        opt.MaxChunkSize = 1536;
        opt.MinChunkSize = 200;
        opt.OverlapSize = 100;          // ~13% overlap
        opt.PreserveSectionHeaders = true;
        opt.EnableChunkBalancing = true;
    });

var chunks = await curator.ChunkAsync(largeDocument);
```

Or use the preset:

```csharp
var curator = new FluxCurator()
    .WithChunkingOptions(ChunkOptions.ForLargeDocument);
```

## Hierarchical Chunking Strategy

The `ChunkingStrategy.Hierarchical` recognizes document structure:

### Supported Structure Markers

| Marker | Level | Description |
|--------|-------|-------------|
| `#` | 1 | Main title / Chapter |
| `##` | 2 | Section |
| `###` | 3 | Subsection |
| `####` | 4 | Sub-subsection |
| `#####` | 5 | Paragraph group |
| `######` | 6 | Detail level |

### Chunk Metadata

Each chunk includes hierarchy information:

```csharp
foreach (var chunk in chunks)
{
    // Hierarchy level (1 = top, 6 = deepest)
    var level = chunk.Metadata.Custom?["HierarchyLevel"];

    // Parent chunk reference for tree traversal
    var parentId = chunk.Metadata.Custom?["ParentId"];

    // Child chunk IDs
    var childIds = chunk.Metadata.Custom?["ChildIds"];

    // Section path like "Chapter 1 > Section 2 > Details"
    var sectionPath = chunk.Location.SectionPath;

    Console.WriteLine($"[Level {level}] {sectionPath}");
    Console.WriteLine($"Tokens: ~{chunk.Metadata.EstimatedTokenCount}");
}
```

## Chunk Balancing

Enable `EnableChunkBalancing` to automatically:
- **Merge** undersized chunks (below `MinChunkSize`)
- **Split** oversized chunks (above `MaxChunkSize`)

This ensures consistent chunk sizes while respecting document structure.

```csharp
var options = new ChunkOptions
{
    Strategy = ChunkingStrategy.Hierarchical,
    MinChunkSize = 200,       // Merge chunks smaller than this
    MaxChunkSize = 1536,      // Split chunks larger than this
    EnableChunkBalancing = true
};
```

## Document Size Guidelines

| Document Size | Recommended Strategy | Target Chunk Size | Overlap |
|---------------|---------------------|-------------------|---------|
| Small (<10K tokens) | Sentence/Paragraph | 256-512 | 10% |
| Medium (10K-50K) | Semantic/Hierarchical | 512-768 | 15% |
| Large (50K-100K) | Hierarchical | 768-1024 | 15-20% |
| Very Large (>100K) | Hierarchical | 1024-1536 | 20% |

## Best Practices

### 1. Use Structure-Aware Chunking

```csharp
// Always enable section header preservation for large docs
opt.PreserveSectionHeaders = true;
opt.PreserveParagraphs = true;
```

### 2. Increase Overlap for Context

For technical documents with dense cross-references:

```csharp
// 20% overlap for better context preservation
opt.OverlapSize = (int)(opt.TargetChunkSize * 0.2);
```

### 3. Combine with Text Refinement

Clean noisy content before chunking:

```csharp
var curator = new FluxCurator()
    .WithTextRefinement(TextRefineOptions.ForPdfContent)
    .WithChunkingOptions(ChunkOptions.ForLargeDocument);

var result = await curator.PreprocessAsync(pdfText);
```

### 4. Use Streaming for Memory Efficiency

```csharp
// Process chunks as they're generated
await foreach (var chunk in curator.ChunkStreamAsync(largeText))
{
    await ProcessChunkAsync(chunk);
}
```

## Integration with FileFlux

When processing documents through FileFlux, structure hints are automatically passed:

```csharp
// FileFlux provides document structure
var fileFlux = new FileFlux.DocumentProcessor();
var document = await fileFlux.ProcessAsync("large-manual.pdf");

// FluxCurator uses structure for intelligent chunking
var curator = new FluxCurator()
    .WithChunkingOptions(ChunkOptions.ForLargeDocument);

var chunks = await curator.ChunkAsync(document.Text);
```

## Troubleshooting

### Too Many Small Chunks

**Problem**: Document produces many tiny chunks.

**Solution**: Increase `MinChunkSize` and enable balancing:
```csharp
opt.MinChunkSize = 300;
opt.EnableChunkBalancing = true;
```

### Lost Context at Boundaries

**Problem**: Related content split across chunks.

**Solution**: Increase overlap and use hierarchical strategy:
```csharp
opt.Strategy = ChunkingStrategy.Hierarchical;
opt.OverlapSize = 150;  // Increase overlap
```

### Inconsistent Chunk Sizes

**Problem**: Chunk sizes vary dramatically.

**Solution**: Enable chunk balancing with tighter bounds:
```csharp
opt.MinChunkSize = opt.TargetChunkSize / 2;
opt.MaxChunkSize = opt.TargetChunkSize * 2;
opt.EnableChunkBalancing = true;
```

## Performance Considerations

| Document Size | Approximate Processing Time | Memory Usage |
|---------------|----------------------------|--------------|
| 10K tokens | < 100ms | ~10MB |
| 100K tokens | < 1s | ~50MB |
| 1M tokens | < 10s | ~200MB |

For very large documents, use streaming:
```csharp
await foreach (var chunk in curator.ChunkStreamAsync(text))
{
    // Process incrementally
}
```

## See Also

- [Chunking Strategies](chunking-strategies.md) - Overview of all strategies
- [FileFlux Integration](fileflux-integration.md) - Document processing pipeline
- [Getting Started](getting-started.md) - Basic usage guide
