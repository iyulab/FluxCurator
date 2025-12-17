namespace FluxCurator.Core.Domain;

/// <summary>
/// Configuration options for text chunking operations.
/// </summary>
/// <remarks>
/// <para>
/// All token-based sizes (TargetChunkSize, MinChunkSize, MaxChunkSize, OverlapSize) use
/// <b>estimated token counts</b> based on language-specific heuristics, not actual tokenizer output.
/// </para>
/// <para>
/// Token estimation ratios by language:
/// <list type="bullet">
///   <item><description>English: ~4 characters per token (512 tokens ≈ 2,048 chars)</description></item>
///   <item><description>Korean: ~1.5-2 characters per token (512 tokens ≈ 750-1,024 chars)</description></item>
///   <item><description>Chinese/Japanese: ~1.5-2 characters per token</description></item>
///   <item><description>Mixed content: weighted average based on character types</description></item>
/// </list>
/// </para>
/// <para>
/// For precise token control, verify chunk sizes with your actual tokenizer after chunking.
/// </para>
/// </remarks>
public sealed class ChunkOptions
{
    /// <summary>
    /// Gets or sets the chunking strategy to use.
    /// Default: Auto (automatically select based on content).
    /// </summary>
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.Auto;

    /// <summary>
    /// Gets or sets the target chunk size in estimated tokens.
    /// Token count is estimated using language-specific heuristics (see class remarks).
    /// Default: 512 tokens.
    /// </summary>
    public int TargetChunkSize { get; set; } = 512;

    /// <summary>
    /// Gets or sets the minimum chunk size in estimated tokens.
    /// Chunks smaller than this will be merged with adjacent chunks.
    /// Token count is estimated using language-specific heuristics (see class remarks).
    /// Default: 100 tokens.
    /// </summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum chunk size in estimated tokens.
    /// Chunks larger than this will be split.
    /// Token count is estimated using language-specific heuristics (see class remarks).
    /// Default: 1024 tokens.
    /// </summary>
    public int MaxChunkSize { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the overlap size in estimated tokens between consecutive chunks.
    /// Helps maintain context across chunk boundaries.
    /// Token count is estimated using language-specific heuristics (see class remarks).
    /// Default: 50 tokens.
    /// </summary>
    public int OverlapSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the ISO 639-1 language code for text processing.
    /// When null, language will be auto-detected.
    /// Default: null (auto-detect).
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Gets or sets whether to preserve paragraph boundaries where possible.
    /// Default: true.
    /// </summary>
    public bool PreserveParagraphs { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to preserve sentence boundaries where possible.
    /// Default: true.
    /// </summary>
    public bool PreserveSentences { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to detect and preserve section headers.
    /// Default: true.
    /// </summary>
    public bool PreserveSectionHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the similarity threshold for semantic chunking.
    /// Lower values create more chunks at semantic boundaries.
    /// Range: 0.0 to 1.0. Default: 0.5.
    /// </summary>
    public float SemanticSimilarityThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets whether to include chunk metadata (position, quality scores, etc.).
    /// Default: true.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to trim whitespace from chunk boundaries.
    /// Default: true.
    /// </summary>
    public bool TrimWhitespace { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to normalize whitespace within chunks.
    /// Multiple spaces/newlines become single spaces.
    /// Default: false.
    /// </summary>
    public bool NormalizeWhitespace { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable automatic post-processing to balance chunk sizes.
    /// When enabled, undersized chunks are merged and oversized chunks are split.
    /// Default: true.
    /// </summary>
    public bool EnableChunkBalancing { get; set; } = true;

    /// <summary>
    /// Creates default chunking options.
    /// </summary>
    public static ChunkOptions Default => new();

    /// <summary>
    /// Creates options optimized for RAG (Retrieval-Augmented Generation) workloads.
    /// </summary>
    public static ChunkOptions ForRAG => new()
    {
        Strategy = ChunkingStrategy.Semantic,
        TargetChunkSize = 512,
        MinChunkSize = 128,
        MaxChunkSize = 1024,
        OverlapSize = 64,
        PreserveSentences = true,
        PreserveParagraphs = true,
        IncludeMetadata = true,
        EnableChunkBalancing = true
    };

    /// <summary>
    /// Creates options optimized for Korean text processing.
    /// </summary>
    public static ChunkOptions ForKorean => new()
    {
        Strategy = ChunkingStrategy.Sentence,
        TargetChunkSize = 400,
        MinChunkSize = 80,
        MaxChunkSize = 800,
        OverlapSize = 40,
        LanguageCode = "ko",
        PreserveSentences = true,
        PreserveParagraphs = true,
        EnableChunkBalancing = true
    };

    /// <summary>
    /// Creates options optimized for large documents (50K+ tokens).
    /// Uses hierarchical chunking with larger chunk sizes and increased overlap.
    /// </summary>
    public static ChunkOptions ForLargeDocument => new()
    {
        Strategy = ChunkingStrategy.Hierarchical,
        TargetChunkSize = 768,
        MinChunkSize = 200,
        MaxChunkSize = 1536,
        OverlapSize = 128,
        PreserveSentences = true,
        PreserveParagraphs = true,
        PreserveSectionHeaders = true,
        IncludeMetadata = true,
        EnableChunkBalancing = true
    };

    /// <summary>
    /// Creates options for fixed-size token chunking.
    /// </summary>
    /// <param name="tokenSize">Target token size per chunk.</param>
    /// <param name="overlap">Overlap tokens between chunks.</param>
    public static ChunkOptions FixedSize(int tokenSize, int overlap = 50) => new()
    {
        Strategy = ChunkingStrategy.Token,
        TargetChunkSize = tokenSize,
        MinChunkSize = tokenSize / 4,
        MaxChunkSize = tokenSize * 2,
        OverlapSize = overlap,
        PreserveSentences = false,
        PreserveParagraphs = false
    };
}
