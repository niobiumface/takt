using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal static class SafetyNet
{
    public static void Release(AppServices? services)
    {
        try
        {
            services?.Clicker.ReleaseHeldButton();
        }
        catch
        {
            // still release the other state
        }

        try
        {
            foreach (var button in new[]
                     {
                         ClickButton.Left, ClickButton.Right, ClickButton.Middle, ClickButton.X1, ClickButton.X2
                     })
                MouseInput.Up(button);
            KeyboardInput.ReleaseModifiers();
        }
        catch
        {
            // input release is best-effort during a crash
        }

        try
        {
            AwakeCoordinator.ClearAll();
        }
        catch
        {
            // execution state
        }

        try
        {
            TimerResolution.ForceClear();
        }
        catch
        {
            // timer resolution
        }
    }
}