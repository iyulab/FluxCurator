namespace FluxCurator.Tests;

using System.Collections.Concurrent;
using global::FluxCurator.Core.Core;
using global::FluxCurator.Core.Domain;
using global::FluxCurator.Infrastructure.Embedding;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Semantic chunking runs on a <c>Microsoft.Extensions.AI</c> embedding generator directly — no <see cref="IEmbedder"/> to write.
/// </summary>
public class EmbeddingGeneratorEmbedderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SemanticChunking_EmbedsThroughTheGenerator_AndSplitsWhereTheTopicChanges()
    {
        // Sentences about cats lie on one axis, sentences about the stock market on the other: a boundary between them.
        var generator = new AxisGenerator(text => text.Contains("cat", StringComparison.OrdinalIgnoreCase) ? 0 : 1);
        var curator = new Curator()
            .UseEmbeddingGenerator(generator)
            .WithChunkingOptions(o =>
            {
                o.Strategy = ChunkingStrategy.Semantic;
                o.MinChunkSize = 5;
                o.TargetChunkSize = 40;
            });
        const string text =
            "The cat sleeps on the sofa all afternoon. A cat purrs when it is content. Every cat likes a warm windowsill. " +
            "The stock market fell sharply on Monday. Investors sold shares in the morning. Bond yields rose as prices dropped.";

        var chunks = await curator.ChunkAsync(text, Ct);

        Assert.NotEmpty(generator.Inputs);
        Assert.True(chunks.Count >= 2, $"expected a boundary at the topic change, got {chunks.Count} chunk(s)");
        Assert.Contains(chunks, c => c.Content.Contains("cat", StringComparison.Ordinal) && !c.Content.Contains("stock", StringComparison.Ordinal));
    }

    [Fact]
    public void DependencyInjection_RegistersAnEmbedderOverTheContainersGenerator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new AxisGenerator(_ => 0));
        services.AddEmbeddingGeneratorEmbedder();
        services.AddFluxCurator();
        using var provider = services.BuildServiceProvider();

        var embedder = provider.GetRequiredService<IEmbedder>();

        Assert.IsType<EmbeddingGeneratorEmbedder>(embedder);
        Assert.Equal(AxisGenerator.Dimensions, embedder.EmbeddingDimension);
    }

    [Fact]
    public void TheDimension_ComesFromTheGeneratorOrTheArgument_AndTheyMustAgree()
    {
        Assert.Equal(8, new EmbeddingGeneratorEmbedder(new AxisGenerator(_ => 0, reportDimensions: false), dimensions: 8).EmbeddingDimension);
        var unknown = Assert.Throws<ArgumentException>(() => new EmbeddingGeneratorEmbedder(new AxisGenerator(_ => 0, reportDimensions: false)));
        Assert.Contains("does not report a dimension", unknown.Message, StringComparison.Ordinal);
        var contradicting = Assert.Throws<ArgumentException>(() => new EmbeddingGeneratorEmbedder(new AxisGenerator(_ => 0), dimensions: 3));
        Assert.Contains("reports 4", contradicting.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ABatch_IsOneGeneratorCall_AndAShortResultThrows()
    {
        var generator = new AxisGenerator(_ => 0);
        var vectors = await new EmbeddingGeneratorEmbedder(generator).GenerateEmbeddingsAsync(["a", "b", "c"], Ct);

        Assert.Equal(1, generator.CallCount);
        Assert.Equal(3, vectors.Count);

        var shorted = new EmbeddingGeneratorEmbedder(new AxisGenerator(_ => 0, dropLast: true));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => shorted.GenerateEmbeddingsAsync(["a", "b"], Ct));
        Assert.Contains("positional", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Similarity_IsTheCosine()
    {
        var embedder = new EmbeddingGeneratorEmbedder(new AxisGenerator(_ => 0));

        Assert.Equal(1f, embedder.CalculateSimilarity([1, 0, 0, 0], [2, 0, 0, 0]), 5);
        Assert.Equal(0f, embedder.CalculateSimilarity([1, 0, 0, 0], [0, 1, 0, 0]), 5);
        Assert.Equal(0f, embedder.CalculateSimilarity([0, 0, 0, 0], [1, 0, 0, 0]), 5);
    }

    /// <summary>Puts each text on one axis of a 4-dimension space and records what it embedded.</summary>
    private sealed class AxisGenerator(Func<string, int> axis, bool reportDimensions = true, bool dropLast = false)
        : IEmbeddingGenerator<string, Embedding<float>>
    {
        public const int Dimensions = 4;

        public ConcurrentQueue<string> Inputs { get; } = new();
        public int CallCount;

        private readonly EmbeddingGeneratorMetadata _metadata =
            new("test-provider", providerUri: null, defaultModelId: "test-model", defaultModelDimensions: reportDimensions ? Dimensions : null);

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CallCount);
            var list = values.ToList();
            foreach (var v in list)
                Inputs.Enqueue(v);
            var vectors = list.Select(v =>
            {
                var vector = new float[Dimensions];
                vector[axis(v)] = 1f;
                return new Embedding<float>(vector);
            }).ToList();
            if (dropLast && vectors.Count > 0)
                vectors.RemoveAt(vectors.Count - 1);
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(vectors));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata
            : serviceKey is null && serviceType.IsInstanceOfType(this) ? this
            : null;

        public void Dispose() { }
    }
}
