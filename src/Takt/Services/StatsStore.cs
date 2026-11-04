using System.IO;
using System.Text.Json;
using Takt.Helpers;

namespace Takt.Services;

internal sealed class StatsStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly List<DayStat> _days = [];
    private bool _dirty;

    public StatsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Takt",
            "stats.json");
    }

    public IReadOnlyList<DayStat> Days
    {
        get
        {
            lock (_gate)
                return _days.ToArray();
        }
    }

    public void Load()
    {
        lock (_gate)
        {
            _days.Clear();
            try
            {
                if (!File.Exists(_path))
                    return;
                var loaded = JsonSerializer.Deserialize<List<DayStat>>(File.ReadAllText(_path), SettingsService.JsonOptions);
                if (loaded is not null)
                    _days.AddRange(loaded);
            }
            catch
            {
                AppLog.Error("Stats file could not be read.");
            }
        }
    }

    public void Add(DateTime localDay, int clicks, int typed, int presenceSeconds)
    {
        if (clicks == 0 && typed == 0 && presenceSeconds == 0)
            return;
        lock (_gate)
        {
            StatsBook.Add(_days, localDay, clicks, typed, presenceSeconds);
            _dirty = true;
        }
    }

    public void Flush()
    {
        string json;
        lock (_gate)
        {
            if (!_dirty)
                return;
            json = JsonSerializer.Serialize(_days, SettingsService.JsonOptions);
            _dirty = false;
        }

        var folder = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(_path))
            File.Replace(tmp, _path, destinationBackupFileName: null);
        else
            File.Move(tmp, _path);
        if (File.Exists(tmp))
            File.Delete(tmp);
    }

    public void Reset()
    {
        lock (_gate)
        {
            _days.Clear();
            _dirty = true;
        }

        Flush();
    }
}
