namespace Takt.Helpers;

public static class HotkeyConflicts
{
    public static IReadOnlyList<string> DuplicateIds(IEnumerable<(string Id, int Modifiers, int VirtualKey)> bindings)
    {
        var duplicates = new List<string>();
        var seen = new Dictionary<(int, int), string>();
        foreach (var binding in bindings)
        {
            if (binding.VirtualKey == 0)
                continue;
            var key = (binding.Modifiers, binding.VirtualKey);
            if (seen.TryGetValue(key, out var first))
            {
                if (!duplicates.Contains(first))
                    duplicates.Add(first);
                duplicates.Add(binding.Id);
            }
            else
            {
                seen[key] = binding.Id;
            }
        }

        return duplicates;
    }
}
