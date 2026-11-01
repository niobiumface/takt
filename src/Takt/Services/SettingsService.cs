using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Takt.Helpers;
using Takt.Models;

namespace Takt.Services;

public sealed class SettingsService : IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly object _gate = new();
    private CancellationTokenSource? _debounce;
    private bool _wired;

    public SettingsService(string? path = null)
    {
        var dir = path is null
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Takt")
            : Path.GetDirectoryName(path) ?? path;
        Directory.CreateDirectory(dir);
        _path = path ?? Path.Combine(dir, "settings.json");
        Current = new AppSettings();
    }

    public AppSettings Current { get; private set; }

    public event EventHandler? Changed;

    public bool LastLoadReset { get; private set; }

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                    Current = loaded;
            }
        }
        catch
        {
            LastLoadReset = true;
            AppLog.Error("Settings file could not be read and was reset.");
            try
            {
                if (File.Exists(_path))
                    File.Copy(_path, _path + ".bak", overwrite: true);
            }
            catch
            {
                // ignore backup failure
            }
        }

        EnsureNested();
        var upgraded = SettingsMigration.Migrate(Current);
        Wire(Current);
        _wired = true;
        SettingsNormalizer.ApplyAll(Current);
        if (!File.Exists(_path) || LastLoadReset || upgraded)
            SaveNow();
    }

    public void QueueSave()
    {
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        _ = DebounceAsync(token);
    }

    public void SaveNow()
    {
        lock (_gate)
        {
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_path))
                File.Replace(tmp, _path, destinationBackupFileName: null);
            else
                File.Move(tmp, _path);
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
    }

    public void Dispose()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        SaveNow();
    }

    private async Task DebounceAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(400, token);
            SaveNow();
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // newer save scheduled
        }
    }

    private void EnsureNested()
    {
        Current.Jiggler ??= new JigglerSettings();
        Current.Clicker ??= new ClickerSettings();
        Current.Hotkeys ??= new HotkeySettings();
        Current.Appearance ??= new AppearanceSettings();
        Current.General ??= new GeneralSettings();
        Current.Hotkeys.ToggleJiggler ??= HotkeyBinding.Vk(0x75);
        Current.Hotkeys.ToggleClicker ??= HotkeyBinding.Vk(0x76);
        Current.Hotkeys.ToggleBoth ??= HotkeyBinding.Vk(0x77);
        Current.Hotkeys.Panic ??= HotkeyBinding.Vk(0x78);
        Current.Hotkeys.PickPosition ??= HotkeyBinding.Vk(0x79);
        Current.Hotkeys.ToggleMacro ??= HotkeyBinding.Vk(0x7B);
        Current.Hotkeys.ToggleTyper ??= HotkeyBinding.Vk(0x7A);
        Current.Hotkeys.TogglePresence ??= HotkeyBinding.Vk(0x7C);
        Current.Hotkeys.RecordMacro ??= HotkeyBinding.Vk(0x7B, 0x0002);
        Current.Hotkeys.ShowWindow ??= HotkeyBinding.Vk(0x54, 0x0001 | 0x0002);
        Current.Typer ??= new TyperSettings();
        Current.Typer.Snippets ??= new();
        Current.Presence ??= new PresenceSettings();
        Current.Work ??= new WorkScheduleSettings();
        Current.Macro ??= new MacroSettings();
        Current.Macro.Events ??= new();
        Current.Clicker.Points ??= new();
        Current.Profiles ??= new();
    }

    private void Wire(INotifyPropertyChanged target)
    {
        target.PropertyChanged -= OnChanged;
        target.PropertyChanged += OnChanged;
        foreach (var prop in target.GetType().GetProperties())
        {
            if (prop.GetIndexParameters().Length > 0)
                continue;
            if (prop.GetValue(target) is INotifyPropertyChanged child)
                Wire(child);
            if (prop.GetValue(target) is INotifyCollectionChanged collection)
                Watch(collection);
        }
    }

    private void Watch(INotifyCollectionChanged collection)
    {
        collection.CollectionChanged -= OnCollectionChanged;
        collection.CollectionChanged += OnCollectionChanged;
        if (collection is System.Collections.IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item is INotifyPropertyChanged child)
                    Wire(child);
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is INotifyPropertyChanged child)
                    Wire(child);
            }
        }

        if (_wired)
            QueueSave();
    }

    private void OnChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is INotifyPropertyChanged parent && e.PropertyName is not null)
        {
            var prop = parent.GetType().GetProperty(e.PropertyName);
            if (prop?.GetValue(parent) is INotifyPropertyChanged child)
                Wire(child);
        }

        if (!_wired)
            return;

        SettingsNormalizer.ApplyChanged(sender, e.PropertyName);
        QueueSave();
    }
}
