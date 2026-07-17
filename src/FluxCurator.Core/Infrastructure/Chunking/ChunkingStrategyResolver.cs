namespace FluxCurator.Core.Infrastructure.Chunking;

using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.Languages;

/// <summary>
/// Content-based resolution of <see cref="ChunkingStrategy.Auto"/> to a concrete strategy.
/// Single source of truth — both the FluxCurator orchestrator and external consumers
/// (e.g. FileFlux's ProcessAsync path) resolve Auto through this class, so "Auto" means
/// the same thing everywhere instead of silently degrading to a fixed default.
/// </summary>
public static class ChunkingStrategyResolver
{
    /// <summary>
    /// Resolves <see cref="ChunkingStrategy.Auto"/> to a concrete strategy by analyzing
    /// the text's size and structure. Non-Auto strategies are returned unchanged.
    /// </summary>
    /// <param name="text">The text that will be chunked.</param>
    /// <param name="options">Chunking options (uses <see cref="ChunkOptions.TargetChunkSize"/>).</param>
    public static ChunkingStrategy Resolve(string text, ChunkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Strategy != ChunkingStrategy.Auto)
            return options.Strategy;

        if (string.IsNullOrWhiteSpace(text))
            return ChunkingStrategy.Sentence;

        var profile = LanguageProfileRegistry.Instance.DetectProfile(text);
        var tokenCount = profile.EstimateTokenCount(text);

        // For short texts, use sentence chunking
        if (tokenCount <= options.TargetChunkSize * 2)
            return ChunkingStrategy.Sentence;

        // Check paragraph structure
        var paragraphs = profile.FindParagraphBoundaries(text);
        if (paragraphs.Count > 3)
            return ChunkingStrategy.Paragraph;

        // Check sentence structure
        var sentences = profile.FindSentenceBoundaries(text);
        if (sentences.Count > 5)
            return ChunkingStrategy.Sentence;

        // Default to token chunking for unstructured text
        return ChunkingStrategy.Token;
    }
}
