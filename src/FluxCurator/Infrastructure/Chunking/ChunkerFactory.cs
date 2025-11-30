namespace FluxCurator.Infrastructure.Chunking;

using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;

/// <summary>
/// Factory for creating chunker instances based on chunking strategy.
/// Supports all built-in chunking strategies including semantic chunking.
/// </summary>
public sealed class ChunkerFactory : IChunkerFactory
{
    private readonly IEmbedder? _embedder;
    private readonly Dictionary<ChunkingStrategy, Func<IChunker>> _chunkerFactories;
    private readonly HashSet<ChunkingStrategy> _availableStrategies;

    /// <summary>
    /// Creates a new chunker factory.
    /// </summary>
    /// <param name="embedder">Optional embedder for semantic chunking. If null, semantic chunking will be unavailable.</param>
    public ChunkerFactory(IEmbedder? embedder = null)
    {
        _embedder = embedder;
        _chunkerFactories = new Dictionary<ChunkingStrategy, Func<IChunker>>();
        _availableStrategies = new HashSet<ChunkingStrategy>();

        RegisterBuiltInChunkers();
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<ChunkingStrategy> AvailableStrategies => _availableStrategies;

    /// <inheritdoc/>
    public IChunker DefaultChunker => CreateChunker(ChunkingStrategy.Sentence);

    /// <inheritdoc/>
    public IChunker CreateChunker(ChunkingStrategy strategy)
    {
        if (strategy == ChunkingStrategy.Auto)
        {
            // For Auto strategy, return a chunker that can analyze content
            // Default to Sentence for now, can be enhanced with content analysis
            return CreateChunker(ChunkingStrategy.Sentence);
        }

        if (!_chunkerFactories.TryGetValue(strategy, out var factory))
        {
            throw new ArgumentException(
                $"Chunking strategy '{strategy}' is not available. " +
                $"Available strategies: {string.Join(", ", _availableStrategies)}",
                nameof(strategy));
        }

        return factory();
    }

    /// <inheritdoc/>
    public bool TryCreateChunker(ChunkingStrategy strategy, out IChunker? chunker)
    {
        if (strategy == ChunkingStrategy.Auto)
        {
            chunker = CreateChunker(ChunkingStrategy.Sentence);
            return true;
        }

        if (_chunkerFactories.TryGetValue(strategy, out var factory))
        {
            chunker = factory();
            return true;
        }

        chunker = null;
        return false;
    }

    /// <inheritdoc/>
    public bool IsStrategyAvailable(ChunkingStrategy strategy)
    {
        return strategy == ChunkingStrategy.Auto || _availableStrategies.Contains(strategy);
    }

    /// <summary>
    /// Registers a custom chunker for a strategy.
    /// </summary>
    /// <param name="strategy">The strategy to register.</param>
    /// <param name="factory">Factory function to create the chunker.</param>
    public void RegisterChunker(ChunkingStrategy strategy, Func<IChunker> factory)
    {
        _chunkerFactories[strategy] = factory;
        _availableStrategies.Add(strategy);
    }

    private void RegisterBuiltInChunkers()
    {
        // Register core chunkers (no embedder required)
        RegisterChunker(ChunkingStrategy.Token, () => new TokenChunker());
        RegisterChunker(ChunkingStrategy.Sentence, () => new SentenceChunker());
        RegisterChunker(ChunkingStrategy.Paragraph, () => new ParagraphChunker());

        // Register semantic chunker if embedder is available
        if (_embedder != null)
        {
            RegisterChunker(ChunkingStrategy.Semantic, () => new SemanticChunker(_embedder));
        }

        // TODO: Add Hierarchical chunker when implemented
    }
}
