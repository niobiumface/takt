using Takt.Models;

namespace Takt.Helpers;

public static class ProfileApplicator
{
    public static ActivityProfile Capture(AppSettings settings, string name)
    {
        var profile = new ActivityProfile { Name = string.IsNullOrWhiteSpace(name) ? "Standard" : name.Trim() };
        CopyJiggler(settings.Jiggler, profile.Jiggler);
        CopyClicker(settings.Clicker, profile.Clicker);
        CopyHotkeys(settings.Hotkeys, profile.Hotkeys);
        CopyMacro(settings.Macro, profile.Macro);
        CopyTyper(settings.Typer, profile.Typer);
        CopyPresence(settings.Presence, profile.Presence);
        CopyGeneral(settings.General, profile.General);
        CopyAppearance(settings.Appearance, profile.Appearance);
        CopyWork(settings.Work, profile.Work);
        return profile;
    }

    public static void Overlay(AppSettings from, AppSettings to)
    {
        CopyJiggler(from.Jiggler, to.Jiggler);
        CopyClicker(from.Clicker, to.Clicker);
        CopyHotkeys(from.Hotkeys, to.Hotkeys);
        CopyMacro(from.Macro, to.Macro);
        CopyTyper(from.Typer, to.Typer);
        CopyPresence(from.Presence, to.Presence);
        CopyGeneral(from.General, to.General);
        CopyAppearance(from.Appearance, to.Appearance);
        CopyWork(from.Work, to.Work);
    }

    public static void Apply(ActivityProfile profile, AppSettings settings)
    {
        CopyJiggler(profile.Jiggler, settings.Jiggler);
        CopyClicker(profile.Clicker, settings.Clicker);
        CopyHotkeys(profile.Hotkeys, settings.Hotkeys);
        CopyMacro(profile.Macro, settings.Macro);
        CopyTyper(profile.Typer, settings.Typer);
        CopyPresence(profile.Presence, settings.Presence);
        CopyGeneral(profile.General, settings.General);
        CopyAppearance(profile.Appearance, settings.Appearance);
        CopyWork(profile.Work, settings.Work);
    }

    public static void CopyPresence(PresenceSettings from, PresenceSettings to)
    {
        if (from is null || to is null)
            return;
        to.Enabled = from.Enabled;
        to.IntervalMs = from.IntervalMs;
        to.JitterMs = from.JitterMs;
        to.UseFallbackKey = from.UseFallbackKey;
        to.FallbackKey = from.FallbackKey;
        to.UseWorkHours = from.UseWorkHours;
        to.Monday = from.Monday;
        to.Tuesday = from.Tuesday;
        to.Wednesday = from.Wednesday;
        to.Thursday = from.Thursday;
        to.Friday = from.Friday;
        to.Saturday = from.Saturday;
        to.Sunday = from.Sunday;
        to.ScheduleStart = from.ScheduleStart;
        to.ScheduleEnd = from.ScheduleEnd;
        to.Motion = from.Motion;
        to.PreventScreensaver = from.PreventScreensaver;
        to.PreventDisplayOff = from.PreventDisplayOff;
        to.PreventSleep = from.PreventSleep;
        to.PauseOnUserInput = from.PauseOnUserInput;
        to.ResumeIdleSeconds = from.ResumeIdleSeconds;
    }

