namespace FluxCurator.Core.Core;

using FluxCurator.Core.Domain;

/// <summary>
/// Factory for creating chunker instances based on chunking strategy.
/// </summary>
public interface IChunkerFactory
{
    /// <summary>
    /// Creates a chunker for the specified strategy.
    /// </summary>
    /// <param name="strategy">The chunking strategy to use.</param>
    /// <returns>A chunker instance for the specified strategy.</returns>
    /// <exception cref="ArgumentException">Thrown when the strategy is not supported or requires unavailable dependencies.</exception>
    IChunker CreateChunker(ChunkingStrategy strategy);

    /// <summary>
    /// Gets a chunker for the specified strategy if available.
    /// </summary>
    /// <param name="strategy">The chunking strategy to use.</param>
    /// <param name="chunker">The chunker instance if successful.</param>
    /// <returns>True if the chunker was created successfully, false otherwise.</returns>
    bool TryCreateChunker(ChunkingStrategy strategy, out IChunker? chunker);

    /// <summary>
    /// Gets all available chunking strategies.
    /// </summary>
    IReadOnlyCollection<ChunkingStrategy> AvailableStrategies { get; }

    /// <summary>
    /// Checks if a specific strategy is available.
    /// </summary>
    /// <param name="strategy">The strategy to check.</param>
    /// <returns>True if the strategy is available, false otherwise.</returns>
    bool IsStrategyAvailable(ChunkingStrategy strategy);

    /// <summary>
    /// Gets the default chunker (typically Token or Sentence based).
    /// </summary>
    IChunker DefaultChunker { get; }
}
