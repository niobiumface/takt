using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Takt.Helpers;
using Takt.Models;
using Takt.Services;

namespace Takt.ViewModels;

internal sealed partial class HotkeyEditorRow : ObservableObject
{
    public HotkeyEditorRow(string captureId, string failureName, string label, HotkeyBinding binding, int defaultKey, int defaultMods)
    {
        CaptureId = captureId;
        FailureName = failureName;
        Label = label;
        Binding = binding;
        DefaultKey = defaultKey;
        DefaultModifiers = defaultMods;
    }

    public string CaptureId { get; }
    public string FailureName { get; }
    public string Label { get; }
    public HotkeyBinding Binding { get; }
    public int DefaultKey { get; }
    public int DefaultModifiers { get; }

    [ObservableProperty] private string _display = "";
    [ObservableProperty] private string? _error;

    public void Refresh(bool german, bool duplicate, string? registrationError)
    {
        Display = Binding.ToDisplayString(german);
        Error = duplicate
            ? LocalizationService.Text("Hotkey.Duplicate", "This shortcut is used twice.")
            : registrationError;
    }
}

internal sealed class SettingsNavItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Keywords { get; init; }

    public bool Matches(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;
        return Title.Contains(search, StringComparison.CurrentCultureIgnoreCase)
               || Keywords.Contains(search, StringComparison.CurrentCultureIgnoreCase);
    }
}

internal partial class SettingsViewModel
{
    public ObservableCollection<SettingsNavItem> Navigation { get; } = [];
    public ObservableCollection<HotkeyEditorRow> HotkeyRows { get; } = [];
    public ObservableCollection<ChoiceOption> CloseBehaviors { get; } = [];
    public ObservableCollection<ChoiceOption> OutsideBehaviors { get; } = [];
    public ObservableCollection<ChoiceOption> Motions { get; } = [];
    public ObservableCollection<ChoiceOption> Sounds { get; } = [];
    public ObservableCollection<ChoiceOption> LogLevels { get; } = [];

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedSection = "general";
    [ObservableProperty] private string _holidayText = "";
    [ObservableProperty] private DateTime? _holidayDate;

    partial void OnHolidayDateChanged(DateTime? value)
    {
        if (value is DateTime date)
            HolidayText = date.ToString("yyyy-MM-dd");
    }
    [ObservableProperty] private bool _showGeneral = true;
    [ObservableProperty] private bool _showHotkeys;
    [ObservableProperty] private bool _showClicker;
    [ObservableProperty] private bool _showJiggler;
    [ObservableProperty] private bool _showSchedule;
    [ObservableProperty] private bool _showTyper;
    [ObservableProperty] private bool _showSystem;

    partial void OnSearchTextChanged(string value) => UpdateSectionVisibility();
    partial void OnSelectedSectionChanged(string value) => UpdateSectionVisibility();

    public void BuildCatalog()
    {
        Navigation.Clear();
        Navigation.Add(new() { Id = "general", Title = T("Nav.General", "General"), Keywords = "theme sprache autostart tray profil" });
        Navigation.Add(new() { Id = "hotkeys", Title = T("Nav.Hotkeys", "Hotkeys"), Keywords = "taste klicker jiggler typer präsenz makro notstopp" });
        Navigation.Add(new() { Id = "clicker", Title = T("Nav.Clicker", "Clicker"), Keywords = "intervall taste halten failsafe timing" });
        Navigation.Add(new() { Id = "jiggler", Title = T("Nav.Jiggler", "Jiggler"), Keywords = "präsenz f15 schlaf bildschirm" });
        Navigation.Add(new() { Id = "schedule", Title = T("Nav.Schedule", "Hours"), Keywords = "arbeitszeit feiertag pause fenster" });
        Navigation.Add(new() { Id = "typer", Title = T("Nav.Typer", "Typer"), Keywords = "text tippen baustein tempo" });
        Navigation.Add(new() { Id = "system", Title = T("Nav.System", "System"), Keywords = "ton log update export" });
        Replace(CloseBehaviors,
            new(CloseBehavior.ToTray, T("Close.Tray", "Close to tray")),
            new(CloseBehavior.Exit, T("Close.Exit", "Exit")));
        Replace(OutsideBehaviors,
            new(OutsideHoursBehavior.PauseAndResume, T("Outside.Pause", "Pause and resume")),
            new(OutsideHoursBehavior.Stop, T("Outside.Stop", "Stop")),
            new(OutsideHoursBehavior.HintOnly, T("Outside.Hint", "Hint only")));
        Replace(Motions,
            new(PresenceMotion.Invisible, T("Motion.Invisible", "Invisible nudge")),
            new(PresenceMotion.SmallMove, T("Motion.Small", "Small visible move")),
            new(PresenceMotion.SmallCircle, T("Motion.Circle", "Small circle")));
        Replace(Sounds,
            new("SystemAsterisk", "Asterisk"),
            new("SystemExclamation", "Exclamation"),
            new("SystemHand", "Hand"),
            new("SystemQuestion", "Question"),
            new("SystemDefault", "Default"));
        Replace(LogLevels,
            new(LogLevel.Error, T("Log.Error", "Errors")),
            new(LogLevel.Info, T("Log.Info", "Info")),
            new(LogLevel.Debug, T("Log.Debug", "Debug")));
        BuildHotkeyRows();
        UpdateSectionVisibility();
    }

