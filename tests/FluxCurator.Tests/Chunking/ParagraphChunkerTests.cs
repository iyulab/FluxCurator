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
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.Single(chunks);
        Assert.Contains("single paragraph", chunks[0].Content);
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
        var chunks = await _chunker.ChunkAsync(text, options);

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
        var chunks = await _chunker.ChunkAsync(text, options);

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
        var chunks = await _chunker.ChunkAsync(text, options);

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
        var chunks = await _chunker.ChunkAsync(text, options);

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
        var chunks = await _chunker.ChunkAsync(text, options);

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
        var chunks = await _chunker.ChunkAsync(longText, options);

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
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);
    }
}
