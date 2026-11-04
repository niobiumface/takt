namespace Takt.Helpers;

public static class AppProfileMatcher
{
    public static string? Match(string? processName, IEnumerable<(string Process, string Profile, bool Enabled)> rules)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;
        var name = FileStem(processName);
        foreach (var rule in rules)
        {
            if (!rule.Enabled || string.IsNullOrWhiteSpace(rule.Process) || string.IsNullOrWhiteSpace(rule.Profile))
                continue;
            var wanted = FileStem(rule.Process);
            if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                return rule.Profile;
        }

        return null;
    }

    private static string FileStem(string value)
    {
        var trimmed = value.Trim().Trim('"');
        var slash = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
        var file = slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
        var dot = file.LastIndexOf('.');
        return dot > 0 ? file[..dot] : file;
    }

    public static bool DebounceElapsed(DateTime changedUtc, DateTime nowUtc, int debounceMs) =>
        nowUtc - changedUtc >= TimeSpan.FromMilliseconds(Math.Max(0, debounceMs));
}

public sealed class ProfileSwitchGate
{
    private string? _seen;
    private DateTime _changedUtc;
    private string? _applied;

    public string? Tick(
        bool enabled,
        string? processName,
        IEnumerable<(string Process, string Profile, bool Enabled)> rules,
        DateTime nowUtc,
        int debounceMs)
    {
        if (!enabled)
        {
            _applied = null;
            _seen = null;
            return null;
        }

        var name = processName ?? "";
        if (!string.Equals(name, _seen, StringComparison.OrdinalIgnoreCase))
        {
            _seen = name;
            _changedUtc = nowUtc;
            return null;
        }

        if (!AppProfileMatcher.DebounceElapsed(_changedUtc, nowUtc, debounceMs))
            return null;

        var profile = AppProfileMatcher.Match(name, rules);
        if (profile is null || string.Equals(profile, _applied, StringComparison.OrdinalIgnoreCase))
            return null;
        _applied = profile;
        return profile;
    }
}
