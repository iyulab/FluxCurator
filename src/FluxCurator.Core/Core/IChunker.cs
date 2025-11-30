namespace FluxCurator.Core.Core;

using FluxCurator.Core.Domain;

/// <summary>
/// Interface for text chunking operations.
/// </summary>
public interface IChunker
{
    /// <summary>
    /// Gets the name of this chunking strategy.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Gets whether this chunker requires an embedder for semantic operations.
    /// </summary>
    bool RequiresEmbedder { get; }

    /// <summary>
    /// Chunks the given text into smaller segments.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="options">Chunking configuration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of document chunks.</returns>
    Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the number of chunks that would be produced for the given text.
    /// </summary>
    /// <param name="text">The text to estimate.</param>
    /// <param name="options">Chunking configuration options.</param>
    /// <returns>Estimated chunk count.</returns>
    int EstimateChunkCount(string text, ChunkOptions options);
}
