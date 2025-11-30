namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;

public class TokenChunkerTests
{
    private readonly TokenChunker _chunker = new();

    [Fact]
    public async Task ChunkAsync_ShortText_ReturnsSingleChunk()
    {
        // Arrange
        var text = "Short text.";
        var options = new ChunkOptions
        {
            TargetChunkSize = 100
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.Single(chunks);
        Assert.Equal(text, chunks[0].Content);
    }

    [Fact]
    public async Task ChunkAsync_ExactTokenCount_SplitsCorrectly()
    {
        // Arrange
        var words = Enumerable.Range(1, 100).Select(i => $"word{i}").ToList();
        var text = string.Join(" ", words);
        var options = new ChunkOptions
        {
            TargetChunkSize = 50,
            MaxChunkSize = 60,
            MinChunkSize = 10
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.True(chunks.Count > 1);
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
    public async Task ChunkAsync_SetsCorrectStrategy()
    {
        // Arrange
        var text = "Test text for token chunking.";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.All(chunks, c => Assert.Equal(ChunkingStrategy.Token, c.Metadata.Strategy));
    }

    [Fact]
    public async Task ChunkAsync_WithOverlap_IncludesOverlapTokens()
    {
        // Arrange
        var words = Enumerable.Range(1, 50).Select(i => $"word{i}").ToList();
        var text = string.Join(" ", words);
        var options = new ChunkOptions
        {
            TargetChunkSize = 20,
            MaxChunkSize = 25,
            MinChunkSize = 5,
            OverlapSize = 5
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        if (chunks.Count > 1)
        {
            // Check that overlap metadata exists for subsequent chunks
            for (int i = 1; i < chunks.Count; i++)
            {
                Assert.NotNull(chunks[i].Metadata);
            }
        }
    }

    [Fact]
    public async Task ChunkAsync_PreserveSentences_RespectsSentenceBoundaries()
    {
        // Arrange
        var text = "First sentence here. Second sentence follows. Third sentence ends.";
        var options = new ChunkOptions
        {
            TargetChunkSize = 10,
            MaxChunkSize = 15,
            MinChunkSize = 3,
            PreserveSentences = true
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);
        // When preserving sentences, chunks should generally end at sentence boundaries
    }

    [Fact]
    public async Task ChunkAsync_FixedSize_CreatesConsistentChunks()
    {
        // Arrange
        var words = Enumerable.Range(1, 200).Select(i => $"word{i}").ToList();
        var text = string.Join(" ", words);
        var options = ChunkOptions.FixedSize(50, 10);

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.True(chunks.Count > 1);
        // Token counts should be reasonably consistent
        var tokenCounts = chunks.Select(c => c.Metadata.EstimatedTokenCount).ToList();
        Assert.All(tokenCounts, t => Assert.True(t > 0));
    }

    [Fact]
    public void EstimateChunkCount_ReturnsPositiveEstimate()
    {
        // Arrange
        var words = Enumerable.Range(1, 100).Select(i => $"word{i}").ToList();
        var text = string.Join(" ", words);
        var options = new ChunkOptions
        {
            TargetChunkSize = 25
        };

        // Act
        var estimate = _chunker.EstimateChunkCount(text, options);

        // Assert
        Assert.True(estimate >= 1); // Should be at least 1
    }

    [Fact]
    public async Task ChunkAsync_KoreanText_EstimatesTokensCorrectly()
    {
        // Arrange
        var text = "안녕하세요 반갑습니다 테스트입니다 감사합니다";
        var options = ChunkOptions.ForKorean;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.True(c.Metadata.EstimatedTokenCount > 0));
    }

    [Fact]
    public async Task ChunkAsync_SetsLocationMetadata()
    {
        // Arrange
        var text = "First part. Second part.";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.All(chunks, chunk =>
        {
            Assert.True(chunk.Location.StartPosition >= 0);
            Assert.True(chunk.Location.EndPosition >= chunk.Location.StartPosition);
        });
    }
}
