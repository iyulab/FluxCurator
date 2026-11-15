using FluxCurator.Core.Domain;
using FluxCurator.Infrastructure.Chunking;

namespace FluxCurator.Tests.Chunking;

/// <summary>
/// A chunk that starts with the tail of the previous one (the overlap) must keep the line break the document had between
/// them: the chunk text is trimmed, so the whitespace that separated the end of one chunk from the next text has to travel
/// with the overlap, or the last line of the overlap fuses with the first line of the chunk's own text (a closing sentence
/// with the next paragraph, a marker line with the fence that follows it).
/// </summary>
public sealed class ChunkOverlapSeamTests
{
    private static ChunkOptions Options(bool trim, int overlap) => new()
    {
        MaxChunkSize = 120,
        MinChunkSize = 20,
        TargetChunkSize = 60,
        OverlapSize = overlap,
        EnableChunkBalancing = false,
        PreserveParagraphs = true,
        PreserveSentences = true,
        TrimWhitespace = trim,
    };

    /// <summary>Prose paragraphs separated by blank lines, each one sentence on one line.</summary>
    private static string Paragraphs() =>
        string.Join("\n\n", Enumerable.Range(0, 40).Select(i => "Paragraph " + i.ToString("000", System.Globalization.CultureInfo.InvariantCulture) + " is about alpha and beta."));

    /// <summary>A comment marker on a line of its own, then a fenced code block: a break between the two lines is the case that fused.</summary>
    private static string MarkedCode() =>
        string.Join("\n", Enumerable.Range(0, 30).Select(i =>
            "## Section " + i + "\n\nProse of section " + i + " about alpha.\n\n<!-- CODE_START -->\n```text\ncode line " + i + "\n```\n<!-- CODE_END -->\n"));

    private static async Task<List<string>> FusedLinesAsync(string text, ChunkOptions options)
    {
        var sourceLines = text.Split('\n').Where(line => line.Length > 0).ToList();
        var chunks = await new ChunkerFactory().CreateChunker(ChunkingStrategy.Paragraph).ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // A chunk line is a source line (or the end of one cut at a sentence boundary); a line that is no part of any source
        // line is two of them run together.
        return [.. chunks.SelectMany(chunk => chunk.Content.Split('\n'))
            .Where(line => line.Length > 0 && !sourceLines.Any(source => source.Contains(line, StringComparison.Ordinal)))
            .Distinct()];
    }

    [Theory]
    [InlineData(true, 30)]
    [InlineData(true, 60)]
    [InlineData(false, 30)]
    public async Task The_overlap_of_a_paragraph_chunk_does_not_fuse_with_the_next_paragraph(bool trim, int overlap)
    {
        var fused = await FusedLinesAsync(Paragraphs(), Options(trim, overlap));

        Assert.Empty(fused);
    }

    [Theory]
    [InlineData(true, 30)]
    [InlineData(true, 60)]
    [InlineData(false, 30)]
    public async Task The_overlap_does_not_glue_a_marker_line_to_the_fence_that_follows_it(bool trim, int overlap)
    {
        var fused = await FusedLinesAsync(MarkedCode(), Options(trim, overlap));

        Assert.Empty(fused);
    }

    [Fact]
    public async Task The_overlap_recorded_in_the_metadata_is_the_text_without_the_break_it_carries()
    {
        var chunks = await new ChunkerFactory().CreateChunker(ChunkingStrategy.Paragraph)
            .ChunkAsync(Paragraphs(), Options(trim: true, overlap: 30), TestContext.Current.CancellationToken);

        var overlaps = chunks.Select(chunk => chunk.Metadata.OverlapFromPrevious).Where(overlap => overlap is not null).ToList();

        Assert.NotEmpty(overlaps);
        Assert.All(overlaps, overlap => Assert.Equal(overlap!.TrimEnd(), overlap));
    }

    [Fact]
    public async Task Without_an_overlap_nothing_is_added_and_the_text_is_unchanged()
    {
        var text = Paragraphs();
        var chunks = await new ChunkerFactory().CreateChunker(ChunkingStrategy.Paragraph)
            .ChunkAsync(text, Options(trim: true, overlap: 0), TestContext.Current.CancellationToken);

        Assert.All(chunks, chunk => Assert.Contains(chunk.Content, text, StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_long_paragraph_split_by_sentences_keeps_the_break_after_the_overlap()
    {
        var sentences = string.Join(" ", Enumerable.Range(0, 60).Select(i => "Sentence " + i.ToString("000", System.Globalization.CultureInfo.InvariantCulture) + " says alpha."));
        var text = sentences + "\n\nNext paragraph starts here.";
        var chunks = await new ChunkerFactory().CreateChunker(ChunkingStrategy.Paragraph)
            .ChunkAsync(text, Options(trim: true, overlap: 30), TestContext.Current.CancellationToken);

        Assert.DoesNotContain(chunks, chunk => chunk.Content.Contains("alpha.Next paragraph", StringComparison.Ordinal) || chunk.Content.Contains("alpha.Sentence", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Content.Contains("Next paragraph starts here.", StringComparison.Ordinal));
    }
}
