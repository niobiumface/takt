using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Takt.Helpers;
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
        Settings.Hotkeys.ToggleMacro.PropertyChanged += (_, _) => NotifyHotkeyDisplays();
        Settings.Hotkeys.ToggleTyper.PropertyChanged += (_, _) => NotifyHotkeyDisplays();
        _services.Hotkeys.RegistrationFailed += name => RefreshHotkeyRows(name);
        BuildCatalog();
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var name = string.IsNullOrWhiteSpace(ProfileName) ? "Standard" : ProfileName.Trim();
        var existing = Settings.Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            Settings.Profiles.Remove(existing);
        var captured = ProfileApplicator.Capture(Settings, name);
        Settings.Profiles.Add(captured);
        SelectedProfile = captured;
        ProfileName = name;
    }

    [RelayCommand]
    private void ApplyProfile()
    {
        if (SelectedProfile is null)
            return;
        ProfileApplicator.Apply(SelectedProfile, Settings);
        _main.RefreshHotkeyLabels();
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile is null)
            return;
        Settings.Profiles.Remove(SelectedProfile);
        SelectedProfile = null;
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
    public ObservableCollection<ChoiceOption> PresenceKeys { get; } = [];

    [ObservableProperty] private string? _capturingHotkey;

    public bool IsGerman => Settings.Appearance.Language != AppLanguage.English;
    public string JigglerHotkeyDisplay => Settings.Hotkeys.ToggleJiggler.ToDisplayString(IsGerman);
    public string ClickerHotkeyDisplay => Settings.Hotkeys.ToggleClicker.ToDisplayString(IsGerman);
    public string BothHotkeyDisplay => Settings.Hotkeys.ToggleBoth.ToDisplayString(IsGerman);
    public string PanicHotkeyDisplay => Settings.Hotkeys.Panic.ToDisplayString(IsGerman);
    public string PickHotkeyDisplay => Settings.Hotkeys.PickPosition.ToDisplayString(IsGerman);
    public string MacroHotkeyDisplay => Settings.Hotkeys.ToggleMacro.ToDisplayString(IsGerman);
    public string TyperHotkeyDisplay => Settings.Hotkeys.ToggleTyper.ToDisplayString(IsGerman);

    [ObservableProperty] private string _snippetName = "";
    [ObservableProperty] private string _snippetText = "";

    [ObservableProperty] private string _profileName = "";
    [ObservableProperty] private ActivityProfile? _selectedProfile;

    [RelayCommand]
    private void SelectAccent(string hex)
    {
        Settings.Appearance.AccentHex = hex;
        _services.Theme.ApplyAccent(hex);
    }

    [RelayCommand]
    private void PickPosition() => _main.PickPositionCommand.Execute(null);

    [RelayCommand]
    private void AddPoint() => _main.AddSequencePointCommand.Execute(null);

    [RelayCommand]
    private void RemovePoint(ClickPoint? point) => _main.RemoveSequencePointCommand.Execute(point);

    [RelayCommand]
    private void PickRadius() => _main.PickRadiusCommand.Execute(null);

    [RelayCommand]
    private void CaptureHotkey(string name)
    {
        CapturingHotkey = name;
        _services.Hotkeys.Suspend();
    }

    public void CancelCapture()
    {
        if (CapturingHotkey is null)
            return;
        FinishCapture();
    }

    public bool TryCompleteCapture(KeyEventArgs args)
    {
        if (CapturingHotkey is null)
            return false;
        if (args.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System)
            return true;
        if (args.Key == Key.Escape)
        {
            FinishCapture();
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
            "macro" => Settings.Hotkeys.ToggleMacro,
            "typer" => Settings.Hotkeys.ToggleTyper,
            "presence" => Settings.Hotkeys.TogglePresence,
            "record" => Settings.Hotkeys.RecordMacro,
            "show" => Settings.Hotkeys.ShowWindow,
            _ => null
        };

        if (binding is not null)
        {
            binding.Modifiers = mods;
            binding.VirtualKey = vk;
            _main.RefreshHotkeyLabels();
            NotifyHotkeyDisplays();
        }

        FinishCapture();
        args.Handled = true;
        return true;
    }

    private void FinishCapture()
    {
        CapturingHotkey = null;
        _services.Hotkeys.Resume();
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
            new(ClickButton.Middle, T("Button.Middle", "Middle")),
            new(ClickButton.X1, T("Button.X1", "X1")),
            new(ClickButton.X2, T("Button.X2", "X2")),
            new(ClickButton.WheelUp, T("Button.WheelUp", "Wheel up")),
            new(ClickButton.WheelDown, T("Button.WheelDown", "Wheel down")));
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
            new(ClickPositionMode.Fixed, T("Position.Fixed", "Fixed position")),
            new(ClickPositionMode.Sequence, T("Position.Sequence", "Point sequence")));
        Replace(Themes,
            new(ThemeMode.Dark, T("Theme.Dark", "Dark")),
            new(ThemeMode.Light, T("Theme.Light", "Light")),
            new(ThemeMode.System, T("Theme.System", "System")));
        Replace(Languages,
            new(AppLanguage.English, "English"),
            new(AppLanguage.German, "Deutsch"));
        Replace(PresenceKeys,
            new(PresenceKey.F15, T("Presence.KeyF15", "F15")),
            new(PresenceKey.Shift, T("Presence.KeyShift", "Shift")),
            new(PresenceKey.Control, T("Presence.KeyCtrl", "Ctrl")),
            new(PresenceKey.None, T("Presence.KeyNone", "None")));
        Replace(Corners,
            new(FailsafeCorner.TopLeft, T("Corner.TopLeft", "Top left")),
            new(FailsafeCorner.TopRight, T("Corner.TopRight", "Top right")),
            new(FailsafeCorner.BottomLeft, T("Corner.BottomLeft", "Bottom left")),
            new(FailsafeCorner.BottomRight, T("Corner.BottomRight", "Bottom right")));
        OnPropertyChanged(nameof(IsGerman));
        NotifyHotkeyDisplays();
        if (Navigation.Count > 0)
            BuildCatalog();
    }

    private void NotifyHotkeyDisplays()
    {
        OnPropertyChanged(nameof(JigglerHotkeyDisplay));
        OnPropertyChanged(nameof(ClickerHotkeyDisplay));
        OnPropertyChanged(nameof(BothHotkeyDisplay));
        OnPropertyChanged(nameof(PanicHotkeyDisplay));
        OnPropertyChanged(nameof(PickHotkeyDisplay));
        OnPropertyChanged(nameof(MacroHotkeyDisplay));
        OnPropertyChanged(nameof(TyperHotkeyDisplay));
    }

    [RelayCommand]
    private void AddSnippet()
    {
        if (string.IsNullOrWhiteSpace(SnippetText))
            return;
        Settings.Typer.Snippets ??= new();
        Settings.Typer.Snippets.Add(new TextSnippet
        {
            Name = string.IsNullOrWhiteSpace(SnippetName) ? "Text" : SnippetName.Trim(),
            Text = SnippetText
        });
    }

    [RelayCommand]
    private void DeleteSnippet(TextSnippet? snippet)
    {
        if (snippet is not null)
            Settings.Typer.Snippets?.Remove(snippet);
    }

    private static void Replace(ObservableCollection<ChoiceOption> target, params ChoiceOption[] items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }

    private static string T(string key, string fallback) => LocalizationService.Text(key, fallback);
}
