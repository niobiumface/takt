using System.Windows;
using System.Windows.Media;
using Takt.Models;

namespace Takt.Services;

internal sealed class ThemeService
{
    public static readonly string[] AccentPresets =
    [
        "#5B8DEF",
        "#3BA89A",
        "#8B7CF6",
        "#E0A24B",
        "#D96B8A",
        "#6BBF6B"
    ];

    public void Apply(AppSettings settings)
    {
        var app = Application.Current;
        if (app is null)
            return;

        var dark = IsDark(settings.Appearance.Theme);
        var themeUri = dark ? "Themes/Dark.xaml" : "Themes/Light.xaml";

        ReplaceDictionary("takt-theme", new ResourceDictionary { Source = new Uri(themeUri, UriKind.Relative) });
        ApplyAccent(settings.Appearance.AccentHex);
        app.MainWindow?.SetValue(Window.TopmostProperty, settings.Appearance.AlwaysOnTop);
        TitleBarTheme.ApplyAll(dark);
    }

    public static bool IsDark(ThemeMode mode)
    {
        if (mode == ThemeMode.Light)
            return false;
        if (mode == ThemeMode.Dark)
            return true;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not int light || light == 0;
        }
        catch
        {
            return true;
        }
    }

    public void ApplyAccent(string hex)
    {
        var app = Application.Current;
        if (app is null)
            return;

        var color = Parse(hex);
        app.Resources["AccentColor"] = color;
        app.Resources["AccentBrush"] = new SolidColorBrush(color);
        var hover = Color.FromRgb(
            (byte)Math.Min(255, color.R + 18),
            (byte)Math.Min(255, color.G + 18),
            (byte)Math.Min(255, color.B + 18));
        app.Resources["AccentHoverBrush"] = new SolidColorBrush(hover);
        var muted = Color.FromArgb(48, color.R, color.G, color.B);
        app.Resources["AccentMutedBrush"] = new SolidColorBrush(muted);
    }

    public static Color Parse(string hex)
    {
        try
        {
            var parsed = ColorConverter.ConvertFromString(hex);
            if (parsed is Color color)
                return color;
        }
        catch
        {
            // fallback
        }

        return Color.FromRgb(91, 141, 239);
    }

    private static void ReplaceDictionary(string key, ResourceDictionary dictionary)
    {
        var app = Application.Current;
        if (app is null)
            return;

        dictionary["TaktDictionaryKey"] = key;
        for (var i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            var existing = app.Resources.MergedDictionaries[i];
            if (Equals(existing["TaktDictionaryKey"], key))
                app.Resources.MergedDictionaries.RemoveAt(i);
        }

        app.Resources.MergedDictionaries.Insert(0, dictionary);
    }
}
