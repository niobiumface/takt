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
    Light
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
