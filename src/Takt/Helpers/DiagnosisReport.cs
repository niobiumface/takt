using System.Globalization;

namespace Takt.Helpers;

public enum CheckMark
{
    Pass,
    Warn,
    Fail
}

public readonly record struct CheckResult(string Id, CheckMark Mark, string TextKey, string Fallback, object[]? Args = null)
{
    public string Explanation => Args is { Length: > 0 }
        ? string.Format(CultureInfo.InvariantCulture, Fallback, Args)
        : Fallback;
}

public static class DiagnosisReport
{
    public static CheckResult Idle(bool? registered) => registered switch
    {
        true => new("idle", CheckMark.Pass, "Diagnosis.IdlePass", "The test nudge reset the system idle timer."),
        false => new("idle", CheckMark.Fail, "Diagnosis.IdleFail", "The test nudge did not reset the system idle timer."),
        _ => new("idle", CheckMark.Warn, "Diagnosis.IdleUnknown", "Idle time could not be read.")
    };

    public static CheckResult Hotkeys(int duplicates, int rejected) =>
        duplicates == 0 && rejected == 0
            ? new("hotkeys", CheckMark.Pass, "Diagnosis.HotkeyPass", "No duplicate or rejected shortcuts.")
            : new("hotkeys", CheckMark.Fail, "Diagnosis.HotkeyFail", "{0} duplicate, {1} rejected by Windows.", [duplicates, rejected]);

    public static CheckResult LockPolicy(int? seconds) => seconds is > 0
        ? new("lock", CheckMark.Warn, "Diagnosis.LockWarn", "A policy can still lock the session after {0} seconds. Takt does not bypass it.", [seconds])
        : new("lock", CheckMark.Pass, "Diagnosis.LockPass", "No group-policy inactivity lock was found.");

    public static CheckResult Screensaver(bool policyForcesScreensaver) => policyForcesScreensaver
        ? new("saver", CheckMark.Warn, "Diagnosis.SaverWarn", "Policy enables a screensaver. Display-required can ask it to wait, but policy still wins.")
        : new("saver", CheckMark.Pass, "Diagnosis.SaverPass", "No forced screensaver policy was found.");
}
