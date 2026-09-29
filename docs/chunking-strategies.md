# Chunking Strategies

FluxCurator provides six chunking strategies, each optimized for different use cases.

## Strategy Overview

| Strategy | Description | Embedder Required | Best For |
|----------|-------------|-------------------|----------|
| `Auto` | Automatically selects best strategy | No | General use |
| `Sentence` | Split by sentence boundaries | No | Conversational text |
| `Paragraph` | Split by paragraph boundaries | No | Structured documents |
| `Token` | Split by token count | No | Consistent chunk sizes |
| `Semantic` | Split by semantic similarity | **Yes** | RAG applications |
| `Hierarchical` | Preserve document structure | No | Technical docs, Markdown |

## Sentence Strategy

Splits text at sentence boundaries while respecting chunk size constraints.

The examples on this page create chunkers from a `ChunkerFactory` (namespace `FluxCurator.Infrastructure.Chunking`);
`Curator` does the same for you from `ChunkOptions.Strategy`.

```csharp
using FluxCurator.Infrastructure.Chunking;

var factory = new ChunkerFactory();
var chunker = factory.CreateChunker(ChunkingStrategy.Sentence);

var options = new ChunkOptions
{
    TargetChunkSize = 512,
    MaxChunkSize = 1024,
    PreserveSentences = true
};

var chunks = await chunker.ChunkAsync(text, options);
```

### Sentence Detection

FluxCurator includes language-aware sentence detection:

- **English**: Period, question mark, exclamation mark
- **Korean**: Korean sentence endings (습니다, 요, 다)
- **Japanese**: Japanese punctuation (。)
- **Chinese**: Chinese punctuation (。)

### When to Use

- Conversational content (chat logs, dialogues)
- Q&A datasets
- Social media content
- General-purpose text

## Paragraph Strategy

Splits text at paragraph boundaries (double newlines).

```csharp
var chunker = factory.CreateChunker(ChunkingStrategy.Paragraph);

var options = new ChunkOptions
{
    TargetChunkSize = 1000,
    PreserveParagraphs = true
};

var chunks = await chunker.ChunkAsync(text, options);
```

### When to Use

- Blog posts and articles
- Essays and reports
- Email content
- Any content with clear paragraph structure

## Token Strategy

Splits text by approximate token count, ideal for LLM context window management.

```csharp
var chunker = factory.CreateChunker(ChunkingStrategy.Token);

var options = new ChunkOptions
{
    TargetChunkSize = 256,     // Target token count
    MaxChunkSize = 512,        // Maximum tokens
    OverlapSize = 32,          // Overlap for context continuity
    PreserveSentences = true   // Try to end at sentence boundaries
};

var chunks = await chunker.ChunkAsync(text, options);
```

### Token Estimation

FluxCurator estimates tokens using language-aware heuristics:

- **English**: ~4 characters per token
- **Korean**: ~2-3 characters per token (due to complex characters)
- **CJK**: ~1.5-2 characters per token

### When to Use

- When you need consistent chunk sizes
- For embedding models with fixed input sizes
- When managing LLM context windows

## Hierarchical Strategy

Preserves document structure with parent-child relationships based on headers.

```csharp
var chunker = factory.CreateChunker(ChunkingStrategy.Hierarchical);

var options = new ChunkOptions
{
    MaxChunkSize = 1024,
    PreserveSectionHeaders = true
};

var chunks = await chunker.ChunkAsync(markdownText, options);

foreach (var chunk in chunks)
{
    var level = chunk.Metadata.Custom?["HierarchyLevel"];
    var parentId = chunk.Metadata.Custom?["ParentId"];
    var sectionPath = chunk.Location.SectionPath;

    Console.WriteLine($"[Level {level}] {sectionPath}");
    Console.WriteLine(chunk.Content);
}
```

### Hierarchy Metadata

Each chunk's `Metadata.Custom` holds these keys (the names are constants on `HierarchicalChunker`):

- `HierarchyLevel` (`HierarchyLevelKey`): the header depth (1 = `#`, 6 = `######`); 0 for text before the first header
- `ParentId` (`ParentIdKey`): ID of the parent chunk (absent for a root)
- `ChildIds` (`ChildIdsKey`): list of child chunk IDs
- `SectionTitle` (`SectionTitleKey`): title of the current section

### Header Detection

Supports Markdown-style headers:

