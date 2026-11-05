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
    private bool _suppressUndo;

    public SettingsUndoStack Undo { get; } = new();

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

        EnsureShape(Current);
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
            if (File.Exists(_path))
            {
                var previous = File.ReadAllText(_path);
                if (!string.Equals(previous, json, StringComparison.Ordinal))
                {
                    if (!_suppressUndo)
                        Undo.Push(previous);
                    try
                    {
                        SettingsBackup.Rotate(_path);
                    }
                    catch
                    {
                        AppLog.Error("Settings backup could not be written.");
                    }
                }
            }

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

    public bool TryUndo()
    {
        var snapshot = Undo.Pop();
        if (string.IsNullOrWhiteSpace(snapshot))
            return false;
        AppSettings? loaded;
        try
        {
            loaded = JsonSerializer.Deserialize<AppSettings>(snapshot, JsonOptions);
        }
        catch
        {
            loaded = null;
        }

        if (loaded is null)
        {
            Undo.Push(snapshot);
            return false;
        }

        EnsureShape(loaded);
        SettingsMigration.Migrate(loaded);
        SettingsNormalizer.ApplyAll(loaded);
        _suppressUndo = true;
        try
        {
            ProfileApplicator.Overlay(loaded, Current);
            Current.Profiles.Clear();
            foreach (var profile in loaded.Profiles)
                Current.Profiles.Add(profile);
            SaveNow();
        }
        finally
        {
            _suppressUndo = false;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
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

    internal static void EnsureShape(AppSettings current)
    {
        current.Jiggler ??= new JigglerSettings();
        current.Clicker ??= new ClickerSettings();
        current.Hotkeys ??= new HotkeySettings();
        current.Appearance ??= new AppearanceSettings();
        current.General ??= new GeneralSettings();
        current.Hotkeys.ToggleJiggler ??= HotkeyBinding.Vk(0x75);
        current.Hotkeys.ToggleClicker ??= HotkeyBinding.Vk(0x76);
        current.Hotkeys.ToggleBoth ??= HotkeyBinding.Vk(0x77);
        current.Hotkeys.Panic ??= HotkeyBinding.Vk(0x78);
        current.Hotkeys.PickPosition ??= HotkeyBinding.Vk(0x79);
        current.Hotkeys.ToggleMacro ??= HotkeyBinding.Vk(0x7B);
        current.Hotkeys.ToggleTyper ??= HotkeyBinding.Vk(0x7A);
        current.Hotkeys.TogglePresence ??= HotkeyBinding.Vk(0x7C);
        current.Hotkeys.RecordMacro ??= HotkeyBinding.Vk(0x7B, 0x0002);
        current.Hotkeys.ShowWindow ??= HotkeyBinding.Vk(0x54, 0x0001 | 0x0002);
        current.Hotkeys.ToggleCompact ??= HotkeyBinding.Vk(0x4D, 0x0002 | 0x0004);
        current.Hotkeys.TogglePause ??= HotkeyBinding.Vk(0x74);
        current.Typer ??= new TyperSettings();
        current.Typer.Snippets ??= new();
        current.Presence ??= new PresenceSettings();
        current.Work ??= new WorkScheduleSettings();
        current.Macro ??= new MacroSettings();
        current.Macro.Events ??= new();
        current.Macro.Saved ??= new();
        current.Clicker.Points ??= new();
        current.Clicker.SavedSequences ??= new();
        current.Profiles ??= new();
        current.General.AppRules ??= new();
        current.General.Plan ??= new();
        current.General.OnlyWhileProcess ??= "";
        current.Jiggler.StopAtTime ??= "";
        current.Clicker.StopAtTime ??= "";
        current.Typer.StopAtTime ??= "";
        current.Presence.StopAtTime ??= "";
        current.Macro.StopAtTime ??= "";
        foreach (var snippet in current.Typer.Snippets)
            snippet.Hotkey ??= new HotkeyBinding { VirtualKey = 0 };
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
