namespace FluxCurator.Core.Infrastructure.Chunking;

using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Chunks text by token count for consistent-size chunks.
/// Attempts to align to sentence boundaries when possible.
/// </summary>
public sealed class TokenChunker : ChunkerBase
{
    /// <inheritdoc/>
    public override string StrategyName => "Token";

    /// <inheritdoc/>
    protected override ChunkingStrategy GetChunkingStrategy() => ChunkingStrategy.Token;

    /// <inheritdoc/>
    public override Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult<IReadOnlyList<DocumentChunk>>([]);

        var profile = GetLanguageProfile(text, options);
        var totalTokens = profile.EstimateTokenCount(text);

        // If text fits in one chunk, return as-is
        if (totalTokens <= options.MaxChunkSize)
        {
            var singleChunk = CreateChunk(
                content: text,
                index: 0,
                totalChunks: 1,
                startPosition: 0,
                endPosition: text.Length,
                profile: profile,
                options: options);
            return Task.FromResult<IReadOnlyList<DocumentChunk>>([singleChunk]);
        }

        var chunks = new List<DocumentChunk>();
        var sentenceBoundaries = options.PreserveSentences
            ? profile.FindSentenceBoundaries(text)
            : [];

        int currentStart = 0;
        string? overlapContent = null;

        while (currentStart < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate target end position
            var targetEnd = FindTargetEndPosition(
                text,
                currentStart,
                options.TargetChunkSize,
                options.MaxChunkSize,
                profile,
                sentenceBoundaries,
                options.PreserveSentences);

            // Extract chunk content
            var chunkContent = text[currentStart..targetEnd];

            // Add overlap from previous chunk if needed
            if (!string.IsNullOrEmpty(overlapContent) && chunks.Count > 0)
            {
                chunkContent = overlapContent + chunkContent;
            }

            // Determine boundary alignment
            bool startsAtBoundary = currentStart == 0 ||
                                    (sentenceBoundaries.Count > 0 && sentenceBoundaries.Contains(currentStart));
            bool endsAtBoundary = targetEnd == text.Length ||
                                  (sentenceBoundaries.Count > 0 && sentenceBoundaries.Contains(targetEnd));

            var chunk = CreateChunk(
                content: chunkContent,
                index: chunks.Count,
                totalChunks: 0,
                startPosition: currentStart,
                endPosition: targetEnd,
                profile: profile,
                options: options,
                startsAtBoundary: startsAtBoundary,
                endsAtBoundary: endsAtBoundary,
                overlapContent: chunks.Count > 0 ? overlapContent : null);

            chunks.Add(chunk);

            // Prepare overlap for next chunk
            overlapContent = options.OverlapSize > 0
                ? ExtractOverlapContent(text[currentStart..targetEnd], options.OverlapSize, profile)
                : null;

            currentStart = targetEnd;
        }

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

        // Estimate based on target size minus overlap
        var effectiveSize = options.TargetChunkSize - options.OverlapSize;
        if (effectiveSize <= 0)
            effectiveSize = options.TargetChunkSize / 2;

        return (int)Math.Ceiling((double)totalTokens / effectiveSize);
    }

    /// <summary>
    /// Finds the optimal end position for a chunk.
    /// </summary>
    private int FindTargetEndPosition(
        string text,
        int startPosition,
        int targetTokens,
        int maxTokens,
        ILanguageProfile profile,
        IReadOnlyList<int> sentenceBoundaries,
        bool preserveSentences)
    {
        // Calculate approximate character position for target tokens
        var charsPerToken = (float)text.Length / profile.EstimateTokenCount(text);
        var targetCharPos = startPosition + (int)(targetTokens * charsPerToken);
        var maxCharPos = startPosition + (int)(maxTokens * charsPerToken);

        // Ensure we don't exceed text length
        targetCharPos = Math.Min(targetCharPos, text.Length);
        maxCharPos = Math.Min(maxCharPos, text.Length);

        // If we've reached the end, return text length
        if (targetCharPos >= text.Length - 10)
            return text.Length;

        // Try to align to sentence boundary if enabled
        if (preserveSentences && sentenceBoundaries.Count > 0)
        {
            // Find the closest sentence boundary near target position
            int bestBoundary = targetCharPos;
            int minDistance = int.MaxValue;

            foreach (var boundary in sentenceBoundaries)
            {
                if (boundary <= startPosition)
                    continue;

                if (boundary > maxCharPos)
                    break;

                var distance = Math.Abs(boundary - targetCharPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestBoundary = boundary;
                }
            }

            // Use boundary if found within reasonable range
            if (bestBoundary > startPosition && bestBoundary <= maxCharPos)
                return bestBoundary;
        }

        // Fall back to word boundary
        return FindWordBoundary(text, targetCharPos, maxCharPos);
    }

    /// <summary>
    /// Finds the nearest word boundary.
    /// </summary>
    private static int FindWordBoundary(string text, int targetPos, int maxPos)
    {
        // Look forward for a space
        for (int i = targetPos; i < maxPos && i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
                return i;
        }

        // Look backward for a space
        for (int i = targetPos; i > 0; i--)
        {
            if (char.IsWhiteSpace(text[i]))
                return i + 1;
        }

        // No good boundary found, use target position
        return Math.Min(targetPos, text.Length);
    }
}
