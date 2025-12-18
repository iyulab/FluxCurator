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

    // ========================================
    // Token Optimization Tests
    // ========================================

    #region NormalizeRepeatedCharacters Tests

    [Theory]
    [InlineData("====", "====")]           // Exactly 4, keep as is
    [InlineData("=====", "====")]          // 5 -> 4
    [InlineData("========================", "====")] // 24 -> 4
    [InlineData("----Title----", "----Title----")] // 4 each side, keep
    [InlineData("=====Title=====", "====Title====")] // 5 each -> 4
    public void NormalizeRepeatedCharacters_ReducesToMaxRepeats(string input, string expected)
    {
        // Arrange
        var options = new TextRefineOptions
        {
            NormalizeRepeatedCharacters = true,
            MaxConsecutiveRepeats = 4,
            NormalizeSeparators = false,  // Disable to test only repeated chars
            RemoveBase64Data = false
        };

        // Act
        var result = _refiner.Refine(input, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("===", "===")]  // Less than 4, keep
    [InlineData("--", "--")]    // Less than 4, keep
    [InlineData("*", "*")]      // Single char
    public void NormalizeRepeatedCharacters_KeepsShortSequences(string input, string expected)
    {
        // Arrange
        var options = new TextRefineOptions
        {
            NormalizeRepeatedCharacters = true,
            MaxConsecutiveRepeats = 4,
            NormalizeSeparators = false,  // Disable to test only repeated chars
            RemoveBase64Data = false
        };

        // Act
        var result = _refiner.Refine(input, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("안녕하세요", "안녕하세요")]           // Korean text preserved
    [InlineData("ㅋㅋㅋㅋㅋㅋ", "ㅋㅋㅋㅋㅋㅋ")]       // Korean jamo preserved
    [InlineData("ㅎㅎㅎㅎㅎㅎㅎㅎ", "ㅎㅎㅎㅎㅎㅎㅎㅎ")] // Korean jamo preserved
    [InlineData("中国語テスト", "中国語テスト")]       // CJK preserved
    [InlineData("あああああ", "あああああ")]           // Japanese hiragana preserved
    [InlineData("カタカナ", "カタカナ")]               // Japanese katakana preserved
    public void NormalizeRepeatedCharacters_PreservesCJKCharacters(string input, string expected)
    {
        // Arrange
        var options = new TextRefineOptions
        {
            NormalizeRepeatedCharacters = true,
            MaxConsecutiveRepeats = 4,
            NormalizeSeparators = false,
            RemoveBase64Data = false
        };

        // Act
        var result = _refiner.Refine(input, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeRepeatedCharacters_WithMaxRepeats3_ReducesToThree()
    {
        // Arrange
        var text = "=====Title=====";
        var options = new TextRefineOptions
        {
            NormalizeRepeatedCharacters = true,
            MaxConsecutiveRepeats = 3,
            NormalizeSeparators = false,
            RemoveBase64Data = false
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("===Title===", result);
    }

    [Fact]
    public void NormalizeRepeatedCharacters_MixedContent_OnlyNormalizesSpecialChars()
    {
        // Arrange
        var text = "==== Section Title ==== normal words aaaa bbbb";
        var options = new TextRefineOptions
        {
            NormalizeRepeatedCharacters = true,
            MaxConsecutiveRepeats = 4,
            NormalizeSeparators = false,
            RemoveBase64Data = false
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("==== Section Title ====", result);
        Assert.Contains("aaaa", result);  // Word chars not affected
        Assert.Contains("bbbb", result);
    }

    #endregion

    #region NormalizeSeparators Tests

    [Theory]
    [InlineData("-----", "---")]
    [InlineData("=====", "---")]
    [InlineData("*****", "---")]
    [InlineData("~~~~~", "---")]
    [InlineData("#####", "---")]
    [InlineData("_____", "---")]
    public void NormalizeSeparators_AsciiSeparators_NormalizesToMarkdown(string input, string expected)
    {
        // Arrange
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            SeparatorReplacement = "---"
        };

        // Act
        var result = _refiner.Refine(input, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("─────────────")]
    [InlineData("━━━━━━━━━━━━━")]
    [InlineData("═══════════════")]
    public void NormalizeSeparators_UnicodeBoxDrawingLines_Normalizes(string input)
    {
        // Arrange
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            SeparatorReplacement = "---"
        };

        // Act
        var result = _refiner.Refine(input, options);

        // Assert
        Assert.Equal("---", result);
    }

    [Theory]
    [InlineData("■■■■■■■■■")]
    [InlineData("□□□□□□□□□")]
    [InlineData("●●●●●●●●●")]
    [InlineData("○○○○○○○○○")]
    [InlineData("◆◆◆◆◆◆◆◆◆")]
    [InlineData("★★★★★★★★★")]
    public void NormalizeSeparators_GeometricSymbols_Normalizes(string input)
    {
        // Arrange
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            SeparatorReplacement = "---"
        };

        // Act
        var result = _refiner.Refine(input, options);

        // Assert
        Assert.Equal("---", result);
    }

    [Fact]
    public void NormalizeSeparators_WithSpaces_StillNormalizes()
    {
        // Arrange
        var text = "  =================  ";
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            SeparatorReplacement = "---"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("---", result);
    }

    [Fact]
    public void NormalizeSeparators_InlineText_NotAffected()
    {
        // Arrange - separator chars within text line should not be affected
        var text = "a==b";
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            SeparatorReplacement = "---"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("a==b", result);
    }

    [Fact]
    public void NormalizeSeparators_EmptyReplacement_RemovesSeparators()
    {
        // Arrange
        var text = "Content\n=================\nMore content";
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            SeparatorReplacement = ""
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.DoesNotContain("===", result);
    }

    [Fact]
    public void NormalizeSeparators_MultipleInDocument_AllNormalized()
    {
        // Arrange
        var text = "Title\n=================\nSection 1\n-----------------\nSection 2";
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            SeparatorReplacement = "---"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("Title\n---\nSection 1\n---\nSection 2", result);
    }

    #endregion

    #region RemoveAsciiArt Tests

    [Fact]
    public void RemoveAsciiArt_SimpleBox_RemovesBoxKeepsContent()
    {
        // Arrange
        var text = "╔════════════════════════╗\n║  Title                 ║\n╚════════════════════════╝";
        var options = new TextRefineOptions { RemoveAsciiArt = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("Title", result);
        Assert.DoesNotContain("╔", result);
        Assert.DoesNotContain("║", result);
        Assert.DoesNotContain("═", result);
    }

    [Fact]
    public void RemoveAsciiArt_LightBox_RemovesBoxKeepsContent()
    {
        // Arrange
        var text = "┌──────────────┐\n│ Content here │\n└──────────────┘";
        var options = new TextRefineOptions { RemoveAsciiArt = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("Content here", result);
        Assert.DoesNotContain("┌", result);
        Assert.DoesNotContain("│", result);
        Assert.DoesNotContain("─", result);
    }

    [Fact]
    public void RemoveAsciiArt_Table_PreservesContentRemovesDrawing()
    {
        // Arrange
        var text = "┏━━━━━━━━━━━━━━┓\n┃ Row 1        ┃\n┣━━━━━━━━━━━━━━┫\n┃ Row 2        ┃\n┗━━━━━━━━━━━━━━┛";
        var options = new TextRefineOptions { RemoveAsciiArt = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("Row 1", result);
        Assert.Contains("Row 2", result);
        Assert.DoesNotContain("┏", result);
        Assert.DoesNotContain("━", result);
    }

    [Fact]
    public void RemoveAsciiArt_RoundedCorners_Removed()
    {
        // Arrange
        var text = "╭──────────────╮\n│ Rounded box  │\n╰──────────────╯";
        var options = new TextRefineOptions { RemoveAsciiArt = true };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("Rounded box", result);
        Assert.DoesNotContain("╭", result);
        Assert.DoesNotContain("╯", result);
    }

    #endregion

    #region RemoveBase64Data Tests

    [Fact]
    public void RemoveBase64Data_ImageDataUri_ReplacedWithPlaceholder()
    {
        // Arrange - Using a valid base64 image data URI (must be 50+ chars)
        var base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        var text = $"Image here: data:image/png;base64,{base64Data}";
        var options = new TextRefineOptions
        {
            RemoveBase64Data = true,
            Base64Placeholder = "[embedded-data]"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("[embedded-data]", result);
        Assert.DoesNotContain("iVBORw0KGgo", result);
    }

    [Fact]
    public void RemoveBase64Data_FontDataUri_ReplacedWithPlaceholder()
    {
        // Arrange - Font data URI
        var base64Data = new string('A', 100); // Simulating long base64 string
        var text = $"@font-face {{ src: url(data:font/woff2;base64,{base64Data}); }}";
        var options = new TextRefineOptions
        {
            RemoveBase64Data = true,
            Base64Placeholder = "[font]"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("[font]", result);
        Assert.DoesNotContain(base64Data, result);
    }

    [Fact]
    public void RemoveBase64Data_MultipleOccurrences_AllReplaced()
    {
        // Arrange
        var base64Data = new string('B', 100);
        var text = $"img1: data:image/png;base64,{base64Data} and img2: data:image/jpeg;base64,{base64Data}";
        var options = new TextRefineOptions
        {
            RemoveBase64Data = true,
            Base64Placeholder = "[img]"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal(2, result.Split("[img]").Length - 1); // Two placeholders
        Assert.DoesNotContain(base64Data, result);
    }

    [Fact]
    public void RemoveBase64Data_ShortBase64_NotAffected()
    {
        // Arrange - Short base64 strings (< 50 chars) should not be replaced to avoid false positives
        var text = "data:image/png;base64,abc123";
        var options = new TextRefineOptions
        {
            RemoveBase64Data = true,
            Base64Placeholder = "[data]"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert - Short base64 not removed
        Assert.Contains("abc123", result);
    }

    [Fact]
    public void RemoveBase64Data_CustomPlaceholder_UsesCustomText()
    {
        // Arrange
        var base64Data = new string('C', 100);
        var text = $"data:image/svg+xml;base64,{base64Data}";
        var options = new TextRefineOptions
        {
            RemoveBase64Data = true,
            Base64Placeholder = "[SVG_IMAGE_REMOVED]"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("[SVG_IMAGE_REMOVED]", result);
    }

    #endregion

    #region Preset Tests

    [Fact]
    public void ForTokenOptimization_NormalizesRepeatedCharacters()
    {
        // Arrange
        var text = "========================Section========================";
        var options = TextRefineOptions.ForTokenOptimization;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("====Section====", result);
    }

    [Fact]
    public void ForTokenOptimization_NormalizesSeparators()
    {
        // Arrange
        var text = "Title\n========================\nContent";
        var options = TextRefineOptions.ForTokenOptimization;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("---", result);
        Assert.DoesNotContain("========================", result);
    }

    [Fact]
    public void ForAggressiveTokenOptimization_RemovesAsciiArtAndBase64()
    {
        // Arrange
        var base64Data = new string('D', 100);
        var text = $"╔══════╗\n║ Box  ║\n╚══════╝\ndata:image/png;base64,{base64Data}";
        var options = TextRefineOptions.ForAggressiveTokenOptimization;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Contains("Box", result);
        Assert.Contains("[embedded-data]", result);
        Assert.DoesNotContain("╔", result);
        Assert.DoesNotContain(base64Data, result);
    }

    [Fact]
    public void ForAggressiveTokenOptimization_UsesMaxRepeats3()
    {
        // Arrange
        var text = "=====Title=====";
        var options = TextRefineOptions.ForAggressiveTokenOptimization;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.Equal("===Title===", result);
    }

    [Fact]
    public void ForAggressiveTokenOptimization_RemovesSeparatorsEntirely()
    {
        // Arrange
        var text = "Title\n------------------------\nContent";
        var options = TextRefineOptions.ForAggressiveTokenOptimization;

        // Act
        var result = _refiner.Refine(text, options);

        // Assert
        Assert.DoesNotContain("---", result);
        Assert.DoesNotContain("------------------------", result);
    }

    #endregion

    #region Pipeline Order Tests

    [Fact]
    public void Refine_PipelineOrder_Base64BeforeAsciiArt()
    {
        // Arrange - Both Base64 and ASCII art present
        var base64Data = new string('E', 100);
        var text = $"╔══════╗\ndata:image/png;base64,{base64Data}\n╚══════╝";
        var options = new TextRefineOptions
        {
            RemoveBase64Data = true,
            RemoveAsciiArt = true
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert - Both should be processed
        Assert.Contains("[embedded-data]", result);
        Assert.DoesNotContain("╔", result);
    }

    [Fact]
    public void Refine_PipelineOrder_SeparatorsBeforeRepeatedChars()
    {
        // Arrange - Separator line that is also repeated chars
        var text = "========================";
        var options = new TextRefineOptions
        {
            NormalizeSeparators = true,
            NormalizeRepeatedCharacters = true,
            SeparatorReplacement = "---"
        };

        // Act
        var result = _refiner.Refine(text, options);

        // Assert - Should become "---" (separator normalized first)
        Assert.Equal("---", result);
    }

    #endregion

    #region Token Reduction Verification Tests

    [Fact]
    public void ForTokenOptimization_ReducesTokenCount_WebContent()
    {
        // Arrange - Simulating web-extracted content with lots of noise
        var text = """
            ======================== TITLE ========================

            Content paragraph here.
            Content paragraph here.

            ■■■■■■■■■■■■■■■■■■■■

            More content.

            ─────────────────────────────────────────────

            -
            - Item 1
            -

            ************************
            """;
        var options = TextRefineOptions.ForTokenOptimization;

        // Act
        var result = _refiner.Refine(text, options);
        var originalLength = text.Length;
        var resultLength = result.Length;

        // Assert - Should have meaningful reduction
        Assert.True(resultLength < originalLength,
            $"Expected reduction, but original={originalLength}, result={resultLength}");
        Assert.Contains("TITLE", result);
        Assert.Contains("Content paragraph here.", result);
        Assert.Contains("More content.", result);
        Assert.Contains("Item 1", result);
    }

    [Fact]
    public void ForAggressiveTokenOptimization_SignificantReduction()
    {
        // Arrange - Heavy noise content
        var base64Data = new string('X', 200);
        var text = $"""
            ╔════════════════════════════════════════╗
            ║        DOCUMENT TITLE                  ║
            ╚════════════════════════════════════════╝

            ======================== Section 1 ========================

            Some actual content here that matters.

            data:image/png;base64,{base64Data}

            ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

            More useful content.

            ─────────────────────────────────────────────
            """;
        var options = TextRefineOptions.ForAggressiveTokenOptimization;

        // Act
        var result = _refiner.Refine(text, options);
        var originalLength = text.Length;
        var resultLength = result.Length;
        var reductionPercent = (1.0 - (double)resultLength / originalLength) * 100;

        // Assert - Should have at least 30% reduction
        Assert.True(reductionPercent > 30,
            $"Expected >30% reduction, but got {reductionPercent:F1}% (original={originalLength}, result={resultLength})");
        Assert.Contains("DOCUMENT TITLE", result);
        Assert.Contains("Some actual content here that matters.", result);
        Assert.Contains("More useful content.", result);
    }

    #endregion
}
