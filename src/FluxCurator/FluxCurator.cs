namespace FluxCurator;

using global::FluxCurator.Core;
using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;
using global::FluxCurator.Core.Infrastructure.Filtering;
using global::FluxCurator.Core.Infrastructure.Languages;
using global::FluxCurator.Core.Infrastructure.PII;
using global::FluxCurator.Core.Infrastructure.Refining;
using global::FluxCurator.Infrastructure.Chunking;

/// <summary>
/// Main entry point for FluxCurator text preprocessing operations.
/// Provides fluent API for text chunking, PII masking, content filtering, and text refinement.
/// </summary>
/// <remarks>
/// <para>
/// FluxCurator is designed with zero dependencies as its core philosophy.
/// Semantic chunking features require an optional IEmbedder implementation.
/// </para>
/// <example>
/// Basic usage with fluent builder:
/// <code>
/// var curator = FluxCurator.Create()
///     .WithTextRefinement(TextRefineOptions.ForKorean)
///     .WithPIIMasking()
///     .WithChunkingOptions(ChunkOptions.ForKorean)
///     .Build();
///
/// var result = await curator.PreprocessAsync(text);
/// </code>
/// </example>
/// <example>
/// With semantic chunking:
/// <code>
/// var curator = FluxCurator.Create()
///     .UseEmbedder(myEmbedder)
///     .WithChunkingOptions(opt => opt.Strategy = ChunkingStrategy.Semantic)
///     .Build();
///
/// var chunks = await curator.ChunkAsync(text);
/// </code>
/// </example>
/// </remarks>
public sealed class FluxCurator : IFluxCurator
{
    private IEmbedder? _embedder;
    private ChunkOptions _chunkOptions = ChunkOptions.Default;
    private PIIMaskingOptions _piiOptions = PIIMaskingOptions.Default;
    private ContentFilterOptions _filterOptions = ContentFilterOptions.Default;
    private PIIMasker? _piiMasker;
    private ContentFilterManager? _filterManager;
    private TextRefineOptions? _refineOptions;
    private readonly TextRefiner _refiner = TextRefiner.Instance;
    private readonly Dictionary<ChunkingStrategy, IChunker> _chunkers = new();

    /// <summary>
    /// Creates a new FluxCurator instance with default configuration.
    /// </summary>
    public FluxCurator()
    {
        // Register built-in chunkers
        RegisterChunker(ChunkingStrategy.Sentence, new SentenceChunker());
        RegisterChunker(ChunkingStrategy.Paragraph, new ParagraphChunker());
        RegisterChunker(ChunkingStrategy.Token, new TokenChunker());
        RegisterChunker(ChunkingStrategy.Hierarchical, new HierarchicalChunker());
    }

    /// <summary>
    /// Creates a new FluxCurator builder for fluent configuration.
    /// </summary>
    /// <returns>A new FluxCurator instance for configuration.</returns>
    public static FluxCurator Create() => new();

    /// <summary>
    /// Finalizes the configuration and returns this instance.
    /// This method is optional but completes the fluent builder pattern.
    /// </summary>
    /// <returns>This configured instance.</returns>
    public FluxCurator Build() => this;

    #region Properties

    /// <inheritdoc />
    public bool HasEmbedder => _embedder is not null;

    /// <inheritdoc />
    public bool HasPIIMasking => _piiMasker is not null;

    /// <inheritdoc />
    public bool HasContentFiltering => _filterManager is not null;

    /// <inheritdoc />
    public bool HasTextRefinement => _refineOptions is not null;

    /// <inheritdoc />
    public ChunkOptions ChunkingOptions => _chunkOptions;

    /// <inheritdoc />
    public PIIMaskingOptions PIIMaskingOptions => _piiOptions;

    /// <inheritdoc />
    public ContentFilterOptions ContentFilterOptions => _filterOptions;

    /// <inheritdoc />
    public TextRefineOptions? TextRefineOptions => _refineOptions;

    #endregion

    #region Fluent Configuration

    /// <summary>
    /// Configures an embedder for semantic chunking capabilities.
    /// Automatically registers the SemanticChunker strategy.
    /// </summary>
    /// <param name="embedder">The embedder implementation.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator UseEmbedder(IEmbedder embedder)
    {
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        RegisterChunker(ChunkingStrategy.Semantic, new SemanticChunker(embedder));
        return this;
    }

    /// <summary>
    /// Enables text refinement with default (Light) options.
    /// Text refinement cleans and normalizes raw text before further processing.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithTextRefinement()
    {
        _refineOptions = global::FluxCurator.Core.Domain.TextRefineOptions.Light;
        return this;
    }

    /// <summary>
    /// Enables text refinement with the specified options.
    /// </summary>
    /// <param name="options">The text refinement options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithTextRefinement(TextRefineOptions options)
    {
        _refineOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    /// <summary>
    /// Enables text refinement using a builder action.
    /// </summary>
    /// <param name="configure">Action to configure options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithTextRefinement(Action<TextRefineOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _refineOptions = new TextRefineOptions();
        configure(_refineOptions);
        return this;
    }

