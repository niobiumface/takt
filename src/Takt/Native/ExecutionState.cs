namespace Takt.Native;

internal static class ExecutionState
{
    public static void Apply(bool preventSleep, bool preventDisplay)
    {
        uint flags = Win32.EsContinuous;
        if (preventSleep)
            flags |= Win32.EsSystemRequired;
        if (preventDisplay)
            flags |= Win32.EsDisplayRequired;
        Win32.SetThreadExecutionState(flags);
    }

    public static void Clear() => Win32.SetThreadExecutionState(Win32.EsContinuous);
}
