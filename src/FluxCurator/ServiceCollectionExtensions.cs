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
    /// - IFluxCurator (as transient)
    ///
    /// If an IEmbedder is already registered, it will be used for semantic chunking.
    /// To enable semantic chunking, register an IEmbedder implementation before calling this method:
    /// <code>
    /// services.AddSingleton&lt;IEmbedder&gt;(myEmbedderInstance);
    /// services.AddFluxCurator();
    /// </code>
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

        // Register FluxCurator via IFluxCurator interface (each request gets new instance)
        services.TryAdd(new ServiceDescriptor(typeof(global::FluxCurator.Core.IFluxCurator), sp =>
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
        }, ServiceLifetime.Transient));

        return services;
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

