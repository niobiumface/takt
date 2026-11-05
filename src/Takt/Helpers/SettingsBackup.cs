using System.IO;
using System.Text.Json;

namespace Takt.Helpers;

public static class SettingsBackup
{
    public const int Keep = 3;

    public static void Rotate(string path, int keep = Keep)
    {
        if (keep < 1 || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;
        var oldest = Name(path, keep);
        if (File.Exists(oldest))
            File.Delete(oldest);
        for (var index = keep - 1; index >= 1; index--)
        {
            var source = Name(path, index);
            if (!File.Exists(source))
                continue;
            File.Move(source, Name(path, index + 1), overwrite: true);
        }

        File.Copy(path, Name(path, 1), overwrite: true);
    }

    public static string Name(string path, int index) => path + ".bak" + index;
}

public static class SettingsImport
{
    public static bool ContainsProfiles(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "profiles", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public static class StatsCsv
{
    public static string Format(IEnumerable<DayStat> days)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("date,clicks,typed,presenceSeconds\r\n");
        foreach (var day in days.OrderBy(item => item.Date, StringComparer.Ordinal))
        {
            builder.Append(day.Date);
            builder.Append(',');
            builder.Append(day.Clicks);
            builder.Append(',');
            builder.Append(day.Typed);
            builder.Append(',');
            builder.Append(day.PresenceSeconds);
            builder.Append("\r\n");
        }

        return builder.ToString();
    }
}
