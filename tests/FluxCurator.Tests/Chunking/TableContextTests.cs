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

    // A court decision's shareholder tables: the caption is followed by a units line the parser made a heading, header
    // cells that fell out of the table, and footnotes — and the next table on the page has the same shape.
    private const string ShareholderTable =
        "| | 계 | 보통주 | 우선주 | |\n" +
        "| --- | --- | --- | --- | --- |\n" +
        "| 갑 | 100 | 80 | 20 | 50.0 |";

    private static string ShareholderBlock(string caption) =>
        "비고 비금융4)･상장\n" +
        "- 자료출처: 기업집단포털시스템\n" +
        caption + "\n" +
        "## 2022. 5. 1. 기준, 단위: 주, %) 지분율 비고\n" +
        "주주명 소유주식수\n" +
        "3. 이하 회사명을 기재할 때 ‘주식회사’는 생략한다.\n" +
        "4. C는 한국표준산업분류상 ‘정보서비스업(J63)’을 영위하고 있다.\n" +
        ShareholderTable;

    [Fact]
    public async Task ACaptionAboveNotesAndStrayHeaderCells_NamesTheTable()
    {
        var text = Prose + "\n\n" + ShareholderBlock("<표 5> C의 주주현황") + "\n\n" + Prose + "\n\n" + ShareholderBlock("<표 6> D의 주주현황");

        var tables = (await Chunk(text)).Where(c => c.Metadata.Custom?.ContainsKey("table") == true).ToList();

        Assert.Equal(2, tables.Count);
        // The caption comes first, then the short line directly above the table; the footnotes are skipped.
        Assert.Equal("<표 5> C의 주주현황\n주주명 소유주식수", tables[0].Metadata.Custom!["table_context"]);
        Assert.Equal("<표 6> D의 주주현황\n주주명 소유주식수", tables[1].Metadata.Custom!["table_context"]);
        Assert.StartsWith("<표 5> C의 주주현황\n", tables[0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public void WithOneLine_TheCaptionWinsOverTheLineDirectlyAbove()
    {
        var text = ShareholderBlock("[표 5] C의 주주현황");

        Assert.Equal("[표 5] C의 주주현황", global::FluxCurator.Core.Infrastructure.Chunking.TableAwareChunker.TableContext(text, text.IndexOf("| |", StringComparison.Ordinal), maxLines: 1));
    }

    [Fact]
    public void ACaptionBelongingToTheTableAbove_IsNotTaken()
    {
        var text = "<표 1> 앞 표\n" + Table + "\n" + Title + "\n" + Table;

        Assert.Equal(Title, global::FluxCurator.Core.Infrastructure.Chunking.TableAwareChunker.TableContext(text, text.LastIndexOf("| 연도", StringComparison.Ordinal), maxLines: 2));
    }

    [Theory]
    [InlineData("<표 5> C의 주주현황", true)]
    [InlineData("[표 2] 연도별 생산량", true)]
    [InlineData("표 3. 지역별 현황", true)]
    [InlineData("표 3-1 세부 내역", true)]
    [InlineData("Table 4: Results by region", true)]
    [InlineData("Tab. 2 Summary", true)]
    [InlineData("## <표 7> 재무 현황", true)]
    [InlineData("<그림 2> 추이", false)]
    [InlineData("Figure 3: Trend", false)]
    [InlineData("표준 운영 절차", false)]
    [InlineData("Tables are listed below", false)]
    public void Caption_Shapes(string line, bool expected) => Assert.Equal(expected, global::FluxCurator.Core.Infrastructure.Chunking.TableAwareChunker.IsCaption(line));

    [Theory]
    [InlineData("3. 이하 회사명을 기재할 때 ‘주식회사’는 생략한다.", true)]
    [InlineData("4) 비상장 회사", true)]
    [InlineData("* 잠정치", true)]
    [InlineData("※ 단위 환산 기준", true)]
    [InlineData("주) 연말 기준", true)]
    [InlineData("- 자료출처: 기업집단포털시스템", true)]
    [InlineData("Source: national statistics", true)]
    [InlineData("1. 기준금리", false)]
    [InlineData("(단위: 만 캐럿)", false)]
    public void Note_Shapes(string line, bool expected) => Assert.Equal(expected, global::FluxCurator.Core.Infrastructure.Chunking.TableAwareChunker.IsNote(line));
}
