using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.Refining;

namespace FluxCurator.Tests.Refining;

/// <summary>
/// The refiner tidies prose; a Markdown fenced code block is code and comes back exactly as written, whatever the preset:
/// its indentation, repeated lines, blank lines and trailing spaces are the code's own.
/// </summary>
public class TextRefinerFencedCodeTests
{
    private static readonly string[] Blocks =
    [
        "```python\ndef f(x):\n    if x:\n        return {\n            \"a\": 1,\n        }\n    return None\n\n\ndef g():\n\ttabbed = 1   \n    pass\n```",
        "```c\nif (a) {\n  if (b) {\n    run();\n  }\n}\n}\n}\n42\nEND\nEND\nEND\n```",
        "~~~sh\nset -e\n\n\n\necho   spaced    words\n\n---\n    deep\n~~~",
        "````markdown\n```\n    nested fence\n```\n````",
    ];

    private static string Document() =>
        "  Intro line with spaces  \n\n\n" + string.Join(
            "\n\nprose between blocks\n\n",
            Blocks) + "\n\n  closing prose  \n";

    public static TheoryData<string> Presets() =>
    [
        nameof(TextRefineOptions.Light),
        nameof(TextRefineOptions.Standard),
        nameof(TextRefineOptions.ForWebContent),
        nameof(TextRefineOptions.ForKorean),
        nameof(TextRefineOptions.ForPdfContent),
        nameof(TextRefineOptions.ForTokenOptimization),
        nameof(TextRefineOptions.ForAggressiveTokenOptimization),
    ];

    private static TextRefineOptions Preset(string name) => name switch
    {
        nameof(TextRefineOptions.Light) => TextRefineOptions.Light,
        nameof(TextRefineOptions.Standard) => TextRefineOptions.Standard,
        nameof(TextRefineOptions.ForWebContent) => TextRefineOptions.ForWebContent,
        nameof(TextRefineOptions.ForKorean) => TextRefineOptions.ForKorean,
        nameof(TextRefineOptions.ForPdfContent) => TextRefineOptions.ForPdfContent,
        nameof(TextRefineOptions.ForTokenOptimization) => TextRefineOptions.ForTokenOptimization,
        _ => TextRefineOptions.ForAggressiveTokenOptimization,
    };

    [Theory]
    [MemberData(nameof(Presets))]
    public void Every_preset_leaves_a_fenced_code_block_as_written(string preset)
    {
        var result = TextRefiner.Instance.Refine(Document(), Preset(preset));

        foreach (var block in Blocks)
            Assert.True(result.Contains(block, StringComparison.Ordinal), $"preset {preset} changed a code block; result:\n{result}");
    }

    [Fact]
    public void Custom_patterns_do_not_apply_inside_a_fenced_block()
    {
        var options = new TextRefineOptions { RemovePatterns = ["^END$"], ReplacePatterns = new Dictionary<string, string> { ["return"] = "RET" } };
        const string text = "END\nreturn 1\n```c\nEND\nreturn 2\n```\nEND\n";

        var result = TextRefiner.Instance.Refine(text, options);

        Assert.Equal("\nRET 1\n```c\nEND\nreturn 2\n```\n\n", result);
    }

    [Fact]
    public void Prose_around_a_block_is_tidied_as_before()
    {
        var options = new TextRefineOptions { TrimLines = true, RemoveDuplicateLines = true, CollapseBlankLines = true };
        const string text = "  same line  \n  same line  \n\n\n\nnext\n```\n    keep   \n    keep   \n```\n  tail  \n";

        var result = TextRefiner.Instance.Refine(text, options);

        Assert.Equal("same line\n\nnext\n```\n    keep   \n    keep   \n```\ntail", result);
    }

    [Fact]
    public void A_text_without_a_fence_is_refined_exactly_as_before()
    {
        var options = new TextRefineOptions { TrimLines = true, RemoveDuplicateLines = true };

        Assert.Equal("a\nb", TextRefiner.Instance.Refine("  a  \n  a  \n  b  ", options));
    }

    [Fact]
    public void An_unclosed_fence_does_not_hide_the_rest_of_the_text_from_the_refiner()
    {
        // A line of tildes is as often an ASCII separator as a fence: only a closed fence is a block here.
        var options = new TextRefineOptions { TrimLines = true };
        const string text = "  prose  \n```\n   code   \n   code   ";

        Assert.Equal("prose\n```\ncode\ncode", TextRefiner.Instance.Refine(text, options));
    }

    [Fact]
    public void A_custom_pattern_that_removes_html_comments_does_not_skip_the_refinement()
    {
        // The placeholder of a hidden block is not markup: a «<!--.*?-->» pattern leaves it, and the other steps still run.
        var options = new TextRefineOptions { RemoveDuplicateLines = true, RemovePatterns = ["<!--.*?-->"] };
        const string text = "dup\ndup\n```\n  c  \n```\n";

        var result = TextRefiner.Instance.Refine(text, options);

        Assert.Equal("dup\n```\n  c  \n```\n", result);
    }

    [Theory]
    [MemberData(nameof(Presets))]
    public void A_preset_that_collapses_whitespace_keeps_the_fences_on_lines_of_their_own(string preset)
    {
        const string text = "Intro\n\n```sh\nls\n```\n\nOutro\n";

        var result = TextRefiner.Instance.Refine(text, Preset(preset));

        Assert.Matches("(^|\n)```sh\nls\n```(\n|$)", result);
    }

    [Fact]
    public void Separator_banners_of_tildes_are_not_a_fenced_block()
    {
        var result = TextRefiner.Instance.Refine("~~~~~~\nSection A\n~~~~~~\nSection B\n~~~~~~\n", new TextRefineOptions { NormalizeSeparators = true });

        Assert.Equal(3, result.Split('\n').Count(line => line == "---"));
    }

    [Fact]
    public void The_shield_can_be_used_directly_and_maps_offsets_back()
    {
        const string text = "ab\n```\nx\ny\n```\ncd\n";

        MarkdownFences.FenceShield shield = MarkdownFences.Shield(text);

        Assert.False(shield.IsEmpty);
        Assert.Equal(text, shield.Restore(shield.Text));
        Assert.Equal(text.IndexOf("cd", StringComparison.Ordinal), shield.ToOriginalOffset(shield.Text.IndexOf("cd", StringComparison.Ordinal)));
    }

    [Fact]
    public void A_line_of_tildes_is_still_a_separator()
    {
        Assert.Equal("---", TextRefiner.Instance.Refine("~~~~~", new TextRefineOptions { NormalizeSeparators = true }));
    }
}
