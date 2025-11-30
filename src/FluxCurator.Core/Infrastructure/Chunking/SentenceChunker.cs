namespace FluxCurator.Core.Infrastructure.Chunking;

using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Chunks text by sentence boundaries while respecting token size constraints.
/// Optimal for conversational or narrative content where sentence integrity matters.
/// </summary>
public sealed class SentenceChunker : ChunkerBase
{
    /// <inheritdoc/>
    public override string StrategyName => "Sentence";

    /// <inheritdoc/>
    protected override ChunkingStrategy GetChunkingStrategy() => ChunkingStrategy.Sentence;

    /// <inheritdoc/>
    public override Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult<IReadOnlyList<DocumentChunk>>([]);

        var profile = GetLanguageProfile(text, options);
        var sentenceBoundaries = profile.FindSentenceBoundaries(text);
        var chunks = new List<DocumentChunk>();

        int currentStart = 0;
        var currentChunkContent = new System.Text.StringBuilder();
        int currentTokenCount = 0;
        string? overlapContent = null;

        foreach (var boundary in sentenceBoundaries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sentence = text[currentStart..boundary];
            var sentenceTokens = profile.EstimateTokenCount(sentence);

            // Check if adding this sentence would exceed max size
            if (currentTokenCount + sentenceTokens > options.MaxChunkSize && currentChunkContent.Length > 0)
            {
                // Finalize current chunk
                var chunkText = currentChunkContent.ToString();
                if (options.TrimWhitespace)
                    chunkText = chunkText.Trim();

                if (!string.IsNullOrWhiteSpace(chunkText))
                {
                    var chunk = CreateChunk(
                        content: chunkText,
                        index: chunks.Count,
                        totalChunks: 0, // Will update later
                        startPosition: currentStart - currentChunkContent.Length,
                        endPosition: currentStart,
                        profile: profile,
                        options: options,
                        startsAtBoundary: true,
                        endsAtBoundary: true,
                        overlapContent: overlapContent
                    );
                    chunks.Add(chunk);

                    // Prepare overlap for next chunk
                    overlapContent = options.OverlapSize > 0
                        ? ExtractOverlapContent(chunkText, options.OverlapSize, profile)
                        : null;
                }

                // Start new chunk
                currentChunkContent.Clear();
                currentTokenCount = 0;

                // Add overlap to new chunk if needed
                if (!string.IsNullOrEmpty(overlapContent))
                {
                    currentChunkContent.Append(overlapContent);
                    currentTokenCount = profile.EstimateTokenCount(overlapContent);
                }
            }

            // Add sentence to current chunk
            currentChunkContent.Append(sentence);
            currentTokenCount += sentenceTokens;
            currentStart = boundary;
        }

        // Handle remaining content
        if (currentChunkContent.Length > 0)
        {
            var chunkText = currentChunkContent.ToString();
            if (options.TrimWhitespace)
                chunkText = chunkText.Trim();

            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                var chunk = CreateChunk(
                    content: chunkText,
                    index: chunks.Count,
                    totalChunks: 0,
                    startPosition: text.Length - currentChunkContent.Length,
                    endPosition: text.Length,
                    profile: profile,
                    options: options,
                    startsAtBoundary: true,
                    endsAtBoundary: true,
                    overlapContent: overlapContent
                );
                chunks.Add(chunk);
            }
        }

        // Handle case where chunk is below minimum size
        chunks = MergeSmallChunks(chunks, options, profile);

        // Update total chunk count
        foreach (var chunk in chunks)
        {
            chunk.TotalChunks = chunks.Count;
        }

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
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

        // Estimate based on target size with overlap
        var effectiveSize = options.TargetChunkSize - options.OverlapSize;
        if (effectiveSize <= 0)
            effectiveSize = options.TargetChunkSize / 2;

        return (int)Math.Ceiling((double)totalTokens / effectiveSize);
    }

    /// <summary>
    /// Merges chunks that are below the minimum size threshold.
    /// </summary>
    private List<DocumentChunk> MergeSmallChunks(
        List<DocumentChunk> chunks,
        ChunkOptions options,
        ILanguageProfile profile)
    {
        if (chunks.Count <= 1)
            return chunks;

        var result = new List<DocumentChunk>();
        DocumentChunk? pendingChunk = null;

        foreach (var chunk in chunks)
        {
            if (pendingChunk == null)
            {
                if (chunk.Metadata.EstimatedTokenCount < options.MinChunkSize)
                {
                    pendingChunk = chunk;
                }
                else
                {
                    result.Add(chunk);
                }
            }
            else
            {
                // Merge pending chunk with current
                var mergedContent = pendingChunk.Content + " " + chunk.Content;
                var mergedTokens = profile.EstimateTokenCount(mergedContent);

                if (mergedTokens <= options.MaxChunkSize)
                {
                    // Merge is possible
                    var merged = CreateChunk(
                        content: mergedContent,
                        index: result.Count,
                        totalChunks: 0,
                        startPosition: pendingChunk.Location.StartPosition,
                        endPosition: chunk.Location.EndPosition,
                        profile: profile,
                        options: options,
                        startsAtBoundary: pendingChunk.Metadata.StartsAtSentenceBoundary,
                        endsAtBoundary: chunk.Metadata.EndsAtSentenceBoundary
                    );

                    if (merged.Metadata.EstimatedTokenCount < options.MinChunkSize)
                    {
                        // Still too small, keep as pending
                        pendingChunk = merged;
                    }
                    else
                    {
                        result.Add(merged);
                        pendingChunk = null;
                    }
                }
                else
                {
                    // Cannot merge, add pending and start fresh
                    result.Add(pendingChunk);
                    if (chunk.Metadata.EstimatedTokenCount < options.MinChunkSize)
                    {
                        pendingChunk = chunk;
                    }
                    else
                    {
                        result.Add(chunk);
                        pendingChunk = null;
                    }
                }
            }
        }

        // Don't forget the last pending chunk
        if (pendingChunk != null)
        {
            result.Add(pendingChunk);
        }

        return result;
    }
}
