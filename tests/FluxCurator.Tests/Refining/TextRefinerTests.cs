using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.Refining;

namespace FluxCurator.Tests.Refining;

/// <summary>
/// Unit tests for TextRefiner.
/// </summary>
public class TextRefinerTests
{
    private readonly TextRefiner _refiner = TextRefiner.Instance;

    [Fact]
    public void Refine_NullOrEmptyText_ReturnsOriginal()
    {
        // Arrange
        var options = TextRefineOptions.Standard;

        // Act & Assert
        Assert.Equal("", _refiner.Refine("", options));
        Assert.Null(_refiner.Refine(null!, options));
    }

    [Fact]
    public void Refine_RemoveBlankLines_RemovesEmptyLines()
    {
        // Arrange
        var text = "Line 1\n\n\nLine 2\n\nLine 3";
        var options = new TextRefineOptions { RemoveBlankLines = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("Line 1\nLine 2\nLine 3", result);
    }

    [Fact]
    public void Refine_CollapseBlankLines_CollapsesMultipleToOne()
    {
        // Arrange
        var text = "Line 1\n\n\n\nLine 2\n\nLine 3";
        var options = new TextRefineOptions { CollapseBlankLines = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("Line 1\n\nLine 2\n\nLine 3", result);
    }

    [Fact]
    public void Refine_RemoveDuplicateLines_RemovesConsecutiveDuplicates()
    {
        // Arrange
        var text = "Line 1\nLine 1\nLine 2\nLine 2\nLine 2\nLine 3";
        var options = new TextRefineOptions { RemoveDuplicateLines = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("Line 1\nLine 2\nLine 3", result);
    }

    [Fact]
    public void Refine_RemoveEmptyListItems_RemovesUnorderedMarkers()
    {
        // Arrange
        var text = "- Item 1\n- \n* Item 2\n* \n• Item 3\n• ";
        var options = new TextRefineOptions { RemoveEmptyListItems = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("- Item 1", result);
        Assert.Contains("* Item 2", result);
        Assert.Contains("• Item 3", result);
        Assert.DoesNotContain("- \n", result);
        Assert.DoesNotContain("* \n", result);
    }

    [Fact]
    public void Refine_RemoveEmptyListItems_RemovesOrderedMarkers()
    {
        // Arrange
        var text = "1. Item 1\n2. \n3. Item 3\na. \nb. Item B";
        var options = new TextRefineOptions { RemoveEmptyListItems = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("1. Item 1", result);
        Assert.Contains("3. Item 3", result);
        Assert.Contains("b. Item B", result);
    }

    [Fact]
    public void Refine_RemoveEmptyListItems_RemovesKoreanMarkers()
    {
        // Arrange
        var text = "○ 항목1\n○ \n● 항목2\n● \n가. 항목3\n나. ";
        var options = new TextRefineOptions { RemoveEmptyListItems = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("○ 항목1", result);
        Assert.Contains("● 항목2", result);
        Assert.Contains("가. 항목3", result);
    }

    [Fact]
    public void Refine_TrimLines_TrimsWhitespaceFromLines()
    {
        // Arrange
        var text = "  Line 1  \n\tLine 2\t\n   Line 3   ";
        var options = new TextRefineOptions { TrimLines = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("Line 1\nLine 2\nLine 3", result);
    }

    [Fact]
    public void Refine_MinLineLength_RemovesShortLines()
    {
        // Arrange
        var text = "AB\nABCDE\nX\nHello World";
        var options = new TextRefineOptions { MinLineLength = 3, TrimLines = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        // "AB" and "X" are shorter than 3 chars
        var lines = result.Split('\n');
        Assert.DoesNotContain("AB", lines);
        Assert.DoesNotContain("X", lines);
        Assert.Contains("ABCDE", lines);
        Assert.Contains("Hello World", lines);
    }

    [Fact]
    public void Refine_NormalizeWhitespace_CollapsesSpacesAndNewlines()
    {
        // Arrange
        var text = "Word1   Word2\n\n\nWord3\t\tWord4";
        var options = new TextRefineOptions { NormalizeWhitespace = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("Word1 Word2 Word3 Word4", result);
    }

    [Fact]
    public void Refine_RemovePatterns_RemovesCustomRegexPatterns()
    {
        // Arrange
        var text = "Content\n# 댓글\nMore content\n[광고] Buy now!";
        var options = new TextRefineOptions
        {
            RemovePatterns =
            [
                @"^#\s*댓글\s*$",
                @"^\[광고\].*$"
            ]
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("Content", result);
        Assert.Contains("More content", result);
        Assert.DoesNotContain("댓글", result);
        Assert.DoesNotContain("광고", result);
    }

    [Fact]
    public void Refine_ReplacePatterns_ReplacesMatchingPatterns()
    {
        // Arrange
        var text = "Call 010-1234-5678 for info";
        var options = new TextRefineOptions
        {
            ReplacePatterns = new Dictionary<string, string>
            {
                { @"\d{3}-\d{4}-\d{4}", "[PHONE]" }
            }
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("Call [PHONE] for info", result);
    }

    [Fact]
    public void Refine_ForWebContent_AppliesAllWebContentOptions()
    {
        // Arrange
        var text = "Title\n\n\n- \n\nContent line\nContent line\n\nMore content";
        var options = TextRefineOptions.ForWebContent;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        // Should remove blanks, duplicates, empty list items, normalize
        Assert.DoesNotContain("\n\n", result);
        Assert.DoesNotContain("- ", result);
    }

    [Fact]
    public void Refine_ForKorean_AppliesKoreanSpecificPatterns()
    {
        // Arrange
        var text = "본문 내용\n# 댓글\n더 많은 내용\nCopyright © 2024";
        var options = TextRefineOptions.ForKorean;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("본문 내용", result);
        Assert.Contains("더 많은 내용", result);
        Assert.DoesNotContain("댓글", result);
        Assert.DoesNotContain("Copyright", result);
    }

    [Fact]
    public void Refine_Standard_AppliesStandardOptions()
    {
        // Arrange
        var text = "Line 1\nLine 1\n- \n  Trimmed  \n\n\nLine 2";
        var options = TextRefineOptions.Standard;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        // Standard: RemoveDuplicateLines, RemoveEmptyListItems, TrimLines, CollapseBlankLines
        Assert.DoesNotContain("- ", result);
    }

    [Fact]
    public void Refine_Light_AppliesLightOptions()
    {
        // Arrange
        var text = "- \n  Content  \n\n\n\nMore";
        var options = TextRefineOptions.Light;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        // Light: RemoveEmptyListItems, TrimLines, CollapseBlankLines
        Assert.Contains("Content", result);
        Assert.DoesNotContain("- ", result);
    }

    [Fact]
    public void Refine_None_DoesNotModifyText()
    {
        // Arrange
        var text = "- \n  Content  \n\n\nMore";
        var options = TextRefineOptions.None;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal(text, result);
    }

    [Fact]
    public void Refine_InvalidRegexPattern_SkipsPattern()
    {
        // Arrange
        var text = "Some content";
        var options = new TextRefineOptions
        {
            RemovePatterns = ["[invalid regex"]  // Unclosed bracket
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert - should not throw, just skip invalid pattern
        Assert.Equal("Some content", result);
    }

    [Fact]
    public void Refine_CombinedOptions_AppliesInCorrectOrder()
    {
        // Arrange - "- " with no content after is an empty list marker
        var text = "  - \nLine 1\nLine 1\n\n\n\nLine 2  ";
        var options = new TextRefineOptions
        {
            RemoveEmptyListItems = true,
            TrimLines = true,
            RemoveDuplicateLines = true,
            CollapseBlankLines = true
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.DoesNotContain("  ", result);  // Trimmed
        Assert.Equal(1, result.Split("Line 1").Length - 1);  // Only one Line 1 (duplicates removed)
        Assert.Contains("Line 2", result);
    }

    [Fact]
    public void Refine_ForPdfContent_RemovesPageNumbers()
    {
        // Arrange
        var text = "Content here\n42\n- 5 -\nMore content\n123\nEnd";
        var options = TextRefineOptions.ForPdfContent;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("Content here", result);
        Assert.Contains("More content", result);
        Assert.DoesNotContain("- 5 -", result);
    }

    [Fact]
    public void RefineWithDefaultOptions_UsesLightOptions()
    {
        // Arrange
        var text = "- \nContent";

        // Act
        var result = _refiner.Refine(text);

        // Assert - Light options remove empty list items
        Assert.DoesNotContain("- ", result);
        Assert.Contains("Content", result);
    }
}
