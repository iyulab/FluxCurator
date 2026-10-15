using FluxCurator.Core.Core;
using Microsoft.Extensions.AI;

namespace FluxCurator.Infrastructure.Embedding;

/// <summary>
/// An <see cref="IEmbedder"/> over a <c>Microsoft.Extensions.AI</c> embedding generator, so semantic chunking runs on any
/// generator (OpenAI, Azure OpenAI, Ollama, LMSupply's <c>LocalEmbeddingGenerator</c>, IronHive's adapter) without an
/// adapter class.
/// </summary>
/// <remarks>
/// <para>
/// Semantic chunking compares sentences with each other, so it uses one generator (a symmetric role — for an asymmetric
/// model, the passage side). The embedder does not dispose the generator.
/// </para>
/// <para>
/// The dimension is read from the generator's <see cref="EmbeddingGeneratorMetadata"/> when it is needed, not at
/// construction, so the embedder can be built (for example by a container that registers it for every host) over a
/// generator that does not report one: only embedding with it fails, naming the dimension to pass.
/// </para>
/// </remarks>
public sealed class EmbeddingGeneratorEmbedder : IEmbedder
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly int? _dimensions;

    /// <summary>Creates the embedder.</summary>
    /// <param name="generator">The generator.</param>
    /// <param name="dimensions">
    /// The vector size, when the generator's <see cref="EmbeddingGeneratorMetadata.DefaultModelDimensions"/> is null; when
    /// both are set they must agree.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="dimensions"/> is not positive.</exception>
    public EmbeddingGeneratorEmbedder(IEmbeddingGenerator<string, Embedding<float>> generator, int? dimensions = null)
    {
        ArgumentNullException.ThrowIfNull(generator);
        if (dimensions is <= 0)
            throw new ArgumentException($"The dimension must be positive (got {dimensions}).", nameof(dimensions));
        _generator = generator;
        _dimensions = dimensions;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">No dimension is known, or the given one contradicts the generator's.</exception>
    public int EmbeddingDimension
    {
        get
        {
            var metadata = _generator.GetService<EmbeddingGeneratorMetadata>();
            var reported = metadata?.DefaultModelDimensions;
            var model = metadata?.DefaultModelId ?? "(unnamed model)";
            if (_dimensions is int set && reported is int known && set != known)
            {
                throw new InvalidOperationException(
                    $"The dimension given is {set} but the generator reports {known} for model '{model}'.");
            }
            var dimension = _dimensions ?? reported ?? throw new InvalidOperationException(
                $"The embedding generator does not report a dimension for model '{model}'; pass the model's dimension " +
                "(EmbeddingGeneratorEmbedder's dimensions argument, or AddEmbeddingGeneratorEmbedder(dimensions)).");
            return dimension > 0
                ? dimension
                : throw new InvalidOperationException($"The embedding generator reports dimension {dimension} for model '{model}'.");
        }
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var expected = EmbeddingDimension;
        var generated = await _generator.GenerateAsync([text], options: null, cancellationToken).ConfigureAwait(false);
        if (generated.Count != 1)
            throw new InvalidOperationException($"The embedding generator returned {generated.Count} embedding(s) for one input.");
        return Checked(generated[0], expected);
    }

    /// <summary>Embeds every text in one generator call.</summary>
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var inputs = texts.ToList();
        if (inputs.Count == 0)
            return [];

        var expected = EmbeddingDimension;
        var generated = await _generator.GenerateAsync(inputs, options: null, cancellationToken).ConfigureAwait(false);
        if (generated.Count != inputs.Count)
        {
            throw new InvalidOperationException(
                $"The embedding generator returned {generated.Count} embedding(s) for {inputs.Count} input(s). " +
                "Results are positional, so a partial set cannot be matched to its inputs.");
        }
        return [.. generated.Select(e => Checked(e, expected))];
    }

    /// <summary>Cosine similarity; 0 when either vector is zero.</summary>
    public float CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        ArgumentNullException.ThrowIfNull(embedding1);
        ArgumentNullException.ThrowIfNull(embedding2);
        if (embedding1.Length != embedding2.Length)
            throw new ArgumentException($"The embeddings differ in length ({embedding1.Length} vs {embedding2.Length}).", nameof(embedding2));

        double dot = 0, norm1 = 0, norm2 = 0;
        for (var i = 0; i < embedding1.Length; i++)
        {
            dot += embedding1[i] * embedding2[i];
            norm1 += embedding1[i] * embedding1[i];
            norm2 += embedding2[i] * embedding2[i];
        }
        return norm1 == 0 || norm2 == 0 ? 0f : (float)(dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2)));
    }

    private static float[] Checked(Embedding<float> embedding, int expected)
    {
        if (embedding.Vector.Length != expected)
        {
            throw new InvalidOperationException(
                $"The embedding generator returned a {embedding.Vector.Length}-dimension vector; expected {expected}.");
        }
        return embedding.Vector.ToArray();
    }
}