    /// <summary>
    /// Configures chunking options.
    /// </summary>
    /// <param name="options">The chunking options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithChunkingOptions(ChunkOptions options)
    {
        _chunkOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }

    /// <summary>
    /// Configures chunking options using a builder action.
    /// </summary>
    /// <param name="configure">Action to configure options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithChunkingOptions(Action<ChunkOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_chunkOptions);
        return this;
    }

    /// <summary>
    /// Enables PII masking with default options.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithPIIMasking()
    {
        _piiMasker = new PIIMasker(_piiOptions);
        return this;
    }

    /// <summary>
    /// Enables PII masking with specified options.
    /// </summary>
    /// <param name="options">The PII masking options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithPIIMasking(PIIMaskingOptions options)
    {
        _piiOptions = options ?? throw new ArgumentNullException(nameof(options));
        _piiMasker = new PIIMasker(_piiOptions);
        return this;
    }

    /// <summary>
    /// Enables PII masking with configuration action.
    /// </summary>
    /// <param name="configure">Action to configure PII options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithPIIMasking(Action<PIIMaskingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_piiOptions);
        _piiMasker = new PIIMasker(_piiOptions);
        return this;
    }

    /// <summary>
    /// Registers a custom PII detector.
    /// PII masking must be enabled first via WithPIIMasking().
    /// </summary>
    /// <param name="detector">The custom detector to register.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PII masking is not enabled.</exception>
    public FluxCurator RegisterPIIDetector(IPIIDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
        EnsurePIIMaskerConfigured();
        _piiMasker!.RegisterDetector(detector);
        return this;
    }

    /// <summary>
    /// Enables content filtering with default options.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithContentFiltering()
    {
        _filterManager = new ContentFilterManager(_filterOptions);
        return this;
    }

    /// <summary>
    /// Enables content filtering with specified options.
    /// </summary>
    /// <param name="options">The content filtering options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithContentFiltering(ContentFilterOptions options)
    {
        _filterOptions = options ?? throw new ArgumentNullException(nameof(options));
        _filterManager = new ContentFilterManager(_filterOptions);
        return this;
    }

    /// <summary>
    /// Enables content filtering with configuration action.
    /// </summary>
    /// <param name="configure">Action to configure filtering options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator WithContentFiltering(Action<ContentFilterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_filterOptions);
        _filterManager = new ContentFilterManager(_filterOptions);
        return this;
    }

    /// <summary>
    /// Registers a custom chunker for a specific strategy.
    /// </summary>
    /// <param name="strategy">The strategy to register for.</param>
    /// <param name="chunker">The chunker implementation.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator RegisterChunker(ChunkingStrategy strategy, IChunker chunker)
    {
        ArgumentNullException.ThrowIfNull(chunker);
        _chunkers[strategy] = chunker;
        return this;
    }

    #endregion

    #region Chunking Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return ChunkAsync(text, _chunkOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var strategy = ResolveStrategy(text, options);
        var chunker = GetChunker(strategy);

        if (chunker.RequiresEmbedder && _embedder is null)
        {
            throw new InvalidOperationException(
                $"Strategy '{chunker.StrategyName}' requires an embedder. " +
                "Call UseEmbedder() first or choose a different strategy.");
        }

        var chunks = await chunker.ChunkAsync(text, options, cancellationToken).ConfigureAwait(false);

        // Apply chunk balancing if enabled
        if (options.EnableChunkBalancing && chunks.Count > 1)
        {
            chunks = await ChunkBalancer.Instance.BalanceAsync(chunks, options, cancellationToken)
                .ConfigureAwait(false);
        }

        return chunks;
    }

    /// <inheritdoc />
    public int EstimateChunkCount(string text)
    {
        return EstimateChunkCount(text, _chunkOptions);
    }

    /// <inheritdoc />
    public int EstimateChunkCount(string text, ChunkOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var strategy = ResolveStrategy(text, options);
        var chunker = GetChunker(strategy);
        return chunker.EstimateChunkCount(text, options);
    }

    /// <inheritdoc />
    public string DetectLanguage(string text)
    {
        return LanguageProfileRegistry.DetectLanguage(text);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<DocumentChunk> ChunkStreamAsync(
        string text,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChunkStreamAsync(text, _chunkOptions, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<DocumentChunk> ChunkStreamAsync(
        string text,
        ChunkOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var strategy = ResolveStrategy(text, options);
        var chunker = GetChunker(strategy);

        if (chunker.RequiresEmbedder && _embedder is null)
        {
            throw new InvalidOperationException(
                $"Strategy '{chunker.StrategyName}' requires an embedder. " +
                "Call UseEmbedder() first or choose a different strategy.");
        }

        // Get all chunks
        var chunks = await chunker.ChunkAsync(text, options, cancellationToken).ConfigureAwait(false);

        // Apply chunk balancing if enabled (requires buffering all chunks)
        if (options.EnableChunkBalancing && chunks.Count > 1)
        {
            chunks = await ChunkBalancer.Instance.BalanceAsync(chunks, options, cancellationToken)
                .ConfigureAwait(false);
        }

        // Yield chunks one by one for streaming support
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }

    #endregion

    #region Text Refinement Operations

    /// <inheritdoc />
    public string RefineText(string text)
    {
        var options = _refineOptions ?? global::FluxCurator.Core.Domain.TextRefineOptions.Light;
        return _refiner.Refine(text, options);
    }

    /// <inheritdoc />
    public string RefineText(string text, TextRefineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return _refiner.Refine(text, options);
    }

    #endregion

    #region PII Operations

    /// <inheritdoc />
    public PIIMaskingResult MaskPII(string text)
    {
        EnsurePIIMaskerConfigured();
        return _piiMasker!.Mask(text);
    }

    /// <inheritdoc />
    public IReadOnlyList<PIIMatch> DetectPII(string text)
    {
        EnsurePIIMaskerConfigured();
        return _piiMasker!.Detect(text);
    }

    /// <inheritdoc />
    public bool ContainsPII(string text)
    {
        EnsurePIIMaskerConfigured();
        return _piiMasker!.ContainsPII(text);
    }

    #endregion

    #region Content Filtering Operations

    /// <inheritdoc />
    public ContentFilterResult FilterContent(string text)
    {
        EnsureFilterManagerConfigured();
        return _filterManager!.Filter(text);
    }

    /// <inheritdoc />
    public bool ContainsFilteredContent(string text)
    {
        EnsureFilterManagerConfigured();
        return _filterManager!.ContainsFilteredContent(text);
    }

    #endregion

    #region Pipeline Operations

    /// <inheritdoc />
    public async Task<PreprocessingResult> PreprocessAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var processedText = text;
        ContentFilterResult? filterResult = null;
        PIIMaskingResult? maskingResult = null;
        string? refinedText = null;

        // Step 0: Apply text refinement if configured
        if (_refineOptions is not null)
        {
            processedText = _refiner.Refine(processedText, _refineOptions);
            refinedText = processedText;
        }

        // Step 1: Apply content filtering if configured
        if (_filterManager is not null)
        {
            filterResult = _filterManager.Filter(processedText);
            if (filterResult.IsBlocked)
            {
                return new PreprocessingResult
                {
                    OriginalText = text,
                    ProcessedText = string.Empty,
                    Chunks = [],
                    PIIMaskingResult = null,
                    ContentFilterResult = filterResult,
                    RefinedText = refinedText
                };
            }
            processedText = filterResult.FilteredText;
        }

        // Step 2: Apply PII masking if configured
        if (_piiMasker is not null)
        {
            maskingResult = _piiMasker.Mask(processedText);
            processedText = maskingResult.MaskedText;
        }

        // Step 3: Apply chunking
        var chunks = await ChunkAsync(processedText, cancellationToken).ConfigureAwait(false);

        return new PreprocessingResult
        {
            OriginalText = text,
            ProcessedText = processedText,
            Chunks = chunks,
            PIIMaskingResult = maskingResult,
            ContentFilterResult = filterResult,
            RefinedText = refinedText
        };
    }

    /// <summary>
    /// Creates a batch processor for processing multiple texts efficiently.
    /// </summary>
    /// <returns>A new batch processor instance.</returns>
    public BatchProcessor CreateBatchProcessor()
    {
        return new BatchProcessor(this);
    }

    #endregion

    #region Private Helpers

    private void EnsurePIIMaskerConfigured()
    {
        if (_piiMasker is null)
        {
            throw new InvalidOperationException(
                "PII masking is not enabled. Call WithPIIMasking() first.");
        }
    }

    private void EnsureFilterManagerConfigured()
    {
        if (_filterManager is null)
        {
            throw new InvalidOperationException(
                "Content filtering is not enabled. Call WithContentFiltering() first.");
        }
    }

    private static ChunkingStrategy ResolveStrategy(string text, ChunkOptions options)
    {
        if (options.Strategy != ChunkingStrategy.Auto)
            return options.Strategy;

        var profile = LanguageProfileRegistry.Instance.DetectProfile(text);
        var tokenCount = profile.EstimateTokenCount(text);

        // For short texts, use sentence chunking
        if (tokenCount <= options.TargetChunkSize * 2)
            return ChunkingStrategy.Sentence;

        // Check paragraph structure
        var paragraphs = profile.FindParagraphBoundaries(text);
        if (paragraphs.Count > 3)
            return ChunkingStrategy.Paragraph;

        // Check sentence structure
        var sentences = profile.FindSentenceBoundaries(text);
        if (sentences.Count > 5)
            return ChunkingStrategy.Sentence;

        // Default to token chunking for unstructured text
        return ChunkingStrategy.Token;
    }

    private IChunker GetChunker(ChunkingStrategy strategy)
    {
        if (_chunkers.TryGetValue(strategy, out var chunker))
            return chunker;

        // Fallback to token chunker
        return _chunkers[ChunkingStrategy.Token];
    }

    #endregion
}
