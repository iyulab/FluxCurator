namespace FluxCurator.Core.Core;

/// <summary>
/// Interface for embedding service integration.
/// Supports optional dependency injection for semantic chunking capabilities.
/// </summary>
public interface IEmbedder
{
    /// <summary>
    /// Gets the dimension of embedding vectors produced by this embedder.
    /// </summary>
    int EmbeddingDimension { get; }

    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A float array representing the embedding vector.</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of embedding vectors.</returns>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates cosine similarity between two embedding vectors.
    /// </summary>
    /// <param name="embedding1">First embedding vector.</param>
    /// <param name="embedding2">Second embedding vector.</param>
    /// <returns>Similarity score between -1 and 1.</returns>
    float CalculateSimilarity(float[] embedding1, float[] embedding2);
}
