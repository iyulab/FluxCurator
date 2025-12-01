namespace FluxCurator.Core.Infrastructure.Chunking;

using System.Text;
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.Languages;

/// <summary>
/// Default implementation of chunk balancing that merges undersized chunks
/// and splits oversized chunks while preserving natural text boundaries.
/// </summary>
public sealed class ChunkBalancer : IChunkBalancer
{
    /// <summary>
    /// Gets the singleton instance of ChunkBalancer.
    /// </summary>
    public static ChunkBalancer Instance { get; } = new();

    private ChunkBalancer() { }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DocumentChunk>> BalanceAsync(
        IReadOnlyList<DocumentChunk> chunks,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return Task.FromResult<IReadOnlyList<DocumentChunk>>([]);

        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Merge undersized chunks
        var merged = MergeUndersizedChunks(chunks, options);

        cancellationToken.ThrowIfCancellationRequested();

        // Step 2: Split oversized chunks
        var balanced = SplitOversizedChunks(merged, options);

        // Step 3: Reindex and update metadata
        var result = ReindexChunks(balanced);

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(result);
    }

    /// <inheritdoc/>
    public ChunkBalanceStats CalculateStats(IReadOnlyList<DocumentChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return new ChunkBalanceStats { ChunkCount = 0 };
        }

        var tokenCounts = chunks.Select(c => c.Metadata.EstimatedTokenCount).ToList();
        var minTokens = tokenCounts.Min();
        var maxTokens = tokenCounts.Max();
        var avgTokens = tokenCounts.Average();

        // Calculate standard deviation
        var variance = tokenCounts.Sum(t => Math.Pow(t - avgTokens, 2)) / tokenCounts.Count;
        var stdDev = Math.Sqrt(variance);

