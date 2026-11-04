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

internal sealed class MacroFile
{
    public int LoopCount { get; set; } = 1;
    public List<MacroEvent>? Events { get; set; }
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
    [ObservableProperty] private bool _showMacro;
    [ObservableProperty] private bool _showSystem;
    [ObservableProperty] private bool _showHealth;

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
        Navigation.Add(new() { Id = "typer", Title = T("Nav.Typer", "Typer"), Keywords = "text tippen baustein tempo datum clipboard" });
        Navigation.Add(new() { Id = "macro", Title = T("Nav.Macro", "Macro"), Keywords = "makro schritte wartezeit schleife json" });
        Navigation.Add(new() { Id = "system", Title = T("Nav.System", "System"), Keywords = "ton log update export" });
        Navigation.Add(new() { Id = "health", Title = T("Nav.Health", "Stats"), Keywords = "statistik diagnose timer profil prozess" });
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
        AddRow("compact", nameof(HotkeyAction.ToggleCompact), T("Hotkey.Compact", "Compact window"), Settings.Hotkeys.ToggleCompact, 0x4D, 0x0006);
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
        if (!SettingReset.Apply(Settings, key))
            return;
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
        ShowMacro = SectionVisible("macro");
        ShowSystem = SectionVisible("system");
        ShowHealth = SectionVisible("health");
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

    [RelayCommand]
    private void UndoSettings()
    {
        if (!_services.Settings.TryUndo())
            return;
        _services.Theme.Apply(Settings);
        _main.RefreshHotkeyLabels();
        BuildCatalog();
    }

    [RelayCommand]
    private void OpenWizard() => _main.OpenWizardCommand.Execute(null);

    [RelayCommand]
    private void RemovePoint()
    {
        if (SelectedPoint is not null)
            _main.RemoveSequencePointCommand.Execute(SelectedPoint);
    }

