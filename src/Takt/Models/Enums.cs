namespace Takt.Models;

public enum JigglerMode
{
    Nudge,
    Circle,
    Figure8,
    Random,
    Invisible
}

public enum ClickButton
{
    Left,
    Right,
    Middle,
    X1,
    X2,
    WheelUp,
    WheelDown
}

public enum ClickAction
{
    Single,
    Double,
    Hold
}

public enum ClickPositionMode
{
    CurrentCursor,
    Fixed,
    Sequence
}

public enum RepeatMode
{
    Infinite,
    Count,
    Duration
}

public enum FailsafeCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public enum ThemeMode
{
    Dark,
    Light,
    System
}

public enum CloseBehavior
{
    ToTray,
    Exit
}

public enum OutsideHoursBehavior
{
    PauseAndResume,
    Stop,
    HintOnly
}

public enum ScheduleEffect
{
    Run,
    Pause,
    Stop,
    Hint
}

public enum PresenceMotion
{
    Invisible,
    SmallMove,
    SmallCircle
}

public enum LogLevel
{
    Error,
    Info,
    Debug
}

public enum AppLanguage
{
    German,
    English
}

public enum EngineStatus
{
    Stopped,
    Running,
    Paused
}

public enum PauseReason
{
    None,
    Schedule,
    Fullscreen,
    WaitingForIdle,
    UserInput,
    StartDelay
}

[Flags]
public enum WeekdayMask
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    All = Weekdays | Saturday | Sunday
}

public enum PresenceKey
{
    None,
    F15,
    Shift,
    Control
}

public enum PresenceWarning
{
    None,
    IdleNotReset,
    PolicyLock,
    SessionLocked
}
