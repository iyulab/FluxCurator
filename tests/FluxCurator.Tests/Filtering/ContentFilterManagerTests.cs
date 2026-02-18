namespace FluxCurator.Tests.Filtering;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.Filtering;

public class ContentFilterManagerTests
{
    #region Constructor

    [Fact]
    public void Constructor_Default_RegistersRuleBasedFilter()
    {
        var manager = new ContentFilterManager();

        var filters = manager.GetRegisteredFilters();

        Assert.Single(filters);
        Assert.Contains("RuleBasedFilter", filters);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentFilterManager(null!));
    }

    [Fact]
    public void Constructor_WithOptions_UsesProvidedOptions()
    {
        var options = ContentFilterOptions.Strict;
        var manager = new ContentFilterManager(options);

        Assert.Same(options, manager.Options);
    }

    #endregion

    #region RegisterFilter / RemoveFilter

    [Fact]
    public void RegisterFilter_NullFilter_ThrowsArgumentNull()
    {
        var manager = new ContentFilterManager();

        Assert.Throws<ArgumentNullException>(() => manager.RegisterFilter(null!));
    }

    [Fact]
    public void RemoveFilter_ExistingFilter_ReturnsTrue()
    {
        var manager = new ContentFilterManager();

        var removed = manager.RemoveFilter("RuleBasedFilter");

        Assert.True(removed);
        Assert.Empty(manager.GetRegisteredFilters());
    }

    [Fact]
    public void RemoveFilter_NonExistentFilter_ReturnsFalse()
    {
        var manager = new ContentFilterManager();

        var removed = manager.RemoveFilter("NonExistent");

        Assert.False(removed);
    }

    #endregion

    #region Filter — Empty/Null Input

    [Fact]
    public void Filter_NullInput_ReturnsNoMatch()
    {
        var manager = new ContentFilterManager();

        var result = manager.Filter(null!);

        Assert.Equal("", result.FilteredText);
        Assert.False(result.HasFilteredContent);
    }

    [Fact]
    public void Filter_EmptyInput_ReturnsNoMatch()
    {
        var manager = new ContentFilterManager();

        var result = manager.Filter("");

        Assert.False(result.HasFilteredContent);
    }

    #endregion

    #region Filter — Delegates to Registered Filters

    [Fact]
    public void Filter_WithBlocklist_FiltersContent()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Replace,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "blocked" }
        };
        var manager = new ContentFilterManager(options);

        var result = manager.Filter("This is blocked content");

        Assert.True(result.HasFilteredContent);
        Assert.DoesNotContain("blocked", result.FilteredText);
    }

    [Fact]
    public void Filter_BlockAction_ReturnsBlocked()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Block,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "danger" }
        };
        var manager = new ContentFilterManager(options);

        var result = manager.Filter("This is danger text");

        Assert.True(result.IsBlocked);
        Assert.Equal("", result.FilteredText);
    }

    [Fact]
    public void Filter_NoMatch_ReturnsOriginal()
    {
        var manager = new ContentFilterManager();

        var result = manager.Filter("Clean and safe text");

        Assert.Equal("Clean and safe text", result.FilteredText);
        Assert.False(result.HasFilteredContent);
    }

    #endregion

    #region ContainsFilteredContent

    [Fact]
    public void ContainsFilteredContent_NullInput_ReturnsFalse()
    {
        var manager = new ContentFilterManager();

        Assert.False(manager.ContainsFilteredContent(null!));
    }

    [Fact]
    public void ContainsFilteredContent_EmptyInput_ReturnsFalse()
    {
        var manager = new ContentFilterManager();

        Assert.False(manager.ContainsFilteredContent(""));
    }

    [Fact]
    public void ContainsFilteredContent_WithMatch_ReturnsTrue()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "match" }
        };
        var manager = new ContentFilterManager(options);

        Assert.True(manager.ContainsFilteredContent("This is a match"));
    }

    [Fact]
    public void ContainsFilteredContent_NoMatch_ReturnsFalse()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "match" }
        };
        var manager = new ContentFilterManager(options);

        Assert.False(manager.ContainsFilteredContent("No matching word"));
    }

    #endregion

    #region GetRegisteredFilters

    [Fact]
    public void GetRegisteredFilters_NoFilters_ReturnsEmpty()
    {
        var manager = new ContentFilterManager();
        manager.RemoveFilter("RuleBasedFilter");

        Assert.Empty(manager.GetRegisteredFilters());
    }

    #endregion

    #region ContentFilterResult Properties

    [Fact]
    public void FilterResult_CountByCategory_CorrectCount()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Flag,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "one", "two" }
        };
        var manager = new ContentFilterManager(options);

        var result = manager.Filter("one and two found");

        Assert.True(result.CountByCategory.ContainsKey(ContentCategory.Custom));
        Assert.Equal(2, result.CountByCategory[ContentCategory.Custom]);
    }

    [Fact]
    public void FilterResult_DetectedCategories_ContainsCustom()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Flag,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "word" }
        };
        var manager = new ContentFilterManager(options);

        var result = manager.Filter("A word here");

        Assert.Contains(ContentCategory.Custom, result.DetectedCategories);
    }

    [Fact]
    public void FilterResult_GetSummary_WithFiltered_IncludesCount()
    {
        var options = new ContentFilterOptions
        {
            CategoriesToFilter = ContentCategory.Custom,
            DefaultAction = FilterAction.Flag,
            CustomBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "word" }
        };
        var manager = new ContentFilterManager(options);

        var result = manager.Filter("A word here");

        Assert.Contains("Filtered", result.GetSummary());
        Assert.Contains("Custom", result.GetSummary());
    }

    #endregion
}
