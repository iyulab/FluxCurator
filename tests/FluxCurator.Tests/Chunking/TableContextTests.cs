namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Infrastructure.Chunking;

/// <summary>
/// A table chunk carries the line(s) that name the table — the title or unit line right above it, else the nearest
/// heading — so a numbers-only table piece is found by the words that name the table.
/// </summary>
public class TableContextTests
{
    private const string Prose =
        "앙골라는 아프리카 남서부에 있는 나라로, 석유와 다이아몬드가 수출의 대부분을 차지한다. 다이아몬드 산업은 국영 기업이 주도하며 최근 몇 년간 생산량이 크게 변동했다.";

    private const string Title = "앙골라 연도별 다이아몬드 생산량";
    private const string Unit = "(단위: 만 캐럿)";

    private const string Table =
        "| 연도 | 2018 | 2019 | 2020 | 2021 | 2022 | 2023 |\n" +
        "| --- | --- | --- | --- | --- | --- | --- |\n" +
        "| 생산량 | 943 | 912 | 799 | 914 | 875 | 980 |";

    private static Task<IReadOnlyList<global::FluxCurator.Core.Domain.DocumentChunk>> Chunk(string text, ChunkOptions? options = null) =>
        new ChunkerFactory().CreateChunker(ChunkingStrategy.Token)
            .ChunkAsync(text, options ?? new ChunkOptions { Strategy = ChunkingStrategy.Token, MaxChunkSize = 512 },
                TestContext.Current.CancellationToken);

    private static global::FluxCurator.Core.Domain.DocumentChunk TableChunk(IEnumerable<global::FluxCurator.Core.Domain.DocumentChunk> chunks) =>
        Assert.Single(chunks, c => c.Metadata.Custom?.ContainsKey("table") == true);

    [Fact]
    public async Task TheTitleAndUnitLines_AreRepeatedAboveTheHeader_AndRecorded()
    {
        var text = Prose + "\n\n" + Title + "\n" + Unit + "\n\n" + Table + "\n\n" + Prose;

        var chunks = await Chunk(text);

        var table = TableChunk(chunks);
        Assert.StartsWith(Title + "\n" + Unit + "\n| 연도 |", table.Content, StringComparison.Ordinal);
        Assert.Equal(Title + "\n" + Unit, table.Metadata.Custom!["table_context"]);
        // The lines stay in the text before the table too.
        Assert.Contains(chunks, c => c != table && c.Content.Contains(Title, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EveryPieceOfASplitTable_CarriesTheContext()
    {
        var rows = string.Join("\n", Enumerable.Range(1, 60).Select(i => $"| 품목 {i} | {i * 10} | {i * 7} | 비고 {i}번 항목의 설명 |"));
        var table = "| 품목 | 수량 | 금액 | 비고 |\n| --- | --- | --- | --- |\n" + rows;
        var text = Prose + "\n\n" + Title + "\n\n" + table;

        var chunks = await Chunk(text, new ChunkOptions { Strategy = ChunkingStrategy.Token, MaxChunkSize = 200, TargetChunkSize = 150 });

        var pieces = chunks.Where(c => c.Metadata.Custom?.ContainsKey("table") == true).ToList();
        Assert.True(pieces.Count > 1);
        Assert.All(pieces, p =>
        {
            Assert.StartsWith(Title + "\n| 품목 |", p.Content, StringComparison.Ordinal);
            Assert.Equal(Title, p.Metadata.Custom!["table_context"]);
        });
    }

    [Fact]
    public async Task WithOnlyLongProseAbove_TheNearestHeadingIsUsed()
    {
        var text = "## 광물 생산 현황\n\n" + Prose + "\n\n" + Table;

        var table = TableChunk(await Chunk(text));

        Assert.Equal("## 광물 생산 현황", table.Metadata.Custom!["table_context"]);
        Assert.StartsWith("## 광물 생산 현황\n| 연도 |", table.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithNothingToName_TheTableIsUnchanged()
    {
        var text = Prose + "\n\n" + Table;

        var table = TableChunk(await Chunk(text));

        Assert.Equal(Table, table.Content);
        Assert.False(table.Metadata.Custom!.ContainsKey("table_context"));
    }

    [Fact]
    public async Task ZeroLines_TurnsItOff()
    {
        var text = Prose + "\n\n" + Title + "\n\n" + Table;

        var table = TableChunk(await Chunk(text, new ChunkOptions { Strategy = ChunkingStrategy.Token, MaxChunkSize = 512, TableContextLines = 0 }));

        Assert.Equal(Table, table.Content);
    }

    [Fact]
    public async Task OnlyTheConfiguredNumberOfLines_AreTaken()
    {
        var text = Prose + "\n\nA heading-like line\n" + Title + "\n" + Unit + "\n" + Table;

        var table = TableChunk(await Chunk(text, new ChunkOptions { Strategy = ChunkingStrategy.Token, MaxChunkSize = 512, TableContextLines = 1 }));

        Assert.Equal(Unit, table.Metadata.Custom!["table_context"]);
    }
}
