namespace FluxCurator.Core.Core;

using FluxCurator.Core.Domain;

/// <summary>
/// Interface for content filtering operations.
/// </summary>
public interface IContentFilter
{
    /// <summary>
    /// Gets the name of this filter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority of this filter (lower = higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Filters the given text and returns a result.
    /// </summary>
    /// <param name="text">The text to filter.</param>
    /// <returns>The filtering result.</returns>
    ContentFilterResult Filter(string text);

    /// <summary>
    /// Checks if the text contains any filtered content.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if filtered content is found.</returns>
    bool ContainsFilteredContent(string text);
}

/// <summary>
/// Interface for managing content filtering operations.
/// </summary>
public interface IContentFilterManager
{
    /// <summary>
    /// Gets the configured filtering options.
    /// </summary>
    ContentFilterOptions Options { get; }

    /// <summary>
    /// Filters the given text using all registered filters.
    /// </summary>
    /// <param name="text">The text to filter.</param>
    /// <returns>The combined filtering result.</returns>
    ContentFilterResult Filter(string text);

    /// <summary>
    /// Checks if the text contains any filtered content.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if any filtered content is found.</returns>
    bool ContainsFilteredContent(string text);

    /// <summary>
    /// Registers a custom filter.
    /// </summary>
    /// <param name="filter">The filter to register.</param>
    void RegisterFilter(IContentFilter filter);

    /// <summary>
    /// Removes a filter by name.
    /// </summary>
    /// <param name="filterName">The name of the filter to remove.</param>
    /// <returns>True if the filter was removed.</returns>
    bool RemoveFilter(string filterName);
}
