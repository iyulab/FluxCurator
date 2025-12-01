namespace FluxCurator;

using global::FluxCurator.Core;
using global::FluxCurator.Core.Domain;

/// <summary>
/// Batch processor for chunking multiple texts efficiently.
/// </summary>
public sealed class BatchProcessor
{
    private readonly IFluxCurator _curator;
    private readonly List<string> _texts = [];
    private int _maxConcurrency = Environment.ProcessorCount;

    /// <summary>
    /// Creates a new batch processor.
    /// </summary>
    /// <param name="curator">The FluxCurator instance to use for processing.</param>
    public BatchProcessor(IFluxCurator curator)
    {
        _curator = curator ?? throw new ArgumentNullException(nameof(curator));
    }

    /// <summary>
    /// Gets the number of texts in the batch.
    /// </summary>
    public int Count => _texts.Count;

    /// <summary>
    /// Adds a text to the batch.
    /// </summary>
    /// <param name="text">The text to add.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public BatchProcessor AddText(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            _texts.Add(text);
        return this;
    }

    /// <summary>
    /// Adds multiple texts to the batch.
    /// </summary>
    /// <param name="texts">The texts to add.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public BatchProcessor AddTexts(IEnumerable<string> texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        foreach (var text in texts)
            AddText(text);
        return this;
    }

    /// <summary>
    /// Sets the maximum concurrency for parallel processing.
    /// </summary>
    /// <param name="maxConcurrency">Maximum number of concurrent operations (1-32).</param>
    /// <returns>This instance for fluent chaining.</returns>
    public BatchProcessor WithMaxConcurrency(int maxConcurrency)
    {
        _maxConcurrency = Math.Max(1, Math.Min(maxConcurrency, 32));
        return this;
    }

    /// <summary>
    /// Processes all texts in the batch using chunking.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of chunk results for each text.</returns>
    public async Task<IReadOnlyList<IReadOnlyList<DocumentChunk>>> ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        if (_texts.Count == 0)
            return [];

        var semaphore = new SemaphoreSlim(_maxConcurrency);
        var tasks = _texts.Select(async text =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _curator.ChunkAsync(text, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    /// <summary>
    /// Processes all texts in the batch using the full preprocessing pipeline.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of preprocessing results for each text.</returns>
    public async Task<IReadOnlyList<PreprocessingResult>> PreprocessAsync(
        CancellationToken cancellationToken = default)
    {
        if (_texts.Count == 0)
            return [];

        var semaphore = new SemaphoreSlim(_maxConcurrency);
        var tasks = _texts.Select(async text =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _curator.PreprocessAsync(text, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    /// <summary>
    /// Gets the total estimated chunk count for all texts.
    /// </summary>
    /// <returns>Total estimated chunk count.</returns>
    public int GetTotalEstimatedChunks()
    {
        return _texts.Sum(t => _curator.EstimateChunkCount(t));
    }

    /// <summary>
    /// Clears all texts from the batch.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public BatchProcessor Clear()
    {
        _texts.Clear();
        return this;
    }
}
