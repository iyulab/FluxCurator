namespace FluxCurator.Core.Infrastructure.Chunking;

using System.Text;
using System.Text.RegularExpressions;
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Chunks text while preserving document structure through hierarchical sections.
/// Optimal for structured documents with headers and nested content.
/// </summary>
public sealed partial class HierarchicalChunker : ChunkerBase
{
    /// <summary>
    /// Metadata key for parent chunk ID.
    /// </summary>
    public const string ParentIdKey = "ParentId";

    /// <summary>
    /// Metadata key for child chunk IDs.
    /// </summary>
    public const string ChildIdsKey = "ChildIds";

    /// <summary>
    /// Metadata key for hierarchy level (0 = root, 1 = first level, etc.).
    /// </summary>
    public const string HierarchyLevelKey = "HierarchyLevel";

    /// <summary>
    /// Metadata key for section title.
    /// </summary>
    public const string SectionTitleKey = "SectionTitle";

    /// <inheritdoc/>
    public override string StrategyName => "Hierarchical";

    /// <inheritdoc/>
    protected override ChunkingStrategy GetChunkingStrategy() => ChunkingStrategy.Hierarchical;

    /// <inheritdoc/>
    public override Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult<IReadOnlyList<DocumentChunk>>([]);

        var profile = GetLanguageProfile(text, options);
        var sections = ParseSections(text);
        var chunks = new List<DocumentChunk>();
        var parentStack = new Stack<DocumentChunk>();

        foreach (var section in sections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Pop parent stack until we find a parent with lower level
            while (parentStack.Count > 0)
            {
                var parentLevel = GetHierarchyLevel(parentStack.Peek());
                if (parentLevel < section.Level)
                    break;
                parentStack.Pop();
            }

            // Create chunk for this section
            var sectionChunks = CreateSectionChunks(
                section,
                parentStack.Count > 0 ? parentStack.Peek() : null,
                chunks.Count,
                profile,
                options);

            foreach (var chunk in sectionChunks)
            {
                chunks.Add(chunk);
            }

            // Push main section chunk to parent stack if it has a title
            if (sectionChunks.Count > 0 && !string.IsNullOrEmpty(section.Title))
            {
                parentStack.Push(sectionChunks[0]);
            }
        }

        // Update total chunk count and child references
        foreach (var chunk in chunks)
        {
            chunk.TotalChunks = chunks.Count;
        }

