namespace FluxCurator.Core.Infrastructure.Chunking;

using FluxCurator.Core.Core;
using FluxCurator.Core.Domain;
using System.Text.RegularExpressions;
using FluxCurator.Core.Infrastructure.Languages;

/// <summary>
/// Keeps Markdown pipe tables whole across any chunking strategy. Text between tables goes to the inner chunker; each
/// table becomes its own chunk when it fits <see cref="ChunkOptions.MaxChunkSize"/>, and otherwise is split only between
/// rows with its header row(s) repeated at the top of every piece — so a chunk holding table rows always says which column
/// each value belongs to, and no chunk (or overlap) starts inside a row.
/// </summary>
/// <remarks>
/// Table chunks carry <c>Metadata.Custom</c> entries <c>table</c> (true), <c>table_index</c> (0-based position of the
/// table among the tables of the input text), <c>table_piece</c> / <c>table_pieces</c>
/// (1-based piece and count) and <c>table_row_start</c> / <c>table_row_end</c> (0-based body rows, header excluded), and
/// <c>table_context</c> when the line(s) naming the table are repeated above the header (<see cref="ChunkOptions.TableContextLines"/>).
/// Turn it off with <see cref="ChunkOptions.PreserveTables"/> = false.
/// </remarks>
public sealed partial class TableAwareChunker : IChunker
{
    private readonly IChunker _inner;

    /// <summary>Wraps <paramref name="inner"/>, which chunks everything that is not a table.</summary>
    public TableAwareChunker(IChunker inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc/>
    public string StrategyName => _inner.StrategyName;

    /// <inheritdoc/>
    public bool RequiresEmbedder => _inner.RequiresEmbedder;

    /// <inheritdoc/>
    public int EstimateChunkCount(string text, ChunkOptions options) => _inner.EstimateChunkCount(text, options);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        ChunkOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.PreserveTables || string.IsNullOrWhiteSpace(text))
            return await _inner.ChunkAsync(text, options, cancellationToken).ConfigureAwait(false);

        var segments = SplitIntoSegments(text);
        if (!segments.Any(s => s.IsTable))
            return await _inner.ChunkAsync(text, options, cancellationToken).ConfigureAwait(false);

        var profile = string.IsNullOrEmpty(options.LanguageCode)
            ? LanguageProfileRegistry.Instance.DetectProfile(text)
            : LanguageProfileRegistry.Instance.GetProfile(options.LanguageCode);