        return new ChunkBalanceStats
        {
            ChunkCount = chunks.Count,
            MinTokenCount = minTokens,
            MaxTokenCount = maxTokens,
            AverageTokenCount = avgTokens,
            StandardDeviation = stdDev
        };
    }

    /// <summary>
    /// Calculates statistics with undersized/oversized counts based on options.
    /// </summary>
    public ChunkBalanceStats CalculateStats(IReadOnlyList<DocumentChunk> chunks, ChunkOptions options)
    {
        var stats = CalculateStats(chunks);

        if (chunks.Count > 0)
        {
            stats.UndersizedChunkCount = chunks.Count(c =>
                c.Metadata.EstimatedTokenCount < options.MinChunkSize);
            stats.OversizedChunkCount = chunks.Count(c =>
                c.Metadata.EstimatedTokenCount > options.MaxChunkSize);
        }

        return stats;
    }

    private List<DocumentChunk> MergeUndersizedChunks(
        IReadOnlyList<DocumentChunk> chunks,
        ChunkOptions options)
    {
        if (chunks.Count <= 1)
            return chunks.ToList();

        var result = new List<DocumentChunk>();
        var profile = GetLanguageProfile(chunks[0]);
        var buffer = new StringBuilder();
        DocumentChunk? bufferSource = null;
        int bufferStartPos = 0;

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var tokenCount = chunk.Metadata.EstimatedTokenCount;

            // If current chunk is undersized, add to buffer
            if (tokenCount < options.MinChunkSize)
            {
                if (buffer.Length > 0)
                {
                    buffer.AppendLine();
                    buffer.AppendLine();
                }
                else
                {
                    bufferSource = chunk;
                    bufferStartPos = chunk.Location.StartPosition;
                }
                buffer.Append(chunk.Content);
            }
            else
            {
                // Flush buffer if exists
                if (buffer.Length > 0)
                {
                    // Determine merge direction: merge with current chunk if combined size is acceptable
                    var bufferContent = buffer.ToString();
                    var bufferTokens = profile.EstimateTokenCount(bufferContent);
                    var combinedTokens = bufferTokens + tokenCount;

                    if (combinedTokens <= options.MaxChunkSize)
                    {
                        // Merge buffer with current chunk
                        var mergedContent = bufferContent + "\n\n" + chunk.Content;
                        var mergedChunk = CreateMergedChunk(
                            mergedContent,
                            bufferStartPos,
                            chunk.Location.EndPosition,
                            profile,
                            bufferSource!,
                            chunk);
                        result.Add(mergedChunk);
                    }
                    else
                    {
                        // Buffer is significant enough on its own, or would exceed max
                        if (bufferTokens >= options.MinChunkSize)
                        {
                            result.Add(CreateChunkFromBuffer(
                                bufferContent, bufferStartPos, bufferSource!, profile));
                        }
                        else if (result.Count > 0)
                        {
                            // Merge with previous chunk
                            var prev = result[^1];
                            var mergedContent = prev.Content + "\n\n" + bufferContent;
                            var mergedChunk = CreateMergedChunk(
                                mergedContent,
                                prev.Location.StartPosition,
                                bufferSource!.Location.EndPosition,
                                profile,
                                prev,
                                bufferSource!);
                            result[^1] = mergedChunk;
                        }
                        else
                        {
                            // No choice but to add undersized buffer
                            result.Add(CreateChunkFromBuffer(
                                bufferContent, bufferStartPos, bufferSource!, profile));
                        }
                        result.Add(chunk);
                    }
                    buffer.Clear();
                    bufferSource = null;
                }
                else
                {
                    result.Add(chunk);
                }
            }
        }

        // Handle remaining buffer
        if (buffer.Length > 0)
        {
            var bufferContent = buffer.ToString();
            var bufferTokens = profile.EstimateTokenCount(bufferContent);

            if (result.Count > 0)
            {
                var prev = result[^1];
                var prevTokens = prev.Metadata.EstimatedTokenCount;

                if (prevTokens + bufferTokens <= options.MaxChunkSize)
                {
                    // Merge with previous
                    var mergedContent = prev.Content + "\n\n" + bufferContent;
                    var mergedChunk = CreateMergedChunk(
                        mergedContent,
                        prev.Location.StartPosition,
                        bufferSource!.Location.EndPosition,
                        profile,
                        prev,
                        bufferSource!);
                    result[^1] = mergedChunk;
                }
                else
                {
                    // Add as separate chunk (may still be undersized, final chunk exception)
                    result.Add(CreateChunkFromBuffer(
                        bufferContent, bufferStartPos, bufferSource!, profile));
                }
            }
            else
            {
                result.Add(CreateChunkFromBuffer(
                    bufferContent, bufferStartPos, bufferSource!, profile));
            }
        }

        return result;
    }

    private List<DocumentChunk> SplitOversizedChunks(
        List<DocumentChunk> chunks,
        ChunkOptions options)
    {
        var result = new List<DocumentChunk>();

        foreach (var chunk in chunks)
        {
            if (chunk.Metadata.EstimatedTokenCount <= options.MaxChunkSize)
            {
                result.Add(chunk);
                continue;
            }

            // Need to split this chunk
            var splitChunks = SplitChunk(chunk, options);
            result.AddRange(splitChunks);
        }

        return result;
    }

    private List<DocumentChunk> SplitChunk(DocumentChunk chunk, ChunkOptions options)
    {
        var profile = GetLanguageProfile(chunk);
        var content = chunk.Content;
        var result = new List<DocumentChunk>();

        // Try to split at natural boundaries
        var splitPoints = FindSplitPoints(content, options, profile);

        if (splitPoints.Count == 0)
        {
            // No good split points, use token-based splitting
            return SplitByTokens(chunk, options, profile);
        }

        int startPos = 0;
        foreach (var splitPoint in splitPoints)
        {
            if (splitPoint <= startPos)
                continue;

            var segmentContent = content[startPos..splitPoint].Trim();
            if (!string.IsNullOrEmpty(segmentContent))
            {
                var segmentTokens = profile.EstimateTokenCount(segmentContent);

                // If segment is still too large, recursively split
                if (segmentTokens > options.MaxChunkSize)
                {
                    var tempChunk = CreateSplitChunk(
                        segmentContent, chunk, startPos, splitPoint, profile);
                    result.AddRange(SplitByTokens(tempChunk, options, profile));
                }
                else
                {
                    result.Add(CreateSplitChunk(
                        segmentContent, chunk, startPos, splitPoint, profile));
                }
            }

            startPos = splitPoint;
        }

        // Handle remaining content
        if (startPos < content.Length)
        {
            var remaining = content[startPos..].Trim();
            if (!string.IsNullOrEmpty(remaining))
            {
                var remainingTokens = profile.EstimateTokenCount(remaining);
                if (remainingTokens > options.MaxChunkSize)
                {
                    var tempChunk = CreateSplitChunk(
                        remaining, chunk, startPos, content.Length, profile);
                    result.AddRange(SplitByTokens(tempChunk, options, profile));
                }
                else
                {
                    result.Add(CreateSplitChunk(
                        remaining, chunk, startPos, content.Length, profile));
                }
            }
        }

        return result;
    }

    private List<int> FindSplitPoints(
        string content,
        ChunkOptions options,
        ILanguageProfile profile)
    {
        var splitPoints = new List<int>();
        var targetSize = options.TargetChunkSize;

        // Priority 1: Paragraph boundaries
        if (options.PreserveParagraphs)
        {
            var paragraphs = profile.FindParagraphBoundaries(content);
            splitPoints.AddRange(paragraphs);
        }

        // Priority 2: Sentence boundaries
        if (options.PreserveSentences && splitPoints.Count < 2)
        {
            var sentences = profile.FindSentenceBoundaries(content);
            splitPoints.AddRange(sentences);
        }

        // Filter and optimize split points
        splitPoints = splitPoints.Distinct().OrderBy(x => x).ToList();

        // Select split points that create chunks close to target size
        var optimizedPoints = new List<int>();
        int lastPoint = 0;
        int accumulatedTokens = 0;

        foreach (var point in splitPoints)
        {
            if (point <= lastPoint)
                continue;

            var segment = content[lastPoint..point];
            var segmentTokens = profile.EstimateTokenCount(segment);
            accumulatedTokens += segmentTokens;

            if (accumulatedTokens >= targetSize)
            {
                optimizedPoints.Add(point);
                accumulatedTokens = 0;
                lastPoint = point;
            }
        }

        return optimizedPoints;
    }

    private List<DocumentChunk> SplitByTokens(
        DocumentChunk chunk,
        ChunkOptions options,
        ILanguageProfile profile)
    {
        var result = new List<DocumentChunk>();
        var content = chunk.Content;
        var targetSize = options.TargetChunkSize;

        // Estimate characters per token for this profile
        var totalTokens = profile.EstimateTokenCount(content);
        var charsPerToken = content.Length / (double)Math.Max(totalTokens, 1);
        var targetChars = (int)(targetSize * charsPerToken);

        int startPos = 0;
        while (startPos < content.Length)
        {
            int endPos = Math.Min(startPos + targetChars, content.Length);

            // Try to find a sentence boundary near the target
            if (endPos < content.Length && options.PreserveSentences)
            {
                var searchStart = Math.Max(startPos, endPos - targetChars / 4);
                var searchEnd = Math.Min(content.Length, endPos + targetChars / 4);
                var searchRange = content[searchStart..searchEnd];

                var boundaries = profile.FindSentenceBoundaries(searchRange);
                if (boundaries.Count > 0)
                {
                    // Find the boundary closest to our target
                    var targetOffset = endPos - searchStart;
                    var bestBoundary = boundaries.MinBy(b => Math.Abs(b - targetOffset));
                    endPos = searchStart + bestBoundary;
                }
            }

            // Ensure we make progress
            if (endPos <= startPos)
                endPos = Math.Min(startPos + targetChars, content.Length);

            var segmentContent = content[startPos..endPos].Trim();
            if (!string.IsNullOrEmpty(segmentContent))
            {
                result.Add(CreateSplitChunk(
                    segmentContent, chunk, startPos, endPos, profile));
            }

            startPos = endPos;
        }

        return result;
    }

    private static DocumentChunk CreateMergedChunk(
        string content,
        int startPos,
        int endPos,
        ILanguageProfile profile,
        DocumentChunk first,
        DocumentChunk last)
    {
        return new DocumentChunk
        {
            Content = content,
            Location = new ChunkLocation
            {
                StartPosition = startPos,
                EndPosition = endPos,
                StartLine = first.Location.StartLine,
                EndLine = last.Location.EndLine,
                SectionPath = first.Location.SectionPath
            },
            Metadata = new ChunkMetadata
            {
                LanguageCode = first.Metadata.LanguageCode,
                EstimatedTokenCount = profile.EstimateTokenCount(content),
                Strategy = first.Metadata.Strategy,
                StartsAtSentenceBoundary = first.Metadata.StartsAtSentenceBoundary,
                EndsAtSentenceBoundary = last.Metadata.EndsAtSentenceBoundary,
                Custom = MergeCustomMetadata(first.Metadata.Custom, last.Metadata.Custom)
            }
        };
    }

    private static DocumentChunk CreateChunkFromBuffer(
        string content,
        int startPos,
        DocumentChunk source,
        ILanguageProfile profile)
    {
        return new DocumentChunk
        {
            Content = content,
            Location = new ChunkLocation
            {
                StartPosition = startPos,
                EndPosition = source.Location.EndPosition,
                StartLine = source.Location.StartLine,
                EndLine = source.Location.EndLine,
                SectionPath = source.Location.SectionPath
            },
            Metadata = new ChunkMetadata
            {
                LanguageCode = source.Metadata.LanguageCode,
                EstimatedTokenCount = profile.EstimateTokenCount(content),
                Strategy = source.Metadata.Strategy,
                StartsAtSentenceBoundary = source.Metadata.StartsAtSentenceBoundary,
                EndsAtSentenceBoundary = source.Metadata.EndsAtSentenceBoundary,
                Custom = source.Metadata.Custom
            }
        };
    }

    private static DocumentChunk CreateSplitChunk(
        string content,
        DocumentChunk source,
        int relativeStart,
        int relativeEnd,
        ILanguageProfile profile)
    {
        return new DocumentChunk
        {
            Content = content,
            Location = new ChunkLocation
            {
                StartPosition = source.Location.StartPosition + relativeStart,
                EndPosition = source.Location.StartPosition + relativeEnd,
                SectionPath = source.Location.SectionPath
            },
            Metadata = new ChunkMetadata
            {
                LanguageCode = source.Metadata.LanguageCode,
                EstimatedTokenCount = profile.EstimateTokenCount(content),
                Strategy = source.Metadata.Strategy,
                Custom = source.Metadata.Custom != null
                    ? new Dictionary<string, object>(source.Metadata.Custom)
                    : null
            }
        };
    }

    private static IReadOnlyList<DocumentChunk> ReindexChunks(List<DocumentChunk> chunks)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Index = i;
            chunks[i].TotalChunks = chunks.Count;
        }
        return chunks;
    }

    private static ILanguageProfile GetLanguageProfile(DocumentChunk chunk)
    {
        var langCode = chunk.Metadata.LanguageCode ?? "en";
        return LanguageProfileRegistry.Instance.GetProfile(langCode);
    }

    private static Dictionary<string, object>? MergeCustomMetadata(
        Dictionary<string, object>? first,
        Dictionary<string, object>? second)
    {
        if (first == null && second == null)
            return null;

        var result = new Dictionary<string, object>();

        if (first != null)
        {
            foreach (var kvp in first)
                result[kvp.Key] = kvp.Value;
        }

        if (second != null)
        {
            foreach (var kvp in second)
            {
                if (!result.ContainsKey(kvp.Key))
                    result[kvp.Key] = kvp.Value;
            }
        }

        return result.Count > 0 ? result : null;
    }
}
