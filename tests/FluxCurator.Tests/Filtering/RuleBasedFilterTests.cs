namespace FluxCurator.Tests.Filtering;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Filtering;

public class RuleBasedFilterTests
{
    #region Properties

    [Fact]
    public void Name_ReturnsRuleBasedFilter()
    {
        var filter = new RuleBasedFilter();

        Assert.Equal("RuleBasedFilter", filter.Name);
    }

    [Fact]
    public void Priority_Returns100()
    {
        var filter = new RuleBasedFilter();

        Assert.Equal(100, filter.Priority);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RuleBasedFilter(null!));
    }

    [Fact]
    public void Constructor_WithBlocklist_RegistersRules()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        Assert.True(filter.ContainsFilteredContent("This has badword in it"));
    }

    [Fact]
    public void Constructor_WithPatterns_RegistersRules()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CustomPatterns = new Dictionary<string, string>
            {
                { "digits", @"\d{5}" }
            }
        };
        var filter = new RuleBasedFilter(options);

        Assert.True(filter.ContainsFilteredContent("Code: 12345"));
    }

    #endregion

    #region Filter — Empty/Null Input

    [Fact]
    public void Filter_NullInput_ReturnsNoMatch()
    {
        var filter = new RuleBasedFilter();

        var result = filter.Filter(null!);

        Assert.Equal("", result.FilteredText);
        Assert.False(result.HasFilteredContent);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public void Filter_EmptyInput_ReturnsNoMatch()
    {
        var filter = new RuleBasedFilter();

        var result = filter.Filter("");

        Assert.Equal("", result.FilteredText);
        Assert.False(result.HasFilteredContent);
    }

    [Fact]
    public void Filter_NoMatchingContent_ReturnsOriginal()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This is clean text");

        Assert.Equal("This is clean text", result.FilteredText);
        Assert.False(result.HasFilteredContent);
    }

    #endregion

    #region Filter — Replace Action (Default)

    [Fact]
    public void Filter_ReplaceAction_ReplacesWithToken()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Replace,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This has badword here");

        Assert.Contains("[FILTERED]", result.FilteredText);
        Assert.DoesNotContain("badword", result.FilteredText);
        Assert.True(result.HasFilteredContent);
    }

    [Fact]
    public void Filter_ReplaceAction_CustomReplacement()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Replace,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" },
            CustomReplacements = new Dictionary<ContentCategory, string>
            {
                { ContentCategory.Custom, "***" }
            }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This has badword here");

        Assert.Contains("***", result.FilteredText);
    }

    #endregion

    #region Filter — Remove Action

    [Fact]
    public void Filter_RemoveAction_RemovesCompletely()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Remove,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("Before badword after");

        Assert.DoesNotContain("badword", result.FilteredText);
        Assert.Contains("Before", result.FilteredText);
        Assert.Contains("after", result.FilteredText);
    }

    #endregion

    #region Filter — Redact Action

    [Fact]
    public void Filter_RedactAction_ReplacesWithCharacter()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Redact,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This has badword here");

        // Redact replaces with RedactCharacter ('*') × length
        Assert.Contains(new string('*', "badword".Length), result.FilteredText);
        Assert.DoesNotContain("badword", result.FilteredText);
    }

    [Fact]
    public void Filter_RedactAction_CustomCharacter()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Redact,
            RedactCharacter = '#',
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This has badword here");

        Assert.Contains(new string('#', "badword".Length), result.FilteredText);
    }

    #endregion

    #region Filter — Flag Action

    [Fact]
    public void Filter_FlagAction_KeepsOriginalText()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Flag,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This has badword here");

        // Flag keeps original text unchanged
        Assert.Equal("This has badword here", result.FilteredText);
        Assert.True(result.HasFilteredContent);
        Assert.Single(result.Matches);
    }

    #endregion

    #region Filter — Block Action

    [Fact]
    public void Filter_BlockAction_BlocksEntireText()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Block,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This has badword here");

        Assert.True(result.IsBlocked);
        Assert.Equal("", result.FilteredText);
        Assert.True(result.HasFilteredContent);
    }

    #endregion

    #region Filter — CaseInsensitive

    [Fact]
    public void Filter_CaseInsensitive_MatchesAnyCase()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CaseInsensitive = true,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        Assert.True(filter.ContainsFilteredContent("This has BADWORD here"));
        Assert.True(filter.ContainsFilteredContent("This has BadWord here"));
    }

    [Fact]
    public void Filter_CaseSensitive_OnlyMatchesExactCase()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CaseInsensitive = false,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        Assert.True(filter.ContainsFilteredContent("This has badword here"));
        Assert.False(filter.ContainsFilteredContent("This has BADWORD here"));
    }

    #endregion

    #region Filter — WholeWordOnly

    [Fact]
    public void Filter_WholeWordOnly_DoesNotMatchPartialWords()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            WholeWordOnly = true,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bad" }
        };
        var filter = new RuleBasedFilter(options);

        Assert.True(filter.ContainsFilteredContent("This is bad"));
        Assert.False(filter.ContainsFilteredContent("This is badge"));
    }

    [Fact]
    public void Filter_NotWholeWordOnly_MatchesPartialWords()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            WholeWordOnly = false,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bad" }
        };
        var filter = new RuleBasedFilter(options);

        Assert.True(filter.ContainsFilteredContent("This is badge"));
    }

    #endregion

    #region Filter — Allowlist

    [Fact]
    public void Filter_AllowlistedWord_Ignored()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" },
            CustomAllowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This has badword here");

        Assert.False(result.HasFilteredContent);
    }

    #endregion

    #region Filter — Confidence Threshold

    [Fact]
    public void Filter_LowConfidence_SkippedAboveThreshold()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            MinConfidence = 0.9f
        };
        var filter = new RuleBasedFilter(options);

        // Add a rule with confidence below threshold
        filter.AddRule(ContentCategory.Custom, "maybe", 0.5f);

        var result = filter.Filter("This is maybe filtered");

        Assert.False(result.HasFilteredContent);
    }

    [Fact]
    public void Filter_HighConfidence_MatchedAboveThreshold()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            MinConfidence = 0.5f
        };
        var filter = new RuleBasedFilter(options);

        filter.AddRule(ContentCategory.Custom, "definitely", 0.9f);

        var result = filter.Filter("This is definitely filtered");

        Assert.True(result.HasFilteredContent);
    }

    #endregion

    #region Filter — Category Filtering

    [Fact]
    public void Filter_CategoryNotInFilter_SkipsRules()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Profanity // Only profanity
        };
        var filter = new RuleBasedFilter(options);

        // Add rule to Custom category (not in filter)
        filter.AddRule(ContentCategory.Custom, "test");

        Assert.False(filter.ContainsFilteredContent("This is a test"));
    }

    [Fact]
    public void Filter_CategoryInFilter_AppliesRules()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Profanity
        };
        var filter = new RuleBasedFilter(options);

        filter.AddRule(ContentCategory.Profanity, @"\bbadword\b");

        Assert.True(filter.ContainsFilteredContent("This has badword here"));
    }

    #endregion

    #region Filter — Custom Patterns (Regex)

    [Fact]
    public void Filter_CustomRegexPattern_Matches()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CustomPatterns = new Dictionary<string, string>
            {
                { "phone", @"\d{3}-\d{4}-\d{4}" }
            }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("Call 010-1234-5678 now");

        Assert.True(result.HasFilteredContent);
        Assert.Single(result.Matches);
        Assert.Equal("010-1234-5678", result.Matches[0].Value);
    }

    #endregion

    #region AddRule / AddBlocklistWord

    [Fact]
    public void AddRule_WithCategory_CreatesRule()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Spam
        };
        var filter = new RuleBasedFilter(options);

        filter.AddRule(ContentCategory.Spam, @"buy\s+now", 0.95f);

        Assert.True(filter.ContainsFilteredContent("Click to buy now!"));
    }

    [Fact]
    public void AddBlocklistWord_AddsEscapedPattern()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom
        };
        var filter = new RuleBasedFilter(options);

        filter.AddBlocklistWord("special.chars+here");

        // The word should be regex-escaped
        Assert.True(filter.ContainsFilteredContent("This has special.chars+here text"));
    }

    #endregion

    #region ContainsFilteredContent

    [Fact]
    public void ContainsFilteredContent_NullInput_ReturnsFalse()
    {
        var filter = new RuleBasedFilter();

        Assert.False(filter.ContainsFilteredContent(null!));
    }

    [Fact]
    public void ContainsFilteredContent_EmptyInput_ReturnsFalse()
    {
        var filter = new RuleBasedFilter();

        Assert.False(filter.ContainsFilteredContent(""));
    }

    #endregion

    #region Filter — Multiple Matches

    [Fact]
    public void Filter_MultipleMatches_ReturnsAll()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Replace,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bad", "evil" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("bad and evil together");

        Assert.True(result.HasFilteredContent);
        Assert.Equal(2, result.Matches.Count);
    }

    #endregion

    #region Filter — Text Preservation

    [Fact]
    public void Filter_PreservesSurroundingText()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Redact,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "badword" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("Before badword after");

        Assert.StartsWith("Before ", result.FilteredText);
        Assert.EndsWith(" after", result.FilteredText);
    }

    #endregion

    #region ContentFilterResult

    [Fact]
    public void FilterResult_Blocked_GetSummary()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Block,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "blocked" }
        };
        var filter = new RuleBasedFilter(options);

        var result = filter.Filter("This is blocked content");

        Assert.Contains("blocked", result.GetSummary().ToLowerInvariant());
    }

    [Fact]
    public void FilterResult_NoMatch_GetSummary()
    {
        var filter = new RuleBasedFilter();

        var result = filter.Filter("Clean text");

        Assert.Equal("No filtered content detected.", result.GetSummary());
    }

    #endregion

    #region ContentFilterOptions Factory Methods

    [Fact]
    public void Options_Default_UsesReplaceAction()
    {
        var options = ContentFilterOptions.Default;

        Assert.Equal(FilterAction.Replace, options.DefaultAction);
        Assert.Equal(ContentCategory.Common, options.CategoriesToFilter);
    }

    [Fact]
    public void Options_Strict_UsesBlockAction()
    {
        var options = ContentFilterOptions.Strict;

        Assert.Equal(FilterAction.Block, options.DefaultAction);
        Assert.Equal(ContentCategory.All, options.CategoriesToFilter);
        Assert.Equal(0.7f, options.MinConfidence);
    }

    [Fact]
    public void Options_Lenient_UsesRedactAction()
    {
        var options = ContentFilterOptions.Lenient;

        Assert.Equal(FilterAction.Redact, options.DefaultAction);
        Assert.Equal(0.9f, options.MinConfidence);
    }

    [Fact]
    public void Options_FlagOnly_UsesFlagAction()
    {
        var options = ContentFilterOptions.FlagOnly;

        Assert.Equal(FilterAction.Flag, options.DefaultAction);
    }

    [Fact]
    public void Options_GetReplacementText_DefaultTokens()
    {
        var options = new ContentFilterOptions();

        Assert.Equal("[PROFANITY]", options.GetReplacementText(ContentCategory.Profanity));
        Assert.Equal("[HATE_SPEECH]", options.GetReplacementText(ContentCategory.HateSpeech));
        Assert.Equal("[VIOLENCE]", options.GetReplacementText(ContentCategory.Violence));
        Assert.Equal("[FILTERED]", options.GetReplacementText(ContentCategory.Custom));
    }

    [Fact]
    public void Options_GetAction_CustomOverridesDefault()
    {
        var options = new ContentFilterOptions
        {
            DefaultAction = FilterAction.Replace,
            CustomActions = new Dictionary<ContentCategory, FilterAction>
            {
                { ContentCategory.Violence, FilterAction.Block }
            }
        };

        Assert.Equal(FilterAction.Block, options.GetAction(ContentCategory.Violence));
        Assert.Equal(FilterAction.Replace, options.GetAction(ContentCategory.Profanity));
    }

    #endregion
}
