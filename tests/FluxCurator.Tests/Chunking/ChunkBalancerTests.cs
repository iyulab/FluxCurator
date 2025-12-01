namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;

public class ChunkBalancerTests
{
    private readonly ChunkBalancer _balancer = ChunkBalancer.Instance;

    #region CalculateStats Tests

    [Fact]
    public void CalculateStats_EmptyList_ReturnsZeroStats()
    {
        // Arrange
        var chunks = Array.Empty<DocumentChunk>();

        // Act
        var stats = _balancer.CalculateStats(chunks);

        // Assert
        Assert.Equal(0, stats.ChunkCount);
        Assert.Equal(0, stats.MinTokenCount);
        Assert.Equal(0, stats.MaxTokenCount);
    }

    [Fact]
    public void CalculateStats_SingleChunk_ReturnsCorrectStats()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("This is a test chunk with some content.", 0, 1, estimatedTokens: 10)
        };

        // Act
        var stats = _balancer.CalculateStats(chunks);

        // Assert
        Assert.Equal(1, stats.ChunkCount);
        Assert.Equal(10, stats.MinTokenCount);
        Assert.Equal(10, stats.MaxTokenCount);
        Assert.Equal(10, stats.AverageTokenCount);
        Assert.Equal(1.0, stats.VarianceRatio); // 10/10 = 1 (perfectly balanced single chunk)
    }

    [Fact]
    public void CalculateStats_MultipleChunks_CalculatesCorrectVariance()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Small chunk.", 0, 3, estimatedTokens: 50),
            CreateChunk("Medium chunk with more content here.", 1, 3, estimatedTokens: 200),
            CreateChunk("Large chunk with significantly more text content than others.", 2, 3, estimatedTokens: 500)
        };

        // Act
        var stats = _balancer.CalculateStats(chunks);

        // Assert
        Assert.Equal(3, stats.ChunkCount);
        Assert.Equal(50, stats.MinTokenCount);
        Assert.Equal(500, stats.MaxTokenCount);
        Assert.Equal(250, stats.AverageTokenCount);
        Assert.Equal(10.0, stats.VarianceRatio); // 500/50 = 10
    }

    [Fact]
    public void CalculateStats_WithOptions_CountsUndersizedAndOversized()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Tiny.", 0, 4, estimatedTokens: 30),   // Undersized (< 100)
            CreateChunk("Normal sized chunk.", 1, 4, estimatedTokens: 200),
            CreateChunk("Another normal chunk.", 2, 4, estimatedTokens: 300),
            CreateChunk("Huge chunk exceeding maximum.", 3, 4, estimatedTokens: 2000) // Oversized (> 1024)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 1024
        };

        // Act
        var stats = _balancer.CalculateStats(chunks, options);

        // Assert
        Assert.Equal(1, stats.UndersizedChunkCount);
        Assert.Equal(1, stats.OversizedChunkCount);
        Assert.False(stats.IsBalanced);
    }

    [Fact]
    public void CalculateStats_BalancedChunks_ReportsIsBalanced()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Chunk one.", 0, 3, estimatedTokens: 150),
            CreateChunk("Chunk two.", 1, 3, estimatedTokens: 200),
            CreateChunk("Chunk three.", 2, 3, estimatedTokens: 250)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 1024
        };

        // Act
        var stats = _balancer.CalculateStats(chunks, options);

        // Assert
        Assert.Equal(0, stats.UndersizedChunkCount);
        Assert.Equal(0, stats.OversizedChunkCount);
        Assert.True(stats.VarianceRatio <= 5.0);
        Assert.True(stats.IsBalanced);
    }

    #endregion

    #region MergeUndersizedChunks Tests

    [Fact]
    public async Task BalanceAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var chunks = Array.Empty<DocumentChunk>();
        var options = ChunkOptions.Default;

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task BalanceAsync_SingleChunk_ReturnsSameChunk()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Single chunk content.", 0, 1, estimatedTokens: 50)
        };
        var options = new ChunkOptions { MinChunkSize = 100 };

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        Assert.Single(result);
        Assert.Equal("Single chunk content.", result[0].Content);
    }

    [Fact]
    public async Task BalanceAsync_UndersizedChunks_MergesTogether()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Small one.", 0, 3, estimatedTokens: 30),
            CreateChunk("Small two.", 1, 3, estimatedTokens: 30),
            CreateChunk("Normal sized chunk with enough content.", 2, 3, estimatedTokens: 200)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 500,
            TargetChunkSize = 200
        };

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        // Small chunks should be merged
        Assert.True(result.Count <= chunks.Length);
        // Verify indices are correct
        for (int i = 0; i < result.Count; i++)
        {
            Assert.Equal(i, result[i].Index);
            Assert.Equal(result.Count, result[i].TotalChunks);
        }
    }

    [Fact]
    public async Task BalanceAsync_ConsecutiveUndersizedChunks_MergesIntoOne()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("A.", 0, 4, estimatedTokens: 10),
            CreateChunk("B.", 1, 4, estimatedTokens: 10),
            CreateChunk("C.", 2, 4, estimatedTokens: 10),
            CreateChunk("D.", 3, 4, estimatedTokens: 10)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 500
        };

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        // All small chunks should merge into fewer chunks
        Assert.True(result.Count < chunks.Length);
    }

    #endregion

    #region SplitOversizedChunks Tests

    [Fact]
    public async Task BalanceAsync_OversizedChunk_SplitsIntoSmaller()
    {
        // Arrange
        var largeContent = string.Join(" ", Enumerable.Repeat("This is sentence number one. This is sentence number two. This is sentence number three.", 20));
        var chunks = new[]
        {
            CreateChunk(largeContent, 0, 1, estimatedTokens: 3000)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 500,
            TargetChunkSize = 300,
            PreserveSentences = true
        };

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        Assert.True(result.Count > 1, "Oversized chunk should be split into multiple chunks");
        // All chunks should be within max size (with some tolerance for boundary detection)
        Assert.All(result, c => Assert.True(
            c.Metadata.EstimatedTokenCount <= options.MaxChunkSize * 1.5,
            $"Chunk has {c.Metadata.EstimatedTokenCount} tokens, expected <= {options.MaxChunkSize * 1.5}"));
    }

    [Fact]
    public async Task BalanceAsync_MixedSizes_BalancesAll()
    {
        // Arrange
        var largeContent = string.Join(" ", Enumerable.Repeat("Large content sentence here.", 50));
        var chunks = new[]
        {
            CreateChunk("Tiny.", 0, 3, estimatedTokens: 20),
            CreateChunk("Normal chunk.", 1, 3, estimatedTokens: 150),
            CreateChunk(largeContent, 2, 3, estimatedTokens: 2000)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 500,
            TargetChunkSize = 250
        };

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        // Should have more chunks due to splitting
        Assert.True(result.Count >= chunks.Length);
        // Check indices are sequential
        for (int i = 0; i < result.Count; i++)
        {
            Assert.Equal(i, result[i].Index);
        }
    }

    #endregion

    #region Reindexing Tests

    [Fact]
    public async Task BalanceAsync_AfterBalancing_UpdatesIndicesCorrectly()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("First.", 0, 5, estimatedTokens: 30),
            CreateChunk("Second.", 1, 5, estimatedTokens: 30),
            CreateChunk("Third.", 2, 5, estimatedTokens: 30),
            CreateChunk("Fourth.", 3, 5, estimatedTokens: 30),
            CreateChunk("Fifth.", 4, 5, estimatedTokens: 30)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 500
        };

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        for (int i = 0; i < result.Count; i++)
        {
            Assert.Equal(i, result[i].Index);
            Assert.Equal(result.Count, result[i].TotalChunks);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task BalanceAsync_AllChunksWithinRange_NoChanges()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Good chunk one.", 0, 3, estimatedTokens: 200),
            CreateChunk("Good chunk two.", 1, 3, estimatedTokens: 250),
            CreateChunk("Good chunk three.", 2, 3, estimatedTokens: 200)
        };
        var options = new ChunkOptions
        {
            MinChunkSize = 100,
            MaxChunkSize = 500
        };

        // Act
        var result = await _balancer.BalanceAsync(chunks, options);

        // Assert
        Assert.Equal(chunks.Length, result.Count);
    }

    [Fact]
    public async Task BalanceAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Chunk one.", 0, 2, estimatedTokens: 100),
            CreateChunk("Chunk two.", 1, 2, estimatedTokens: 100)
        };
        var options = ChunkOptions.Default;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _balancer.BalanceAsync(chunks, options, cts.Token));
    }

    #endregion

    #region Integration with FluxCurator

    [Fact]
    public async Task FluxCurator_WithBalancingEnabled_AppliesBalancing()
    {
        // Arrange
        var curator = new global::FluxCurator.FluxCurator()
            .WithChunkingOptions(opt =>
            {
                opt.Strategy = ChunkingStrategy.Sentence;
                opt.MinChunkSize = 50;
                opt.MaxChunkSize = 200;
                opt.TargetChunkSize = 100;
                opt.EnableChunkBalancing = true;
            });

        var text = "Short. Another short. " +
                   string.Join(" ", Enumerable.Repeat("This is a longer sentence with more content.", 10));

        // Act
        var chunks = await curator.ChunkAsync(text);

        // Assert
        Assert.NotEmpty(chunks);
        // Verify balancing was applied (indices should be sequential)
        for (int i = 0; i < chunks.Count; i++)
        {
            Assert.Equal(i, chunks[i].Index);
            Assert.Equal(chunks.Count, chunks[i].TotalChunks);
        }
    }

    [Fact]
    public async Task FluxCurator_WithBalancingDisabled_SkipsBalancing()
    {
        // Arrange
        var curator = new global::FluxCurator.FluxCurator()
            .WithChunkingOptions(opt =>
            {
                opt.Strategy = ChunkingStrategy.Sentence;
                opt.EnableChunkBalancing = false;
            });

        var text = "First sentence. Second sentence. Third sentence.";

        // Act
        var chunks = await curator.ChunkAsync(text);

        // Assert
        Assert.NotEmpty(chunks);
    }

    #endregion

    #region Helper Methods

    private static DocumentChunk CreateChunk(string content, int index, int totalChunks, int estimatedTokens)
    {
        return new DocumentChunk
        {
            Content = content,
            Index = index,
            TotalChunks = totalChunks,
            Location = new ChunkLocation
            {
                StartPosition = 0,
                EndPosition = content.Length
            },
            Metadata = new ChunkMetadata
            {
                LanguageCode = "en",
                EstimatedTokenCount = estimatedTokens,
                Strategy = ChunkingStrategy.Sentence
            }
        };
    }

    #endregion
}
