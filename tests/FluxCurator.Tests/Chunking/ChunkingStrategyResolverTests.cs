namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;

public class ChunkingStrategyResolverTests
{
    [Fact]
    public void Resolve_NonAutoStrategy_ReturnsUnchanged()
    {
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Paragraph };

        var resolved = ChunkingStrategyResolver.Resolve("any text", options);

        Assert.Equal(ChunkingStrategy.Paragraph, resolved);
    }

    [Fact]
    public void Resolve_ShortText_ReturnsSentence()
    {
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Auto, TargetChunkSize = 512 };

        var resolved = ChunkingStrategyResolver.Resolve("A short sentence.", options);

        Assert.Equal(ChunkingStrategy.Sentence, resolved);
    }

    [Fact]
    public void Resolve_ManyParagraphs_ReturnsParagraph()
    {
        var paragraph = string.Join(" ", Enumerable.Repeat("A paragraph sentence with several words in it.", 6));
        var text = string.Join("\n\n", Enumerable.Repeat(paragraph, 6));
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Auto, TargetChunkSize = 50 };

        var resolved = ChunkingStrategyResolver.Resolve(text, options);

        Assert.Equal(ChunkingStrategy.Paragraph, resolved);
    }

    [Fact]
    public void Resolve_EmptyText_ReturnsSentence()
    {
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Auto };

        Assert.Equal(ChunkingStrategy.Sentence, ChunkingStrategyResolver.Resolve("", options));
    }

    [Fact]
    public void Resolve_MatchesOrchestratorBehavior_LongSentenceStructuredText()
    {
        // Long text without paragraph breaks but with many sentences → Sentence
        var text = string.Join(" ", Enumerable.Repeat("This is one sentence with enough words to add up tokens.", 20));
        var options = new ChunkOptions { Strategy = ChunkingStrategy.Auto, TargetChunkSize = 50 };

        var resolved = ChunkingStrategyResolver.Resolve(text, options);

        Assert.Equal(ChunkingStrategy.Sentence, resolved);
    }
}
