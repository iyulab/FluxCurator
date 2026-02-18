namespace FluxCurator.Tests.Factory;

using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;
using global::FluxCurator.Infrastructure.Chunking;
using NSubstitute;

public class ChunkerFactoryTests
{
    private static readonly float[] s_testEmbedding = [0.1f, 0.2f, 0.3f];

    [Fact]
    public void CreateChunker_SentenceStrategy_ReturnsSentenceChunker()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var chunker = factory.CreateChunker(ChunkingStrategy.Sentence);

        // Assert
        Assert.NotNull(chunker);
        Assert.Equal("Sentence", chunker.StrategyName);
    }

    [Fact]
    public void CreateChunker_ParagraphStrategy_ReturnsParagraphChunker()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var chunker = factory.CreateChunker(ChunkingStrategy.Paragraph);

        // Assert
        Assert.NotNull(chunker);
        Assert.Equal("Paragraph", chunker.StrategyName);
    }

    [Fact]
    public void CreateChunker_TokenStrategy_ReturnsTokenChunker()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var chunker = factory.CreateChunker(ChunkingStrategy.Token);

        // Assert
        Assert.NotNull(chunker);
        Assert.Equal("Token", chunker.StrategyName);
    }

    [Fact]
    public void CreateChunker_HierarchicalStrategy_ReturnsHierarchicalChunker()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var chunker = factory.CreateChunker(ChunkingStrategy.Hierarchical);

        // Assert
        Assert.NotNull(chunker);
        Assert.Equal("Hierarchical", chunker.StrategyName);
    }

    [Fact]
    public void CreateChunker_AutoStrategy_ReturnsDefaultChunker()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var chunker = factory.CreateChunker(ChunkingStrategy.Auto);

        // Assert
        Assert.NotNull(chunker);
    }

    [Fact]
    public void CreateChunker_SemanticWithoutEmbedder_ThrowsArgumentException()
    {
        // Arrange
        var factory = new ChunkerFactory(); // No embedder provided

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            factory.CreateChunker(ChunkingStrategy.Semantic));
    }

    [Fact]
    public void CreateChunker_SemanticWithEmbedder_ReturnsSemanticChunker()
    {
        // Arrange
        var mockEmbedder = Substitute.For<IEmbedder>();
        mockEmbedder
            .GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(s_testEmbedding);
        mockEmbedder
            .EmbeddingDimension
            .Returns(3);

        var factory = new ChunkerFactory(mockEmbedder);

        // Act
        var chunker = factory.CreateChunker(ChunkingStrategy.Semantic);

        // Assert
        Assert.NotNull(chunker);
        Assert.Equal("Semantic", chunker.StrategyName);
    }

    [Fact]
    public void IsStrategyAvailable_BuiltInStrategies_ReturnsTrue()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act & Assert
        Assert.True(factory.IsStrategyAvailable(ChunkingStrategy.Sentence));
        Assert.True(factory.IsStrategyAvailable(ChunkingStrategy.Paragraph));
        Assert.True(factory.IsStrategyAvailable(ChunkingStrategy.Token));
        Assert.True(factory.IsStrategyAvailable(ChunkingStrategy.Hierarchical));
        Assert.True(factory.IsStrategyAvailable(ChunkingStrategy.Auto));
    }

    [Fact]
    public void IsStrategyAvailable_SemanticWithoutEmbedder_ReturnsFalse()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act & Assert
        Assert.False(factory.IsStrategyAvailable(ChunkingStrategy.Semantic));
    }

    [Fact]
    public void IsStrategyAvailable_SemanticWithEmbedder_ReturnsTrue()
    {
        // Arrange
        var mockEmbedder = Substitute.For<IEmbedder>();
        var factory = new ChunkerFactory(mockEmbedder);

        // Act & Assert
        Assert.True(factory.IsStrategyAvailable(ChunkingStrategy.Semantic));
    }

    [Fact]
    public void AvailableStrategies_WithoutEmbedder_ExcludesSemantic()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var strategies = factory.AvailableStrategies;

        // Assert
        Assert.Contains(ChunkingStrategy.Sentence, strategies);
        Assert.Contains(ChunkingStrategy.Paragraph, strategies);
        Assert.Contains(ChunkingStrategy.Token, strategies);
        Assert.Contains(ChunkingStrategy.Hierarchical, strategies);
        Assert.DoesNotContain(ChunkingStrategy.Semantic, strategies);
    }

    [Fact]
    public void AvailableStrategies_WithEmbedder_IncludesSemantic()
    {
        // Arrange
        var mockEmbedder = Substitute.For<IEmbedder>();
        var factory = new ChunkerFactory(mockEmbedder);

        // Act
        var strategies = factory.AvailableStrategies;

        // Assert
        Assert.Contains(ChunkingStrategy.Semantic, strategies);
    }

    [Fact]
    public void RegisterChunker_CustomStrategy_CanBeCreated()
    {
        // Arrange
        var factory = new ChunkerFactory();
        var customChunker = new SentenceChunker(); // Using existing chunker for test

        // Override sentence strategy with custom chunker
        factory.RegisterChunker(ChunkingStrategy.Sentence, () => customChunker);

        // Act
        var chunker = factory.CreateChunker(ChunkingStrategy.Sentence);

        // Assert
        Assert.Same(customChunker, chunker);
    }

    [Fact]
    public async Task CreateChunker_WorksWithChunking()
    {
        // Arrange
        var factory = new ChunkerFactory();
        var chunker = factory.CreateChunker(ChunkingStrategy.Sentence);
        var text = "Hello world. This is a test.";

        // Act
        var chunks = await chunker.ChunkAsync(text, ChunkOptions.Default);

        // Assert
        Assert.NotEmpty(chunks);
    }

    [Fact]
    public void CreateChunker_MultipleCallsSameStrategy_ReturnsSameType()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var chunker1 = factory.CreateChunker(ChunkingStrategy.Sentence);
        var chunker2 = factory.CreateChunker(ChunkingStrategy.Sentence);

        // Assert
        Assert.Equal(chunker1.GetType(), chunker2.GetType());
    }

    [Fact]
    public void Constructor_WithNullEmbedder_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new ChunkerFactory(null));
        Assert.Null(exception);
    }

    [Fact]
    public void DefaultChunker_ReturnsSentenceChunker()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var chunker = factory.DefaultChunker;

        // Assert
        Assert.NotNull(chunker);
        Assert.Equal("Sentence", chunker.StrategyName);
    }

    [Fact]
    public void TryCreateChunker_ValidStrategy_ReturnsTrue()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var result = factory.TryCreateChunker(ChunkingStrategy.Sentence, out var chunker);

        // Assert
        Assert.True(result);
        Assert.NotNull(chunker);
    }

    [Fact]
    public void TryCreateChunker_UnavailableStrategy_ReturnsFalse()
    {
        // Arrange
        var factory = new ChunkerFactory(); // No embedder, so semantic is unavailable

        // Act
        var result = factory.TryCreateChunker(ChunkingStrategy.Semantic, out var chunker);

        // Assert
        Assert.False(result);
        Assert.Null(chunker);
    }

    [Fact]
    public void TryCreateChunker_AutoStrategy_ReturnsTrue()
    {
        // Arrange
        var factory = new ChunkerFactory();

        // Act
        var result = factory.TryCreateChunker(ChunkingStrategy.Auto, out var chunker);

        // Assert
        Assert.True(result);
        Assert.NotNull(chunker);
    }
}
