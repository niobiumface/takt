using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Takt.Models;
using Takt.Services;

namespace Takt.ViewModels;

public sealed record ChoiceOption(object Value, string Label);

internal partial class SettingsViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly MainViewModel _main;

    public SettingsViewModel(AppServices services, MainViewModel main)
    {
        _services = services;
        _main = main;
        Settings = services.Settings.Current;
        RefreshChoices();
        NotifyHotkeyDisplays();
        Settings.Hotkeys.ToggleJiggler.PropertyChanged += (_, _) => NotifyHotkeyDisplays();
        Settings.Hotkeys.ToggleClicker.PropertyChanged += (_, _) => NotifyHotkeyDisplays();
        Settings.Hotkeys.ToggleBoth.PropertyChanged += (_, _) => NotifyHotkeyDisplays();
        Settings.Hotkeys.Panic.PropertyChanged += (_, _) => NotifyHotkeyDisplays();
        Settings.Hotkeys.PickPosition.PropertyChanged += (_, _) => NotifyHotkeyDisplays();
    }

    public AppSettings Settings { get; }
    public string[] AccentPresets => ThemeService.AccentPresets;

    public ObservableCollection<ChoiceOption> JigglerModes { get; } = [];
    public ObservableCollection<ChoiceOption> ClickButtons { get; } = [];
    public ObservableCollection<ChoiceOption> ClickActions { get; } = [];
    public ObservableCollection<ChoiceOption> RepeatModes { get; } = [];
    public ObservableCollection<ChoiceOption> PositionModes { get; } = [];
    public ObservableCollection<ChoiceOption> Themes { get; } = [];
    public ObservableCollection<ChoiceOption> Languages { get; } = [];
    public ObservableCollection<ChoiceOption> Corners { get; } = [];

    [ObservableProperty] private string? _capturingHotkey;

    public bool IsGerman => Settings.Appearance.Language != AppLanguage.English;
    public string JigglerHotkeyDisplay => Settings.Hotkeys.ToggleJiggler.ToDisplayString(IsGerman);
    public string ClickerHotkeyDisplay => Settings.Hotkeys.ToggleClicker.ToDisplayString(IsGerman);
    public string BothHotkeyDisplay => Settings.Hotkeys.ToggleBoth.ToDisplayString(IsGerman);
    public string PanicHotkeyDisplay => Settings.Hotkeys.Panic.ToDisplayString(IsGerman);
    public string PickHotkeyDisplay => Settings.Hotkeys.PickPosition.ToDisplayString(IsGerman);

    [RelayCommand]
    private void SelectAccent(string hex)
    {
        Settings.Appearance.AccentHex = hex;
        _services.Theme.ApplyAccent(hex);
    }

    [RelayCommand]
    private void PickPosition() => _main.PickPositionCommand.Execute(null);

    [RelayCommand]
    private void PickRadius() => _main.PickRadiusCommand.Execute(null);

    [RelayCommand]
    private void CaptureHotkey(string name) => CapturingHotkey = name;

    public bool TryCompleteCapture(KeyEventArgs args)
    {
        if (CapturingHotkey is null)
            return false;
        if (args.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System)
            return true;
        if (args.Key == Key.Escape)
        {
            CapturingHotkey = null;
            return true;
        }

        var vk = KeyInterop.VirtualKeyFromKey(args.Key == Key.System ? args.SystemKey : args.Key);
        if (vk == 0)
            return true;

        var mods = 0;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods |= 0x0002;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) mods |= 0x0001;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) mods |= 0x0004;
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) mods |= 0x0008;

        var binding = CapturingHotkey switch
        {
            "jiggler" => Settings.Hotkeys.ToggleJiggler,
            "clicker" => Settings.Hotkeys.ToggleClicker,
            "both" => Settings.Hotkeys.ToggleBoth,
            "panic" => Settings.Hotkeys.Panic,
            "pick" => Settings.Hotkeys.PickPosition,
            _ => null
        };

        if (binding is not null)
        {
            binding.Modifiers = mods;
            binding.VirtualKey = vk;
            _main.RefreshHotkeyLabels();
            NotifyHotkeyDisplays();
        }

        CapturingHotkey = null;
        args.Handled = true;
        return true;
    }

    public void RefreshChoices()
    {
        Replace(JigglerModes,
            new(JigglerMode.Nudge, T("Mode.Nudge", "Nudge")),
            new(JigglerMode.Circle, T("Mode.Circle", "Circle")),
            new(JigglerMode.Figure8, T("Mode.Figure8", "Figure-8")),
            new(JigglerMode.Random, T("Mode.Random", "Random")),
            new(JigglerMode.Invisible, T("Mode.Invisible", "Invisible")));
        Replace(ClickButtons,
            new(ClickButton.Left, T("Button.Left", "Left")),
            new(ClickButton.Right, T("Button.Right", "Right")),
            new(ClickButton.Middle, T("Button.Middle", "Middle")));
        Replace(ClickActions,
            new(ClickAction.Single, T("Action.Single", "Single")),
            new(ClickAction.Double, T("Action.Double", "Double")),
            new(ClickAction.Hold, T("Action.Hold", "Hold")));
        Replace(RepeatModes,
            new(RepeatMode.Infinite, T("Repeat.Infinite", "Unlimited")),
            new(RepeatMode.Count, T("Repeat.Count", "Count")),
            new(RepeatMode.Duration, T("Repeat.Duration", "Duration")));
        Replace(PositionModes,
            new(ClickPositionMode.CurrentCursor, T("Position.Cursor", "Current cursor")),
            new(ClickPositionMode.Fixed, T("Position.Fixed", "Fixed position")));
        Replace(Themes,
            new(ThemeMode.Dark, T("Theme.Dark", "Dark")),
            new(ThemeMode.Light, T("Theme.Light", "Light")));
        Replace(Languages,
            new(AppLanguage.English, "English"),
            new(AppLanguage.German, "Deutsch"));
        Replace(Corners,
            new(FailsafeCorner.TopLeft, T("Corner.TopLeft", "Top left")),
            new(FailsafeCorner.TopRight, T("Corner.TopRight", "Top right")),
            new(FailsafeCorner.BottomLeft, T("Corner.BottomLeft", "Bottom left")),
            new(FailsafeCorner.BottomRight, T("Corner.BottomRight", "Bottom right")));
        OnPropertyChanged(nameof(IsGerman));
        NotifyHotkeyDisplays();
    }

    private void NotifyHotkeyDisplays()
    {
        OnPropertyChanged(nameof(JigglerHotkeyDisplay));
        OnPropertyChanged(nameof(ClickerHotkeyDisplay));
        OnPropertyChanged(nameof(BothHotkeyDisplay));
        OnPropertyChanged(nameof(PanicHotkeyDisplay));
        OnPropertyChanged(nameof(PickHotkeyDisplay));
    }

    private static void Replace(ObservableCollection<ChoiceOption> target, params ChoiceOption[] items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }

    private static string T(string key, string fallback) => LocalizationService.Text(key, fallback);
}
