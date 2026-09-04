using System.Windows;
using Takt.Models;

namespace Takt.Services;

internal sealed class LocalizationService
{
    public void Apply(AppLanguage language)
    {
        var app = Application.Current;
        if (app is null)
            return;

        var uri = language == AppLanguage.German
            ? "Resources/Strings.de.xaml"
            : "Resources/Strings.en.xaml";

        var dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
        dict["TaktDictionaryKey"] = "takt-strings";

        for (var i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (Equals(app.Resources.MergedDictionaries[i]["TaktDictionaryKey"], "takt-strings"))
                app.Resources.MergedDictionaries.RemoveAt(i);
        }

        app.Resources.MergedDictionaries.Add(dict);
    }

    public static string Text(string key, string fallback)
    {
        var value = Application.Current?.TryFindResource(key);
        return value as string ?? fallback;
    }
}
