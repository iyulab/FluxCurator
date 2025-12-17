namespace FluxCurator.Core.Domain;

/// <summary>
/// Represents a chunk of text extracted from a document.
/// </summary>
public sealed class DocumentChunk
{
    /// <summary>
    /// Gets or sets the unique identifier for this chunk.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the text content of the chunk.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index of this chunk within the document.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the total number of chunks in the document.
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Gets or sets the location information for this chunk.
    /// </summary>
    public ChunkLocation Location { get; set; } = new();

    /// <summary>
    /// Gets or sets the metadata associated with this chunk.
    /// </summary>
    public ChunkMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the embedding vector for this chunk (if generated).
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Gets the length of the content in characters.
    /// </summary>
    public int Length => Content.Length;

    /// <summary>
    /// Gets whether this chunk has an embedding.
    /// </summary>
    public bool HasEmbedding => Embedding is { Length: > 0 };

    /// <summary>
    /// Creates a simple chunk with minimal configuration.
    /// </summary>
    public static DocumentChunk Create(string content, int index = 0, int totalChunks = 1)
    {
        return new DocumentChunk
        {
            Content = content,
            Index = index,
            TotalChunks = totalChunks
        };
    }
}

/// <summary>
/// Location information for a chunk within the source document.
/// </summary>
public sealed class ChunkLocation
{
    /// <summary>
    /// Gets or sets the start character position in the original text.
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Gets or sets the end character position in the original text.
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// Gets or sets the start line number (1-based).
    /// </summary>
    public int StartLine { get; set; } = 1;

    /// <summary>
    /// Gets or sets the end line number (1-based).
    /// </summary>
    public int EndLine { get; set; } = 1;

    /// <summary>
    /// Gets or sets the start page number (1-based) in the source document.
    /// This is typically set by document processors (e.g., FileFlux) based on page range hints.
    /// Null if page information is not available.
    /// </summary>
    public int? StartPage { get; set; }

    /// <summary>
    /// Gets or sets the end page number (1-based) in the source document.
    /// This is typically set by document processors (e.g., FileFlux) based on page range hints.
    /// Null if page information is not available.
    /// </summary>
    public int? EndPage { get; set; }

    /// <summary>
    /// Gets or sets the section path (e.g., "Chapter 1 > Section 2").
    /// </summary>
    public string? SectionPath { get; set; }

    /// <summary>
    /// Gets the length of this chunk in characters.
    /// </summary>
    public int Length => EndPosition - StartPosition;

    /// <summary>
    /// Gets whether this chunk has page information.
    /// </summary>
    public bool HasPageInfo => StartPage.HasValue;
}

/// <summary>
/// Metadata associated with a document chunk.
/// </summary>
public sealed class ChunkMetadata
{
    /// <summary>
    /// Gets or sets the detected language code (ISO 639-1).
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Gets or sets the estimated token count for this chunk.
    /// </summary>
    public int EstimatedTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the chunking strategy used to create this chunk.
    /// </summary>
    public ChunkingStrategy Strategy { get; set; }

    /// <summary>
    /// Gets or sets whether this chunk starts at a sentence boundary.
    /// </summary>
    public bool StartsAtSentenceBoundary { get; set; }

    /// <summary>
    /// Gets or sets whether this chunk ends at a sentence boundary.
    /// </summary>
    public bool EndsAtSentenceBoundary { get; set; }

    /// <summary>
    /// Gets or sets whether this chunk contains a section header.
    /// </summary>
    public bool ContainsSectionHeader { get; set; }

    /// <summary>
    /// Gets or sets the overlap content from the previous chunk (if any).
    /// </summary>
    public string? OverlapFromPrevious { get; set; }

    /// <summary>
    /// Gets or sets the quality score for this chunk (0.0 to 1.0).
    /// </summary>
    public float QualityScore { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the information density score (0.0 to 1.0).
    /// </summary>
    public float DensityScore { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets additional custom metadata.
    /// </summary>
    public Dictionary<string, object>? Custom { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
