using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Takt.Helpers;
using Takt.Models;

namespace Takt.Converters;

public sealed class EngineStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value is EngineStatus s ? s : EngineStatus.Stopped;
        var key = status switch
        {
            EngineStatus.Running => "AccentBrush",
            EngineStatus.Paused => "WarningBrush",
            _ => "MutedBrush"
        };
        return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class EngineStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value is EngineStatus s ? s : EngineStatus.Stopped;
        var key = status switch
        {
            EngineStatus.Running => "Status.Running",
            EngineStatus.Paused => "Status.Paused",
            _ => "Status.Stopped"
        };
        return Application.Current?.TryFindResource(key) as string ?? status.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class MillisecondsToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int ms)
            return Math.Round(ms / 1000d, 2);
        return 0d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double seconds)
            return (int)Math.Round(Math.Clamp(seconds, 0.05, 3600) * 1000);
        if (double.TryParse(System.Convert.ToString(value, culture), NumberStyles.Float, culture, out var parsed))
            return (int)Math.Round(Math.Clamp(parsed, 0.05, 3600) * 1000);
        return 5000;
    }
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is false;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is false;
}

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = Takt.Services.ThemeService.Parse(value as string ?? "");
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class EnumMatchVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string text)
            return Visibility.Collapsed;
        var name = value?.ToString() ?? "";
        foreach (var option in text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(name, option, StringComparison.Ordinal))
                return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class HexEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return false;
        return string.Equals(values[0]?.ToString(), values[1]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ClockValidConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        FieldFormat.IsClock(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
