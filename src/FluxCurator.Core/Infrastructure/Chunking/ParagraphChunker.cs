namespace FluxCurator.Core.Infrastructure.Chunking;

using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Chunks text by paragraph boundaries while respecting token size constraints.
/// Optimal for structured documents where paragraphs represent logical units.
/// </summary>
public sealed class ParagraphChunker : ChunkerBase
{
    /// <inheritdoc/>
    public override string StrategyName => "Paragraph";

    /// <inheritdoc/>
    protected override ChunkingStrategy GetChunkingStrategy() => ChunkingStrategy.Paragraph;

    /// <inheritdoc/>
    public override Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult<IReadOnlyList<DocumentChunk>>([]);

        var profile = GetLanguageProfile(text, options);
        var paragraphBoundaries = profile.FindParagraphBoundaries(text);
        var chunks = new List<DocumentChunk>();

        int currentStart = 0;
        var currentChunkContent = new System.Text.StringBuilder();
        int currentTokenCount = 0;
        string? overlapContent = null;

        foreach (var boundary in paragraphBoundaries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var paragraph = text[currentStart..boundary];
            var paragraphTokens = profile.EstimateTokenCount(paragraph);

            // Handle very long paragraphs by splitting at sentence boundaries
            if (paragraphTokens > options.MaxChunkSize)
            {
                // If current chunk has content, finalize it first
                if (currentChunkContent.Length > 0)
                {
                    FinalizeChunk(chunks, currentChunkContent, currentStart, profile, options, ref overlapContent);
                    currentChunkContent.Clear();
                    currentTokenCount = 0;
                }

                // Split long paragraph by sentences
                var sentenceChunks = SplitParagraphBySentences(
                    paragraph,
                    currentStart,
                    profile,
                    options,
                    ref overlapContent);
                chunks.AddRange(sentenceChunks);
            }
            // Check if adding this paragraph would exceed max size
            else if (currentTokenCount + paragraphTokens > options.MaxChunkSize && currentChunkContent.Length > 0)
            {
                // Finalize current chunk
                FinalizeChunk(chunks, currentChunkContent, currentStart, profile, options, ref overlapContent);
                currentChunkContent.Clear();
                currentTokenCount = 0;

                // Add overlap to new chunk if needed
                if (!string.IsNullOrEmpty(overlapContent))
                {
                    currentChunkContent.Append(overlapContent);
                    currentTokenCount = profile.EstimateTokenCount(overlapContent);
                }

                // Add paragraph to new chunk
                currentChunkContent.Append(paragraph);
                currentTokenCount += paragraphTokens;
            }
            else
            {
                // Add paragraph to current chunk
                currentChunkContent.Append(paragraph);
                currentTokenCount += paragraphTokens;
            }

            currentStart = boundary;
        }

        // Handle remaining content
        if (currentChunkContent.Length > 0)
        {
            FinalizeChunk(chunks, currentChunkContent, text.Length, profile, options, ref overlapContent);
        }

        // Update total chunk count and indices
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Index = i;
            chunks[i].TotalChunks = chunks.Count;
        }

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
    }

    /// <inheritdoc/>
    public override int EstimateChunkCount(string text, ChunkOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var profile = GetLanguageProfile(text, options);
        var paragraphs = profile.FindParagraphBoundaries(text);

        // Count paragraphs and estimate based on average size
        int paragraphCount = paragraphs.Count;
        if (paragraphCount == 0)
            return 1;

        var totalTokens = profile.EstimateTokenCount(text);
        var avgParagraphTokens = totalTokens / paragraphCount;

        // Estimate how many paragraphs fit in a chunk
        var paragraphsPerChunk = Math.Max(1, options.TargetChunkSize / Math.Max(1, avgParagraphTokens));

        return (int)Math.Ceiling((double)paragraphCount / paragraphsPerChunk);
    }

    /// <summary>
    /// Finalizes a chunk and adds it to the list.
    /// </summary>
    private void FinalizeChunk(
        List<DocumentChunk> chunks,
        System.Text.StringBuilder content,
        int endPosition,
        ILanguageProfile profile,
        ChunkOptions options,
        ref string? overlapContent)
    {
        var chunkText = content.ToString();
        if (options.TrimWhitespace)
            chunkText = chunkText.Trim();

        if (string.IsNullOrWhiteSpace(chunkText))
            return;

        var chunk = CreateChunk(
            content: chunkText,
            index: chunks.Count,
            totalChunks: 0,
            startPosition: endPosition - content.Length,
            endPosition: endPosition,
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

    /// <summary>
    /// Splits a long paragraph into chunks by sentence boundaries.
    /// </summary>
    private List<DocumentChunk> SplitParagraphBySentences(
        string paragraph,
        int paragraphStart,
        ILanguageProfile profile,
        ChunkOptions options,
        ref string? overlapContent)
    {
        var chunks = new List<DocumentChunk>();
        var sentenceBoundaries = profile.FindSentenceBoundaries(paragraph);

        int currentStart = 0;
        var currentContent = new System.Text.StringBuilder();
        int currentTokens = 0;

        // Add overlap if available
        if (!string.IsNullOrEmpty(overlapContent))
        {
            currentContent.Append(overlapContent);
            currentTokens = profile.EstimateTokenCount(overlapContent);
        }

        foreach (var boundary in sentenceBoundaries)
        {
            var sentence = paragraph[currentStart..boundary];
            var sentenceTokens = profile.EstimateTokenCount(sentence);

            if (currentTokens + sentenceTokens > options.MaxChunkSize && currentContent.Length > 0)
            {
                // Finalize current chunk
                var chunkText = currentContent.ToString();
                if (options.TrimWhitespace)
                    chunkText = chunkText.Trim();

                var chunk = CreateChunk(
                    content: chunkText,
                    index: 0, // Will be updated later
                    totalChunks: 0,
                    startPosition: paragraphStart + currentStart - currentContent.Length,
                    endPosition: paragraphStart + currentStart,
                    profile: profile,
                    options: options,
                    startsAtBoundary: true,
                    endsAtBoundary: true,
                    overlapContent: overlapContent
                );
                chunks.Add(chunk);

                // Prepare overlap
                overlapContent = options.OverlapSize > 0
                    ? ExtractOverlapContent(chunkText, options.OverlapSize, profile)
                    : null;

                // Reset
                currentContent.Clear();
                currentTokens = 0;

                if (!string.IsNullOrEmpty(overlapContent))
                {
                    currentContent.Append(overlapContent);
                    currentTokens = profile.EstimateTokenCount(overlapContent);
                }
            }

            currentContent.Append(sentence);
            currentTokens += sentenceTokens;
            currentStart = boundary;
        }

        // Handle remaining content
        if (currentContent.Length > 0)
        {
            var chunkText = currentContent.ToString();
            if (options.TrimWhitespace)
                chunkText = chunkText.Trim();

            var chunk = CreateChunk(
                content: chunkText,
                index: 0,
                totalChunks: 0,
                startPosition: paragraphStart + paragraph.Length - currentContent.Length,
                endPosition: paragraphStart + paragraph.Length,
                profile: profile,
                options: options,
                startsAtBoundary: true,
                endsAtBoundary: true,
                overlapContent: overlapContent
            );
            chunks.Add(chunk);

            overlapContent = options.OverlapSize > 0
                ? ExtractOverlapContent(chunkText, options.OverlapSize, profile)
                : null;
        }

        return chunks;
    }
}
