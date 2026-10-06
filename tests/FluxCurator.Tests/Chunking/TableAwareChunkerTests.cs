namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;
using global::FluxCurator.Infrastructure.Chunking;

/// <summary>
/// A chunk that holds table rows must say which column each value belongs to: a table that fits is one chunk, a larger
/// one is split only between rows with the header repeated, and no chunk or overlap starts inside a row.
/// </summary>
public class TableAwareChunkerTests
{
    private static string Table(int rows)
    {
        var lines = new List<string> { "| Group | Detail | Done | Planned |", "| --- | --- | --- | --- |" };
        for (var i = 1; i <= rows; i++)
            lines.Add($"| Bank {i} | Household support program number {i} for small business owners | {i * 100} | {i * 37} |");
        lines.Add("| Total | All programs | 10265 | 9076 |");
        return string.Join("\n", lines);
    }

    private static string Prose(int sentences) =>
        string.Join(" ", Enumerable.Range(1, sentences).Select(i => $"Sentence number {i} explains the plan in some detail."));

    [Fact]
    public async Task Token_TableLargerThanTheLimit_EveryPieceStartsWithTheHeader_AndNoRowIsCut()
    {
        var text = Prose(20) + "\n\n" + Table(40) + "\n\n" + Prose(20);
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Token, MaxChunkSize = 200, TargetChunkSize = 150, OverlapSize = 30 };

        var chunks = await new ChunkerFactory().CreateChunker(ChunkingStrategy.Token).ChunkAsync(text, options, TestContext.Current.CancellationToken);

        var tablePieces = chunks.Where(c => c.Metadata.Custom?.ContainsKey("table") == true).ToList();
        Assert.True(tablePieces.Count > 1);
        Assert.All(tablePieces, piece =>
        {
            var lines = piece.Content.Split('\n');
            Assert.Equal("| Group | Detail | Done | Planned |", lines[0]);
            Assert.Equal("| --- | --- | --- | --- |", lines[1]);
            Assert.All(lines, line => Assert.True(line.StartsWith('|') && line.EndsWith('|'), $"cut row: {line}"));
        });
        Assert.Contains(tablePieces, p => p.Content.Contains("| Total | All programs | 10265 | 9076 |", StringComparison.Ordinal));
        Assert.Equal(tablePieces.Count, tablePieces[0].Metadata.Custom!["table_pieces"]);

        // No prose chunk carries a table row (the overlap does not reach into the table).
        Assert.All(chunks.Except(tablePieces), c => Assert.DoesNotContain("| Bank", c.Content, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Sentence_TableThatFits_IsOneChunk()
    {
        var table = Table(3);
        var text = Prose(30) + "\n\n" + table + "\n\n" + Prose(30);
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Sentence, MaxChunkSize = 300, TargetChunkSize = 200 };

        var chunks = await new ChunkerFactory().CreateChunker(ChunkingStrategy.Sentence).ChunkAsync(text, options, TestContext.Current.CancellationToken);

        var tableChunk = Assert.Single(chunks, c => c.Metadata.Custom?.ContainsKey("table") == true);
        Assert.Equal(table, tableChunk.Content);
        Assert.Equal(text.IndexOf(table, StringComparison.Ordinal), tableChunk.Location.StartPosition);
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(c => c.ChunkIndex));
    }

    [Fact]
    public async Task PreserveTablesOff_LeavesTheStrategyAlone()
    {
        var text = Prose(20) + "\n\n" + Table(40);
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Token, MaxChunkSize = 200, TargetChunkSize = 150, PreserveTables = false };

        var wrapped = await new TableAwareChunker(new TokenChunker()).ChunkAsync(text, options, TestContext.Current.CancellationToken);
        var plain = await new TokenChunker().ChunkAsync(text, options, TestContext.Current.CancellationToken);

        Assert.Equal(plain.Select(c => c.Content), wrapped.Select(c => c.Content));
    }

    [Fact]
    public async Task LoneLineWithPipes_StaysProse()
    {
        var text = "Use a | b | c to choose.";

        var chunks = await new TableAwareChunker(new SentenceChunker()).ChunkAsync(text, new ChunkOptions(), TestContext.Current.CancellationToken);

        Assert.All(chunks, c => Assert.Null(c.Metadata.Custom));
    }
}
