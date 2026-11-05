namespace Takt.Helpers;

[Flags]
public enum EngineFlags
{
    None = 0,
    Jiggler = 1,
    Clicker = 2,
    Typer = 4,
    Presence = 8,
    Macro = 16
}

public enum PauseAction
{
    Idle,
    Suspended,
    Resumed
}

public static class PauseResume
{
    public static EngineFlags Capture(bool jiggler, bool clicker, bool typer, bool presence, bool macroPlaying)
    {
        var flags = EngineFlags.None;
        if (jiggler)
            flags |= EngineFlags.Jiggler;
        if (clicker)
            flags |= EngineFlags.Clicker;
        if (typer)
            flags |= EngineFlags.Typer;
        if (presence)
            flags |= EngineFlags.Presence;
        if (macroPlaying)
            flags |= EngineFlags.Macro;
        return flags;
    }

    public static PauseAction Decide(EngineFlags running, EngineFlags held) =>
        held != EngineFlags.None ? PauseAction.Resumed :
        running != EngineFlags.None ? PauseAction.Suspended :
        PauseAction.Idle;
}
