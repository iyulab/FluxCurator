namespace FluxCurator.Core.Infrastructure.Filtering;

using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Manages multiple content filters and coordinates filtering operations.
/// </summary>
public sealed class ContentFilterManager : IContentFilterManager
{
    private readonly SortedList<int, IContentFilter> _filters = new();
    private readonly Dictionary<string, int> _filterPriorities = new();

    /// <summary>
    /// Creates a new content filter manager with default options.
    /// </summary>
    public ContentFilterManager() : this(ContentFilterOptions.Default)
    {
    }

    /// <summary>
    /// Creates a new content filter manager with specified options.
    /// </summary>
    public ContentFilterManager(ContentFilterOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        RegisterDefaultFilters();
    }

    /// <inheritdoc/>
    public ContentFilterOptions Options { get; }

    /// <inheritdoc/>
    public void RegisterFilter(IContentFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Remove existing filter with same name
        RemoveFilter(filter.Name);

        _filters.Add(filter.Priority, filter);
        _filterPriorities[filter.Name] = filter.Priority;
    }

    /// <inheritdoc/>
    public bool RemoveFilter(string filterName)
    {
        if (_filterPriorities.TryGetValue(filterName, out var priority))
        {
            _filters.Remove(priority);
            _filterPriorities.Remove(filterName);
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public ContentFilterResult Filter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return ContentFilterResult.NoMatch(text ?? string.Empty, Options);

        var allMatches = new List<ContentMatch>();
        var currentText = text;
        bool isBlocked = false;

        // Run all filters in priority order
        foreach (var filter in _filters.Values)
        {
            var result = filter.Filter(currentText);

            if (result.IsBlocked)
            {
                isBlocked = true;
                allMatches.AddRange(result.Matches);
                break;
            }

            if (result.HasFilteredContent)
            {
                // Adjust match positions if text has been modified
                if (currentText != text)
                {
                    // For simplicity, we collect all matches from original text
                    // In a production system, you'd need to track position changes
                }
                allMatches.AddRange(result.Matches);
                currentText = result.FilteredText;
            }
        }

        if (isBlocked)
        {
            return ContentFilterResult.Blocked(text, allMatches, Options);
        }

        if (allMatches.Count == 0)
        {
            return ContentFilterResult.NoMatch(text, Options);
        }

        return new ContentFilterResult
        {
            OriginalText = text,
            FilteredText = currentText,
            Matches = allMatches,
            Options = Options,
            IsBlocked = false
        };
    }

    /// <inheritdoc/>
    public bool ContainsFilteredContent(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var filter in _filters.Values)
        {
            if (filter.ContainsFilteredContent(text))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all registered filter names.
    /// </summary>
    public IReadOnlyList<string> GetRegisteredFilters()
    {
        return _filterPriorities.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Registers default filters based on options.
    /// </summary>
    private void RegisterDefaultFilters()
    {
        // Register rule-based filter as default
        RegisterFilter(new RuleBasedFilter(Options));
    }
}
