namespace FluxCurator.Tests;

using global::FluxCurator.Core;
using global::FluxCurator.Core.Domain;
using NSubstitute;

public class BatchProcessorTests
{
    private readonly IFluxCurator _mockCurator;
    private readonly BatchProcessor _processor;

    public BatchProcessorTests()
    {
        _mockCurator = Substitute.For<IFluxCurator>();
        _processor = new BatchProcessor(_mockCurator);
    }

    #region Constructor

    [Fact]
    public void Constructor_NullCurator_ThrowsArgumentNullException()
    {
        var act = () => new BatchProcessor(null!);

        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("curator", ex.ParamName);
    }

    #endregion

    #region AddText

    [Fact]
    public void AddText_ValidText_IncrementsCount()
    {
        _processor.AddText("hello");

        Assert.Equal(1, _processor.Count);
    }

    [Fact]
    public void AddText_NullText_IgnoresIt()
    {
        _processor.AddText(null!);

        Assert.Equal(0, _processor.Count);
    }

    [Fact]
    public void AddText_EmptyText_IgnoresIt()
    {
        _processor.AddText("");

        Assert.Equal(0, _processor.Count);
    }

    [Fact]
    public void AddText_WhitespaceOnly_IgnoresIt()
    {
        _processor.AddText("   ");

        Assert.Equal(0, _processor.Count);
    }

    [Fact]
    public void AddText_FluentChaining()
    {
        var result = _processor.AddText("a").AddText("b").AddText("c");

        Assert.Same(_processor, result);
        Assert.Equal(3, _processor.Count);
    }

    #endregion

    #region AddTexts

    [Fact]
    public void AddTexts_ValidTexts_AddsAll()
    {
        _processor.AddTexts(["one", "two", "three"]);

        Assert.Equal(3, _processor.Count);
    }

    [Fact]
    public void AddTexts_NullCollection_ThrowsArgumentNullException()
    {
        var act = () => _processor.AddTexts(null!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void AddTexts_MixedValidAndEmpty_SkipsEmpty()
    {
        _processor.AddTexts(["valid", "", "  ", "also valid"]);

        Assert.Equal(2, _processor.Count);
    }

    [Fact]
    public void AddTexts_FluentChaining()
    {
        var result = _processor.AddTexts(["a"]);

        Assert.Same(_processor, result);
    }

    #endregion

    #region WithMaxConcurrency

    [Fact]
    public void WithMaxConcurrency_FluentChaining()
    {
        var result = _processor.WithMaxConcurrency(4);

        Assert.Same(_processor, result);
    }

    #endregion

    #region ProcessAsync

    [Fact]
    public async Task ProcessAsync_EmptyBatch_ReturnsEmptyList()
    {
        var results = await _processor.ProcessAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task ProcessAsync_SingleText_CallsChunkAsync()
    {
        var chunks = new List<DocumentChunk>
        {
            new() { Content = "chunk1" }
        };
        _mockCurator.ChunkAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(chunks);

        _processor.AddText("hello");
        var results = await _processor.ProcessAsync();

        Assert.Single(results);
        Assert.Single(results[0]);
        Assert.Equal("chunk1", results[0][0].Content);
    }

    [Fact]
    public async Task ProcessAsync_MultipleTexts_ReturnsAllResults()
    {
        var chunks1 = new List<DocumentChunk> { new() { Content = "c1" } };
        var chunks2 = new List<DocumentChunk> { new() { Content = "c2" } };
        var callCount = 0;
        _mockCurator.ChunkAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                return Interlocked.Increment(ref callCount) == 1 ? chunks1 : chunks2;
            });

        _processor.AddText("text1").AddText("text2");
        var results = await _processor.ProcessAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ProcessAsync_PassesCancellationToken()
    {
        _mockCurator.ChunkAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<DocumentChunk>());
        using var cts = new CancellationTokenSource();

        _processor.AddText("text");
        await _processor.ProcessAsync(cts.Token);

        await _mockCurator.Received(1).ChunkAsync(
            Arg.Any<string>(),
            cts.Token);
    }

    #endregion

    #region PreprocessAsync

    [Fact]
    public async Task PreprocessAsync_EmptyBatch_ReturnsEmptyList()
    {
        var results = await _processor.PreprocessAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task PreprocessAsync_SingleText_CallsPreprocessAsync()
    {
        var result = new PreprocessingResult
        {
            OriginalText = "hello",
            ProcessedText = "hello",
            Chunks = []
        };
        _mockCurator.PreprocessAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(result);

        _processor.AddText("hello");
        var results = await _processor.PreprocessAsync();

        Assert.Single(results);
        Assert.Equal("hello", results[0].OriginalText);
    }

    [Fact]
    public async Task PreprocessAsync_PassesCancellationToken()
    {
        _mockCurator.PreprocessAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PreprocessingResult
            {
                OriginalText = "text",
                ProcessedText = "text",
                Chunks = []
            });
        using var cts = new CancellationTokenSource();

        _processor.AddText("text");
        await _processor.PreprocessAsync(cts.Token);

        await _mockCurator.Received(1).PreprocessAsync(
            Arg.Any<string>(),
            cts.Token);
    }

    #endregion

    #region GetTotalEstimatedChunks

    [Fact]
    public void GetTotalEstimatedChunks_EmptyBatch_ReturnsZero()
    {
        var result = _processor.GetTotalEstimatedChunks();

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetTotalEstimatedChunks_SumsEstimates()
    {
        _mockCurator.EstimateChunkCount(Arg.Any<string>()).Returns(3);

        _processor.AddText("a").AddText("b");
        var result = _processor.GetTotalEstimatedChunks();

        Assert.Equal(6, result);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllTexts()
    {
        _processor.AddText("a").AddText("b");

        _processor.Clear();

        Assert.Equal(0, _processor.Count);
    }

    [Fact]
    public void Clear_FluentChaining()
    {
        var result = _processor.Clear();

        Assert.Same(_processor, result);
    }

    #endregion
}
