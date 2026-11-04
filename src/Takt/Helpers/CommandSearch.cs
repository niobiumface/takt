namespace Takt.Helpers;

public readonly record struct CommandEntry(string Id, string Title, string Keywords, string Section);

public static class CommandSearch
{
    public static IReadOnlyList<CommandEntry> Rank(string? query, IReadOnlyList<CommandEntry> entries)
    {
        if (entries.Count == 0)
            return entries;
        if (string.IsNullOrWhiteSpace(query))
            return entries;

        return entries
            .Select(entry => (entry, score: Score(query, entry.Title + " " + entry.Keywords)))
            .Where(pair => pair.score > 0)
            .OrderByDescending(pair => pair.score)
            .ThenBy(pair => pair.entry.Title, StringComparer.CurrentCultureIgnoreCase)
            .Select(pair => pair.entry)
            .ToArray();
    }

    public static int Score(string query, string text)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(text))
            return 0;
        var hay = text.ToLowerInvariant();
        var needle = query.Trim().ToLowerInvariant();
        var index = hay.IndexOf(needle, StringComparison.Ordinal);
        if (index >= 0)
            return 1_000 - index;

        var needleIndex = 0;
        var score = 0;
        var streak = 0;
        foreach (var ch in hay)
        {
            if (needleIndex < needle.Length && ch == needle[needleIndex])
            {
                needleIndex++;
                streak++;
                score += streak * 3;
            }
            else
            {
                streak = 0;
            }
        }

        return needleIndex == needle.Length ? score : 0;
    }
}