    [RelayCommand]
    private void ImportClipboard()
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText())
                Settings.Typer.Text = System.Windows.Clipboard.GetText();
        }
        catch
        {
            DiagnosisText = T("Typer.ClipboardFail", "Clipboard could not be read.");
        }
    }

    [RelayCommand]
    private void ImportTextFile()
    {
        var dialog = new OpenFileDialog { Filter = "Text|*.txt" };
        if (dialog.ShowDialog() != true)
            return;
        Settings.Typer.Text = File.ReadAllText(dialog.FileName);
    }

    [RelayCommand]
    private void SaveMacroFile()
    {
        var dialog = new SaveFileDialog { Filter = "JSON|*.json", FileName = "takt-macro.json" };
        if (dialog.ShowDialog() != true)
            return;
        var payload = new MacroFile { LoopCount = Settings.Macro.LoopCount, Events = Settings.Macro.Events.ToList() };
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(payload, SettingsService.JsonOptions));
    }

    [RelayCommand]
    private void LoadMacroFile()
    {
        var dialog = new OpenFileDialog { Filter = "JSON|*.json" };
        if (dialog.ShowDialog() != true)
            return;
        var loaded = JsonSerializer.Deserialize<MacroFile>(File.ReadAllText(dialog.FileName), SettingsService.JsonOptions);
        if (loaded?.Events is null)
            return;
        Settings.Macro.LoopCount = Math.Max(1, loaded.LoopCount);
        Settings.Macro.Events.Clear();
        foreach (var item in loaded.Events)
            Settings.Macro.Events.Add(item);
        _main.NotifySummaries();
    }

    [RelayCommand]
    private void InsertDelay()
    {
        var wait = MacroEdit.Wait(500);
        var index = SelectedMacro is null ? -1 : Settings.Macro.Events.IndexOf(SelectedMacro);
        if (index >= 0)
            Settings.Macro.Events.Insert(index + 1, wait);
        else
            Settings.Macro.Events.Add(wait);
        SelectedMacro = wait;
    }

    [RelayCommand]
    private void MoveMacro(string? direction)
    {
        if (SelectedMacro is null)
            return;
        var index = Settings.Macro.Events.IndexOf(SelectedMacro);
        MacroEdit.Move(Settings.Macro.Events, index, direction == "up" ? -1 : 1);
    }

    [RelayCommand]
    private void DeleteMacroStep()
    {
        if (SelectedMacro is null)
            return;
        Settings.Macro.Events.Remove(SelectedMacro);
        SelectedMacro = null;
    }

    [RelayCommand]
    private void MovePoint(string? direction)
    {
        if (SelectedPoint is null)
            return;
        var index = Settings.Clicker.Points.IndexOf(SelectedPoint);
        MacroEdit.Move(Settings.Clicker.Points, index, direction == "up" ? -1 : 1);
    }

    [RelayCommand]
    private void AddRule()
    {
        if (string.IsNullOrWhiteSpace(RuleProcess) || string.IsNullOrWhiteSpace(RuleProfile))
            return;
        Settings.General.AppRules.Add(new AppRule
        {
            Enabled = true,
            ProcessName = RuleProcess.Trim(),
            ProfileName = RuleProfile.Trim()
        });
        RuleProcess = "";
        RuleProfile = "";
    }

    [RelayCommand]
    private void RemoveRule(AppRule? rule)
    {
        if (rule is not null)
            Settings.General.AppRules.Remove(rule);
    }

    [RelayCommand]
    private void ResetStats()
    {
        _services.Stats.Reset();
        _main.NotifySummaries();
    }

    [RelayCommand]
    private void RunDiagnosis()
    {
        bool? idle = null;
        try
        {
            var beforeOk = Takt.Native.SystemQuery.TryGetIdleTime(out var before);
            Takt.Native.MouseInput.PulseRelative(1, 0);
            Takt.Native.MouseInput.PulseRelative(-1, 0);
            System.Threading.Thread.Sleep(60);
            var afterOk = Takt.Native.SystemQuery.TryGetIdleTime(out var after);
            idle = beforeOk && afterOk ? IdleVerification.InputRegistered(after) && after <= before : null;
        }
        catch
        {
            idle = null;
        }

        var duplicates = HotkeyConflicts.DuplicateIds(HotkeyRows.Select(row => (row.FailureName, row.Binding.Modifiers, row.Binding.VirtualKey))).Count;
        var checks = new[]
        {
            DiagnosisReport.Idle(idle),
            DiagnosisReport.Hotkeys(duplicates, _services.Hotkeys.RejectedCount),
            DiagnosisReport.LockPolicy(Takt.Native.SystemQuery.PolicyLockTimeoutSeconds()),
            DiagnosisReport.Screensaver(Takt.Native.SystemQuery.PolicyForcesScreensaver())
        };
        DiagnosisText = string.Join(Environment.NewLine, checks.Select(FormatCheck));
    }

    private static string FormatCheck(CheckResult check)
    {
        var mark = check.Mark switch
        {
            CheckMark.Pass => T("Diagnosis.Pass", "PASS"),
            CheckMark.Warn => T("Diagnosis.Warn", "WARN"),
            _ => T("Diagnosis.Fail", "FAIL")
        };
        var template = T(check.TextKey, check.Fallback);
        var sentence = check.Args is { Length: > 0 }
            ? string.Format(System.Globalization.CultureInfo.CurrentCulture, template, check.Args)
            : template;
        return mark + "  " + sentence;
    }

    [RelayCommand]
    private void ResetPoint()
    {
        if (SelectedPoint is not null)
            RowDefaults.Point(SelectedPoint);
    }

    [RelayCommand]
    private void ResetWindow(TimeSpanWindow? window)
    {
        if (window is not null)
            RowDefaults.Window(window);
    }

    [RelayCommand]
    private void ResetMacroStep()
    {
        if (SelectedMacro is not null)
            RowDefaults.MacroDelay(SelectedMacro);
    }

    [RelayCommand]
    private void ResetRule(AppRule? rule)
    {
        if (rule is not null)
            RowDefaults.Rule(rule);
    }

    [RelayCommand]
    private void ResetSnippetDraft()
    {
        SnippetName = "";
        SnippetText = "";
    }

    private static AppSettings ToSnapshot(ActivityProfile profile)
    {
        var settings = new AppSettings();
        ProfileApplicator.Apply(profile, settings);
        return settings;
    }
}
