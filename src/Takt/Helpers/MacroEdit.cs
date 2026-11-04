using Takt.Models;

namespace Takt.Helpers;

public static class MacroEdit
{
    public static bool Move<T>(IList<T> items, int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || index >= items.Count || target < 0 || target >= items.Count)
            return false;
        var item = items[index];
        items.RemoveAt(index);
        items.Insert(target, item);
        return true;
    }

    public static bool IsWait(string? kind) =>
        string.Equals(kind, "wait", StringComparison.OrdinalIgnoreCase);

    public static MacroEvent Wait(int milliseconds) => new()
    {
        Kind = "wait",
        DelayMs = Math.Max(0, milliseconds)
    };
}
