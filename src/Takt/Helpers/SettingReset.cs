using Takt.Models;

namespace Takt.Helpers;

public static class SettingReset
{
    public static bool Apply(AppSettings settings, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;
        var defaults = new AppSettings();
        switch (key)
        {
            case "theme": settings.Appearance.Theme = defaults.Appearance.Theme; break;
            case "accent": settings.Appearance.AccentHex = defaults.Appearance.AccentHex; break;
            case "language": settings.Appearance.Language = defaults.Appearance.Language; break;
            case "top": settings.Appearance.AlwaysOnTop = defaults.Appearance.AlwaysOnTop; break;
            case "autostart": settings.General.StartWithWindows = defaults.General.StartWithWindows; break;
            case "min": settings.General.StartMinimized = defaults.General.StartMinimized; break;
            case "close": settings.General.CloseBehavior = defaults.General.CloseBehavior; break;
            case "interval": settings.Clicker.IntervalMs = defaults.Clicker.IntervalMs; break;
            case "jitter": settings.Clicker.JitterPercent = defaults.Clicker.JitterPercent; break;
            case "hold": settings.Clicker.HoldDurationMs = defaults.Clicker.HoldDurationMs; break;
            case "precise": settings.Clicker.PreciseTiming = defaults.Clicker.PreciseTiming; break;
            case "button": settings.Clicker.Button = defaults.Clicker.Button; break;
            case "action": settings.Clicker.Action = defaults.Clicker.Action; break;
            case "repeat": settings.Clicker.RepeatMode = defaults.Clicker.RepeatMode; break;
            case "count": settings.Clicker.RepeatCount = defaults.Clicker.RepeatCount; break;
            case "delay": settings.Clicker.StartDelayMs = defaults.Clicker.StartDelayMs; break;
            case "burst": settings.Clicker.BurstSize = defaults.Clicker.BurstSize; break;
            case "burst-pause": settings.Clicker.BurstPauseMs = defaults.Clicker.BurstPauseMs; break;
            case "duration": settings.Clicker.DurationSeconds = defaults.Clicker.DurationSeconds; break;
            case "fixed-x": settings.Clicker.FixedX = defaults.Clicker.FixedX; break;
            case "fixed-y": settings.Clicker.FixedY = defaults.Clicker.FixedY; break;
            case "position": settings.Clicker.PositionMode = defaults.Clicker.PositionMode; break;
            case "failsafe": settings.General.FailsafeEnabled = defaults.General.FailsafeEnabled; break;
            case "corner": settings.General.FailsafeCorner = defaults.General.FailsafeCorner; break;
            case "corner-size": settings.General.FailsafeSize = defaults.General.FailsafeSize; break;
            case "jiggler-mode": settings.Jiggler.Mode = defaults.Jiggler.Mode; break;
            case "jiggler-interval": settings.Jiggler.IntervalMs = defaults.Jiggler.IntervalMs; break;
            case "jiggler-jitter": settings.Jiggler.JitterMs = defaults.Jiggler.JitterMs; break;
            case "amplitude": settings.Jiggler.Amplitude = defaults.Jiggler.Amplitude; break;
            case "radius": settings.Jiggler.RadiusPx = defaults.Jiggler.RadiusPx; break;
            case "fullscreen": settings.Jiggler.PauseWhenFullscreen = defaults.Jiggler.PauseWhenFullscreen; break;
            case "fixed-center": settings.Jiggler.UseFixedCenter = defaults.Jiggler.UseFixedCenter; break;
            case "jiggler-sleep": settings.Jiggler.PreventSleep = defaults.Jiggler.PreventSleep; break;
            case "jiggler-display": settings.Jiggler.PreventDisplayOff = defaults.Jiggler.PreventDisplayOff; break;
            case "jiggler-resume": settings.Jiggler.ResumeIdleSeconds = defaults.Jiggler.ResumeIdleSeconds; break;
            case "presence-motion": settings.Presence.Motion = defaults.Presence.Motion; break;
            case "presence-interval": settings.Presence.IntervalMs = defaults.Presence.IntervalMs; break;
            case "presence-jitter": settings.Presence.JitterMs = defaults.Presence.JitterMs; break;
            case "presence-fallback": settings.Presence.UseFallbackKey = defaults.Presence.UseFallbackKey; break;
            case "presence-key": settings.Presence.FallbackKey = defaults.Presence.FallbackKey; break;
            case "presence-saver": settings.Presence.PreventScreensaver = defaults.Presence.PreventScreensaver; break;
            case "presence-display": settings.Presence.PreventDisplayOff = defaults.Presence.PreventDisplayOff; break;
            case "presence-sleep": settings.Presence.PreventSleep = defaults.Presence.PreventSleep; break;
            case "presence-pause": settings.Presence.PauseOnUserInput = defaults.Presence.PauseOnUserInput; break;
            case "presence-resume": settings.Presence.ResumeIdleSeconds = defaults.Presence.ResumeIdleSeconds; break;
            case "work": settings.Work.Enabled = defaults.Work.Enabled; break;
            case "outside": settings.Work.Outside = defaults.Work.Outside; break;
            case "cpm": settings.Typer.CharactersPerMinute = defaults.Typer.CharactersPerMinute; break;
            case "typer-jitter": settings.Typer.JitterPercent = defaults.Typer.JitterPercent; break;
            case "punct": settings.Typer.PunctuationPauseMs = defaults.Typer.PunctuationPauseMs; break;
            case "typos": settings.Typer.SimulateTypos = defaults.Typer.SimulateTypos; break;
            case "typo": settings.Typer.TypoPercent = defaults.Typer.TypoPercent; break;
            case "typer-delay": settings.Typer.StartDelayMs = defaults.Typer.StartDelayMs; break;
            case "typer-count": settings.Typer.RepeatCount = defaults.Typer.RepeatCount; break;
            case "typer-text": settings.Typer.Text = defaults.Typer.Text; break;
            case "date-format": settings.Typer.DateFormat = defaults.Typer.DateFormat; break;
            case "time-format": settings.Typer.TimeFormat = defaults.Typer.TimeFormat; break;
            case "datetime-format": settings.Typer.DateTimeFormat = defaults.Typer.DateTimeFormat; break;
            case "sound": settings.General.PlaySoundOnFinish = defaults.General.PlaySoundOnFinish; break;
            case "sound-name": settings.General.SoundName = defaults.General.SoundName; break;
            case "notify-finish": settings.General.NotifyOnFinish = defaults.General.NotifyOnFinish; break;
            case "notify-failsafe": settings.General.NotifyOnFailsafe = defaults.General.NotifyOnFailsafe; break;
            case "notify-presence": settings.General.NotifyOnPresence = defaults.General.NotifyOnPresence; break;
            case "notify-hotkey": settings.General.NotifyOnHotkey = defaults.General.NotifyOnHotkey; break;
            case "log": settings.General.LogLevel = defaults.General.LogLevel; break;
            case "updates": settings.General.CheckForUpdates = defaults.General.CheckForUpdates; break;
            case "loop": settings.Macro.LoopCount = defaults.Macro.LoopCount; break;
            case "macro-speed": settings.Macro.Speed = defaults.Macro.Speed; break;
            case "stop-minutes": settings.General.StopAfterMinutes = defaults.General.StopAfterMinutes; break;
            case "stop-time": settings.General.StopAtTime = defaults.General.StopAtTime; break;
            case "auto-profile": settings.General.AutoProfile = defaults.General.AutoProfile; break;
            case "debounce": settings.General.ProfileDebounceMs = defaults.General.ProfileDebounceMs; break;
            case "stats-opt": settings.General.StatsOptOut = defaults.General.StatsOptOut; break;
            default: return false;
        }

        return true;
    }
}
