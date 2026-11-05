using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Takt.Models;

public partial class AppSettings : ObservableObject
{
    public const int CurrentVersion = 2;

    [ObservableProperty] private int _version = CurrentVersion;
    [ObservableProperty] private JigglerSettings _jiggler = new();
    [ObservableProperty] private ClickerSettings _clicker = new();
    [ObservableProperty] private HotkeySettings _hotkeys = new();
    [ObservableProperty] private AppearanceSettings _appearance = new();
    [ObservableProperty] private GeneralSettings _general = new();
    [ObservableProperty] private MacroSettings _macro = new();
    [ObservableProperty] private TyperSettings _typer = new();
    [ObservableProperty] private PresenceSettings _presence = new();
    [ObservableProperty] private WorkScheduleSettings _work = new();
    [ObservableProperty] private ObservableCollection<ActivityProfile> _profiles = new();
}

public partial class ClickPoint : ObservableObject
{
    [ObservableProperty] private int _x;
    [ObservableProperty] private int _y;
    [ObservableProperty] private int _delayMs;
    [ObservableProperty] private bool _customButton;
    [ObservableProperty] private ClickButton _button = ClickButton.Left;
}

public partial class MacroEvent : ObservableObject
{
    [ObservableProperty] private int _delayMs;
    [ObservableProperty] private int _x;
    [ObservableProperty] private int _y;
    [ObservableProperty] private string _kind = "move";
    [ObservableProperty] private string _button = "Left";
    [ObservableProperty] private int _wheelDelta;
    [ObservableProperty] private bool _anchored;
    [ObservableProperty] private int _relX;
    [ObservableProperty] private int _relY;
    [ObservableProperty] private string _anchorProcess = "";
    [ObservableProperty] private string _anchorTitle = "";
    [ObservableProperty] private string _text = "";
}

public partial class SavedMacro : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _loopCount = 1;
    [ObservableProperty] private MacroSpeed _speed = MacroSpeed.Normal;
    [ObservableProperty] private ObservableCollection<MacroEvent> _events = new();
}

public partial class SavedSequence : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private ObservableCollection<ClickPoint> _points = new();
}

public partial class MacroSettings : ObservableObject
{
    [ObservableProperty] private ObservableCollection<MacroEvent> _events = new();
    [ObservableProperty] private int _loopCount = 1;
    [ObservableProperty] private MacroSpeed _speed = MacroSpeed.Normal;
    [ObservableProperty] private ObservableCollection<SavedMacro> _saved = new();
}

public partial class ActivityProfile : ObservableObject
{
    [ObservableProperty] private string _name = "Standard";
    [ObservableProperty] private JigglerSettings _jiggler = new();
    [ObservableProperty] private ClickerSettings _clicker = new();
    [ObservableProperty] private HotkeySettings _hotkeys = new();
    [ObservableProperty] private MacroSettings _macro = new();
    [ObservableProperty] private TyperSettings _typer = new();
    [ObservableProperty] private PresenceSettings _presence = new();
    [ObservableProperty] private GeneralSettings _general = new();
    [ObservableProperty] private AppearanceSettings _appearance = new();
    [ObservableProperty] private WorkScheduleSettings _work = new();
}

public partial class TextSnippet : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _text = "";
}

public partial class TyperSettings : ObservableObject
{
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private int _charactersPerMinute = 400;
    [ObservableProperty] private int _jitterPercent = 15;
    [ObservableProperty] private int _punctuationPauseMs = 120;
    [ObservableProperty] private bool _simulateTypos;
    [ObservableProperty] private int _typoPercent = 2;
    [ObservableProperty] private string _dateFormat = "yyyy-MM-dd";
    [ObservableProperty] private string _timeFormat = "HH:mm";
    [ObservableProperty] private string _dateTimeFormat = "yyyy-MM-dd HH:mm";
    [ObservableProperty] private int _startDelayMs;
    [ObservableProperty] private RepeatMode _repeatMode = RepeatMode.Count;
    [ObservableProperty] private int _repeatCount = 1;
    [ObservableProperty] private int _durationSeconds = 60;
    [ObservableProperty] private ObservableCollection<TextSnippet> _snippets = new();
}

public partial class PresenceSettings : ObservableObject
{
    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private int _intervalMs = 45_000;
    [ObservableProperty] private int _jitterMs = 8_000;
    [ObservableProperty] private bool _useFallbackKey = true;
    [ObservableProperty] private PresenceKey _fallbackKey = PresenceKey.F15;
    [ObservableProperty] private PresenceMotion _motion = PresenceMotion.Invisible;
    [ObservableProperty] private bool _preventScreensaver = true;
    [ObservableProperty] private bool _preventDisplayOff = true;
    [ObservableProperty] private bool _preventSleep = true;
    [ObservableProperty] private bool _pauseOnUserInput = true;
    [ObservableProperty] private int _resumeIdleSeconds = 3;
    [ObservableProperty] private bool _useWorkHours = true;
    [ObservableProperty] private bool _monday = true;
    [ObservableProperty] private bool _tuesday = true;
    [ObservableProperty] private bool _wednesday = true;
    [ObservableProperty] private bool _thursday = true;
    [ObservableProperty] private bool _friday = true;
    [ObservableProperty] private bool _saturday;
    [ObservableProperty] private bool _sunday;
    [ObservableProperty] private string _scheduleStart = "08:00";
    [ObservableProperty] private string _scheduleEnd = "17:00";