```markdown
# Level 1 (HierarchyLevel = 1)
## Level 2 (HierarchyLevel = 2)
### Level 3 (HierarchyLevel = 3)
...
###### Level 6 (HierarchyLevel = 6)
```

### When to Use

- Technical documentation
- Markdown/RST documents
- API documentation
- Structured reports with clear sections
- Any content with heading hierarchy

## Semantic Strategy

Splits text based on semantic similarity using embeddings.

```csharp
using FluxCurator.Infrastructure.Chunking;

// Requires an IEmbedder implementation (inject your own, e.g. OpenAI, LMSupply)
var semanticFactory = new ChunkerFactory(myEmbedder);

var chunker = semanticFactory.CreateChunker(ChunkingStrategy.Semantic);

var options = new ChunkOptions
{
    TargetChunkSize = 512,
    SemanticSimilarityThreshold = 0.5f  // Lower = more splits
};

var chunks = await chunker.ChunkAsync(text, options);
```

### How It Works

1. Split text into initial segments (sentences or small paragraphs)
2. Generate embeddings for each segment
3. Calculate cosine similarity between adjacent segments
4. Split when similarity drops below threshold
5. Merge small chunks to meet minimum size

### Similarity Threshold

- **0.3-0.4**: Aggressive splitting (many small, focused chunks)
- **0.5-0.6**: Balanced (recommended for most RAG use cases)
- **0.7-0.8**: Conservative (fewer, larger chunks)

### When to Use

- RAG applications
- Question answering systems
- Document search and retrieval
- Content where topic boundaries matter

## Auto Strategy

Selects a strategy from the text itself. `Auto` is the default `ChunkOptions.Strategy`, and `Curator` resolves it per
call:

```csharp
var curator = new Curator();              // ChunkOptions.Default: Strategy = Auto
var chunks = await curator.ChunkAsync(text);
```

A `ChunkerFactory` cannot see the text, so `factory.CreateChunker(ChunkingStrategy.Auto)` returns the Sentence chunker.
To resolve `Auto` yourself, call `ChunkingStrategyResolver.Resolve(text, options)` first.

### Selection Logic

1. Short text (at most twice `TargetChunkSize` in estimated tokens): **Sentence**
2. More than three paragraphs: **Paragraph**
3. More than five sentences: **Sentence**
4. Otherwise: **Token**

`Auto` never selects Semantic or Hierarchical — ask for them explicitly.

## Chunk Overlap

All strategies support overlap for context continuity:

```csharp
var options = new ChunkOptions
{
    OverlapSize = 50  // Number of tokens to overlap
};
```

The overlapping text is stored in `chunk.Metadata.OverlapFromPrevious` (the text carried over from the previous chunk,
or `null` for the first).

## Language Support

FluxCurator includes profiles for 13 languages:

| Language | Code | Special Features |
|----------|------|------------------|
| Korean | `ko` | 습니다체/해요체 endings |
| English | `en` | Standard boundaries |
| Japanese | `ja` | Japanese punctuation |
| Chinese (Simplified and Traditional) | `zh` (`zh-TW`, `zh-CN`, ... resolve to it) | Chinese punctuation |
| Spanish | `es` | Spanish punctuation |
| French | `fr` | French punctuation |
| German | `de` | German punctuation |
| Portuguese | `pt` | Portuguese punctuation |
| Russian | `ru` | Cyrillic support |
| Arabic | `ar` | RTL support |

### Language Auto-Detection

```csharp
var options = new ChunkOptions
{
    LanguageCode = null  // Auto-detect language
};
```

Or specify explicitly:

```csharp
var options = new ChunkOptions
{
    LanguageCode = "ko"  // Force Korean
};
```

## Choosing a Strategy

### Decision Tree

```
Is content semantically important for retrieval?
├─ Yes → Do you have an embedder?
│       ├─ Yes → Semantic
│       └─ No → Sentence or Paragraph
│
└─ No → Does content have clear structure?
        ├─ Yes → Does it use headers?
        │       ├─ Yes → Hierarchical
        │       └─ No → Paragraph
        │
        └─ No → Do you need fixed sizes?
                ├─ Yes → Token
                └─ No → Sentence
```

### Recommendations by Content Type

| Content Type | Recommended Strategy |
|--------------|---------------------|
| Chat logs | Sentence |
| Technical docs | Hierarchical |
| Research papers | Semantic |
| Blog posts | Paragraph |
| Code documentation | Hierarchical |
| News articles | Sentence or Semantic |
| Legal documents | Paragraph |
| Social media | Sentence |