    public void BuildHotkeyRows()
    {
        HotkeyRows.Clear();
        AddRow("clicker", nameof(HotkeyAction.ToggleClicker), T("Hotkey.Clicker", "Toggle clicker"), Settings.Hotkeys.ToggleClicker, 0x76, 0);
        AddRow("jiggler", nameof(HotkeyAction.ToggleJiggler), T("Hotkey.Jiggler", "Toggle jiggler"), Settings.Hotkeys.ToggleJiggler, 0x75, 0);
        AddRow("typer", nameof(HotkeyAction.ToggleTyper), T("Hotkey.Typer", "Toggle typer"), Settings.Hotkeys.ToggleTyper, 0x7A, 0);
        AddRow("presence", nameof(HotkeyAction.TogglePresence), T("Hotkey.Presence", "Toggle presence"), Settings.Hotkeys.TogglePresence, 0x7C, 0);
        AddRow("record", nameof(HotkeyAction.RecordMacro), T("Hotkey.RecordMacro", "Record macro"), Settings.Hotkeys.RecordMacro, 0x7B, 0x0002);
        AddRow("macro", nameof(HotkeyAction.ToggleMacro), T("Hotkey.Macro", "Play macro"), Settings.Hotkeys.ToggleMacro, 0x7B, 0);
        AddRow("panic", nameof(HotkeyAction.Panic), T("Hotkey.Panic", "Panic stop"), Settings.Hotkeys.Panic, 0x78, 0);
        AddRow("show", nameof(HotkeyAction.ShowWindow), T("Hotkey.Show", "Show or hide"), Settings.Hotkeys.ShowWindow, 0x54, 0x0003);
        RefreshHotkeyRows(null);
    }

    public void RefreshHotkeyRows(string? failedAction)
    {
        var duplicates = HotkeyConflicts.DuplicateIds(HotkeyRows.Select(row => (row.FailureName, row.Binding.Modifiers, row.Binding.VirtualKey)));
        foreach (var row in HotkeyRows)
        {
            var taken = failedAction == row.FailureName
                ? LocalizationService.Text("Hotkey.Taken", "Windows or another app already uses this shortcut.")
                : null;
            row.Refresh(IsGerman, duplicates.Contains(row.FailureName), taken);
        }
    }

    [RelayCommand]
    private void ResetHotkey(HotkeyEditorRow? row)
    {
        if (row is null)
            return;
        row.Binding.Modifiers = row.DefaultModifiers;
        row.Binding.VirtualKey = row.DefaultKey;
        _main.RefreshHotkeyLabels();
        RefreshHotkeyRows(null);
    }

    [RelayCommand]
    private void ClearHotkey(HotkeyEditorRow? row)
    {
        if (row is null)
            return;
        row.Binding.Modifiers = 0;
        row.Binding.VirtualKey = 0;
        _main.RefreshHotkeyLabels();
        RefreshHotkeyRows(null);
    }

    [RelayCommand]
    private void ResetSetting(string? key)
    {
        var defaults = new AppSettings();
        switch (key)
        {
            case "theme": Settings.Appearance.Theme = defaults.Appearance.Theme; break;
            case "accent": Settings.Appearance.AccentHex = defaults.Appearance.AccentHex; break;
            case "language": Settings.Appearance.Language = defaults.Appearance.Language; break;
            case "top": Settings.Appearance.AlwaysOnTop = defaults.Appearance.AlwaysOnTop; break;
            case "autostart": Settings.General.StartWithWindows = defaults.General.StartWithWindows; break;
            case "min": Settings.General.StartMinimized = defaults.General.StartMinimized; break;
            case "close": Settings.General.CloseBehavior = defaults.General.CloseBehavior; break;
            case "interval": Settings.Clicker.IntervalMs = defaults.Clicker.IntervalMs; break;
            case "jitter": Settings.Clicker.JitterPercent = defaults.Clicker.JitterPercent; break;
            case "hold": Settings.Clicker.HoldDurationMs = defaults.Clicker.HoldDurationMs; break;
            case "precise": Settings.Clicker.PreciseTiming = defaults.Clicker.PreciseTiming; break;
            case "presence-interval": Settings.Presence.IntervalMs = defaults.Presence.IntervalMs; break;
            case "presence-jitter": Settings.Presence.JitterMs = defaults.Presence.JitterMs; break;
            case "cpm": Settings.Typer.CharactersPerMinute = defaults.Typer.CharactersPerMinute; break;
            case "typo": Settings.Typer.TypoPercent = defaults.Typer.TypoPercent; break;
            case "punct": Settings.Typer.PunctuationPauseMs = defaults.Typer.PunctuationPauseMs; break;
        }

        _services.Theme.Apply(Settings);
        _main.RefreshHotkeyLabels();
    }

