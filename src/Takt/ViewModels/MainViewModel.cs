using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Takt.Helpers;
using Takt.Models;
using Takt.Services;
using Takt.Views;

namespace Takt.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    private readonly AppServices _services;
    private bool _ignoreProfile;
    private int _statsClicks;
    private int _statsTyped;
    private CompactWindow? _compact;

    public MainViewModel(AppServices services)
    {
        _services = services;
        Jiggler.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(JigglerEngine.IsEnabled))
                OnPropertyChanged(nameof(JigglerOn));
            if (e.PropertyName is nameof(JigglerEngine.Status) or nameof(JigglerEngine.PauseReason) or nameof(JigglerEngine.IsEnabled))
                OnPropertyChanged(nameof(JigglerStatusText));
            if (e.PropertyName is nameof(JigglerEngine.InputFault) && Jiggler.InputFault)
                ShowInputFault();
            if (e.PropertyName is nameof(JigglerEngine.IsEnabled) or nameof(JigglerEngine.Status))
                RefreshTray();
        };
        Clicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ClickerEngine.IsEnabled))
                OnPropertyChanged(nameof(ClickerOn));
            if (e.PropertyName is nameof(ClickerEngine.Status) or nameof(ClickerEngine.PauseReason) or nameof(ClickerEngine.IsEnabled))
                OnPropertyChanged(nameof(ClickerStatusText));
            if (e.PropertyName is nameof(ClickerEngine.ClickCount) or nameof(ClickerEngine.Status))
                OnPropertyChanged(nameof(ClickerProgress));
            if (e.PropertyName is nameof(ClickerEngine.ClickCount) or nameof(ClickerEngine.IsEnabled) or nameof(ClickerEngine.RunStartedUtc))
                OnPropertyChanged(nameof(RemainderText));
            if (e.PropertyName is nameof(ClickerEngine.IsEnabled))
                TestClickCommand.NotifyCanExecuteChanged();
            if (e.PropertyName is nameof(ClickerEngine.ClickCount))
                NoteClicks(Clicker.ClickCount);
            if (e.PropertyName is nameof(ClickerEngine.InputFault) && Clicker.InputFault)
                ShowInputFault();
            if (e.PropertyName is nameof(ClickerEngine.IsEnabled) or nameof(ClickerEngine.Status))
                RefreshTray();
        };
        _services.Stopped += ShowStopped;
        _services.Hotkeys.Pressed += OnHotkey;
        _services.Hotkeys.ShowRequested += ShowMainWindow;
        _services.Hotkeys.RegistrationFailed += name =>
            StatusMessage = LocalizationService.Text("Hotkey.Conflict", "Hotkey already in use.") + " " + HotkeyLabel(name);
        Settings.Clicker.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ClickerProgress));
        Clicker.CompletedNaturally += OnRunFinished;
        Macro.CompletedNaturally += OnRunFinished;
        Typer.CompletedNaturally += OnRunFinished;
        Typer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TyperEngine.IsEnabled))
                OnPropertyChanged(nameof(TyperOn));
            if (e.PropertyName is nameof(TyperEngine.Status) or nameof(TyperEngine.PauseReason) or nameof(TyperEngine.IsEnabled))
                OnPropertyChanged(nameof(TyperStatusText));
            if (e.PropertyName is nameof(TyperEngine.TypedCount))
            {
                OnPropertyChanged(nameof(TyperProgress));
                NoteTyped(Typer.TypedCount);
            }
            if (e.PropertyName is nameof(TyperEngine.PassCount) or nameof(TyperEngine.IsEnabled) or nameof(TyperEngine.RunStartedUtc))
                OnPropertyChanged(nameof(RemainderText));
            if (e.PropertyName is nameof(TyperEngine.IsEnabled))
                TestTypeCommand.NotifyCanExecuteChanged();
            if (e.PropertyName is nameof(TyperEngine.InputFault) && Typer.InputFault)
                ShowInputFault();
            if (e.PropertyName is nameof(TyperEngine.IsEnabled) or nameof(TyperEngine.Status))
                RefreshTray();
        };
        Settings.Typer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TyperSettings.CharactersPerMinute))
                OnPropertyChanged(nameof(TyperWpm));
        };
        Presence.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PresenceEngine.IsEnabled))
                OnPropertyChanged(nameof(PresenceOn));
            if (e.PropertyName is nameof(PresenceEngine.IsEnabled) or nameof(PresenceEngine.Status)
                or nameof(PresenceEngine.IdleSeconds) or nameof(PresenceEngine.Warning) or nameof(PresenceEngine.PauseReason))
                OnPropertyChanged(nameof(PresenceStatusText));
            if (e.PropertyName is nameof(PresenceEngine.IsEnabled) or nameof(PresenceEngine.Status))
                RefreshTray();
        };
        Macro.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(MacroSummary));
            OnPropertyChanged(nameof(RecordLabel));
            OnPropertyChanged(nameof(PlayLabel));
            RefreshTray();
        };
        _services.Tray.ShowWindowRequested += ShowMainWindow;
        _services.Tray.ToggleJigglerRequested += () => ToggleJiggler();
        _services.Tray.ToggleClickerRequested += () => ToggleClicker();
        _services.Tray.PanicRequested += Panic;
        _services.Tray.ExitRequested += ExitApp;
        _services.Tray.SettingsRequested += () => ShowSettings(null);
        _services.Tray.ToggleCompactRequested += () => ToggleCompact();
        _services.Tray.PauseRequested += () => TogglePause();
        _services.PauseStateChanged += () =>
        {
            OnPropertyChanged(nameof(PauseLabel));
            OnPropertyChanged(nameof(RemainderText));
        };
        _services.ProfileRequested += SelectProfileByName;
        _services.DeadlineChanged += text =>
        {
            DeadlineText = text;
            OnPropertyChanged(nameof(RemainderText));
            var detail = string.IsNullOrWhiteSpace(text) ? PresenceStatusText : text;
            _services.Tray.SetDetail(detail);
            if (DateTime.UtcNow - _weekUi < TimeSpan.FromSeconds(1))
                return;
            _weekUi = DateTime.UtcNow;
            NotifyWeek();
        };
        Settings.Clicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ClickerSettings.Button) or nameof(ClickerSettings.Action) or nameof(ClickerSettings.RepeatMode))
                OnPropertyChanged(nameof(ClickSummary));
        };
        _services.TimerDue += () =>
            StatusMessage = LocalizationService.Text("Status.Timer", "Stopped by the timer.");
        _services.Settings.Changed += (_, _) => NotifyWeek();
        _services.Settings.Current.Hotkeys.PropertyChanged += (_, _) => RefreshHotkeyLabels();
        _services.Settings.Current.Hotkeys.ToggleJiggler.PropertyChanged += (_, _) => RefreshHotkeyLabels();
        _services.Settings.Current.Hotkeys.ToggleClicker.PropertyChanged += (_, _) => RefreshHotkeyLabels();
        _services.Settings.Current.Hotkeys.ToggleTyper.PropertyChanged += (_, _) => RefreshHotkeyLabels();
        _services.Settings.Current.Appearance.PropertyChanged += OnAppearanceChanged;
        _services.Settings.Current.General.PropertyChanged += OnGeneralChanged;
        RefreshHotkeyLabels();
        RefreshChoices();
        RefreshTray();
    }

    public ObservableCollection<ChoiceOption> JigglerModes { get; } = [];
    public ObservableCollection<PaceChoice> ClickPaces { get; } = [];
    public ObservableCollection<PaceChoice> JigglePaces { get; } = [];
    public ObservableCollection<PaceChoice> TyperPaces { get; } = [];
    public AppSettings Settings => _services.Settings.Current;
    public JigglerEngine Jiggler => _services.Jiggler;
    public ClickerEngine Clicker => _services.Clicker;
    public MacroEngine Macro => _services.Macro;
    public TyperEngine Typer => _services.Typer;
    public PresenceEngine Presence => _services.Presence;

    [ObservableProperty] private TextSnippet? _selectedSnippet;
    [ObservableProperty] private ActivityProfile? _activeProfile;

    partial void OnActiveProfileChanged(ActivityProfile? value)
    {
        if (value is null || _ignoreProfile)
            return;
        ProfileApplicator.Apply(value, Settings);
        _services.Theme.Apply(Settings);
        RefreshHotkeyLabels();
    }

    public void SelectProfileByName(string name)
    {
        var profile = Settings.Profiles.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
            return;
        _ignoreProfile = true;
        ActiveProfile = profile;
        _ignoreProfile = false;
    }

    partial void OnSelectedSnippetChanged(TextSnippet? value)
    {
        if (value is not null)
            Settings.Typer.Text = value.Text ?? "";
    }
    public AppServices Services => _services;
    private SettingsViewModel? _editor;
    private DateTime _weekUi;
    internal SettingsViewModel Editor => _editor ??= new SettingsViewModel(_services, this);

    public string ClickSummary => ClickSummaryText.Format(
        ChoiceLabel(Settings.Clicker.Button),
        ChoiceLabel(Settings.Clicker.Action),
        ChoiceLabel(Settings.Clicker.RepeatMode));

    [ObservableProperty] private string _jigglerHotkeyText = "F6";
    [ObservableProperty] private string _clickerHotkeyText = "F7";
    [ObservableProperty] private string _panicHotkeyText = "F9";
    [ObservableProperty] private string _typerHotkeyText = "F11";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _deadlineText = "";
    [ObservableProperty] private string _activityHex = "#78808C";

    public string WeekSummary
    {
        get
        {
            if (Settings.General.StatsOptOut)
                return LocalizationService.Text("Stats.Off", "Local stats are off.");
            return string.Join("   ", WeekCells.Select(cell => $"{cell.Day}  {cell.Detail}"));
        }
    }

    public bool ShowWeek => !Settings.General.StatsOptOut;
    public bool HideWeek => Settings.General.StatsOptOut;

    public IReadOnlyList<WeekCell> WeekCells =>
        WeekStrip.Build(StatsBook.LastDays(_services.Stats.Days, DateTime.Today, 7));

    private void NotifyWeek()
    {
        OnPropertyChanged(nameof(WeekSummary));
        OnPropertyChanged(nameof(WeekCells));
        OnPropertyChanged(nameof(ShowWeek));
        OnPropertyChanged(nameof(HideWeek));
    }

    public bool JigglerOn
    {
        get => Jiggler.IsEnabled;
        set
        {
            if (value == Jiggler.IsEnabled)
            {
                OnPropertyChanged();
                return;
            }

            _services.ForgetPause();
            if (value)
                Jiggler.Start();
            else
                Jiggler.Stop();
            OnPropertyChanged();
        }
    }

    public bool ClickerOn
    {
        get => Clicker.IsEnabled;
        set
        {
            if (value == Clicker.IsEnabled)
            {
                OnPropertyChanged();
                return;
            }

            _services.ForgetPause();
            if (value)
                Clicker.Start();
            else
                Clicker.Stop();
            OnPropertyChanged();
        }
    }

    public string JigglerStatusText => Describe(Jiggler.Status, Jiggler.PauseReason);

    public string ClickerStatusText => Describe(Clicker.Status, Clicker.PauseReason);

    public string MacroSummary =>
        LocalizationService.Text("Macro.Title", "Macro") + " · " +
        LocalizationService.Text("Macro.Count", "Events") + " " + Macro.EventCount;

    public string RecordLabel => Macro.IsRecording
        ? LocalizationService.Text("Macro.Stop", "Stop")
        : LocalizationService.Text("Macro.Record", "Record");

    public string PlayLabel => Macro.IsPlaying
        ? LocalizationService.Text("Macro.Stop", "Stop")
        : LocalizationService.Text("Macro.Play", "Play");

    public bool TyperOn
    {
        get => Typer.IsEnabled;
        set
        {
            if (value == Typer.IsEnabled)
            {
                OnPropertyChanged();
                return;
            }

            _services.ForgetPause();
            if (value)
                Typer.Start();
            else
                Typer.Stop();
            OnPropertyChanged();
        }
    }

    public string TyperStatusText => Describe(Typer.Status, Typer.PauseReason);

    public string TyperProgress => Typer.TypedCount.ToString();

    public string TyperWpm => TyperTiming.WordsPerMinute(Settings.Typer.CharactersPerMinute) + " WPM";

    public bool PresenceOn
    {
        get => Presence.IsEnabled;
        set
        {
            if (value == Presence.IsEnabled)
            {
                OnPropertyChanged();
                return;
            }

            _services.ForgetPause();
            if (value)
            {
                Settings.Presence.Enabled = true;
                Presence.Start();
            }
            else
            {
                Presence.Stop(persistOff: true);
            }

            OnPropertyChanged();
        }
    }

    public string PresenceStatusText
    {
        get
        {
            if (!Presence.IsEnabled && Presence.Status == EngineStatus.Stopped)
                return "";
            if (Presence.Warning == PresenceWarning.SessionLocked)
                return LocalizationService.Text("Presence.Locked", "The session locked. Takt does not override a policy lock.");
            if (Presence.Warning == PresenceWarning.PolicyLock)
                return LocalizationService.Text("Presence.Policy", "Windows may still lock this session because of policy. Takt does not bypass that.");
            if (Presence.Warning == PresenceWarning.IdleNotReset)
                return LocalizationService.Text("Presence.IdleWarning", "Input did not reset the idle timer.");
            if (Presence.Status == EngineStatus.Paused)
                return LocalizationService.Text("Presence.OutsideHours", "Paused · outside working hours");
            if (Presence.Status == EngineStatus.Running)
                return string.Format(LocalizationService.Text("Presence.Active", "Presence on · last activity {0} s ago"), Presence.IdleSeconds);
            return "";
        }
    }

    public string ClickerProgress =>
        Settings.Clicker.RepeatMode == RepeatMode.Count
            ? $"{Clicker.ClickCount} / {Math.Max(1, Settings.Clicker.RepeatCount)}"
            : Clicker.ClickCount.ToString();

    public string RemainderText
    {
        get
        {
            var parts = new List<string>();
            if (Clicker.IsEnabled)
            {
                var clicks = RunRemainder.CountLeft(Settings.Clicker.RepeatMode, Clicker.ClickCount, Settings.Clicker.RepeatCount);
                if (clicks is int left)
                    parts.Add(string.Format(LocalizationService.Text("Remain.Clicks", "Clicks left: {0}"), left));
                var seconds = RunRemainder.SecondsLeft(Settings.Clicker.RepeatMode, Clicker.RunStartedUtc, DateTime.UtcNow, Settings.Clicker.DurationSeconds);
                if (seconds is int clickerSeconds)
                    parts.Add(string.Format(LocalizationService.Text("Remain.ClickerTime", "Clicker time left: {0} s"), clickerSeconds));
            }

            if (Typer.IsEnabled)
            {
                var passes = RunRemainder.CountLeft(Settings.Typer.RepeatMode, Typer.PassCount, Settings.Typer.RepeatCount);
                if (passes is int left)
                    parts.Add(string.Format(LocalizationService.Text("Remain.Passes", "Passes left: {0}"), left));
                var seconds = RunRemainder.SecondsLeft(Settings.Typer.RepeatMode, Typer.RunStartedUtc, DateTime.UtcNow, Settings.Typer.DurationSeconds);
                if (seconds is int typerSeconds)
                    parts.Add(string.Format(LocalizationService.Text("Remain.TyperTime", "Typing time left: {0} s"), typerSeconds));
            }

            return string.Join("  ·  ", parts);
        }
    }

    public string PauseHotkeyText => Settings.Hotkeys.TogglePause.ToDisplayString(Settings.Appearance.Language != AppLanguage.English);

    public string PauseLabel =>
        (_services.Held == EngineFlags.None
            ? LocalizationService.Text("Pause.Hold", "Pause")
            : LocalizationService.Text("Pause.Resume", "Resume"))
        + "  " + PauseHotkeyText;

    [RelayCommand]
    private void ToggleJiggler()
    {
        CommitFocusedText();
        JigglerOn = !JigglerOn;
    }

    [RelayCommand]
    private void ToggleClicker()
    {
        CommitFocusedText();
        ClickerOn = !ClickerOn;
    }

    [RelayCommand]
    private void ToggleBoth()
    {
        CommitFocusedText();
        _services.ForgetPause();
        var any = JigglerOn || ClickerOn;
        if (any)
        {
            Jiggler.Stop();
            Clicker.Stop();
        }
        else
        {
            Jiggler.Start();
            Clicker.Start();
        }
        OnPropertyChanged(nameof(JigglerOn));
        OnPropertyChanged(nameof(ClickerOn));
    }

    [RelayCommand]
    private void ToggleRecord()
    {
        _services.ForgetPause();
        Macro.ToggleRecord();
    }

    [RelayCommand]
    private void TogglePlay()
    {
        _services.ForgetPause();
        Macro.TogglePlay();
    }

    [RelayCommand]
    private void ToggleTyper()
    {
        CommitFocusedText();
        TyperOn = !TyperOn;
    }

    [RelayCommand]
    private void Panic()
    {
        _services.Panic();
        OnPropertyChanged(nameof(JigglerOn));
        OnPropertyChanged(nameof(ClickerOn));
        OnPropertyChanged(nameof(TyperOn));
        OnPropertyChanged(nameof(PresenceOn));
        OnPropertyChanged(nameof(PauseLabel));
    }

    [RelayCommand]
    private void TogglePause()
    {
        var action = _services.TogglePause();
        OnPropertyChanged(nameof(JigglerOn));
        OnPropertyChanged(nameof(ClickerOn));
        OnPropertyChanged(nameof(TyperOn));
        OnPropertyChanged(nameof(PresenceOn));
        OnPropertyChanged(nameof(PauseLabel));
        OnPropertyChanged(nameof(RemainderText));
        StatusMessage = action switch
        {
            PauseAction.Suspended => LocalizationService.Text("Status.Suspended", "Paused. The same shortcut resumes what was running."),
            PauseAction.Resumed => LocalizationService.Text("Status.Resumed", "Resumed."),
            _ => StatusMessage
        };
    }

    [RelayCommand(CanExecute = nameof(CanTestClick))]
    private void TestClick()
    {
        CommitFocusedText();
        Clicker.StartOnce();
    }

    private bool CanTestClick() => !Clicker.IsEnabled;

    [RelayCommand(CanExecute = nameof(CanTestType))]
    private void TestType()
    {
        CommitFocusedText();
        Typer.StartOnce();
    }

    private bool CanTestType() => !Typer.IsEnabled;

    [RelayCommand]
    private void OpenSettings() => ShowSettings(null);

    public void ShowSettings(string? section, string? target = null)
    {
        SettingsWindow? window = null;
        foreach (Window open in Application.Current.Windows)
        {
            if (open is SettingsWindow existing)
            {
                window = existing;
                break;
            }
        }

        if (window is null)
        {
            window = new SettingsWindow
            {
                Owner = Application.Current.MainWindow,
                DataContext = Editor
            };
            window.Show();
        }

        if (window.DataContext is SettingsViewModel vm && !string.IsNullOrWhiteSpace(section))
            vm.SelectedSection = section;

        window.Activate();
        window.Dispatcher.BeginInvoke(() => window.FocusTarget(section, target));
    }

    [RelayCommand]
    private void PickPosition()
    {
        if (!ScreenPicker.TryPickPosition(out var x, out var y))
            return;
        Settings.Clicker.PositionMode = ClickPositionMode.Fixed;
        Settings.Clicker.FixedX = x;
        Settings.Clicker.FixedY = y;
        StatusMessage = $"{x}, {y}";
    }

    [RelayCommand]
    private void AddSequencePoint()
    {
        if (!ScreenPicker.TryPickPosition(out var x, out var y))
            return;
        Settings.Clicker.Points ??= new();
        Settings.Clicker.PositionMode = ClickPositionMode.Sequence;
        Settings.Clicker.Points.Add(new ClickPoint { X = x, Y = y });
        StatusMessage = $"{x}, {y}";
    }

    [RelayCommand]
    private void RemoveSequencePoint(ClickPoint? point)
    {
        if (point is not null)
            Settings.Clicker.Points?.Remove(point);
    }

    [RelayCommand]
    private void PickRadius()
    {
        if (!ScreenPicker.TryPickRadius(out var centerX, out var centerY, out var radius))
            return;
        Settings.Jiggler.UseFixedCenter = true;
        Settings.Jiggler.CenterX = centerX;
        Settings.Jiggler.CenterY = centerY;
        Settings.Jiggler.RadiusPx = radius;
        StatusMessage = LocalizationService.Text("Radius.Set", "Radius") +
                        $": {radius} px @ {centerX},{centerY}";
    }

    public void AttachHotkeys(Window window) => _services.Hotkeys.Attach(window);

    public void ShowHookFailure()
    {
        AppLog.Error("Low-level keyboard or mouse hook failed.");
        StatusMessage = LocalizationService.Text(
            "Status.HookFailed",
            "Could not watch keyboard and mouse. Idle pause will not work.");
    }

    public void ShowSettingsReset() =>
        StatusMessage = LocalizationService.Text(
            "Status.SettingsReset",
            "Settings could not be read and were reset.");

    private void ShowInputFault() =>
        StatusMessage = LocalizationService.Text(
            "Status.SendInputFailed",
            "Input was rejected. An elevated window may be blocking it.");

    private void ShowStopped()
    {
        void Apply() => StatusMessage = LocalizationService.Text("Status.Panic", "Stopped.");
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            Apply();
        else
            dispatcher.Invoke(Apply);
    }

    private static void CommitFocusedText()
    {
        if (Keyboard.FocusedElement is TextBox textBox)
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private static string Describe(EngineStatus status, PauseReason reason)
    {
        var label = status switch
        {
            EngineStatus.Running => LocalizationService.Text("Status.Running", "Active"),
            EngineStatus.Paused => LocalizationService.Text("Status.Paused", "Paused"),
            _ => LocalizationService.Text("Status.Stopped", "Off")
        };
        if (status != EngineStatus.Paused || reason == PauseReason.None)
            return label;

        var why = reason switch
        {
            PauseReason.Schedule => LocalizationService.Text("Pause.Schedule", "Time window"),
            PauseReason.Fullscreen => LocalizationService.Text("Pause.Fullscreen", "Fullscreen"),
            PauseReason.WaitingForIdle => LocalizationService.Text("Pause.Idle", "Waiting for idle"),
            PauseReason.UserInput => LocalizationService.Text("Pause.Input", "User input"),
            PauseReason.StartDelay => LocalizationService.Text("Pause.Delay", "Start delay"),
            _ => ""
        };
        return string.IsNullOrEmpty(why) ? label : label + " · " + why;
    }

    private static string HotkeyLabel(string action) => action switch
    {
        nameof(HotkeyAction.ToggleJiggler) => LocalizationService.Text("Hotkey.Jiggler", "Toggle jiggler"),
        nameof(HotkeyAction.ToggleClicker) => LocalizationService.Text("Hotkey.Clicker", "Toggle clicker"),
        nameof(HotkeyAction.ToggleBoth) => LocalizationService.Text("Hotkey.Both", "Toggle both"),
        nameof(HotkeyAction.Panic) => LocalizationService.Text("Hotkey.Panic", "Panic stop"),
        nameof(HotkeyAction.PickPosition) => LocalizationService.Text("Hotkey.Pick", "Pick position"),
        nameof(HotkeyAction.ToggleMacro) => LocalizationService.Text("Hotkey.Macro", "Play macro"),
        nameof(HotkeyAction.ToggleTyper) => LocalizationService.Text("Hotkey.Typer", "Toggle typer"),
        nameof(HotkeyAction.TogglePause) => LocalizationService.Text("Hotkey.Pause", "Pause and resume"),
        _ => action
    };

    public void RefreshChoices()
    {
        JigglerModes.Clear();
        JigglerModes.Add(new ChoiceOption(JigglerMode.Nudge, LocalizationService.Text("Mode.Nudge", "Nudge")));
        JigglerModes.Add(new ChoiceOption(JigglerMode.Circle, LocalizationService.Text("Mode.Circle", "Circle")));
        JigglerModes.Add(new ChoiceOption(JigglerMode.Figure8, LocalizationService.Text("Mode.Figure8", "Figure-8")));
        JigglerModes.Add(new ChoiceOption(JigglerMode.Random, LocalizationService.Text("Mode.Random", "Random")));
        JigglerModes.Add(new ChoiceOption(JigglerMode.Invisible, LocalizationService.Text("Mode.Invisible", "Invisible")));
        FillPaces(ClickPaces, PaceCatalog.Click);
        FillPaces(JigglePaces, PaceCatalog.Jiggle);
        FillPaces(TyperPaces, PaceCatalog.Typer);
    }

    private static void FillPaces(ObservableCollection<PaceChoice> target, IEnumerable<PacePreset> presets)
    {
        target.Clear();
        foreach (var preset in presets)
            target.Add(new PaceChoice(preset.Value, LocalizationService.Text(preset.Key, preset.Fallback)));
    }

    [RelayCommand]
    private void SetClickPace(int value) => Settings.Clicker.IntervalMs = value;

    [RelayCommand]
    private void SetJigglePace(int value) => Settings.Jiggler.IntervalMs = value;

    [RelayCommand]
    private void SetTyperPace(int value) => Settings.Typer.CharactersPerMinute = value;

    public void RefreshHotkeyLabels()
    {
        var german = Settings.Appearance.Language != AppLanguage.English;
        JigglerHotkeyText = Settings.Hotkeys.ToggleJiggler.ToDisplayString(german);
        ClickerHotkeyText = Settings.Hotkeys.ToggleClicker.ToDisplayString(german);
        PanicHotkeyText = Settings.Hotkeys.Panic.ToDisplayString(german);
        TyperHotkeyText = Settings.Hotkeys.ToggleTyper.ToDisplayString(german);
        OnPropertyChanged(nameof(PauseHotkeyText));
        OnPropertyChanged(nameof(PauseLabel));
        OnPropertyChanged(nameof(SafetyHint));
        OnPropertyChanged(nameof(FirstRunHint));
        _services.Hotkeys.Rebind();
    }

    private void OnHotkey(HotkeyAction action)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            switch (action)
            {
                case HotkeyAction.ToggleJiggler:
                    ToggleJiggler();
                    break;
                case HotkeyAction.ToggleClicker:
                    ToggleClicker();
                    break;
                case HotkeyAction.ToggleBoth:
                    ToggleBoth();
                    break;
                case HotkeyAction.Panic:
                    Panic();
                    break;
                case HotkeyAction.PickPosition:
                    PickPosition();
                    break;
                case HotkeyAction.ToggleMacro:
                    TogglePlay();
                    break;
                case HotkeyAction.ToggleTyper:
                    ToggleTyper();
                    break;
                case HotkeyAction.TogglePresence:
                    PresenceOn = !PresenceOn;
                    break;
                case HotkeyAction.ShowWindow:
                    ToggleWindow();
                    break;
                case HotkeyAction.RecordMacro:
                    ToggleRecord();
                    break;
                case HotkeyAction.ToggleCompact:
                    ToggleCompact();
                    break;
                case HotkeyAction.TogglePause:
                    TogglePause();
                    break;
            }
        });
    }

    private void OnAppearanceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _services.Theme.Apply(Settings);
        if (e.PropertyName == nameof(AppearanceSettings.Language))
        {
            _services.Localization.Apply(Settings.Appearance.Language);
            RefreshHotkeyLabels();
            RefreshChoices();
            OnPropertyChanged(nameof(JigglerStatusText));
            OnPropertyChanged(nameof(ClickerStatusText));
            OnPropertyChanged(nameof(ClickerProgress));
            OnPropertyChanged(nameof(SafetyHint));
            OnPropertyChanged(nameof(FirstRunHint));
            OnPropertyChanged(nameof(MacroSummary));
            OnPropertyChanged(nameof(RecordLabel));
            OnPropertyChanged(nameof(PlayLabel));
            OnPropertyChanged(nameof(TyperStatusText));
            OnPropertyChanged(nameof(TyperWpm));
            OnPropertyChanged(nameof(PresenceStatusText));
            OnPropertyChanged(nameof(ClickSummary));
            OnPropertyChanged(nameof(RemainderText));
            OnPropertyChanged(nameof(PauseLabel));
            RefreshTray();
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext is SettingsViewModel settingsVm)
                    settingsVm.RefreshChoices();
            }
        }
        else if (e.PropertyName == nameof(AppearanceSettings.AccentHex))
            RefreshTray();
    }

    private void OnGeneralChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeneralSettings.StartWithWindows))
            AutostartService.Apply(Settings.General.StartWithWindows);
        if (e.PropertyName == nameof(GeneralSettings.LogLevel))
            AppLog.Minimum = Settings.General.LogLevel;
        if (e.PropertyName == nameof(GeneralSettings.CloseBehavior))
            Settings.General.MinimizeToTray = Settings.General.CloseBehavior == CloseBehavior.ToTray;
        if (e.PropertyName == nameof(GeneralSettings.CheckForUpdates) && Settings.General.CheckForUpdates)
            _ = CheckForUpdateAsync();
        if (e.PropertyName == nameof(GeneralSettings.StatsOptOut))
            NotifyWeek();
        if (e.PropertyName is nameof(GeneralSettings.FailsafeCorner) or nameof(GeneralSettings.FirstRunDismissed))
        {
            OnPropertyChanged(nameof(SafetyHint));
            OnPropertyChanged(nameof(FirstRunHint));
            OnPropertyChanged(nameof(ShowFirstRunHint));
        }
    }

    public bool ShowFirstRunHint => !Settings.General.FirstRunDismissed;

    public string SafetyHint =>
        LocalizationService.Text("Safety.Panic", "Panic") + " " + PanicHotkeyText +
        "  ·  " + LocalizationService.Text("Pause.Hold", "Pause") + " " + PauseHotkeyText +
        "  ·  " + CornerLabel();

    public string FirstRunHint =>
        string.Format(
            LocalizationService.Text("FirstRun.Hint", "Panic is {0}. Moving the pointer into the {1} corner stops everything."),
            PanicHotkeyText,
            CornerLabel());

    [RelayCommand]
    private void DismissFirstRun()
    {
        Settings.General.FirstRunDismissed = true;
        OnPropertyChanged(nameof(ShowFirstRunHint));
    }

    private string ChoiceLabel(ClickButton button) => button switch
    {
        ClickButton.Right => LocalizationService.Text("Button.Right", "Right"),
        ClickButton.Middle => LocalizationService.Text("Button.Middle", "Middle"),
        ClickButton.X1 => LocalizationService.Text("Button.X1", "X1"),
        ClickButton.X2 => LocalizationService.Text("Button.X2", "X2"),
        ClickButton.WheelUp => LocalizationService.Text("Button.WheelUp", "Wheel up"),
        ClickButton.WheelDown => LocalizationService.Text("Button.WheelDown", "Wheel down"),
        _ => LocalizationService.Text("Button.Left", "Left")
    };

    private string ChoiceLabel(ClickAction action) => action switch
    {
        ClickAction.Double => LocalizationService.Text("Action.Double", "Double"),
        ClickAction.Hold => LocalizationService.Text("Action.Hold", "Hold"),
        _ => LocalizationService.Text("Action.Single", "Single")
    };

    private string ChoiceLabel(RepeatMode mode) => mode switch
    {
        RepeatMode.Count => LocalizationService.Text("Repeat.Count", "Count"),
        RepeatMode.Duration => LocalizationService.Text("Repeat.Duration", "Duration"),
        _ => LocalizationService.Text("Repeat.Infinite", "Infinite")
    };

    private string CornerLabel() => Settings.General.FailsafeCorner switch
    {
        FailsafeCorner.TopRight => LocalizationService.Text("Corner.TopRight", "Top right"),
        FailsafeCorner.BottomLeft => LocalizationService.Text("Corner.BottomLeft", "Bottom left"),
        FailsafeCorner.BottomRight => LocalizationService.Text("Corner.BottomRight", "Bottom right"),
        _ => LocalizationService.Text("Corner.TopLeft", "Top left")
    };

    private void RefreshTray()
    {
        var tone = ActivityTonePicker.Choose(
            Typer.Status == EngineStatus.Running,
            Clicker.Status == EngineStatus.Running,
            Jiggler.Status == EngineStatus.Running || Presence.Status == EngineStatus.Running || Macro.IsPlaying || Macro.IsRecording,
            Jiggler.Status == EngineStatus.Paused || Clicker.Status == EngineStatus.Paused ||
            Typer.Status == EngineStatus.Paused || Presence.Status == EngineStatus.Paused);
        ActivityHex = ActivityTonePicker.Hex(tone, Settings.Appearance.AccentHex);
        var detail = string.IsNullOrWhiteSpace(DeadlineText) ? PresenceStatusText : DeadlineText;
        _services.Tray.Refresh(Settings.Appearance.AccentHex, tone, detail);
        NotifyWeek();
    }

    [RelayCommand]
    private void ToggleCompact()
    {
        if (_compact is { IsVisible: true })
        {
            _compact.Hide();
            return;
        }

        _compact ??= new CompactWindow { DataContext = this };
        if (Settings.General.CompactLeft is double left && Settings.General.CompactTop is double top)
        {
            _compact.WindowStartupLocation = WindowStartupLocation.Manual;
            _compact.Left = left;
            _compact.Top = top;
        }

        _compact.Show();
        _compact.Activate();
    }

    [RelayCommand]
    private void OpenPalette()
    {
        var palette = new CommandPaletteWindow { Owner = Application.Current.MainWindow, DataContext = this };
        palette.ShowDialog();
    }

    [RelayCommand]
    private void OpenWizard()
    {
        var wizard = new WizardWindow { Owner = Application.Current.MainWindow, DataContext = Editor };
        wizard.ShowDialog();
    }

    public void NotifySummaries()
    {
        OnPropertyChanged(nameof(MacroSummary));
        NotifyWeek();
        OnPropertyChanged(nameof(RecordLabel));
        OnPropertyChanged(nameof(PlayLabel));
    }

    public void RememberCompact(double left, double top)
    {
        Settings.General.CompactLeft = left;
        Settings.General.CompactTop = top;
    }

    private void NoteClicks(int count)
    {
        var delta = count >= _statsClicks ? count - _statsClicks : count;
        _statsClicks = count;
        if (delta > 0 && !Settings.General.StatsOptOut)
            _services.Stats.Add(DateTime.Now, delta, 0, 0);
    }

    private void NoteTyped(int count)
    {
        var delta = count >= _statsTyped ? count - _statsTyped : count;
        _statsTyped = count;
        if (delta > 0 && !Settings.General.StatsOptOut)
            _services.Stats.Add(DateTime.Now, 0, delta, 0);
    }

    private static void ShowMainWindow()
    {
        var window = Application.Current.MainWindow;
        if (window is null)
            return;
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    public void CheckForUpdateIfEnabled()
    {
        if (Settings.General.CheckForUpdates)
            _ = CheckForUpdateAsync();
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            var current = typeof(MainViewModel).Assembly.GetName().Version ?? new Version(1, 0, 0);
            var newer = await UpdateChecker.FindNewerTagAsync(current);
            if (newer is null)
                return;
            var message = string.Format(LocalizationService.Text("Status.Update", "Update {0} is available."), newer);
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
                StatusMessage = message;
            else
                dispatcher.Invoke(() => StatusMessage = message);
        }
        catch (Exception ex)
        {
            AppLog.Error("Update check failed.", ex);
        }
    }

    private void OnRunFinished()
    {
        if (Settings.General.PlaySoundOnFinish)
            CompletionFeedback.PlaySound(Settings.General.SoundName);
        if (Settings.General.NotifyOnFinish)
            _services.Tray.Notify(LocalizationService.Text("Notify.Done", "Finished."));
    }

    private static void ToggleWindow()
    {
        var window = Application.Current?.MainWindow;
        if (window is null)
            return;
        if (window.IsVisible && window.WindowState != WindowState.Minimized)
            window.Hide();
        else
            ShowMainWindow();
    }

    private void ExitApp()
    {
        _services.Panic();
        Application.Current.Shutdown();
    }
}
