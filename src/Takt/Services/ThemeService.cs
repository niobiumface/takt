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

        var themeUri = settings.Appearance.Theme == ThemeMode.Light
            ? "Themes/Light.xaml"
            : "Themes/Dark.xaml";

        ReplaceDictionary("takt-theme", new ResourceDictionary { Source = new Uri(themeUri, UriKind.Relative) });
        ApplyAccent(settings.Appearance.AccentHex);
        app.MainWindow?.SetValue(Window.TopmostProperty, settings.Appearance.AlwaysOnTop);
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
