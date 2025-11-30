namespace FluxCurator;

using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;
using global::FluxCurator.Core.Infrastructure.Filtering;
using global::FluxCurator.Core.Infrastructure.Languages;
using global::FluxCurator.Core.Infrastructure.PII;
using global::FluxCurator.Infrastructure.Chunking;

/// <summary>
/// Main entry point for FluxCurator text preprocessing operations.
/// Provides fluent API for text chunking, PII masking, and content filtering.
/// </summary>
/// <remarks>
/// <para>
/// FluxCurator is designed with zero dependencies as its core philosophy.
/// Semantic chunking features require an optional IEmbedder implementation.
/// </para>
/// <example>
/// Basic usage:
/// <code>
/// var curator = new FluxCurator()
///     .WithChunkingOptions(ChunkOptions.ForKorean);
///
/// var chunks = await curator.ChunkAsync(text);
/// </code>
/// </example>
/// <example>
/// With semantic chunking:
/// <code>
/// var curator = new FluxCurator()
///     .UseEmbedder(myEmbedder)
///     .WithChunkingOptions(opt => opt.Strategy = ChunkingStrategy.Semantic);
///
/// var chunks = await curator.ChunkAsync(text);
/// </code>
/// </example>
/// </remarks>
public sealed class FluxCurator
{
    private IEmbedder? _embedder;
    private ChunkOptions _chunkOptions = ChunkOptions.Default;
    private PIIMaskingOptions _piiOptions = PIIMaskingOptions.Default;
    private ContentFilterOptions _filterOptions = ContentFilterOptions.Default;
    private IPIIMasker? _piiMasker;
    private IContentFilterManager? _filterManager;
    private readonly Dictionary<ChunkingStrategy, IChunker> _chunkers = new();

    /// <summary>
    /// Creates a new FluxCurator instance.
    /// </summary>
    public FluxCurator()
    {
        // Register built-in chunkers
        RegisterChunker(ChunkingStrategy.Sentence, new SentenceChunker());
        RegisterChunker(ChunkingStrategy.Paragraph, new ParagraphChunker());
        RegisterChunker(ChunkingStrategy.Token, new TokenChunker());
    }

    /// <summary>
    /// Gets whether an embedder is configured for semantic operations.
    /// </summary>
    public bool HasEmbedder => _embedder is not null;

    /// <summary>
    /// Gets whether PII masking is enabled.
    /// </summary>
    public bool HasPIIMasking => _piiMasker is not null;

    /// <summary>
    /// Gets whether content filtering is enabled.
    /// </summary>
    public bool HasContentFiltering => _filterManager is not null;

    /// <summary>
    /// Gets the current chunking options.
    /// </summary>
    public ChunkOptions ChunkingOptions => _chunkOptions;

    /// <summary>
    /// Gets the current PII masking options.
    /// </summary>
    public PIIMaskingOptions PIIMaskingOptions => _piiOptions;

    /// <summary>
    /// Gets the current content filtering options.
    /// </summary>
    public ContentFilterOptions ContentFilterOptions => _filterOptions;

