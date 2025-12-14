namespace FluxCurator;

using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Infrastructure.Chunking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Import the actual namespaces for options classes
using PIIMaskingOptionsType = global::FluxCurator.Core.Domain.PIIMaskingOptions;
using ContentFilterOptionsType = global::FluxCurator.Core.Domain.ContentFilterOptions;

/// <summary>
/// Extension methods for registering FluxCurator services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds FluxCurator services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Registers:
    /// - IChunkerFactory (as singleton)
    /// - FluxCurator (as transient)
    ///
    /// If an IEmbedder is already registered, it will be used for semantic chunking.
    /// </remarks>
    public static IServiceCollection AddFluxCurator(this IServiceCollection services)
    {
        return services.AddFluxCurator(_ => { });
    }

    /// <summary>
    /// Adds FluxCurator services with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure FluxCurator options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFluxCurator(
        this IServiceCollection services,
        Action<FluxCuratorOptions> configure)
    {
        var options = new FluxCuratorOptions();
        configure(options);

        // Register chunker factory
        services.TryAddSingleton<IChunkerFactory>(sp =>
        {
            var embedder = sp.GetService<IEmbedder>();
            return new ChunkerFactory(embedder);
        });

        // Register FluxCurator as transient (each request gets new instance)
        services.TryAddTransient(sp =>
        {
            var curator = new FluxCurator();

            // Configure embedder if available
            var embedder = sp.GetService<IEmbedder>();
            if (embedder != null)
            {
                curator.UseEmbedder(embedder);
            }

            // Apply default options
            if (options.DefaultChunkOptions != null)
            {
                curator.WithChunkingOptions(options.DefaultChunkOptions);
            }

            // Enable PII masking if configured
            if (options.EnablePIIMasking)
            {
                if (options.PIIMaskingOptions != null)
                    curator.WithPIIMasking(options.PIIMaskingOptions);
                else
                    curator.WithPIIMasking();
            }

            // Enable content filtering if configured
            if (options.EnableContentFiltering)
            {
                if (options.ContentFilterOptions != null)
                    curator.WithContentFiltering(options.ContentFilterOptions);
                else
                    curator.WithContentFiltering();
            }

            return curator;
        });

        return services;
    }

    /// <summary>
    /// Adds FluxCurator with LocalEmbedder for semantic chunking capabilities.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers LocalEmbedder as the IEmbedder implementation,
    /// enabling semantic chunking capabilities.
    /// </remarks>
    public static IServiceCollection AddFluxCuratorWithLocalEmbedder(this IServiceCollection services)
    {
        return services.AddFluxCuratorWithLocalEmbedder(_ => { });
    }

    /// <summary>
    /// Adds FluxCurator with LocalEmbedder and configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure FluxCurator options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFluxCuratorWithLocalEmbedder(
        this IServiceCollection services,
        Action<FluxCuratorOptions> configure)
    {
        // Register LocalEmbedder as IEmbedder
        services.TryAddSingleton<IEmbedder, LocalEmbedderAdapter>();

        // Add FluxCurator services
        return services.AddFluxCurator(configure);
    }
}

/// <summary>
/// Configuration options for FluxCurator dependency injection.
/// </summary>
public sealed class FluxCuratorOptions
{
    /// <summary>
    /// Gets or sets the default chunking options.
    /// </summary>
    public ChunkOptions? DefaultChunkOptions { get; set; }

    /// <summary>
    /// Gets or sets whether to enable PII masking by default.
    /// </summary>
    public bool EnablePIIMasking { get; set; }

    /// <summary>
    /// Gets or sets the PII masking options (if EnablePIIMasking is true).
    /// </summary>
    public PIIMaskingOptionsType? PIIMaskingOptions { get; set; }

    /// <summary>
    /// Gets or sets whether to enable content filtering by default.
    /// </summary>
    public bool EnableContentFiltering { get; set; }

    /// <summary>
    /// Gets or sets the content filtering options (if EnableContentFiltering is true).
    /// </summary>
    public ContentFilterOptionsType? ContentFilterOptions { get; set; }
}

/// <summary>
/// Adapter to wrap LocalAI.Embedder IEmbeddingModel as IEmbedder.
/// Lazily loads the embedding model on first use.
/// </summary>
internal sealed class LocalEmbedderAdapter : IEmbedder, IAsyncDisposable
{
    private const string DefaultModel = "all-MiniLM-L6-v2";
    private const int DefaultDimension = 384;
    private LocalAI.Embedder.IEmbeddingModel? _model;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc/>
    public int EmbeddingDimension => _model?.Dimensions ?? DefaultDimension;

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(text))
            return new float[EmbeddingDimension];

        var model = await GetModelAsync(cancellationToken).ConfigureAwait(false);
        return await model.EmbedAsync(text, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var textList = texts as IReadOnlyList<string> ?? texts.ToList();
        if (textList.Count == 0)
            return [];

        var model = await GetModelAsync(cancellationToken).ConfigureAwait(false);
        var embeddings = await model.EmbedAsync(textList, cancellationToken).ConfigureAwait(false);
        return embeddings.ToList();
    }

    /// <inheritdoc/>
    public float CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        return LocalAI.Embedder.LocalEmbedder.CosineSimilarity(embedding1, embedding2);
    }

    private async Task<LocalAI.Embedder.IEmbeddingModel> GetModelAsync(CancellationToken cancellationToken)
    {
        if (_model != null)
            return _model;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_model != null)
                return _model;

            _model = await LocalAI.Embedder.LocalEmbedder.LoadAsync(DefaultModel).ConfigureAwait(false);
            return _model;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_model != null)
        {
            await _model.DisposeAsync().ConfigureAwait(false);
        }
        _initLock.Dispose();
    }
}
