using Takt.Models;

namespace Takt.Helpers;

public static class SettingsNormalizer
{
    public static void ApplyAll(AppSettings settings)
    {
        ApplyJiggler(settings.Jiggler);
        ApplyClicker(settings.Clicker);
        settings.General.FailsafeSize = SettingsLimits.ClampFailsafe(settings.General.FailsafeSize);
    }

    public static void ApplyJiggler(JigglerSettings settings)
    {
        settings.Amplitude = SettingsLimits.ClampAmplitude(settings.Amplitude);
        settings.RadiusPx = SettingsLimits.ClampRadius(settings.RadiusPx);
        settings.IntervalMs = SettingsLimits.ClampInterval(settings.IntervalMs);
        settings.JitterMs = SettingsLimits.ClampJitter(settings.JitterMs);
        settings.IdleThresholdSeconds = SettingsLimits.ClampIdleSeconds(settings.IdleThresholdSeconds);
        settings.ScheduleStart = SettingsLimits.ClampSchedule(settings.ScheduleStart, "09:00");
        settings.ScheduleEnd = SettingsLimits.ClampSchedule(settings.ScheduleEnd, "18:00");
    }

    public static void ApplyClicker(ClickerSettings settings)
    {
        settings.IntervalMs = SettingsLimits.ClampInterval(settings.IntervalMs);
        settings.JitterMs = SettingsLimits.ClampJitter(settings.JitterMs);
        settings.BurstSize = SettingsLimits.ClampBurst(settings.BurstSize);
        settings.BurstPauseMs = SettingsLimits.ClampJitter(settings.BurstPauseMs);
        settings.RepeatCount = SettingsLimits.ClampRepeatCount(settings.RepeatCount);
        settings.DurationSeconds = SettingsLimits.ClampDuration(settings.DurationSeconds);
        settings.StartDelayMs = SettingsLimits.ClampStartDelay(settings.StartDelayMs);
    }

    public static void ApplyChanged(object? sender, string? propertyName)
    {
        switch (sender)
        {
            case JigglerSettings jiggler:
                ApplyJigglerProperty(jiggler, propertyName);
                break;
            case ClickerSettings clicker:
                ApplyClickerProperty(clicker, propertyName);
                break;
            case GeneralSettings general when propertyName == nameof(GeneralSettings.FailsafeSize):
                general.FailsafeSize = SettingsLimits.ClampFailsafe(general.FailsafeSize);
                break;
        }
    }

    private static void ApplyJigglerProperty(JigglerSettings settings, string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(JigglerSettings.Amplitude):
                settings.Amplitude = SettingsLimits.ClampAmplitude(settings.Amplitude);
                break;
            case nameof(JigglerSettings.RadiusPx):
                settings.RadiusPx = SettingsLimits.ClampRadius(settings.RadiusPx);
                break;
            case nameof(JigglerSettings.IntervalMs):
                settings.IntervalMs = SettingsLimits.ClampInterval(settings.IntervalMs);
                break;
            case nameof(JigglerSettings.JitterMs):
                settings.JitterMs = SettingsLimits.ClampJitter(settings.JitterMs);
                break;
            case nameof(JigglerSettings.IdleThresholdSeconds):
                settings.IdleThresholdSeconds = SettingsLimits.ClampIdleSeconds(settings.IdleThresholdSeconds);
                break;
            case nameof(JigglerSettings.ScheduleStart):
                settings.ScheduleStart = SettingsLimits.ClampSchedule(settings.ScheduleStart, "09:00");
                break;
            case nameof(JigglerSettings.ScheduleEnd):
                settings.ScheduleEnd = SettingsLimits.ClampSchedule(settings.ScheduleEnd, "18:00");
                break;
        }
    }

    private static void ApplyClickerProperty(ClickerSettings settings, string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(ClickerSettings.IntervalMs):
                settings.IntervalMs = SettingsLimits.ClampInterval(settings.IntervalMs);
                break;
            case nameof(ClickerSettings.JitterMs):
                settings.JitterMs = SettingsLimits.ClampJitter(settings.JitterMs);
                break;
            case nameof(ClickerSettings.BurstSize):
                settings.BurstSize = SettingsLimits.ClampBurst(settings.BurstSize);
                break;
            case nameof(ClickerSettings.BurstPauseMs):
                settings.BurstPauseMs = SettingsLimits.ClampJitter(settings.BurstPauseMs);
                break;
            case nameof(ClickerSettings.RepeatCount):
                settings.RepeatCount = SettingsLimits.ClampRepeatCount(settings.RepeatCount);
                break;
            case nameof(ClickerSettings.DurationSeconds):
                settings.DurationSeconds = SettingsLimits.ClampDuration(settings.DurationSeconds);
                break;
            case nameof(ClickerSettings.StartDelayMs):
                settings.StartDelayMs = SettingsLimits.ClampStartDelay(settings.StartDelayMs);
                break;
        }
    }
}
