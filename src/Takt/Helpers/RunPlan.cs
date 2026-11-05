using Takt.Models;

namespace Takt.Helpers;

public static class PlanLimits
{
    public const int MaxSteps = 12;

    public static int Seconds(PlanKind kind, int seconds) =>
        kind == PlanKind.Jiggler ? Math.Clamp(seconds, 1, 86_400) : Math.Clamp(seconds, 0, 86_400);
}

public static class PlanLibrary
{
    public static SavedSequence? FindSequence(IEnumerable<SavedSequence>? items, string? name)
    {
        if (items is null || string.IsNullOrWhiteSpace(name))
            return null;
        return items.FirstOrDefault(item => string.Equals(item.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static TextSnippet? FindSnippet(IEnumerable<TextSnippet>? items, string? name)
    {
        if (items is null || string.IsNullOrWhiteSpace(name))
            return null;
        return items.FirstOrDefault(item => string.Equals(item.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class PlanCursor
{
    private readonly object _gate = new();
    private int _ticket = 1;

    public int Index { get; private set; } = -1;

    public DateTime? StartedUtc { get; private set; }

    public int Ticket
    {
        get
        {
            lock (_gate)
                return _ticket;
        }
    }

    public bool IsActive
    {
        get
        {
            lock (_gate)
                return Index >= 0;
        }
    }

    public bool Start(int count, DateTime nowUtc)
    {
        lock (_gate)
        {
            if (count <= 0)
                return false;
            Index = 0;
            StartedUtc = nowUtc;
            _ticket++;
            return true;
        }
    }

    public bool TryTimeout(DateTime nowUtc, int seconds, int count, out bool hasNext)
    {
        lock (_gate)
        {
            hasNext = false;
            if (Index < 0 || StartedUtc is null || seconds <= 0)
                return false;
            if (nowUtc - StartedUtc.Value < TimeSpan.FromSeconds(seconds))
                return false;
            return LeaveLocked(count, nowUtc, out hasNext);
        }
    }

    public bool Leave(int ticket, int count, DateTime nowUtc, out bool hasNext)
    {
        lock (_gate)
        {
            hasNext = false;
            if (Index < 0 || ticket != _ticket)
                return false;
            return LeaveLocked(count, nowUtc, out hasNext);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            Index = -1;
            StartedUtc = null;
            _ticket++;
        }
    }

    private bool LeaveLocked(int count, DateTime nowUtc, out bool hasNext)
    {
        _ticket++;
        var next = Index + 1;
        if (next >= count)
        {
            Index = -1;
            StartedUtc = null;
            hasNext = false;
            return true;
        }

        Index = next;
        StartedUtc = nowUtc;
        hasNext = true;
        return true;
    }
}
