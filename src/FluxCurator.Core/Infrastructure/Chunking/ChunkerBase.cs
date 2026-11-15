namespace FluxCurator.Core.Infrastructure.Chunking;

using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.Languages;

/// <summary>
/// Base class for chunker implementations with common functionality.
/// </summary>
public abstract class ChunkerBase : IChunker
{
    /// <summary>
    /// Gets the language profile registry for language detection.
    /// </summary>
    protected LanguageProfileRegistry LanguageRegistry { get; } = LanguageProfileRegistry.Instance;

    /// <inheritdoc/>
    public abstract string StrategyName { get; }

    /// <inheritdoc/>
    public virtual bool RequiresEmbedder => false;

    /// <inheritdoc/>
    public abstract Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract int EstimateChunkCount(string text, ChunkOptions options);

    /// <summary>
    /// Gets the language profile for the given options and text.
    /// </summary>
    protected ILanguageProfile GetLanguageProfile(string text, ChunkOptions options)
    {
        if (!string.IsNullOrEmpty(options.LanguageCode))
            return LanguageRegistry.GetProfile(options.LanguageCode);

        return LanguageRegistry.DetectProfile(text);
    }

    /// <summary>
    /// Creates a DocumentChunk with proper metadata.
    /// </summary>
    protected DocumentChunk CreateChunk(
        string content,
        int index,
        int totalChunks,
        int startPosition,
        int endPosition,
        ILanguageProfile profile,
        ChunkOptions options,
        bool startsAtBoundary = true,
        bool endsAtBoundary = true,
        string? overlapContent = null)
    {
        var chunk = new DocumentChunk
        {
            Content = options.TrimWhitespace ? content.Trim() : content,
            ChunkIndex = index,
            TotalChunks = totalChunks,
            Location = new ChunkLocation
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                StartLine = CountLines(content, 0, startPosition),
                EndLine = CountLines(content, 0, endPosition)
            },
            Metadata = new ChunkMetadata
            {
                LanguageCode = profile.LanguageCode,
                EstimatedTokenCount = profile.EstimateTokenCount(content),
                Strategy = GetChunkingStrategy(),
                StartsAtSentenceBoundary = startsAtBoundary,
                EndsAtSentenceBoundary = endsAtBoundary,
                // The overlap text itself: the break it carries to the next slice is not part of it.
                OverlapFromPrevious = options.TrimWhitespace ? overlapContent?.TrimEnd() : overlapContent
            }
        };

        if (options.NormalizeWhitespace)
        {
            chunk.Content = NormalizeWhitespace(chunk.Content);
        }

        return chunk;
    }

    /// <summary>
    /// Gets the chunking strategy enum value for this chunker.
    /// </summary>
    protected abstract ChunkingStrategy GetChunkingStrategy();

    /// <summary>
    /// Counts line numbers up to a position in text.
    /// </summary>
    protected static int CountLines(string text, int start, int end)
    {
        if (string.IsNullOrEmpty(text) || end <= start)
            return 1;

        int lineCount = 1;
        int maxPos = Math.Min(end, text.Length);

        for (int i = start; i < maxPos; i++)
        {
            if (text[i] == '\n')
                lineCount++;
        }

        return lineCount;
    }

    /// <summary>
    /// Normalizes whitespace in text.
    /// </summary>
    protected static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new System.Text.StringBuilder(text.Length);
        bool lastWasWhitespace = false;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasWhitespace)
                {
                    result.Append(' ');
                    lastWasWhitespace = true;
                }
            }
            else
            {
                result.Append(c);
                lastWasWhitespace = false;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Overlap for the next chunk, taken from a chunk that was built as <paramref name="untrimmedChunk"/>. A trimmed chunk lost
    /// the whitespace that separated its end from the next text of the document, so the overlap carries it: without it the
    /// last line of the overlap fuses with the first line of the next slice (a closing sentence with the next paragraph, a
    /// marker line with the code fence after it). Append it with <see cref="AppendOverlap"/>.
    /// </summary>
    protected static string? ExtractOverlapContent(string untrimmedChunk, ChunkOptions options, ILanguageProfile profile)
    {
        if (options.OverlapSize <= 0)
            return null;

        var chunk = options.TrimWhitespace ? untrimmedChunk.Trim() : untrimmedChunk;
        var overlap = ExtractOverlapContent(chunk, options.OverlapSize, profile);
        if (overlap is null || !options.TrimWhitespace)
            return overlap;

        return overlap + untrimmedChunk[untrimmedChunk.TrimEnd().Length..];
    }

    /// <summary>
    /// Puts an overlap in front of <paramref name="nextSlice"/>, the text of the document that follows it: the whitespace the
    /// overlap carries is dropped when the slice already starts with a line break of its own, so a break is never written
    /// twice. A slice that starts with indentation only (spaces of a fenced block inside a list item) has no line break of its
    /// own, and the overlap keeps the one it carries.
    /// </summary>
    protected static void AppendOverlap(System.Text.StringBuilder buffer, string overlap, string nextSlice, ChunkOptions options) =>
        buffer.Append(options.TrimWhitespace && StartsWithLineBreak(nextSlice) ? overlap.TrimEnd() : overlap);

    private static bool StartsWithLineBreak(string text)
    {
        foreach (var c in text)
        {
            if (c is '\n' or '\r')
                return true;

            if (!char.IsWhiteSpace(c))
                return false;
        }

        return false;
    }

    /// <summary>
    /// Extracts overlap content from the end of a chunk.
    /// </summary>
    protected static string? ExtractOverlapContent(string previousChunk, int overlapSize, ILanguageProfile profile)
    {
        if (overlapSize <= 0 || string.IsNullOrEmpty(previousChunk))
            return null;

        int targetTokens = overlapSize;
        int currentTokens = 0;
        int startIndex = previousChunk.Length;

        // Work backwards to find overlap start
        for (int i = previousChunk.Length - 1; i >= 0 && currentTokens < targetTokens; i--)
        {
            startIndex = i;
            currentTokens = profile.EstimateTokenCount(previousChunk[i..]);
        }

        // Try to align to sentence boundary
        var sentenceBoundaries = profile.FindSentenceBoundaries(previousChunk);
        foreach (var boundary in sentenceBoundaries.Reverse())
        {
            if (boundary <= startIndex)
            {
                startIndex = boundary;
                break;
            }
        }

        return previousChunk[startIndex..];
    }
}
