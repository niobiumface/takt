using System.Runtime.InteropServices;

namespace Takt.Services;

internal static class CompletionFeedback
{
    public static void PlaySound(string? alias = null)
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(alias) ? "SystemAsterisk" : alias;
            PlaySound(name, nint.Zero, 0x00010000 | 0x0001);
        }
        catch (DllNotFoundException)
        {
            // no sound device mapping on this host
        }
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool PlaySound(string name, nint module, uint flags);
}
