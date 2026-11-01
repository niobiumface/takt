namespace Takt.Native;

internal static class AwakeCoordinator
{
    private static bool _jiggler;
    private static bool _jigglerSleep;
    private static bool _jigglerDisplay;
    private static bool _presence;

    public static void SetJiggler(bool active, bool preventSleep, bool preventDisplay)
    {
        _jiggler = active;
        _jigglerSleep = preventSleep;
        _jigglerDisplay = preventDisplay;
        Apply();
    }

    public static void SetPresence(bool active)
    {
        _presence = active;
        Apply();
    }

    public static void ClearAll()
    {
        _jiggler = false;
        _presence = false;
        ExecutionState.Clear();
    }

    private static void Apply()
    {
        if (_presence)
        {
            ExecutionState.Apply(preventSleep: true, preventDisplay: true);
            return;
        }

        if (_jiggler && (_jigglerSleep || _jigglerDisplay))
        {
            ExecutionState.Apply(_jigglerSleep, _jigglerDisplay);
            return;
        }

        ExecutionState.Clear();
    }
}