        var chunks = new List<DocumentChunk>();
        var tableIndex = 0;
        foreach (var segment in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (segment.IsTable)
            {
                chunks.AddRange(TableChunks(text, segment, tableIndex++, options, profile));
                continue;
            }

            var prose = text[segment.Start..segment.End];
            if (string.IsNullOrWhiteSpace(prose))
                continue;

            foreach (var chunk in await _inner.ChunkAsync(prose, options, cancellationToken).ConfigureAwait(false))
            {
                chunk.Location.StartPosition += segment.Start;
                chunk.Location.EndPosition += segment.Start;
                chunks.Add(chunk);
            }
        }

        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i].ChunkIndex = i;
            chunks[i].TotalChunks = chunks.Count;
        }

        return chunks;
    }

    private IEnumerable<DocumentChunk> TableChunks(string text, Segment table, int tableIndex, ChunkOptions options, ILanguageProfile profile)
    {
        var context = TableContext(text, table.Start, options.TableContextLines);
        var prefix = context is null ? string.Empty : context + "\n";
        var rows = table.Lines;
        var headerCount = rows.Count > 1 && IsDelimiter(text[rows[1].Start..rows[1].End]) ? 2 : 0;
        var header = string.Join("\n", rows.Take(headerCount).Select(r => text[r.Start..r.End]));
        var body = rows.Skip(headerCount).ToList();
        var whole = text[table.Start..table.End];

        if (body.Count == 0 || profile.EstimateTokenCount(whole) <= options.MaxChunkSize)
        {
            yield return TableChunk(prefix + whole, table.Start, table.End, tableIndex, piece: 1, pieces: 1, rowStart: 0, rowEnd: body.Count - 1, context, profile);
            yield break;
        }

        // Split between rows; every piece starts with the header. A single row larger than the limit is a piece of its own
        // rather than being cut inside a cell.
        var pieces = new List<(int From, int To)>();
        var from = 0;
        var headerTokens = (header.Length == 0 ? 0 : profile.EstimateTokenCount(header)) +
                           (context is null ? 0 : profile.EstimateTokenCount(context));
        var tokens = headerTokens;
        for (var i = 0; i < body.Count; i++)
        {
            var rowTokens = profile.EstimateTokenCount(text[body[i].Start..body[i].End]);
            if (i > from && tokens + rowTokens > options.MaxChunkSize)
            {
                pieces.Add((from, i - 1));
                from = i;
                tokens = headerTokens;
            }

            tokens += rowTokens;
        }

        pieces.Add((from, body.Count - 1));

        for (var p = 0; p < pieces.Count; p++)
        {
            var (first, last) = pieces[p];
            var rowsText = text[body[first].Start..body[last].End];
            var content = prefix + (header.Length == 0 ? rowsText : header + "\n" + rowsText);
            var start = p == 0 ? table.Start : body[first].Start;
            yield return TableChunk(content, start, body[last].End, tableIndex, p + 1, pieces.Count, first, last, context, profile);
        }
    }

    private DocumentChunk TableChunk(string content, int start, int end, int tableIndex, int piece, int pieces, int rowStart, int rowEnd, string? context, ILanguageProfile profile)
    {
        var custom = new Dictionary<string, object>
        {
            ["table"] = true,
            ["table_index"] = tableIndex,
            ["table_piece"] = piece,
            ["table_pieces"] = pieces,
            ["table_row_start"] = rowStart,
            ["table_row_end"] = rowEnd,
        };
        if (context is not null)
            custom["table_context"] = context;

        return new()
        {
            Content = content,
            Location = new ChunkLocation { StartPosition = start, EndPosition = end },
            Metadata = new ChunkMetadata
            {
                LanguageCode = profile.LanguageCode,
                EstimatedTokenCount = profile.EstimateTokenCount(content),
                Strategy = StrategyOf(_inner),
                StartsAtSentenceBoundary = true,
                EndsAtSentenceBoundary = true,
                Custom = custom,
            },
        };
    }

    /// <summary>The longest line, in characters, that counts as a line naming the table (a title or a unit line).</summary>
    private const int MaxContextLineLength = 80;

    /// <summary>How far above a table, in non-blank lines, a caption is looked for.</summary>
    private const int CaptionWindow = 8;

    /// <summary>
    /// The line(s) that name the table starting at <paramref name="tableStart"/>. A caption-shaped line
    /// (<see cref="IsCaption"/>) within <see cref="CaptionWindow"/> non-blank lines above it comes first, since it is what
    /// names the table even when notes or stray header cells sit between it; then the short lines directly above the table
    /// fill the remaining slots (blank and note lines skipped, stopping at the first longer line or table row). With
    /// neither, the nearest heading above it. Null when there is none or <paramref name="maxLines"/> is 0.
    /// </summary>
    internal static string? TableContext(string text, int tableStart, int maxLines)
    {
        if (maxLines <= 0 || tableStart <= 0)
            return null;

        var above = text[..tableStart].Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        var caption = FindCaption(above);
        var picked = new List<string>();
        var slots = caption is null ? maxLines : maxLines - 1;
        for (var i = above.Length - 1; i >= 0 && picked.Count < slots; i--)
        {
            var line = above[i].Trim();
            if (line.Length == 0 || IsNote(line))
                continue;
            if (line == caption)
                break;
            // A title or unit line is short and is not a sentence; a line ending like one is the prose above the table.
            if (line.Length > MaxContextLineLength || line.Count(c => c == '|') >= 2 || EndsLikeASentence(line))
                break;
            picked.Insert(0, line);
        }

        if (caption is not null)
            picked.Insert(0, caption);

        if (picked.Count > 0)
            return string.Join("\n", picked);

        for (var i = above.Length - 1; i >= 0; i--)
        {
            var line = above[i].Trim();
            if (line.StartsWith('#') && line.TrimStart('#').StartsWith(' ') && line.Length <= MaxContextLineLength * 2)
                return line;
        }

        return null;
    }

    /// <summary>
    /// The nearest caption-shaped line within <see cref="CaptionWindow"/> non-blank lines above a table, stopping at a
    /// table row (that caption would belong to the table above).
    /// </summary>
    private static string? FindCaption(string[] above)
    {
        var seen = 0;
        for (var i = above.Length - 1; i >= 0 && seen < CaptionWindow; i--)
        {
            var line = above[i].Trim();
            if (line.Length == 0)
                continue;
            if (line.Count(c => c == '|') >= 2)
                return null;
            if (line.Length <= MaxContextLineLength * 2 && IsCaption(line))
                return line;
            seen++;
        }

        return null;
    }

    /// <summary>
    /// A table caption: «&lt;표 5&gt; …», «[표 5] …», «표 5. …», «표 5-1 …», «Table 5: …», «Tab. 5 …» (a Markdown heading
    /// marker in front is allowed). Figure captions do not name a table.
    /// </summary>
    internal static bool IsCaption(string line) => CaptionPattern().IsMatch(line);

    /// <summary>
    /// A note under a table or text block, not a line that names a table: footnotes («3. …» ending as a sentence, «4) …»),
    /// markers («*», «※», «주)», «주:», «Note:») and source lines («자료출처:», «자료:», «출처:», «Source:»).
    /// </summary>
    internal static bool IsNote(string line) =>
        NoteMarkerPattern().IsMatch(line) || (NumberedLinePattern().IsMatch(line) && EndsLikeASentence(line));

    [GeneratedRegex(@"^(#+\s*)?(<\s*(표|table|tab\.?)\s*\d+[^>]*>|\[\s*(표|table|tab\.?)\s*\d+[^\]]*\]|(표|table|tab\.)\s*\d+([-.]\d+)*\s*([.:)]|\s|$))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CaptionPattern();

    [GeneratedRegex(@"^(\*|※|주\s*[):]|注|note\s*:|notes\s*:|-?\s*자료\s*출처\s*:|-?\s*자료\s*:|-?\s*출처\s*:|-?\s*source\s*:|\d{1,2}\)\s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoteMarkerPattern();

    [GeneratedRegex(@"^\d{1,2}\.\s")]
    private static partial Regex NumberedLinePattern();

    private static bool EndsLikeASentence(string line) => line[^1] is '.' or '!' or '?' or '。' or '！' or '？';

    private static ChunkingStrategy StrategyOf(IChunker chunker) =>
        Enum.TryParse<ChunkingStrategy>(chunker.StrategyName, ignoreCase: true, out var strategy) ? strategy : default;

    private sealed record Line(int Start, int End);

    private sealed record Segment(int Start, int End, bool IsTable, IReadOnlyList<Line> Lines);

    /// <summary>
    /// Splits <paramref name="text"/> into prose and table segments. A table is a run of pipe rows that has a delimiter
    /// row or at least two rows; a lone line with pipes stays prose.
    /// </summary>
    private static List<Segment> SplitIntoSegments(string text)
    {
        var lines = new List<Line>();
        var pos = 0;
        while (pos <= text.Length)
        {
            var nl = text.IndexOf('\n', pos);
            var end = nl < 0 ? text.Length : nl;
            var lineEnd = end > pos && text[end - 1] == '\r' ? end - 1 : end;
            lines.Add(new Line(pos, lineEnd));
            if (nl < 0)
                break;
            pos = nl + 1;
        }

        var segments = new List<Segment>();
        var proseStart = 0;
        var i = 0;
        while (i < lines.Count)
        {
            if (!IsRow(text, lines[i]))
            {
                i++;
                continue;
            }

            var j = i;
            while (j + 1 < lines.Count && IsRow(text, lines[j + 1]))
                j++;

            var block = lines.GetRange(i, j - i + 1);
            if (block.Count >= 2)
            {
                if (lines[i].Start > proseStart)
                    segments.Add(new Segment(proseStart, lines[i].Start, false, []));
                segments.Add(new Segment(lines[i].Start, lines[j].End, true, block));
                proseStart = lines[j].End;
            }

            i = j + 1;
        }

        if (proseStart < text.Length)
            segments.Add(new Segment(proseStart, text.Length, false, []));

        return segments;
    }

    private static bool IsRow(string text, Line line)
    {
        var s = text.AsSpan(line.Start, line.End - line.Start).Trim();
        if (s.Length == 0 || s[0] == '>')
            return false;

        var pipes = 0;
        for (var k = 0; k < s.Length; k++)
        {
            if (s[k] == '|' && (k == 0 || s[k - 1] != '\\'))
                pipes++;
        }

        return pipes >= 2;
    }

    private static bool IsDelimiter(string line)
    {
        var t = line.Trim().Trim('|');
        if (t.Length == 0)
            return false;

        foreach (var cell in t.Split('|'))
        {
            var c = cell.Trim();
            if (c.Length < 3 || c.Trim(':').Length == 0 || c.Trim(':').Any(ch => ch != '-'))
                return false;
        }

        return true;
    }
}