    public WeekdayMask Days =>
        (Monday ? WeekdayMask.Monday : WeekdayMask.None) |
        (Tuesday ? WeekdayMask.Tuesday : WeekdayMask.None) |
        (Wednesday ? WeekdayMask.Wednesday : WeekdayMask.None) |
        (Thursday ? WeekdayMask.Thursday : WeekdayMask.None) |
        (Friday ? WeekdayMask.Friday : WeekdayMask.None) |
        (Saturday ? WeekdayMask.Saturday : WeekdayMask.None) |
        (Sunday ? WeekdayMask.Sunday : WeekdayMask.None);
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
    [ObservableProperty] private int _resumeIdleSeconds = 2;
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
    [ObservableProperty] private int _jitterPercent;
    [ObservableProperty] private int _holdDurationMs = 1000;
    [ObservableProperty] private bool _preciseTiming = true;
    [ObservableProperty] private int _burstSize;
    [ObservableProperty] private int _burstPauseMs = 500;
    [ObservableProperty] private RepeatMode _repeatMode = RepeatMode.Infinite;
    [ObservableProperty] private int _repeatCount = 100;
    [ObservableProperty] private int _durationSeconds = 60;
    [ObservableProperty] private int _startDelayMs;
    [ObservableProperty] private ClickPositionMode _positionMode = ClickPositionMode.CurrentCursor;
    [ObservableProperty] private int _fixedX;
    [ObservableProperty] private int _fixedY;
    [ObservableProperty] private ObservableCollection<ClickPoint> _points = new();
    [ObservableProperty] private ObservableCollection<SavedSequence> _savedSequences = new();
}

public partial class HotkeySettings : ObservableObject
{
    [ObservableProperty] private HotkeyBinding _toggleJiggler = HotkeyBinding.Vk(0x75);
    [ObservableProperty] private HotkeyBinding _toggleClicker = HotkeyBinding.Vk(0x76);
    [ObservableProperty] private HotkeyBinding _toggleBoth = HotkeyBinding.Vk(0x77);
    [ObservableProperty] private HotkeyBinding _panic = HotkeyBinding.Vk(0x78);
    [ObservableProperty] private HotkeyBinding _pickPosition = HotkeyBinding.Vk(0x79);
    [ObservableProperty] private HotkeyBinding _toggleMacro = HotkeyBinding.Vk(0x7B);
    [ObservableProperty] private HotkeyBinding _toggleTyper = HotkeyBinding.Vk(0x7A);
    [ObservableProperty] private HotkeyBinding _togglePresence = HotkeyBinding.Vk(0x7C);
    [ObservableProperty] private HotkeyBinding _recordMacro = HotkeyBinding.Vk(0x7B, 0x0002);
    [ObservableProperty] private HotkeyBinding _showWindow = HotkeyBinding.Vk(0x54, 0x0001 | 0x0002);
    [ObservableProperty] private HotkeyBinding _toggleCompact = HotkeyBinding.Vk(0x4D, 0x0002 | 0x0004);
    [ObservableProperty] private HotkeyBinding _togglePause = HotkeyBinding.Vk(0x74);
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
    [ObservableProperty] private bool _checkForUpdates;
    [ObservableProperty] private bool _playSoundOnFinish;
    [ObservableProperty] private string _soundName = "SystemAsterisk";
    [ObservableProperty] private bool _notifyOnFinish;
    [ObservableProperty] private bool _notifyOnFailsafe = true;
    [ObservableProperty] private bool _notifyOnPresence;
    [ObservableProperty] private bool _notifyOnHotkey;
    [ObservableProperty] private LogLevel _logLevel = LogLevel.Info;
    [ObservableProperty] private CloseBehavior _closeBehavior = CloseBehavior.ToTray;
    [ObservableProperty] private bool _wizardCompleted;
    [ObservableProperty] private bool _statsOptOut;
    [ObservableProperty] private int _stopAfterMinutes;
    [ObservableProperty] private string _stopAtTime = "";
    [ObservableProperty] private double? _compactLeft;
    [ObservableProperty] private double? _compactTop;
    [ObservableProperty] private bool _autoProfile;
    [ObservableProperty] private int _profileDebounceMs = 800;
    [ObservableProperty] private ObservableCollection<AppRule> _appRules = new();
}

public partial class AppRule : ObservableObject
{
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private string _processName = "";
    [ObservableProperty] private string _profileName = "";
}

public partial class WorkScheduleSettings : ObservableObject
{
    public WorkScheduleSettings()
    {
        if (Days.Count == 0)
        {
            foreach (var day in new[]
                     {
                         DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                         DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
                     })
                Days.Add(new DayWindows { Day = day });
        }
    }

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private OutsideHoursBehavior _outside = OutsideHoursBehavior.PauseAndResume;
    [ObservableProperty] private ObservableCollection<DayWindows> _days = new();
    [ObservableProperty] private ObservableCollection<string> _holidays = new();
}

public partial class DayWindows : ObservableObject
{
    [ObservableProperty] private DayOfWeek _day;
    [ObservableProperty] private ObservableCollection<TimeSpanWindow> _windows = new();
}

public partial class TimeSpanWindow : ObservableObject
{
    [ObservableProperty] private string _start = "08:00";
    [ObservableProperty] private string _end = "17:00";
}
