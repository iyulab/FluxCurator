namespace FluxCurator.Core.Domain;

/// <summary>
/// Defines available chunking strategies.
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>
    /// Automatically select the best strategy based on content analysis.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Chunk by sentences. Best for conversational or narrative content.
    /// </summary>
    Sentence = 1,

    /// <summary>
    /// Chunk by paragraphs. Best for structured documents.
    /// </summary>
    Paragraph = 2,

    /// <summary>
    /// Chunk by token count. Best for consistent-size chunks.
    /// </summary>
    Token = 3,

    /// <summary>
    /// Semantic chunking using embeddings to find natural boundaries.
    /// Requires an IEmbedder implementation.
    /// </summary>
    Semantic = 4,

    /// <summary>
    /// Hierarchical chunking preserving document structure.
    /// Creates nested chunks for sections, subsections, etc.
    /// </summary>
    Hierarchical = 5
}
