using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Takt.Models;
using Takt.Services;
using Takt.Views;

namespace Takt.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    private readonly AppServices _services;

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
        _services.Tray.ShowWindowRequested += ShowMainWindow;
        _services.Tray.ToggleJigglerRequested += () => ToggleJiggler();
        _services.Tray.ToggleClickerRequested += () => ToggleClicker();
        _services.Tray.PanicRequested += Panic;
        _services.Tray.ExitRequested += ExitApp;
        _services.Settings.Current.Hotkeys.PropertyChanged += (_, _) => RefreshHotkeyLabels();
        _services.Settings.Current.Hotkeys.ToggleJiggler.PropertyChanged += (_, _) => RefreshHotkeyLabels();
        _services.Settings.Current.Hotkeys.ToggleClicker.PropertyChanged += (_, _) => RefreshHotkeyLabels();
        _services.Settings.Current.Appearance.PropertyChanged += OnAppearanceChanged;
        _services.Settings.Current.General.PropertyChanged += OnGeneralChanged;
        RefreshHotkeyLabels();
        RefreshChoices();
        RefreshTray();
    }

    public ObservableCollection<ChoiceOption> JigglerModes { get; } = [];
    public AppSettings Settings => _services.Settings.Current;
    public JigglerEngine Jiggler => _services.Jiggler;
    public ClickerEngine Clicker => _services.Clicker;
    public AppServices Services => _services;

    [ObservableProperty] private string _jigglerHotkeyText = "F6";
    [ObservableProperty] private string _clickerHotkeyText = "F7";
    [ObservableProperty] private string _panicHotkeyText = "F9";
    [ObservableProperty] private string _statusMessage = "";

    public bool JigglerOn
    {
        get => Jiggler.IsEnabled;
        set
        {
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
            if (value)
                Clicker.Start();
            else
                Clicker.Stop();
            OnPropertyChanged();
        }
    }

    public string JigglerStatusText => Describe(Jiggler.Status, Jiggler.PauseReason);

    public string ClickerStatusText => Describe(Clicker.Status, Clicker.PauseReason);

    public string ClickerProgress =>
        Settings.Clicker.RepeatMode == RepeatMode.Count
            ? $"{Clicker.ClickCount} / {Math.Max(1, Settings.Clicker.RepeatCount)}"
            : Clicker.ClickCount.ToString();

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
    private void Panic()
    {
        _services.Panic();
        OnPropertyChanged(nameof(JigglerOn));
        OnPropertyChanged(nameof(ClickerOn));
    }

    [RelayCommand]
    private void OpenSettings()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is SettingsWindow existing)
            {
                existing.Activate();
                return;
            }
        }

        var settings = new SettingsWindow
        {
            Owner = Application.Current.MainWindow,
            DataContext = new SettingsViewModel(_services, this)
        };
        settings.Show();
    }

    [RelayCommand]
    private void PickPosition()
    {
        var picker = new PositionPickerWindow();
        if (picker.ShowDialog() == true)
        {
            Settings.Clicker.PositionMode = ClickPositionMode.Fixed;
            Settings.Clicker.FixedX = picker.PickedX;
            Settings.Clicker.FixedY = picker.PickedY;
            StatusMessage = $"{picker.PickedX}, {picker.PickedY}";
        }
    }

    [RelayCommand]
    private void PickRadius()
    {
        var picker = new RadiusPickerWindow();
        if (picker.ShowDialog() == true)
        {
            Settings.Jiggler.UseFixedCenter = true;
            Settings.Jiggler.CenterX = picker.CenterX;
            Settings.Jiggler.CenterY = picker.CenterY;
            Settings.Jiggler.RadiusPx = picker.RadiusPx;
            StatusMessage = LocalizationService.Text("Radius.Set", "Radius") +
                            $": {picker.RadiusPx} px @ {picker.CenterX},{picker.CenterY}";
        }
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
    }

    public void RefreshHotkeyLabels()
    {
        var german = Settings.Appearance.Language != AppLanguage.English;
        JigglerHotkeyText = Settings.Hotkeys.ToggleJiggler.ToDisplayString(german);
        ClickerHotkeyText = Settings.Hotkeys.ToggleClicker.ToDisplayString(german);
        PanicHotkeyText = Settings.Hotkeys.Panic.ToDisplayString(german);
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

    private string CornerLabel() => Settings.General.FailsafeCorner switch
    {
        FailsafeCorner.TopRight => LocalizationService.Text("Corner.TopRight", "Top right"),
        FailsafeCorner.BottomLeft => LocalizationService.Text("Corner.BottomLeft", "Bottom left"),
        FailsafeCorner.BottomRight => LocalizationService.Text("Corner.BottomRight", "Bottom right"),
        _ => LocalizationService.Text("Corner.TopLeft", "Top left")
    };

    private void RefreshTray()
    {
        var presence = TrayPresence.Off;
        if (Jiggler.Status == EngineStatus.Running || Clicker.Status == EngineStatus.Running)
            presence = TrayPresence.Active;
        else if (Jiggler.Status == EngineStatus.Paused || Clicker.Status == EngineStatus.Paused)
            presence = TrayPresence.Paused;
        _services.Tray.Refresh(Settings.Appearance.AccentHex, presence);
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

    private void ExitApp()
    {
        _services.Panic();
        Application.Current.Shutdown();
    }
}
