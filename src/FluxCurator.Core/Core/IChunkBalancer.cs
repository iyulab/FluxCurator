namespace FluxCurator.Core.Core;

using FluxCurator.Core.Domain;

/// <summary>
/// Provides post-processing balancing for document chunks to ensure
/// consistent chunk sizes and improve RAG retrieval quality.
/// </summary>
public interface IChunkBalancer
{
    /// <summary>
    /// Balances chunk sizes by merging undersized chunks and splitting oversized ones.
    /// </summary>
    /// <param name="chunks">The chunks to balance.</param>
    /// <param name="options">Chunking options containing size constraints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Balanced chunks with updated indices and metadata.</returns>
    Task<IReadOnlyList<DocumentChunk>> BalanceAsync(
        IReadOnlyList<DocumentChunk> chunks,
        ChunkOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates statistics about chunk size distribution.
    /// </summary>
    /// <param name="chunks">The chunks to analyze.</param>
    /// <returns>Statistics about the chunk distribution.</returns>
    ChunkBalanceStats CalculateStats(IReadOnlyList<DocumentChunk> chunks);
}

/// <summary>
/// Statistics about chunk size distribution.
/// </summary>
public sealed class ChunkBalanceStats
{
    /// <summary>
    /// Gets or sets the total number of chunks.
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// Gets or sets the minimum chunk size in tokens.
    /// </summary>
    public int MinTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum chunk size in tokens.
    /// </summary>
    public int MaxTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the average chunk size in tokens.
    /// </summary>
    public double AverageTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation of chunk sizes.
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Gets the variance ratio (max/min).
    /// A lower ratio indicates more balanced chunks.
    /// </summary>
    public double VarianceRatio => MinTokenCount > 0 ? (double)MaxTokenCount / MinTokenCount : 0;

    /// <summary>
    /// Gets or sets the number of chunks below minimum size.
    /// </summary>
    public int UndersizedChunkCount { get; set; }

    /// <summary>
    /// Gets or sets the number of chunks above maximum size.
    /// </summary>
    public int OversizedChunkCount { get; set; }

    /// <summary>
    /// Gets whether the chunks are considered well-balanced.
    /// Variance ratio of 5 or less is considered acceptable.
    /// </summary>
    public bool IsBalanced => VarianceRatio <= 5.0 && UndersizedChunkCount == 0 && OversizedChunkCount == 0;

    /// <summary>
    /// Returns a summary of the statistics.
    /// </summary>
    public override string ToString() =>
        $"Chunks: {ChunkCount}, Range: {MinTokenCount}-{MaxTokenCount} tokens, " +
        $"Avg: {AverageTokenCount:F1}, Ratio: {VarianceRatio:F1}x, " +
        $"Undersized: {UndersizedChunkCount}, Oversized: {OversizedChunkCount}";
}
