using Takt.Models;

namespace Takt.Helpers;

public static class SavedLibraries
{
    public static SavedMacro CaptureMacro(string name, MacroSettings source)
    {
        var saved = new SavedMacro
        {
            Name = name,
            LoopCount = Math.Clamp(source.LoopCount, 1, 9_999),
            Speed = source.Speed
        };
        foreach (var item in source.Events ?? [])
            saved.Events.Add(MacroEdit.Clone(item));
        return saved;
    }

    public static void StoreMacro(string? name, MacroSettings source, IList<SavedMacro> library)
    {
        var label = string.IsNullOrWhiteSpace(name) ? "Macro" : name.Trim();
        var existing = library.FirstOrDefault(item => string.Equals(item.Name, label, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            library.Remove(existing);
        library.Add(CaptureMacro(label, source));
    }

    public static void LoadMacro(SavedMacro saved, MacroSettings target)
    {
        target.LoopCount = Math.Clamp(saved.LoopCount, 1, 9_999);
        target.Speed = saved.Speed;
        target.Events ??= new();
        target.Events.Clear();
        foreach (var item in saved.Events ?? [])
            target.Events.Add(MacroEdit.Clone(item));
    }

    public static SavedSequence CaptureSequence(string name, IEnumerable<ClickPoint>? points)
    {
        var saved = new SavedSequence { Name = name };
        foreach (var point in points ?? [])
            saved.Points.Add(MacroEdit.Clone(point));
        return saved;
    }

    public static void StoreSequence(string? name, IEnumerable<ClickPoint>? points, IList<SavedSequence> library)
    {
        var label = string.IsNullOrWhiteSpace(name) ? "Sequence" : name.Trim();
        var existing = library.FirstOrDefault(item => string.Equals(item.Name, label, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            library.Remove(existing);
        library.Add(CaptureSequence(label, points));
    }

    public static void LoadSequence(SavedSequence saved, IList<ClickPoint> target)
    {
        target.Clear();
        foreach (var point in saved.Points ?? [])
            target.Add(MacroEdit.Clone(point));
    }
}
