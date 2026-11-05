using Takt.Models;

namespace Takt.Helpers;

public readonly record struct AnchorSnapshot(int Left, int Top, string Process, string Title);

public static class AnchorNames
{
    public static string Process(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        var trimmed = path.Trim().Trim('"');
        var slash = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
        var file = slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
        var dot = file.LastIndexOf('.');
        return dot > 0 ? file[..dot] : file;
    }
}

public static class MacroAnchor
{
    public static (int RelX, int RelY) Offset(int x, int y, int left, int top) => (x - left, y - top);

    public static (int X, int Y) Place(int relX, int relY, int left, int top) => (left + relX, top + relY);

    public static (int X, int Y) Resolve(MacroEvent item, Func<string, string, AnchorSnapshot?>? find)
    {
        if (!item.Anchored || string.IsNullOrWhiteSpace(item.AnchorProcess) || find is null)
            return (item.X, item.Y);
        var box = find(item.AnchorProcess, item.AnchorTitle ?? "");
        if (box is null)
            return (item.X, item.Y);
        return Place(item.RelX, item.RelY, box.Value.Left, box.Value.Top);
    }
}

public static class MacroPace
{
    public static int Scale(int delayMs, MacroSpeed speed)
    {
        var delay = Math.Max(0, delayMs);
        return speed switch
        {
            MacroSpeed.Half => delay * 2,
            MacroSpeed.Double => delay / 2,
            _ => delay
        };
    }

    public static int PlaybackDelay(int delayMs, string? kind, MacroSpeed speed)
    {
        var cap = MacroEdit.IsWait(kind) ? 60_000 : 2_000;
        var limited = Math.Min(Math.Max(0, delayMs), cap);
        var scaled = Scale(limited, speed);
        var scaledCap = speed == MacroSpeed.Half ? cap * 2 : cap;
        return Math.Min(scaled, scaledCap);
    }

    public static int TextGap(MacroSpeed speed) => speed switch
    {
        MacroSpeed.Half => 16,
        MacroSpeed.Double => 4,
        _ => 8
    };
}

public readonly record struct PreviewDot(double X, double Y);

public static class MacroPath
{
    public static IReadOnlyList<PreviewDot> Dots(IEnumerable<MacroEvent>? events, double width, double height)
    {
        var raw = new List<(double X, double Y)>();
        if (events is null)
            return [];
        foreach (var item in events)
        {
            if (item is null || MacroEdit.IsWait(item.Kind) || MacroEdit.IsText(item.Kind))
                continue;
            raw.Add(item.Anchored ? (item.RelX, item.RelY) : (item.X, item.Y));
        }

        if (raw.Count == 0)
            return [];

        var minX = raw.Min(point => point.X);
        var maxX = raw.Max(point => point.X);
        var minY = raw.Min(point => point.Y);
        var maxY = raw.Max(point => point.Y);
        var spanX = Math.Max(1, maxX - minX);
        var spanY = Math.Max(1, maxY - minY);
        const double pad = 6;
        var innerW = Math.Max(1, width - pad * 2);
        var innerH = Math.Max(1, height - pad * 2);
        return raw.Select(point => new PreviewDot(
            pad + (point.X - minX) / spanX * innerW,
            pad + (point.Y - minY) / spanY * innerH)).ToArray();
    }
}

public static class MacroText
{
    public static IEnumerable<char> Keys(string? text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;
        for (var i = 0; i < text.Length; i++)
        {
            var value = text[i];
            if (value == '\n' && i > 0 && text[i - 1] == '\r')
                continue;
            yield return value == '\n' ? '\r' : value;
        }
    }
}
