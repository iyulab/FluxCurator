namespace FluxCurator.Tests.Chunking;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Chunking;

public class HierarchicalChunkerTests
{
    private readonly HierarchicalChunker _chunker = new();

    [Fact]
    public async Task ChunkAsync_MarkdownHeaders_CreatesHierarchy()
    {
        // Arrange
        var text = """
            # Main Title

            Introduction content here.

            ## Section 1

            Content for section 1.

            ### Subsection 1.1

            Details for subsection 1.1.

            ## Section 2

            Content for section 2.
            """;
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);

        // Check hierarchy metadata
        var hasHierarchyLevel = chunks.Any(c =>
            c.Metadata.Custom?.ContainsKey(HierarchicalChunker.HierarchyLevelKey) == true);
        Assert.True(hasHierarchyLevel);
    }

    [Fact]
    public async Task ChunkAsync_SetsParentChildRelationships()
    {
        // Arrange
        var text = """
            # Root

            Root content.

            ## Child 1

            Child 1 content.

            ## Child 2

            Child 2 content.
            """;
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);

        // Find chunks with parent references
        var chunksWithParents = chunks.Where(c =>
            c.Metadata.Custom?.ContainsKey(HierarchicalChunker.ParentIdKey) == true &&
            c.Metadata.Custom[HierarchicalChunker.ParentIdKey] != null);

        // Some chunks should have parents (subsections)
        Assert.True(chunksWithParents.Any() || chunks.Count == 1);
    }

    [Fact]
    public async Task ChunkAsync_PlainText_CreatesSingleChunk()
    {
        // Arrange
        var text = "Plain text without any markdown headers. Just regular content.";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.Single(chunks);
        Assert.Equal(0, chunks[0].Metadata.Custom?[HierarchicalChunker.HierarchyLevelKey]);
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
        var text = "# Header\n\nContent";
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.All(chunks, c => Assert.Equal(ChunkingStrategy.Hierarchical, c.Metadata.Strategy));
    }

    [Fact]
    public async Task ChunkAsync_SetsSectionPath()
    {
        // Arrange
        var text = """
            # Main

            Main content.

            ## Sub

            Sub content.
            """;
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        var chunksWithSectionPath = chunks.Where(c => !string.IsNullOrEmpty(c.Location.SectionPath));
        Assert.NotEmpty(chunksWithSectionPath);
    }

    [Fact]
    public async Task ChunkAsync_DeepNesting_HandlesCorrectly()
    {
        // Arrange
        var text = """
            # Level 1

            Content.

            ## Level 2

            Content.

            ### Level 3

            Content.

            #### Level 4

            Content.

            ##### Level 5

            Content.

            ###### Level 6

            Content.
            """;
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);

        // Check that we have various hierarchy levels
        var levels = chunks
            .Where(c => c.Metadata.Custom?.ContainsKey(HierarchicalChunker.HierarchyLevelKey) == true)
            .Select(c => (int)c.Metadata.Custom![HierarchicalChunker.HierarchyLevelKey]!)
            .Distinct()
            .ToList();

        Assert.True(levels.Count > 1);
    }

    [Fact]
    public async Task ChunkAsync_PreserveSectionHeaders_IncludesHeaderInContent()
    {
        // Arrange
        var text = """
            # Important Section

            This is the content of the section.
            """;
        var options = new ChunkOptions
        {
            PreserveSectionHeaders = true
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.NotEmpty(chunks);
        // The section header should be captured in metadata
        var hasTitle = chunks.Any(c =>
            c.Metadata.Custom?.ContainsKey(HierarchicalChunker.SectionTitleKey) == true);
        Assert.True(hasTitle);
    }

    [Fact]
    public async Task ChunkAsync_LargeSection_SplitsIntoMultipleChunks()
    {
        // Arrange
        var longContent = string.Join(" ", Enumerable.Repeat("This is a sentence with content.", 100));
        var text = $"""
            # Large Section

            {longContent}
            """;
        var options = new ChunkOptions
        {
            MaxChunkSize = 100,
            MinChunkSize = 10,
            TargetChunkSize = 50
        };

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        Assert.True(chunks.Count > 1);
    }

    [Fact]
    public void EstimateChunkCount_ReturnsReasonableValue()
    {
        // Arrange
        var text = """
            # Section 1

            Content.

            # Section 2

            Content.

            # Section 3

            Content.
            """;
        var options = ChunkOptions.Default;

        // Act
        var estimate = _chunker.EstimateChunkCount(text, options);

        // Assert
        Assert.True(estimate >= 1);
    }

    [Fact]
    public async Task ChunkAsync_ChildIdsPopulated_WhenHasChildren()
    {
        // Arrange
        var text = """
            # Parent

            Parent content.

            ## Child 1

            Child 1 content.

            ## Child 2

            Child 2 content.
            """;
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        var parentChunks = chunks.Where(c =>
            c.Metadata.Custom?.ContainsKey(HierarchicalChunker.ChildIdsKey) == true &&
            c.Metadata.Custom[HierarchicalChunker.ChildIdsKey] is IList<string> children &&
            children.Count > 0);

        // Root should have children
        Assert.True(parentChunks.Any() || chunks.Count <= 1);
    }

    [Fact]
    public async Task ChunkAsync_ChunksHaveUniqueIds()
    {
        // Arrange
        var text = """
            # Section 1

            Content 1.

            # Section 2

            Content 2.

            # Section 3

            Content 3.
            """;
        var options = ChunkOptions.Default;

        // Act
        var chunks = await _chunker.ChunkAsync(text, options);

        // Assert
        var ids = chunks.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
