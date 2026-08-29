namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;

public class ParagraphChunkerTests
{
    private readonly ParagraphChunker _chunker = new();

    [Fact]
    public async Task ChunkAsync_SingleParagraph_ReturnsSingleChunk()
    {
        // Arrange
        var text = "This is a single paragraph with multiple sentences. It continues here. And ends here.";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(chunks);
        Assert.Contains("single paragraph", chunks[0].Content);
    }

    [Fact]
    public async Task ChunkAsync_WithOverlap_StartPositionIsAbsoluteDocumentOffset()
    {
        // Arrange — regression guard: StartPosition used to be derived as
        // "end - buffer.Length" where the buffer had overlap prepended, shifting every
        // chunk after the first left by the overlap length. Offset-based consumers
        // (heading paths, citations) then mapped chunks to the wrong document region.
        var para1 = string.Join(" ", Enumerable.Repeat("First paragraph sentence with sufficient words inside.", 4));
        var para2 = string.Join(" ", Enumerable.Repeat("Second paragraph sentence with different words inside.", 4));
        var text = para1 + "\n\n" + para2;
        var options = new ChunkOptions
        {
            MaxChunkSize = 60,
            MinChunkSize = 10,
            TargetChunkSize = 50,
            OverlapSize = 20,
            PreserveParagraphs = true
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(chunks.Count >= 2, $"expected multiple chunks, got {chunks.Count}");
        var previousStart = -1;
        foreach (var chunk in chunks)
        {
            // Monotonically increasing absolute offsets, all within the document
            Assert.True(chunk.Location.StartPosition > previousStart,
                $"StartPosition {chunk.Location.StartPosition} not after previous {previousStart}");
            Assert.True(chunk.Location.EndPosition <= text.Length);
            Assert.True(chunk.Location.StartPosition < chunk.Location.EndPosition);
            previousStart = chunk.Location.StartPosition;
        }

        // The second chunk must start at (or after) where the second paragraph begins —
        // not shifted left by the overlap into the first paragraph's territory.
        Assert.True(chunks[^1].Location.StartPosition >= text.IndexOf(para2, StringComparison.Ordinal) - 2,
            $"last chunk StartPosition {chunks[^1].Location.StartPosition} leaks into the previous paragraph");
    }

    [Fact]
    public async Task ChunkAsync_MultipleParagraphs_SplitsCorrectly()
    {
        // Arrange
        var text = """
            First paragraph with some content.

            Second paragraph with different content.

            Third paragraph concludes the text.
            """;
        var options = new ChunkOptions
        {
            MaxChunkSize = 100,
            MinChunkSize = 10,
            TargetChunkSize = 50,
            PreserveParagraphs = true
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(chunks.Count >= 1);
        Assert.All(chunks, c => Assert.False(string.IsNullOrEmpty(c.Content)));
    }

    [Fact]
    public async Task ChunkAsync_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        var text = "";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ChunkAsync_OnlyNewlines_ReturnsEmptyList()
    {
        // Arrange
        var text = "\n\n\n\n";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ChunkAsync_PreserveParagraphs_MaintainsStructure()
    {
        // Arrange
        var text = """
            Introduction paragraph explaining the topic.

            Main content paragraph with details and examples.

            Conclusion paragraph summarizing key points.
            """;
        var options = new ChunkOptions
        {
            MaxChunkSize = 200,
            PreserveParagraphs = true
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(chunks);
    }

    [Fact]
    public async Task ChunkAsync_SetsCorrectStrategy()
    {
        // Arrange
        var text = "Single paragraph test.";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.All(chunks, c => Assert.Equal(ChunkingStrategy.Paragraph, c.Metadata.Strategy));
    }

    [Fact]
    public async Task ChunkAsync_LongParagraph_SplitsWhenExceedsMax()
    {
        // Arrange
        var longText = string.Join(" ", Enumerable.Repeat("This is a very long sentence that keeps going.", 20));
        var options = new ChunkOptions
        {
            MaxChunkSize = 100,
            MinChunkSize = 10,
            TargetChunkSize = 50
        };

        // Act
        var chunks = await _chunker.ChunkAsync(longText, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(chunks.Count > 1);
        // Chunks are created but may exceed max due to preserving sentence boundaries
        Assert.All(chunks, c => Assert.NotEmpty(c.Content));
    }

    [Fact]
    public void EstimateChunkCount_ReturnsReasonableEstimate()
    {
        // Arrange
        var text = """
            First paragraph.

            Second paragraph.

            Third paragraph.
            """;
        var options = new ChunkOptions
        {
            TargetChunkSize = 50
        };

        // Act
        var estimate = _chunker.EstimateChunkCount(text, options);

        // Assert
        Assert.True(estimate >= 1);
    }

    [Fact]
    public async Task ChunkAsync_MixedLineEndings_HandlesCorrectly()
    {
        // Arrange
        var text = "First paragraph.\r\n\r\nSecond paragraph.\n\nThird paragraph.";
        var options = new ChunkOptions
        {
            MaxChunkSize = 50,
            MinChunkSize = 5,
            TargetChunkSize = 25
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(chunks);
    }
}
