namespace Takt.Native;

internal static class AwakeCoordinator
{
    private static bool _jiggler;
    private static bool _jigglerSleep;
    private static bool _jigglerDisplay;
    private static bool _presence;
    private static bool _presenceSleep;
    private static bool _presenceDisplay;

    public static void SetJiggler(bool active, bool preventSleep, bool preventDisplay)
    {
        _jiggler = active;
        _jigglerSleep = preventSleep;
        _jigglerDisplay = preventDisplay;
        Apply();
    }

    public static void SetPresence(bool active, bool preventSleep, bool preventDisplay)
    {
        _presence = active;
        _presenceSleep = preventSleep;
        _presenceDisplay = preventDisplay;
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
        if (_presence && (_presenceSleep || _presenceDisplay))
        {
            ExecutionState.Apply(_presenceSleep, _presenceDisplay);
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
