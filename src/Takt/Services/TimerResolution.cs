using System.Runtime.InteropServices;

namespace Takt.Services;

internal static class TimerResolution
{
    public static void Acquire() => timeBeginPeriod(1);

    public static void Release() => timeEndPeriod(1);

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint period);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint period);
}