        UpdateChildReferences(chunks);

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
    }

    /// <inheritdoc/>
    public override int EstimateChunkCount(string text, ChunkOptions options)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var profile = GetLanguageProfile(text, options);
        var sections = ParseSections(text);
        var totalTokens = 0;
        var estimatedChunks = 0;

        foreach (var section in sections)
        {
            var sectionTokens = profile.EstimateTokenCount(section.Content);
            totalTokens += sectionTokens;

            // Estimate chunks needed for this section
            if (sectionTokens <= options.MaxChunkSize)
            {
                estimatedChunks++;
            }
            else
            {
                var effectiveSize = options.TargetChunkSize - options.OverlapSize;
                if (effectiveSize <= 0)
                    effectiveSize = options.TargetChunkSize / 2;
                estimatedChunks += (int)Math.Ceiling((double)sectionTokens / effectiveSize);
            }
        }

        return Math.Max(1, estimatedChunks);
    }

    /// <summary>
    /// Parses the text into hierarchical sections based on markdown headers.
    /// </summary>
    private List<DocumentSection> ParseSections(string text)
    {
        var sections = new List<DocumentSection>();
        var headerPattern = HeaderRegex();
        var matches = headerPattern.Matches(text);

        if (matches.Count == 0)
        {
            // No headers found, treat entire text as single section
            sections.Add(new DocumentSection
            {
                Title = null,
                Content = text,
                Level = 0,
                StartPosition = 0,
                EndPosition = text.Length
            });
            return sections;
        }

        // Add content before first header if exists
        var firstMatch = matches[0];
        if (firstMatch.Index > 0)
        {
            var preContent = text[..firstMatch.Index].Trim();
            if (!string.IsNullOrWhiteSpace(preContent))
            {
                sections.Add(new DocumentSection
                {
                    Title = null,
                    Content = preContent,
                    Level = 0,
                    StartPosition = 0,
                    EndPosition = firstMatch.Index
                });
            }
        }

        // Process each header and its content
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var headerLevel = match.Groups[1].Value.Length; // Count # characters
            var headerTitle = match.Groups[2].Value.Trim();

            // Find content end (start of next header or end of text)
            var contentStart = match.Index + match.Length;
            var contentEnd = i + 1 < matches.Count
                ? matches[i + 1].Index
                : text.Length;

            var content = text[contentStart..contentEnd].Trim();
            var fullContent = match.Value + "\n" + content;

            sections.Add(new DocumentSection
            {
                Title = headerTitle,
                Content = fullContent.Trim(),
                Level = headerLevel,
                StartPosition = match.Index,
                EndPosition = contentEnd,
                HeaderLine = match.Value
            });
        }

        return sections;
    }

    /// <summary>
    /// Creates chunks for a section, splitting if necessary.
    /// </summary>
    private List<DocumentChunk> CreateSectionChunks(
        DocumentSection section,
        DocumentChunk? parent,
        int startIndex,
        ILanguageProfile profile,
        ChunkOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var sectionTokens = profile.EstimateTokenCount(section.Content);

        // Build section path
        var sectionPath = BuildSectionPath(parent, section.Title);

        if (sectionTokens <= options.MaxChunkSize)
        {
            // Section fits in one chunk
            var chunk = CreateHierarchicalChunk(
                content: section.Content,
                index: startIndex,
                startPosition: section.StartPosition,
                endPosition: section.EndPosition,
                profile: profile,
                options: options,
                level: section.Level,
                title: section.Title,
                sectionPath: sectionPath,
                parent: parent);

            chunks.Add(chunk);
        }
        else
        {
            // Section needs to be split
            var splitChunks = SplitSectionContent(
                section,
                parent,
                startIndex,
                profile,
                options,
                sectionPath);

            chunks.AddRange(splitChunks);
        }

        return chunks;
    }

    /// <summary>
    /// Splits section content into multiple chunks while maintaining hierarchy.
    /// </summary>
    private List<DocumentChunk> SplitSectionContent(
        DocumentSection section,
        DocumentChunk? parent,
        int startIndex,
        ILanguageProfile profile,
        ChunkOptions options,
        string? sectionPath)
    {
        var chunks = new List<DocumentChunk>();
        var sentences = profile.FindSentenceBoundaries(section.Content);
        var currentContent = new StringBuilder();
        var currentStart = section.StartPosition;
        var currentTokens = 0;
        string? overlapContent = null;
        var isFirstChunk = true;

        foreach (var boundary in sentences)
        {
            var sentenceStart = currentContent.Length == 0 ? 0 : boundary - (section.Content.Length - currentContent.Length);
            var sentence = section.Content[Math.Max(0, sentenceStart < 0 ? 0 : boundary - options.MaxChunkSize)..boundary];
            var sentenceTokens = profile.EstimateTokenCount(sentence);

            if (currentTokens + sentenceTokens > options.MaxChunkSize && currentContent.Length > 0)
            {
                // Finalize current chunk
                var chunkContent = currentContent.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    // Add section header to first chunk
                    if (isFirstChunk && !string.IsNullOrEmpty(section.HeaderLine))
                    {
                        chunkContent = section.HeaderLine + "\n" + chunkContent;
                        isFirstChunk = false;
                    }

                    var chunk = CreateHierarchicalChunk(
                        content: chunkContent,
                        index: startIndex + chunks.Count,
                        startPosition: currentStart,
                        endPosition: section.StartPosition + boundary,
                        profile: profile,
                        options: options,
                        level: section.Level,
                        title: section.Title,
                        sectionPath: sectionPath,
                        parent: parent,
                        overlapContent: overlapContent);

                    chunks.Add(chunk);

                    // Prepare overlap
                    overlapContent = options.OverlapSize > 0
                        ? ExtractOverlapContent(chunkContent, options.OverlapSize, profile)
                        : null;
                }

                currentContent.Clear();
                currentTokens = 0;
                currentStart = section.StartPosition + boundary;

                // Add overlap to new chunk
                if (!string.IsNullOrEmpty(overlapContent))
                {
                    currentContent.Append(overlapContent);
                    currentTokens = profile.EstimateTokenCount(overlapContent);
                }
            }

            currentContent.Append(section.Content[..boundary].AsSpan(Math.Max(0, boundary - sentence.Length)));
            currentContent.Append(sentence);
            currentTokens += sentenceTokens;
        }

        // Handle remaining content
        if (currentContent.Length > 0)
        {
            var chunkContent = currentContent.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                if (isFirstChunk && !string.IsNullOrEmpty(section.HeaderLine))
                {
                    chunkContent = section.HeaderLine + "\n" + chunkContent;
                }

                var chunk = CreateHierarchicalChunk(
                    content: chunkContent,
                    index: startIndex + chunks.Count,
                    startPosition: currentStart,
                    endPosition: section.EndPosition,
                    profile: profile,
                    options: options,
                    level: section.Level,
                    title: section.Title,
                    sectionPath: sectionPath,
                    parent: parent,
                    overlapContent: overlapContent);

                chunks.Add(chunk);
            }
        }

        // If no chunks created, create one with the full content
        if (chunks.Count == 0)
        {
            var chunk = CreateHierarchicalChunk(
                content: section.Content,
                index: startIndex,
                startPosition: section.StartPosition,
                endPosition: section.EndPosition,
                profile: profile,
                options: options,
                level: section.Level,
                title: section.Title,
                sectionPath: sectionPath,
                parent: parent);

            chunks.Add(chunk);
        }

        return chunks;
    }

    /// <summary>
    /// Creates a chunk with hierarchical metadata.
    /// </summary>
    private DocumentChunk CreateHierarchicalChunk(
        string content,
        int index,
        int startPosition,
        int endPosition,
        ILanguageProfile profile,
        ChunkOptions options,
        int level,
        string? title,
        string? sectionPath,
        DocumentChunk? parent,
        string? overlapContent = null)
    {
        var chunk = CreateChunk(
            content: content,
            index: index,
            totalChunks: 0,
            startPosition: startPosition,
            endPosition: endPosition,
            profile: profile,
            options: options,
            startsAtBoundary: true,
            endsAtBoundary: true,
            overlapContent: overlapContent);

        // Set section path
        chunk.Location.SectionPath = sectionPath;

        // Set hierarchical metadata
        chunk.Metadata.ContainsSectionHeader = !string.IsNullOrEmpty(title);
        chunk.Metadata.Custom ??= new Dictionary<string, object>();
        chunk.Metadata.Custom[HierarchyLevelKey] = level;

        if (!string.IsNullOrEmpty(title))
        {
            chunk.Metadata.Custom[SectionTitleKey] = title;
        }

        if (parent != null)
        {
            chunk.Metadata.Custom[ParentIdKey] = parent.Id;
        }

        // Initialize empty child list
        chunk.Metadata.Custom[ChildIdsKey] = new List<string>();

        // Calculate importance score based on hierarchy level
        // Lower level (closer to root) = higher importance
        chunk.Metadata.QualityScore = Math.Max(0.5f, 1.0f - (level * 0.1f));

        return chunk;
    }

    /// <summary>
    /// Builds the section path from parent and current title.
    /// </summary>
    private static string? BuildSectionPath(DocumentChunk? parent, string? title)
    {
        if (string.IsNullOrEmpty(title))
            return parent?.Location.SectionPath;

        if (parent?.Location.SectionPath == null)
            return title;

        return $"{parent.Location.SectionPath} > {title}";
    }

    /// <summary>
    /// Gets the hierarchy level from chunk metadata.
    /// </summary>
    private static int GetHierarchyLevel(DocumentChunk chunk)
    {
        if (chunk.Metadata.Custom?.TryGetValue(HierarchyLevelKey, out var level) == true)
        {
            return level is int intLevel ? intLevel : 0;
        }
        return 0;
    }

    /// <summary>
    /// Updates child references in parent chunks.
    /// </summary>
    private static void UpdateChildReferences(List<DocumentChunk> chunks)
    {
        var chunkMap = chunks.ToDictionary(c => c.Id);

        foreach (var chunk in chunks)
        {
            if (chunk.Metadata.Custom?.TryGetValue(ParentIdKey, out var parentIdObj) == true &&
                parentIdObj is string parentId &&
                chunkMap.TryGetValue(parentId, out var parent))
            {
                if (parent.Metadata.Custom?.TryGetValue(ChildIdsKey, out var childIdsObj) == true &&
                    childIdsObj is List<string> childIds)
                {
                    childIds.Add(chunk.Id);
                }
            }
        }
    }

    /// <summary>
    /// Regex pattern for markdown headers.
    /// </summary>
    [GeneratedRegex(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeaderRegex();

    /// <summary>
    /// Represents a document section parsed from headers.
    /// </summary>
    private sealed class DocumentSection
    {
        public string? Title { get; init; }
        public required string Content { get; init; }
        public int Level { get; init; }
        public int StartPosition { get; init; }
        public int EndPosition { get; init; }
        public string? HeaderLine { get; init; }
    }
}
