using FluxCurator.Core.Infrastructure.Refining;

namespace FluxCurator.Tests.Refining;

/// <summary>
/// The model-facing layer of <see cref="MarkdownFences.FenceShield"/>: a language model is shown marker lines instead of
/// the fenced code and its answer is accepted only if every block can be put back exactly where it was.
/// </summary>
public sealed class MarkdownFencesModelLayerTests
{
    private const string TwoBlocks = "Intro line\n\n```bash\n# comment\nls -la\n```\n\nMiddle prose\n\n~~~python\ndef f():\n    return 1\n~~~\n\nClosing prose\n";

    [Fact]
    public void The_model_text_shows_a_plain_marker_line_for_each_block_and_no_private_use_character()
    {
        var shield = MarkdownFences.Shield(TwoBlocks);

        var lines = shield.ModelText.Split('\n');

        Assert.Contains("[[code-block 0]]", lines);
        Assert.Contains("[[code-block 1]]", lines);
        Assert.DoesNotContain(shield.ModelText, character => char.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.PrivateUse);
        Assert.DoesNotContain("ls -la", shield.ModelText, StringComparison.Ordinal);
        Assert.DoesNotContain("return 1", shield.ModelText, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unchanged_model_answer_restores_the_text_exactly()
    {
        var shield = MarkdownFences.Shield(TwoBlocks);

        Assert.Equal(TwoBlocks, shield.RestoreModelAnswer(shield.ModelText));
    }

    [Fact]
    public void A_model_that_edits_only_the_prose_keeps_the_code_as_written()
    {
        var shield = MarkdownFences.Shield(TwoBlocks);
        var answer = shield.ModelText.Replace("Middle prose", "A tidied middle paragraph", StringComparison.Ordinal);

        var restored = shield.RestoreModelAnswer(answer);

        Assert.NotNull(restored);
        Assert.Contains("A tidied middle paragraph", restored, StringComparison.Ordinal);
        Assert.Contains("```bash\n# comment\nls -la\n```", restored, StringComparison.Ordinal);
        Assert.Contains("~~~python\ndef f():\n    return 1\n~~~", restored, StringComparison.Ordinal);
    }

    [Fact]
    public void A_model_answer_with_a_dropped_marker_is_refused()
    {
        var shield = MarkdownFences.Shield(TwoBlocks);

        Assert.Null(shield.RestoreModelAnswer(shield.ModelText.Replace("[[code-block 1]]\n", string.Empty, StringComparison.Ordinal)));
    }

    [Fact]
    public void A_model_answer_with_a_repeated_marker_is_refused()
    {
        var shield = MarkdownFences.Shield(TwoBlocks);

        Assert.Null(shield.RestoreModelAnswer(shield.ModelText + "\n[[code-block 0]]\n"));
    }

    [Fact]
    public void A_model_answer_that_moved_a_marker_is_refused()
    {
        var shield = MarkdownFences.Shield(TwoBlocks);
        var swapped = shield.ModelText
            .Replace("[[code-block 0]]", "[[code-block X]]", StringComparison.Ordinal)
            .Replace("[[code-block 1]]", "[[code-block 0]]", StringComparison.Ordinal)
            .Replace("[[code-block X]]", "[[code-block 1]]", StringComparison.Ordinal);

        Assert.Null(shield.RestoreModelAnswer(swapped));
    }

    [Theory]
    [InlineData("```[[code-block 0]]```")]
    [InlineData("see [[code-block 0]] below")]
    [InlineData("`[[code-block 0]]`")]
    public void A_model_answer_that_sets_a_marker_inside_other_characters_is_refused(string wrapped)
    {
        var shield = MarkdownFences.Shield("Intro\n\n```text\ncode\n```\n\nOutro\n");

        Assert.Null(shield.RestoreModelAnswer("Intro\n\n" + wrapped + "\n\nOutro\n"));
    }

    [Fact]
    public void A_marker_with_spaces_around_it_on_its_own_line_still_brings_the_block_back()
    {
        var shield = MarkdownFences.Shield("Intro\n\n```text\ncode\n```\n\nOutro\n");

        Assert.Equal("Intro\n\n```text\ncode\n```\n\nOutro\n", shield.RestoreModelAnswer("Intro\n\n   [[code-block 0]]  \n\nOutro\n"));
    }

    [Fact]
    public void A_model_answer_that_adds_a_fence_of_its_own_is_refused()
    {
        var shield = MarkdownFences.Shield("Intro\n\n```text\ncode\n```\n\nOutro\n");

        Assert.Null(shield.RestoreModelAnswer("Intro\n\n[[code-block 0]]\n\n```\ninvented\n```\n\nOutro\n"));
    }

    [Fact]
    public void A_model_answer_that_wraps_the_marker_in_a_fence_pair_is_refused()
    {
        var shield = MarkdownFences.Shield("Intro\n\n```text\ncode\n```\n\nOutro\n");

        Assert.Null(shield.RestoreModelAnswer("Intro\n\n```\n[[code-block 0]]\n```\n\nOutro\n"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(12)]
    public void Many_blocks_round_trip_through_the_model_text(int count)
    {
        var text = string.Join("\n\nprose\n\n", Enumerable.Range(0, count).Select(i => "```text\nblock " + i + "\n```")) + "\n";
        var shield = MarkdownFences.Shield(text);

        Assert.Equal(text, shield.RestoreModelAnswer(shield.ModelText));
    }

    [Fact]
    public void Windows_line_endings_round_trip_through_the_model_text()
    {
        var text = TwoBlocks.Replace("\n", "\r\n", StringComparison.Ordinal);
        var shield = MarkdownFences.Shield(text);

        Assert.Equal(text, shield.RestoreModelAnswer(shield.ModelText));
    }

    [Fact]
    public void A_text_without_a_fence_is_its_own_model_text_and_answer()
    {
        var shield = MarkdownFences.Shield("Just prose.\n\nMore prose.\n");

        Assert.Equal(shield.Text, shield.ModelText);
        Assert.Equal("Rewritten prose.\n", shield.RestoreModelAnswer("Rewritten prose.\n"));
        Assert.False(shield.UnsafeToSend);
    }

    [Fact]
    public void A_text_with_a_fence_and_nothing_ambiguous_is_safe_to_send()
    {
        Assert.False(MarkdownFences.Shield(TwoBlocks).UnsafeToSend);
    }

    [Fact]
    public void A_text_with_a_marker_line_of_its_own_next_to_fenced_code_is_unsafe_to_send()
    {
        var shield = MarkdownFences.Shield("A page about the marker:\n\n[[code-block 0]]\n\n```text\ncode\n```\n");

        Assert.True(shield.UnsafeToSend);
    }

    [Fact]
    public void A_text_that_holds_the_placeholder_delimiters_next_to_fenced_code_is_unsafe_to_send()
    {
        var shield = MarkdownFences.Shield("A private-use  character\n\n```text\ncode\n```\n");

        Assert.True(shield.IsEmpty);
        Assert.True(shield.UnsafeToSend);
    }

    [Theory]
    [InlineData("A private-use  character\n\n```text\ncode\n```\n")]
    [InlineData("A page about the marker:\n\n[[code-block 0]]\n\n```text\ncode\n```\n")]
    public void An_unsafe_text_never_gets_an_answer_restored(string text)
    {
        var shield = MarkdownFences.Shield(text);

        Assert.True(shield.UnsafeToSend);
        Assert.Null(shield.RestoreModelAnswer(shield.ModelText));
        Assert.Null(shield.RestoreModelAnswer("anything"));
    }

    [Fact]
    public void A_text_that_holds_the_placeholder_delimiters_but_no_fenced_code_is_not_unsafe()
    {
        Assert.False(MarkdownFences.Shield("A private-use  character, no code.\n").UnsafeToSend);
    }
}