    /// <summary>
    /// Configures an embedder for semantic chunking capabilities.
    /// Automatically registers the SemanticChunker strategy.
    /// </summary>
    /// <param name="embedder">The embedder implementation.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public FluxCurator UseEmbedder(IEmbedder embedder)
    {
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));

        // Automatically register semantic chunker when embedder is provided
        RegisterChunker(ChunkingStrategy.Semantic, new SemanticChunker(embedder));

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

    /// <summary>
    /// Chunks the given text according to the configured options.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of document chunks.</returns>
    public Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return ChunkAsync(text, _chunkOptions, cancellationToken);
    }

    /// <summary>
    /// Chunks the given text with custom options.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="options">Custom chunking options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of document chunks.</returns>
    public async Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var strategy = ResolveStrategy(text, options);
        var chunker = GetChunker(strategy);

        // Validate semantic chunking requirements
        if (chunker.RequiresEmbedder && _embedder is null)
        {
            throw new InvalidOperationException(
                $"Strategy '{chunker.StrategyName}' requires an embedder. " +
                "Call UseEmbedder() first or choose a different strategy.");
        }

        return await chunker.ChunkAsync(text, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Estimates the number of chunks for the given text.
    /// </summary>
    /// <param name="text">The text to estimate.</param>
    /// <returns>Estimated chunk count.</returns>
    public int EstimateChunkCount(string text)
    {
        return EstimateChunkCount(text, _chunkOptions);
    }

    /// <summary>
    /// Estimates the number of chunks for the given text with custom options.
    /// </summary>
    /// <param name="text">The text to estimate.</param>
    /// <param name="options">Custom chunking options.</param>
    /// <returns>Estimated chunk count.</returns>
    public int EstimateChunkCount(string text, ChunkOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var strategy = ResolveStrategy(text, options);
        var chunker = GetChunker(strategy);
        return chunker.EstimateChunkCount(text, options);
    }

    /// <summary>
    /// Detects the language of the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>ISO 639-1 language code.</returns>
    public string DetectLanguage(string text)
    {
        return LanguageProfileRegistry.Instance.DetectLanguage(text);
    }

    /// <summary>
    /// Masks PII in the given text.
    /// </summary>
    /// <param name="text">The text to mask.</param>
    /// <returns>The masking result with masked text and detection details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PII masking is not enabled.</exception>
    public PIIMaskingResult MaskPII(string text)
    {
        EnsurePIIMaskerConfigured();
        return _piiMasker!.Mask(text);
    }

    /// <summary>
    /// Detects PII in the given text without masking.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>A list of detected PII matches.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PII masking is not enabled.</exception>
    public IReadOnlyList<PIIMatch> DetectPII(string text)
    {
        EnsurePIIMaskerConfigured();
        return _piiMasker!.Detect(text);
    }

    /// <summary>
    /// Checks if the text contains any PII.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if PII is found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PII masking is not enabled.</exception>
    public bool ContainsPII(string text)
    {
        EnsurePIIMaskerConfigured();
        return _piiMasker!.ContainsPII(text);
    }

    /// <summary>
    /// Filters content in the given text.
    /// </summary>
    /// <param name="text">The text to filter.</param>
    /// <returns>The filtering result with filtered text and detection details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when content filtering is not enabled.</exception>
    public ContentFilterResult FilterContent(string text)
    {
        EnsureFilterManagerConfigured();
        return _filterManager!.Filter(text);
    }

    /// <summary>
    /// Checks if the text contains any filtered content.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if filtered content is found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when content filtering is not enabled.</exception>
    public bool ContainsFilteredContent(string text)
    {
        EnsureFilterManagerConfigured();
        return _filterManager!.ContainsFilteredContent(text);
    }

    /// <summary>
    /// Preprocesses text by filtering content, masking PII, and then chunking.
    /// Combines all preprocessing steps in a single pipeline operation.
    /// </summary>
    /// <param name="text">The text to preprocess.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A preprocessing result with processed chunks.</returns>
    public async Task<PreprocessingResult> PreprocessAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var processedText = text;
        ContentFilterResult? filterResult = null;
        PIIMaskingResult? maskingResult = null;

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
                    ContentFilterResult = filterResult
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
            ContentFilterResult = filterResult
        };
    }

    /// <summary>
    /// Creates a builder for batch processing multiple texts.
    /// </summary>
    /// <returns>A batch processor builder.</returns>
    public BatchProcessor CreateBatchProcessor()
    {
        return new BatchProcessor(this);
    }

    /// <summary>
    /// Ensures PII masker is configured.
    /// </summary>
    private void EnsurePIIMaskerConfigured()
    {
        if (_piiMasker is null)
        {
            throw new InvalidOperationException(
                "PII masking is not enabled. Call WithPIIMasking() first.");
        }
    }

    /// <summary>
    /// Ensures content filter manager is configured.
    /// </summary>
    private void EnsureFilterManagerConfigured()
    {
        if (_filterManager is null)
        {
            throw new InvalidOperationException(
                "Content filtering is not enabled. Call WithContentFiltering() first.");
        }
    }

    /// <summary>
    /// Resolves the actual chunking strategy to use.
    /// </summary>
    private ChunkingStrategy ResolveStrategy(string text, ChunkOptions options)
    {
        if (options.Strategy != ChunkingStrategy.Auto)
            return options.Strategy;

        // Auto-select strategy based on content analysis
        var profile = LanguageProfileRegistry.Instance.DetectProfile(text);
        var tokenCount = profile.EstimateTokenCount(text);

        // For short texts, use sentence chunking
        if (tokenCount <= options.TargetChunkSize * 2)
            return ChunkingStrategy.Sentence;

        // Check paragraph structure
        var paragraphs = profile.FindParagraphBoundaries(text);
        if (paragraphs.Count > 3)
        {
            // Document has clear paragraph structure
            return ChunkingStrategy.Paragraph;
        }

        // Check sentence structure
        var sentences = profile.FindSentenceBoundaries(text);
        if (sentences.Count > 5)
        {
            // Text has clear sentence structure
            return ChunkingStrategy.Sentence;
        }

        // Default to token chunking for unstructured text
        return ChunkingStrategy.Token;
    }

    /// <summary>
    /// Gets the chunker for the specified strategy.
    /// </summary>
    private IChunker GetChunker(ChunkingStrategy strategy)
    {
        if (_chunkers.TryGetValue(strategy, out var chunker))
            return chunker;

        // Fallback to token chunker
        return _chunkers[ChunkingStrategy.Token];
    }
}

/// <summary>
/// Batch processor for chunking multiple texts efficiently.
/// </summary>
public sealed class BatchProcessor
{
    private readonly FluxCurator _curator;
    private readonly List<string> _texts = [];
    private int _maxConcurrency = Environment.ProcessorCount;

    internal BatchProcessor(FluxCurator curator)
    {
        _curator = curator;
    }

    /// <summary>
    /// Adds a text to the batch.
    /// </summary>
    public BatchProcessor AddText(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            _texts.Add(text);
        return this;
    }

    /// <summary>
    /// Adds multiple texts to the batch.
    /// </summary>
    public BatchProcessor AddTexts(IEnumerable<string> texts)
    {
        foreach (var text in texts)
            AddText(text);
        return this;
    }

    /// <summary>
    /// Sets the maximum concurrency for parallel processing.
    /// </summary>
    public BatchProcessor WithMaxConcurrency(int maxConcurrency)
    {
        _maxConcurrency = Math.Max(1, Math.Min(maxConcurrency, 32));
        return this;
    }

    /// <summary>
    /// Processes all texts in the batch.
    /// </summary>
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
    /// Gets the total estimated chunk count for all texts.
    /// </summary>
    public int GetTotalEstimatedChunks()
    {
        return _texts.Sum(t => _curator.EstimateChunkCount(t));
    }
}
