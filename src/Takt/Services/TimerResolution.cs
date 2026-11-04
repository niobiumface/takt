using System.Runtime.InteropServices;

namespace Takt.Services;

internal static class TimerResolution
{
    private static int _held;

    public static void Acquire()
    {
        if (Interlocked.Increment(ref _held) == 1)
            timeBeginPeriod(1);
    }

    public static void Release()
    {
        var left = Interlocked.Decrement(ref _held);
        if (left == 0)
            timeEndPeriod(1);
        else if (left < 0)
            Interlocked.Exchange(ref _held, 0);
    }

    public static void ForceClear()
    {
        if (Interlocked.Exchange(ref _held, 0) > 0)
            timeEndPeriod(1);
    }

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint period);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint period);
}
