using FluxCurator.Core.Core;
using Microsoft.Extensions.AI;

namespace FluxCurator.Infrastructure.Embedding;

/// <summary>
/// An <see cref="IEmbedder"/> over a <c>Microsoft.Extensions.AI</c> embedding generator, so semantic chunking runs on any
/// generator (OpenAI, Azure OpenAI, Ollama, LMSupply's <c>LocalEmbeddingGenerator</c>, IronHive's adapter) without an
/// adapter class.
/// </summary>
/// <remarks>
/// Semantic chunking compares sentences with each other, so it uses one generator (a symmetric role — for an asymmetric
/// model, the passage side). The embedder does not dispose the generator.
/// </remarks>
public sealed class EmbeddingGeneratorEmbedder : IEmbedder
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    /// <summary>Creates the embedder.</summary>
    /// <param name="generator">The generator.</param>
    /// <param name="dimensions">
    /// The vector size, when the generator's <see cref="EmbeddingGeneratorMetadata.DefaultModelDimensions"/> is null; when
    /// both are set they must agree.
    /// </param>
    /// <exception cref="ArgumentException">No dimension is known, or the two disagree.</exception>
    public EmbeddingGeneratorEmbedder(IEmbeddingGenerator<string, Embedding<float>> generator, int? dimensions = null)
    {
        ArgumentNullException.ThrowIfNull(generator);
        _generator = generator;

        var metadata = generator.GetService<EmbeddingGeneratorMetadata>();
        var reported = metadata?.DefaultModelDimensions;
        var model = metadata?.DefaultModelId ?? "(unnamed model)";
        if (dimensions is int set && reported is int known && set != known)
        {
            throw new ArgumentException(
                $"The dimension given is {set} but the generator reports {known} for model '{model}'.", nameof(dimensions));
        }
        EmbeddingDimension = dimensions ?? reported ?? throw new ArgumentException(
            $"The embedding generator does not report a dimension for model '{model}'; pass the model's dimension.",
            nameof(dimensions));
        if (EmbeddingDimension <= 0)
            throw new ArgumentException($"The dimension must be positive (got {EmbeddingDimension}).", nameof(dimensions));
    }

    /// <inheritdoc />
    public int EmbeddingDimension { get; }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var generated = await _generator.GenerateAsync([text], options: null, cancellationToken).ConfigureAwait(false);
        if (generated.Count != 1)
            throw new InvalidOperationException($"The embedding generator returned {generated.Count} embedding(s) for one input.");
        return Checked(generated[0]);
    }

    /// <summary>Embeds every text in one generator call.</summary>
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var inputs = texts.ToList();
        if (inputs.Count == 0)
            return [];

        var generated = await _generator.GenerateAsync(inputs, options: null, cancellationToken).ConfigureAwait(false);
        if (generated.Count != inputs.Count)
        {
            throw new InvalidOperationException(
                $"The embedding generator returned {generated.Count} embedding(s) for {inputs.Count} input(s). " +
                "Results are positional, so a partial set cannot be matched to its inputs.");
        }
        return [.. generated.Select(Checked)];
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

    private float[] Checked(Embedding<float> embedding)
    {
        if (embedding.Vector.Length != EmbeddingDimension)
        {
            throw new InvalidOperationException(
                $"The embedding generator returned a {embedding.Vector.Length}-dimension vector; expected {EmbeddingDimension}.");
        }
        return embedding.Vector.ToArray();
    }
}
