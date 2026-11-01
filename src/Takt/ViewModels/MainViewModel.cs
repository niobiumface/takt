using System.Collections.ObjectModel;
using System.Windows;
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
            if (e.PropertyName is nameof(JigglerEngine.InputFault) && Jiggler.InputFault)
                ShowInputFault();
            RefreshTray();
        };
        Clicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ClickerEngine.IsEnabled))
                OnPropertyChanged(nameof(ClickerOn));
            if (e.PropertyName is nameof(ClickerEngine.InputFault) && Clicker.InputFault)
                ShowInputFault();
            RefreshTray();
        };
        _services.Hotkeys.Pressed += OnHotkey;
        _services.Hotkeys.ShowRequested += ShowMainWindow;
        _services.Hotkeys.RegistrationFailed += _ =>
            StatusMessage = LocalizationService.Text("Hotkey.Conflict", "Hotkey already in use.");
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

    [RelayCommand]
    private void ToggleJiggler() => JigglerOn = !JigglerOn;

    [RelayCommand]
    private void ToggleClicker() => ClickerOn = !ClickerOn;

    [RelayCommand]
    private void ToggleBoth()
    {
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
        StatusMessage = LocalizationService.Text("Status.Panic", "Stopped.");
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

    public void ShowHookFailure() =>
        StatusMessage = LocalizationService.Text(
            "Status.HookFailed",
            "Could not watch keyboard and mouse. Idle pause will not work.");

    private void ShowInputFault() =>
        StatusMessage = LocalizationService.Text(
            "Status.SendInputFailed",
            "Input was rejected. An elevated window may be blocking it.");

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
    }

    private void RefreshTray() =>
        _services.Tray.Refresh(Settings.Appearance.AccentHex, Jiggler.IsEnabled, Clicker.IsEnabled);

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
