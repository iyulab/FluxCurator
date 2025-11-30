namespace FluxCurator.Infrastructure.Chunking;

using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;
using global::FluxCurator.Core.Infrastructure.Languages;

/// <summary>
/// Chunks text using semantic similarity to find natural topic boundaries.
/// Requires an IEmbedder implementation.
/// </summary>
public sealed class SemanticChunker : ChunkerBase
{
    private readonly IEmbedder _embedder;

    /// <summary>
    /// Creates a new semantic chunker with the specified embedder.
    /// </summary>
    /// <param name="embedder">The embedder to use for semantic analysis.</param>
    public SemanticChunker(IEmbedder embedder)
    {
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
    }

    /// <inheritdoc/>
    public override string StrategyName => "Semantic";

    /// <inheritdoc/>
    public override bool RequiresEmbedder => true;

    /// <inheritdoc/>
    protected override ChunkingStrategy GetChunkingStrategy() => ChunkingStrategy.Semantic;

    /// <inheritdoc/>
    public override async Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var profile = GetLanguageProfile(text, options);

        // Step 1: Split text into sentences
        var sentenceBoundaries = profile.FindSentenceBoundaries(text);
        var sentences = ExtractSentences(text, sentenceBoundaries);

        if (sentences.Count <= 1)
        {
            // Single sentence or no clear boundaries, return as single chunk
            var singleChunk = CreateChunk(
                content: text,
                index: 0,
                totalChunks: 1,
                startPosition: 0,
                endPosition: text.Length,
                profile: profile,
                options: options);
            return [singleChunk];
        }

        // Step 2: Generate embeddings for each sentence
        var embeddings = await GenerateSentenceEmbeddingsAsync(sentences, cancellationToken);

        // Step 3: Calculate similarity between consecutive sentences
        var similarities = CalculateConsecutiveSimilarities(embeddings);

        // Step 4: Find semantic breakpoints (low similarity = topic change)
        var breakpoints = FindSemanticBreakpoints(
            similarities,
            options.SemanticSimilarityThreshold,
            sentences,
            profile,
            options.MinChunkSize,
            options.MaxChunkSize);

        // Step 5: Create chunks based on breakpoints
        var chunks = CreateChunksFromBreakpoints(
            text,
            sentences,
            breakpoints,
            profile,
            options);

