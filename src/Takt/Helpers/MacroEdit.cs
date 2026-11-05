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

    public static bool IsText(string? kind) =>
        string.Equals(kind, "text", StringComparison.OrdinalIgnoreCase);

    public static MacroEvent Wait(int milliseconds) => new()
    {
        Kind = "wait",
        DelayMs = Math.Max(0, milliseconds)
    };

    public static MacroEvent Text(string? text, int milliseconds = 0) => new()
    {
        Kind = "text",
        Text = text ?? "",
        DelayMs = Math.Max(0, milliseconds)
    };

    public static MacroEvent Clone(MacroEvent item) => new()
    {
        DelayMs = item.DelayMs,
        X = item.X,
        Y = item.Y,
        Kind = item.Kind,
        Button = item.Button,
        WheelDelta = item.WheelDelta,
        Anchored = item.Anchored,
        RelX = item.RelX,
        RelY = item.RelY,
        AnchorProcess = item.AnchorProcess ?? "",
        AnchorTitle = item.AnchorTitle ?? "",
        Text = item.Text ?? ""
    };

    public static ClickPoint Clone(ClickPoint point) => new()
    {
        X = point.X,
        Y = point.Y,
        DelayMs = point.DelayMs,
        CustomButton = point.CustomButton,
        Button = point.Button
    };
}
