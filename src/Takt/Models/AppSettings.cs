using CommunityToolkit.Mvvm.ComponentModel;

namespace Takt.Models;

public partial class AppSettings : ObservableObject
{
    public const int CurrentVersion = 1;

    [ObservableProperty] private int _version = CurrentVersion;
    [ObservableProperty] private JigglerSettings _jiggler = new();
    [ObservableProperty] private ClickerSettings _clicker = new();
    [ObservableProperty] private HotkeySettings _hotkeys = new();
    [ObservableProperty] private AppearanceSettings _appearance = new();
    [ObservableProperty] private GeneralSettings _general = new();
}

public partial class JigglerSettings : ObservableObject
{
    [ObservableProperty] private JigglerMode _mode = JigglerMode.Nudge;
    [ObservableProperty] private int _intervalMs = 5000;
    [ObservableProperty] private int _jitterMs;
    [ObservableProperty] private int _amplitude = 8;
    [ObservableProperty] private int _radiusPx = 64;
    [ObservableProperty] private bool _useFixedCenter;
    [ObservableProperty] private int _centerX;
    [ObservableProperty] private int _centerY;
    [ObservableProperty] private bool _preventSleep = true;
    [ObservableProperty] private bool _preventDisplayOff = true;
    [ObservableProperty] private bool _waitForIdle;
    [ObservableProperty] private int _idleThresholdSeconds = 30;
    [ObservableProperty] private bool _pauseOnUserInput = true;
    [ObservableProperty] private bool _pauseWhenFullscreen;
    [ObservableProperty] private bool _useSchedule;
    [ObservableProperty] private string _scheduleStart = "09:00";
    [ObservableProperty] private string _scheduleEnd = "18:00";
}

public partial class ClickerSettings : ObservableObject
{
    [ObservableProperty] private ClickButton _button = ClickButton.Left;
    [ObservableProperty] private ClickAction _action = ClickAction.Single;
    [ObservableProperty] private int _intervalMs = 100;
    [ObservableProperty] private int _jitterMs;
    [ObservableProperty] private int _burstSize;
    [ObservableProperty] private int _burstPauseMs = 500;
    [ObservableProperty] private RepeatMode _repeatMode = RepeatMode.Infinite;
    [ObservableProperty] private int _repeatCount = 100;
    [ObservableProperty] private int _durationSeconds = 60;
    [ObservableProperty] private int _startDelayMs;
    [ObservableProperty] private ClickPositionMode _positionMode = ClickPositionMode.CurrentCursor;
    [ObservableProperty] private int _fixedX;
    [ObservableProperty] private int _fixedY;
}

public partial class HotkeySettings : ObservableObject
{
    [ObservableProperty] private HotkeyBinding _toggleJiggler = HotkeyBinding.Vk(0x75);
    [ObservableProperty] private HotkeyBinding _toggleClicker = HotkeyBinding.Vk(0x76);
    [ObservableProperty] private HotkeyBinding _toggleBoth = HotkeyBinding.Vk(0x77);
    [ObservableProperty] private HotkeyBinding _panic = HotkeyBinding.Vk(0x78);
    [ObservableProperty] private HotkeyBinding _pickPosition = HotkeyBinding.Vk(0x79);
}

public partial class AppearanceSettings : ObservableObject
{
    [ObservableProperty] private ThemeMode _theme = ThemeMode.Dark;
    [ObservableProperty] private string _accentHex = "#5B8DEF";
    [ObservableProperty] private AppLanguage _language = AppLanguage.English;
    [ObservableProperty] private bool _alwaysOnTop;
}

public partial class GeneralSettings : ObservableObject
{
    [ObservableProperty] private bool _minimizeToTray = true;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _failsafeEnabled = true;
    [ObservableProperty] private FailsafeCorner _failsafeCorner = FailsafeCorner.TopLeft;
    [ObservableProperty] private int _failsafeSize = 8;
    [ObservableProperty] private bool _firstRunDismissed;
}