        return chunks;
    }

    /// <inheritdoc/>
    public override int EstimateChunkCount(string text, ChunkOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var profile = GetLanguageProfile(text, options);
        var totalTokens = profile.EstimateTokenCount(text);

        if (totalTokens <= options.MaxChunkSize)
            return 1;

        // Estimate based on semantic boundaries (typically more chunks than token-based)
        var sentenceCount = profile.FindSentenceBoundaries(text).Count;
        var avgSentenceTokens = totalTokens / Math.Max(1, sentenceCount);
        var sentencesPerChunk = options.TargetChunkSize / Math.Max(1, avgSentenceTokens);

        return (int)Math.Ceiling((double)sentenceCount / Math.Max(1, sentencesPerChunk));
    }

    /// <summary>
    /// Extracts sentences from text based on boundaries.
    /// </summary>
    private static List<SentenceInfo> ExtractSentences(string text, IReadOnlyList<int> boundaries)
    {
        var sentences = new List<SentenceInfo>();
        int start = 0;

        foreach (var end in boundaries)
        {
            if (end > start)
            {
                var content = text[start..end].Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    sentences.Add(new SentenceInfo
                    {
                        Content = content,
                        StartIndex = start,
                        EndIndex = end
                    });
                }
            }
            start = end;
        }

        return sentences;
    }

    /// <summary>
    /// Generates embeddings for all sentences.
    /// </summary>
    private async Task<List<float[]>> GenerateSentenceEmbeddingsAsync(
        List<SentenceInfo> sentences,
        CancellationToken cancellationToken)
    {
        var texts = sentences.Select(s => s.Content).ToList();
        var embeddings = await _embedder.GenerateEmbeddingsAsync(texts, cancellationToken);
        return embeddings.ToList();
    }

    /// <summary>
    /// Calculates cosine similarity between consecutive sentence embeddings.
    /// </summary>
    private List<float> CalculateConsecutiveSimilarities(List<float[]> embeddings)
    {
        var similarities = new List<float>();

        for (int i = 0; i < embeddings.Count - 1; i++)
        {
            var similarity = _embedder.CalculateSimilarity(embeddings[i], embeddings[i + 1]);
            similarities.Add(similarity);
        }

        return similarities;
    }

    /// <summary>
    /// Finds semantic breakpoints where similarity drops below threshold.
    /// </summary>
    private static List<int> FindSemanticBreakpoints(
        List<float> similarities,
        float threshold,
        List<SentenceInfo> sentences,
        ILanguageProfile profile,
        int minChunkSize,
        int maxChunkSize)
    {
        var breakpoints = new List<int>();
        int currentChunkTokens = 0;

        for (int i = 0; i < similarities.Count; i++)
        {
            var sentenceTokens = profile.EstimateTokenCount(sentences[i].Content);
            currentChunkTokens += sentenceTokens;

            // Force break if we exceed max size
            if (currentChunkTokens >= maxChunkSize)
            {
                breakpoints.Add(i + 1);
                currentChunkTokens = 0;
                continue;
            }

            // Break on low similarity if we have enough content
            if (similarities[i] < threshold && currentChunkTokens >= minChunkSize)
            {
                breakpoints.Add(i + 1);
                currentChunkTokens = 0;
            }
        }

        return breakpoints;
    }

    /// <summary>
    /// Creates chunks based on identified breakpoints.
    /// </summary>
    private List<DocumentChunk> CreateChunksFromBreakpoints(
        string originalText,
        List<SentenceInfo> sentences,
        List<int> breakpoints,
        ILanguageProfile profile,
        ChunkOptions options)
    {
        var chunks = new List<DocumentChunk>();
        int sentenceStart = 0;
        string? overlapContent = null;

        // Add final breakpoint at end
        if (breakpoints.Count == 0 || breakpoints[^1] != sentences.Count)
        {
            breakpoints.Add(sentences.Count);
        }

        foreach (var breakpoint in breakpoints)
        {
            if (breakpoint <= sentenceStart)
                continue;

            // Collect sentences for this chunk
            var chunkSentences = sentences
                .Skip(sentenceStart)
                .Take(breakpoint - sentenceStart)
                .ToList();

            if (chunkSentences.Count == 0)
                continue;

            var startPos = chunkSentences[0].StartIndex;
            var endPos = chunkSentences[^1].EndIndex;
            var content = originalText[startPos..endPos];

            // Add overlap from previous chunk if configured
            if (!string.IsNullOrEmpty(overlapContent) && chunks.Count > 0)
            {
                content = overlapContent + " " + content;
            }

            var chunk = CreateChunk(
                content: content,
                index: chunks.Count,
                totalChunks: 0, // Will be updated later
                startPosition: startPos,
                endPosition: endPos,
                profile: profile,
                options: options,
                startsAtBoundary: true,
                endsAtBoundary: true,
                overlapContent: chunks.Count > 0 ? overlapContent : null);

            chunks.Add(chunk);

            // Prepare overlap for next chunk
            overlapContent = options.OverlapSize > 0
                ? ExtractOverlapContent(content, options.OverlapSize, profile)
                : null;

            sentenceStart = breakpoint;
        }

        // Update total chunk count
        foreach (var chunk in chunks)
        {
            chunk.TotalChunks = chunks.Count;
        }

        return chunks;
    }

    /// <summary>
    /// Represents a sentence with its position in the original text.
    /// </summary>
    private sealed class SentenceInfo
    {
        public required string Content { get; init; }
        public required int StartIndex { get; init; }
        public required int EndIndex { get; init; }
    }
}
