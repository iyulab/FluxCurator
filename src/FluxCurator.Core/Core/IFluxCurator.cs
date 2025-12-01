using FluxCurator.Core.Domain;

namespace FluxCurator.Core;

/// <summary>
/// Interface for FluxCurator text preprocessing operations.
/// Provides text chunking, PII masking, content filtering, and text refinement.
/// </summary>
public interface IFluxCurator
{
    /// <summary>
    /// Gets whether an embedder is configured for semantic operations.
    /// </summary>
    bool HasEmbedder { get; }

    /// <summary>
    /// Gets whether PII masking is enabled.
    /// </summary>
    bool HasPIIMasking { get; }

    /// <summary>
    /// Gets whether content filtering is enabled.
    /// </summary>
    bool HasContentFiltering { get; }

    /// <summary>
    /// Gets whether text refinement is enabled.
    /// </summary>
    bool HasTextRefinement { get; }

    /// <summary>
    /// Gets the current chunking options.
    /// </summary>
    ChunkOptions ChunkingOptions { get; }

    /// <summary>
    /// Gets the current PII masking options.
    /// </summary>
    PIIMaskingOptions PIIMaskingOptions { get; }

    /// <summary>
    /// Gets the current content filtering options.
    /// </summary>
    ContentFilterOptions ContentFilterOptions { get; }

    /// <summary>
    /// Gets the current text refinement options.
    /// </summary>
    TextRefineOptions? TextRefineOptions { get; }

    /// <summary>
    /// Chunks the given text according to the configured options.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of document chunks.</returns>
    Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chunks the given text with custom options.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="options">Custom chunking options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of document chunks.</returns>
    Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the number of chunks for the given text.
    /// </summary>
    /// <param name="text">The text to estimate.</param>
    /// <returns>Estimated chunk count.</returns>
    int EstimateChunkCount(string text);

    /// <summary>
    /// Estimates the number of chunks for the given text with custom options.
    /// </summary>
    /// <param name="text">The text to estimate.</param>
    /// <param name="options">Custom chunking options.</param>
    /// <returns>Estimated chunk count.</returns>
    int EstimateChunkCount(string text, ChunkOptions options);

    /// <summary>
    /// Detects the language of the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>ISO 639-1 language code.</returns>
    string DetectLanguage(string text);

    /// <summary>
    /// Refines text by removing noise and normalizing content.
    /// </summary>
    /// <param name="text">The text to refine.</param>
    /// <returns>The refined text.</returns>
    string RefineText(string text);

    /// <summary>
    /// Refines text with the specified options.
    /// </summary>
    /// <param name="text">The text to refine.</param>
    /// <param name="options">The refinement options.</param>
    /// <returns>The refined text.</returns>
    string RefineText(string text, TextRefineOptions options);

    /// <summary>
    /// Masks PII in the given text.
    /// </summary>
    /// <param name="text">The text to mask.</param>
    /// <returns>The masking result with masked text and detection details.</returns>
    PIIMaskingResult MaskPII(string text);

    /// <summary>
    /// Detects PII in the given text without masking.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>A list of detected PII matches.</returns>
    IReadOnlyList<PIIMatch> DetectPII(string text);

    /// <summary>
    /// Checks if the text contains any PII.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if PII is found.</returns>
    bool ContainsPII(string text);

    /// <summary>
    /// Filters content in the given text.
    /// </summary>
    /// <param name="text">The text to filter.</param>
    /// <returns>The filtering result with filtered text and detection details.</returns>
    ContentFilterResult FilterContent(string text);

    /// <summary>
    /// Checks if the text contains any filtered content.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if filtered content is found.</returns>
    bool ContainsFilteredContent(string text);

    /// <summary>
    /// Preprocesses text by filtering content, masking PII, and then chunking.
    /// Pipeline: Refine → Filter → Mask → Chunk
    /// </summary>
    /// <param name="text">The text to preprocess.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A preprocessing result with processed chunks.</returns>
    Task<PreprocessingResult> PreprocessAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams chunks as they are generated, enabling memory-efficient processing of large texts.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of document chunks.</returns>
    IAsyncEnumerable<DocumentChunk> ChunkStreamAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams chunks as they are generated with custom options.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="options">Custom chunking options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of document chunks.</returns>
    IAsyncEnumerable<DocumentChunk> ChunkStreamAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default);
}
