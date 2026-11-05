using System.Text;
using Takt.Helpers;

namespace Takt.Native;

internal static class WindowAnchorQuery
{
    public static AnchorSnapshot? TryForeground()
    {
        try
        {
            return Read(Win32.GetForegroundWindow(), requireVisible: false);
        }
        catch
        {
            return null;
        }
    }

    public static AnchorSnapshot? TryFind(string? process, string? title)
    {
        if (string.IsNullOrWhiteSpace(process))
            return null;
        try
        {
            AnchorSnapshot? titled = null;
            AnchorSnapshot? any = null;
            Win32.EnumWindows((hwnd, _) =>
            {
                var snap = Read(hwnd, requireVisible: true);
                if (snap is null || !string.Equals(snap.Value.Process, process, StringComparison.OrdinalIgnoreCase))
                    return true;
                any ??= snap;
                if (!string.IsNullOrWhiteSpace(title) && string.Equals(snap.Value.Title, title, StringComparison.Ordinal))
                {
                    titled = snap;
                    return false;
                }

                return true;
            }, nint.Zero);
            return titled ?? any;
        }
        catch
        {
            return null;
        }
    }

    private static AnchorSnapshot? Read(nint hwnd, bool requireVisible)
    {
        if (hwnd == nint.Zero || hwnd == Win32.GetDesktopWindow() || hwnd == Win32.GetShellWindow())
            return null;
        if (requireVisible && !Win32.IsWindowVisible(hwnd))
            return null;
        if (!Win32.GetWindowRect(hwnd, out var rect) || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            return null;
        var process = AnchorNames.Process(ForegroundProcess.TryGetPath(hwnd));
        if (string.IsNullOrEmpty(process))
            return null;
        var title = new StringBuilder(512);
        Win32.GetWindowText(hwnd, title, title.Capacity);
        return new AnchorSnapshot(rect.Left, rect.Top, process, title.ToString());
    }
}