    [RelayCommand]
    private void ResetAll()
    {
        if (MessageBox.Show(
                T("Settings.ResetConfirm", "Reset every setting to the defaults?"),
                T("Settings.Title", "Settings"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var fresh = new AppSettings();
        ProfileApplicator.Overlay(fresh, Settings);
        Settings.Profiles.Clear();
        _services.Theme.Apply(Settings);
        _main.RefreshHotkeyLabels();
        BuildCatalog();
    }

    [RelayCommand]
    private void ExportSettings()
    {
        var dialog = new SaveFileDialog { Filter = "JSON|*.json", FileName = "takt-settings.json" };
        if (dialog.ShowDialog() != true)
            return;
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(Settings, SettingsService.JsonOptions));
    }

    [RelayCommand]
    private void ImportSettings()
    {
        var dialog = new OpenFileDialog { Filter = "JSON|*.json" };
        if (dialog.ShowDialog() != true)
            return;
        var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(dialog.FileName), SettingsService.JsonOptions);
        if (loaded is null)
            return;
        SettingsMigration.Migrate(loaded);
        SettingsNormalizer.ApplyAll(loaded);
        ProfileApplicator.Overlay(loaded, Settings);
        _services.Theme.Apply(Settings);
        _main.RefreshHotkeyLabels();
        BuildCatalog();
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        Directory.CreateDirectory(AppLog.Folder);
        Process.Start(new ProcessStartInfo(AppLog.Folder) { UseShellExecute = true });
    }

    [RelayCommand]
    private void AddWindow(DayWindows? day)
    {
        day?.Windows.Add(new TimeSpanWindow());
    }

    [RelayCommand]
    private void RemoveWindow(TimeSpanWindow? window)
    {
        if (window is null)
            return;
        foreach (var day in Settings.Work.Days)
            day.Windows.Remove(window);
    }

    [RelayCommand]
    private void AddHoliday()
    {
        if (!DateTime.TryParse(HolidayText, out var date))
            return;
        var key = date.ToString("yyyy-MM-dd");
        if (!Settings.Work.Holidays.Contains(key))
            Settings.Work.Holidays.Add(key);
        HolidayText = "";
    }

    [RelayCommand]
    private void RemoveHoliday(string? day)
    {
        if (day is not null)
            Settings.Work.Holidays.Remove(day);
    }

    [RelayCommand]
    private void DuplicateProfile()
    {
        if (SelectedProfile is null)
            return;
        Settings.Profiles.Add(ProfileApplicator.Capture(ToSnapshot(SelectedProfile), SelectedProfile.Name + " 2"));
    }

    [RelayCommand]
    private void RenameProfile()
    {
        if (SelectedProfile is null || string.IsNullOrWhiteSpace(ProfileName))
            return;
        SelectedProfile.Name = ProfileName.Trim();
    }

    private void UpdateSectionVisibility()
    {
        ShowGeneral = SectionVisible("general");
        ShowHotkeys = SectionVisible("hotkeys");
        ShowClicker = SectionVisible("clicker");
        ShowJiggler = SectionVisible("jiggler");
        ShowSchedule = SectionVisible("schedule");
        ShowTyper = SectionVisible("typer");
        ShowSystem = SectionVisible("system");
    }

    private bool SectionVisible(string id)
    {
        var item = Navigation.FirstOrDefault(nav => nav.Id == id);
        if (item is null)
            return false;
        if (!string.IsNullOrWhiteSpace(SearchText))
            return item.Matches(SearchText);
        return SelectedSection == id;
    }

    private readonly HashSet<HotkeyBinding> _wiredHotkeys = [];

    private void AddRow(string captureId, string failureName, string label, HotkeyBinding binding, int key, int mods)
    {
        HotkeyRows.Add(new HotkeyEditorRow(captureId, failureName, label, binding, key, mods));
        if (!_wiredHotkeys.Add(binding))
            return;
        binding.PropertyChanged += (_, _) =>
        {
            _main.RefreshHotkeyLabels();
            RefreshHotkeyRows(null);
        };
    }

    private static AppSettings ToSnapshot(ActivityProfile profile)
    {
        var settings = new AppSettings();
        ProfileApplicator.Apply(profile, settings);
        return settings;
    }
}
