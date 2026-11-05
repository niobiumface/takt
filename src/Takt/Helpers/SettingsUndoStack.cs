namespace Takt.Helpers;

public sealed class SettingsUndoStack
{
    private readonly Stack<string> _snapshots = new();
    private readonly int _limit;

    public SettingsUndoStack(int limit = 15) => _limit = Math.Max(1, limit);

    public int Count => _snapshots.Count;

    public void Push(string snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
            return;
        if (_snapshots.Count > 0 && _snapshots.Peek() == snapshot)
            return;
        _snapshots.Push(snapshot);
        if (_snapshots.Count <= _limit)
            return;
        var kept = _snapshots.Take(_limit).Reverse().ToArray();
        _snapshots.Clear();
        foreach (var item in kept)
            _snapshots.Push(item);
    }

    public string? Pop() => _snapshots.Count == 0 ? null : _snapshots.Pop();
}
