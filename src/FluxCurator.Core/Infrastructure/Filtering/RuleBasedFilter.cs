namespace FluxCurator.Core.Infrastructure.Filtering;

using System.Text.RegularExpressions;
using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;

/// <summary>
/// Rule-based content filter using patterns and blocklists.
/// </summary>
public sealed class RuleBasedFilter : IContentFilter
{
    private readonly ContentFilterOptions _options;
    private readonly Dictionary<ContentCategory, List<FilterRule>> _rules = new();

    /// <summary>
    /// Creates a new rule-based filter with default options.
    /// </summary>
    public RuleBasedFilter() : this(ContentFilterOptions.Default)
    {
    }

    /// <summary>
    /// Creates a new rule-based filter with specified options.
    /// </summary>
    public RuleBasedFilter(ContentFilterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        InitializeDefaultRules();
    }

    /// <inheritdoc/>
    public string Name => "RuleBasedFilter";

    /// <inheritdoc/>
    public int Priority => 100;

    /// <inheritdoc/>
    public ContentFilterResult Filter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return ContentFilterResult.NoMatch(text ?? string.Empty, _options);

        var matches = DetectMatches(text);

        if (matches.Count == 0)
            return ContentFilterResult.NoMatch(text, _options);

        // Check if any match requires blocking
        var shouldBlock = matches.Any(m =>
            _options.GetAction(m.Category) == FilterAction.Block);

        if (shouldBlock)
            return ContentFilterResult.Blocked(text, matches, _options);

        // Apply filtering based on action
        var filteredText = ApplyFiltering(text, matches);

        return new ContentFilterResult
        {
            OriginalText = text,
            FilteredText = filteredText,
            Matches = matches,
            Options = _options,
            IsBlocked = false
        };
    }

    /// <inheritdoc/>
    public bool ContainsFilteredContent(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var (category, rules) in _rules)
        {
            if (!_options.CategoriesToFilter.HasFlag(category))
                continue;

            foreach (var rule in rules)
            {
                if (rule.CompiledPattern.IsMatch(text))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds a custom rule.
    /// </summary>
    public void AddRule(ContentCategory category, string pattern, float confidence = 1.0f)
    {
        if (!_rules.ContainsKey(category))
            _rules[category] = new List<FilterRule>();

        _rules[category].Add(new FilterRule(pattern, _options.CaseInsensitive, confidence));
    }

    /// <summary>
    /// Adds a word to the blocklist.
    /// </summary>
    public void AddBlocklistWord(string word, ContentCategory category = ContentCategory.Custom)
    {
        var pattern = _options.WholeWordOnly
            ? $@"\b{Regex.Escape(word)}\b"
            : Regex.Escape(word);

        AddRule(category, pattern);
    }

    /// <summary>
    /// Detects all matches in the text.
    /// </summary>
    private List<ContentMatch> DetectMatches(string text)
    {
        var matches = new List<ContentMatch>();

        foreach (var (category, rules) in _rules)
        {
            if (!_options.CategoriesToFilter.HasFlag(category))
                continue;

            foreach (var rule in rules)
            {
                var regexMatches = rule.CompiledPattern.Matches(text);
                foreach (Match match in regexMatches)
                {
                    // Check allowlist
                    if (_options.CustomAllowlist.Contains(match.Value))
                        continue;

                    if (rule.Confidence >= _options.MinConfidence)
                    {
                        matches.Add(ContentMatch.Create(
                            category,
                            match.Value,
                            match.Index,
                            rule.Pattern,
                            rule.Confidence));
                    }
                }
            }
        }

        // Sort and remove overlaps
        return ResolveOverlaps(matches);
    }

    /// <summary>
    /// Applies filtering to the text based on detected matches.
    /// </summary>
    private string ApplyFiltering(string text, IReadOnlyList<ContentMatch> matches)
    {
        if (matches.Count == 0)
            return text;

        var sb = new System.Text.StringBuilder(text.Length);
        int currentPos = 0;

        foreach (var match in matches.OrderBy(m => m.StartIndex))
        {
            // Add text before this match
            if (match.StartIndex > currentPos)
            {
                sb.Append(text[currentPos..match.StartIndex]);
            }

            // Apply action
            var action = _options.GetAction(match.Category);
            var replacement = action switch
            {
                FilterAction.Remove => string.Empty,
                FilterAction.Replace => _options.GetReplacementText(match.Category),
                FilterAction.Redact => new string(_options.RedactCharacter, match.Length),
                FilterAction.Flag => match.Value, // Keep original
                _ => match.Value
            };

            match.ReplacementValue = replacement;
            sb.Append(replacement);

            currentPos = match.EndIndex;
        }

        // Add remaining text
        if (currentPos < text.Length)
        {
            sb.Append(text[currentPos..]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Resolves overlapping matches.
    /// </summary>
    private static List<ContentMatch> ResolveOverlaps(List<ContentMatch> matches)
    {
        if (matches.Count <= 1)
            return matches;

        matches.Sort((a, b) =>
        {
            var posCompare = a.StartIndex.CompareTo(b.StartIndex);
            if (posCompare != 0)
                return posCompare;
            return b.Length.CompareTo(a.Length);
        });

        var result = new List<ContentMatch>();
        int lastEnd = -1;

        foreach (var match in matches)
        {
            if (match.StartIndex < lastEnd)
                continue;

            result.Add(match);
            lastEnd = match.EndIndex;
        }

        return result;
    }

    /// <summary>
    /// Initializes default filtering rules.
    /// </summary>
    private void InitializeDefaultRules()
    {
        // Add custom blocklist words from options
        foreach (var word in _options.CustomBlocklist)
        {
            AddBlocklistWord(word, ContentCategory.Custom);
        }

        // Add custom patterns from options
        foreach (var (name, pattern) in _options.CustomPatterns)
        {
            AddRule(ContentCategory.Custom, pattern);
        }

        // Note: We don't include predefined profanity/hate speech lists
        // as they vary by culture and use case. Users should provide
        // their own blocklists via options.CustomBlocklist
    }

    /// <summary>
    /// Internal filter rule representation.
    /// </summary>
    private sealed class FilterRule
    {
        public string Pattern { get; }
        public Regex CompiledPattern { get; }
        public float Confidence { get; }

        public FilterRule(string pattern, bool caseInsensitive, float confidence)
        {
            Pattern = pattern;
            Confidence = confidence;

            var options = RegexOptions.Compiled;
            if (caseInsensitive)
                options |= RegexOptions.IgnoreCase;

            CompiledPattern = new Regex(pattern, options);
        }
    }
}
