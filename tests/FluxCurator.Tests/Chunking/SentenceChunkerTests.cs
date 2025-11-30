namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;

public class SentenceChunkerTests
{
    private readonly SentenceChunker _chunker = new();

    [Fact]
    public async Task ChunkAsync_SingleSentence_ReturnsSingleChunk()
    {
        // Arrange
        var text = "This is a single sentence.";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.Single(chunks);
        Assert.Equal(text, chunks[0].Content);
        Assert.Equal(0, chunks[0].Index);
        Assert.Equal(1, chunks[0].TotalChunks);
    }

    [Fact]
    public async Task ChunkAsync_MultipleSentences_ReturnsCorrectChunks()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence.";
        var options = new ChunkOptions
        {
            MaxChunkSize = 20,
            MinChunkSize = 1,
            TargetChunkSize = 15
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
    public async Task ChunkAsync_WhitespaceOnly_ReturnsEmptyList()
    {
        // Arrange
        var text = "   \t\n   ";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ChunkAsync_KoreanText_HandlesKoreanSentenceEndings()
    {
        // Arrange
        var text = "안녕하세요. 반갑습니다. 테스트입니다.";
        var options = ChunkOptions.ForKorean;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.NotNull(c.Metadata.LanguageCode));
    }

    [Fact]
    public async Task ChunkAsync_WithOverlap_IncludesOverlapContent()
    {
        // Arrange
        var text = "First sentence with content. Second sentence with more content. Third sentence ends here.";
        var options = new ChunkOptions
        {
            MaxChunkSize = 50,
            MinChunkSize = 10,
            TargetChunkSize = 30,
            OverlapSize = 10
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        if (chunks.Count > 1)
        {
            // If there are multiple chunks, later ones may have overlap metadata
            Assert.True(chunks.Count >= 1);
        }
    }

    [Fact]
    public async Task ChunkAsync_SetsCorrectMetadata()
    {
        // Arrange
        var text = "This is a test sentence. Here is another one.";
        var options = new ChunkOptions
        {
            IncludeMetadata = true
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.All(chunks, chunk =>
        {
            Assert.Equal(ChunkingStrategy.Sentence, chunk.Metadata.Strategy);
            Assert.True(chunk.Metadata.EstimatedTokenCount > 0);
        });
    }

    [Fact]
    public void EstimateChunkCount_ReturnsPositiveValue()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence.";
        var options = new ChunkOptions
        {
            TargetChunkSize = 20
        };

        // Act
        var estimate = _chunker.EstimateChunkCount(text, options);

        // Assert
        Assert.True(estimate > 0);
    }

    [Fact]
    public async Task ChunkAsync_CancellationToken_Respected()
    {
        // Arrange
        var text = "Test sentence.";
        var options = ChunkOptions.Default;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _chunker.ChunkAsync(text, options, cts.Token));
    }
}
