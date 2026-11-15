using FluxCurator.Core.Domain;
using FluxCurator.Core.Infrastructure.Refining;

namespace FluxCurator.Tests.Refining;

/// <summary>
/// A text with more than ten fenced blocks: the placeholder of block 10 and up has more than one digit, and a placeholder that
/// cannot be read back leaves its block out of the text - the step is then skipped and the code is not tidied or restored.
/// </summary>
public class MarkdownFencesBlockCountTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(123)]
    public void Every_block_comes_back_whatever_their_number(int count)
    {
        var text = string.Join("\n\nprose\n\n", Enumerable.Range(0, count).Select(i => "```text\nblock " + i + "\n```")) + "\n";

        var shield = MarkdownFences.Shield(text);

        Assert.Equal(text, shield.Restore(shield.Text));
        Assert.Equal(text, MarkdownFences.Shielded(text, source => source));
    }

    [Fact]
    public void The_default_refining_keeps_twelve_blocks_exactly_as_written()
    {
        var blocks = Enumerable.Range(0, 12).Select(i => "```python\ndef f" + i + "():\n    x = 1\n\n\n    return x\n```").ToList();
        var text = "  Intro line  \n\n\n" + string.Join("\n\nprose between blocks\n\n", blocks) + "\n\n  closing prose  \n";

        var refined = new TextRefiner().Refine(text, TextRefineOptions.Standard);

        foreach (var block in blocks)
            Assert.Contains(block, refined);
    }
}