    public static void CopyTyper(TyperSettings from, TyperSettings to)
    {
        if (from is null || to is null)
            return;
        to.Text = from.Text;
        to.CharactersPerMinute = from.CharactersPerMinute;
        to.JitterPercent = from.JitterPercent;
        to.PunctuationPauseMs = from.PunctuationPauseMs;
        to.SimulateTypos = from.SimulateTypos;
        to.TypoPercent = from.TypoPercent;
        to.StartDelayMs = from.StartDelayMs;
        to.RepeatMode = from.RepeatMode;
        to.RepeatCount = from.RepeatCount;
        to.DurationSeconds = from.DurationSeconds;
        to.DateFormat = string.IsNullOrWhiteSpace(from.DateFormat) ? "yyyy-MM-dd" : from.DateFormat;
        to.TimeFormat = string.IsNullOrWhiteSpace(from.TimeFormat) ? "HH:mm" : from.TimeFormat;
        to.DateTimeFormat = string.IsNullOrWhiteSpace(from.DateTimeFormat) ? "yyyy-MM-dd HH:mm" : from.DateTimeFormat;
        to.Snippets ??= new();
        to.Snippets.Clear();
        if (from.Snippets is null)
            return;
        foreach (var snippet in from.Snippets)
            to.Snippets.Add(new TextSnippet { Name = snippet.Name, Text = snippet.Text });
    }

    public static void CopyJiggler(JigglerSettings from, JigglerSettings to)
    {
        to.Mode = from.Mode;
        to.IntervalMs = from.IntervalMs;
        to.JitterMs = from.JitterMs;
        to.Amplitude = from.Amplitude;
        to.RadiusPx = from.RadiusPx;
        to.UseFixedCenter = from.UseFixedCenter;
        to.CenterX = from.CenterX;
        to.CenterY = from.CenterY;
        to.PreventSleep = from.PreventSleep;
        to.PreventDisplayOff = from.PreventDisplayOff;
        to.WaitForIdle = from.WaitForIdle;
        to.IdleThresholdSeconds = from.IdleThresholdSeconds;
        to.PauseOnUserInput = from.PauseOnUserInput;
        to.ResumeIdleSeconds = from.ResumeIdleSeconds;
        to.PauseWhenFullscreen = from.PauseWhenFullscreen;
        to.UseSchedule = from.UseSchedule;
        to.ScheduleStart = from.ScheduleStart;
        to.ScheduleEnd = from.ScheduleEnd;
    }

    public static void CopyClicker(ClickerSettings from, ClickerSettings to)
    {
        to.Button = from.Button;
        to.Action = from.Action;
        to.IntervalMs = from.IntervalMs;
        to.JitterMs = from.JitterMs;
        to.JitterPercent = from.JitterPercent;
        to.HoldDurationMs = from.HoldDurationMs;
        to.PreciseTiming = from.PreciseTiming;
        to.BurstSize = from.BurstSize;
        to.BurstPauseMs = from.BurstPauseMs;
        to.RepeatMode = from.RepeatMode;
        to.RepeatCount = from.RepeatCount;
        to.DurationSeconds = from.DurationSeconds;
        to.StartDelayMs = from.StartDelayMs;
        to.PositionMode = from.PositionMode;
        to.FixedX = from.FixedX;
        to.FixedY = from.FixedY;
        to.Points ??= new();
        to.Points.Clear();
        if (from.Points is null)
            return;
        foreach (var point in from.Points)
            to.Points.Add(new ClickPoint
            {
                X = point.X,
                Y = point.Y,
                DelayMs = point.DelayMs,
                CustomButton = point.CustomButton,
                Button = point.Button
            });
    }

    public static void CopyHotkeys(HotkeySettings from, HotkeySettings to)
    {
        CopyBinding(from.ToggleJiggler, to.ToggleJiggler);
        CopyBinding(from.ToggleClicker, to.ToggleClicker);
        CopyBinding(from.ToggleBoth, to.ToggleBoth);
        CopyBinding(from.Panic, to.Panic);
        CopyBinding(from.PickPosition, to.PickPosition);
        CopyBinding(from.ToggleMacro, to.ToggleMacro);
        CopyBinding(from.ToggleTyper, to.ToggleTyper);
        CopyBinding(from.TogglePresence, to.TogglePresence);
        CopyBinding(from.RecordMacro, to.RecordMacro);
        CopyBinding(from.ShowWindow, to.ShowWindow);
        CopyBinding(from.ToggleCompact, to.ToggleCompact);
    }

    public static void CopyMacro(MacroSettings from, MacroSettings to)
    {
        to.LoopCount = Math.Max(1, from.LoopCount);
        to.Events ??= new();
        to.Events.Clear();
        if (from.Events is null)
            return;
        foreach (var item in from.Events)
        {
            to.Events.Add(new MacroEvent
            {
                DelayMs = item.DelayMs,
                X = item.X,
                Y = item.Y,
                Kind = item.Kind,
                Button = item.Button,
                WheelDelta = item.WheelDelta
            });
        }
    }

    public static void CopyGeneral(GeneralSettings from, GeneralSettings to)
    {
        if (from is null || to is null)
            return;
        to.MinimizeToTray = from.MinimizeToTray;
        to.StartMinimized = from.StartMinimized;
        to.StartWithWindows = from.StartWithWindows;
        to.FailsafeEnabled = from.FailsafeEnabled;
        to.FailsafeCorner = from.FailsafeCorner;
        to.FailsafeSize = from.FailsafeSize;
        to.CheckForUpdates = from.CheckForUpdates;
        to.PlaySoundOnFinish = from.PlaySoundOnFinish;
        to.SoundName = from.SoundName;
        to.NotifyOnFinish = from.NotifyOnFinish;
        to.NotifyOnFailsafe = from.NotifyOnFailsafe;
        to.NotifyOnPresence = from.NotifyOnPresence;
        to.NotifyOnHotkey = from.NotifyOnHotkey;
        to.LogLevel = from.LogLevel;
        to.CloseBehavior = from.CloseBehavior;
        to.FirstRunDismissed = from.FirstRunDismissed;
        to.WizardCompleted = from.WizardCompleted;
        to.StatsOptOut = from.StatsOptOut;
        to.StopAfterMinutes = from.StopAfterMinutes;
        to.StopAtTime = from.StopAtTime ?? "";
        to.CompactLeft = from.CompactLeft;
        to.CompactTop = from.CompactTop;
        to.AutoProfile = from.AutoProfile;
        to.ProfileDebounceMs = from.ProfileDebounceMs;
        to.AppRules ??= new();
        to.AppRules.Clear();
        if (from.AppRules is null)
            return;
        foreach (var rule in from.AppRules)
        {
            to.AppRules.Add(new AppRule
            {
                Enabled = rule.Enabled,
                ProcessName = rule.ProcessName,
                ProfileName = rule.ProfileName
            });
        }
    }

    public static void CopyAppearance(AppearanceSettings from, AppearanceSettings to)
    {
        if (from is null || to is null)
            return;
        to.Theme = from.Theme;
        to.AccentHex = from.AccentHex;
        to.Language = from.Language;
        to.AlwaysOnTop = from.AlwaysOnTop;
    }

    public static void CopyWork(WorkScheduleSettings from, WorkScheduleSettings to)
    {
        if (from is null || to is null)
            return;
        to.Enabled = from.Enabled;
        to.Outside = from.Outside;
        to.Holidays ??= new();
        to.Holidays.Clear();
        if (from.Holidays is not null)
        {
            foreach (var holiday in from.Holidays)
                to.Holidays.Add(holiday);
        }

        foreach (var day in from.Days ?? [])
        {
            var target = to.Days.FirstOrDefault(item => item.Day == day.Day);
            if (target is null)
            {
                target = new DayWindows { Day = day.Day };
                to.Days.Add(target);
            }

            target.Windows ??= new();
            target.Windows.Clear();
            if (day.Windows is null)
                continue;
            foreach (var window in day.Windows)
                target.Windows.Add(new TimeSpanWindow { Start = window.Start, End = window.End });
        }
    }

    private static void CopyBinding(HotkeyBinding? from, HotkeyBinding to)
    {
        if (from is null)
            return;
        to.Modifiers = from.Modifiers;
        to.VirtualKey = from.VirtualKey;
    }
}
