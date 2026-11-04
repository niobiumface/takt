namespace Takt.Helpers;

public enum CheckMark
{
    Pass,
    Warn,
    Fail
}

public readonly record struct CheckResult(string Id, CheckMark Mark, string Explanation);

public static class DiagnosisReport
{
    public static CheckResult Idle(bool? registered) => registered switch
    {
        true => new("idle", CheckMark.Pass, "The test nudge reset the system idle timer."),
        false => new("idle", CheckMark.Fail, "The test nudge did not reset the system idle timer."),
        _ => new("idle", CheckMark.Warn, "Idle time could not be read.")
    };

    public static CheckResult Hotkeys(int duplicates, int rejected) =>
        duplicates == 0 && rejected == 0
            ? new("hotkeys", CheckMark.Pass, "No duplicate or rejected shortcuts.")
            : new("hotkeys", CheckMark.Fail, $"{duplicates} duplicate, {rejected} rejected by Windows.");

    public static CheckResult LockPolicy(int? seconds) => seconds is > 0
        ? new("lock", CheckMark.Warn, $"A policy can still lock the session after {seconds} seconds. Takt does not bypass it.")
        : new("lock", CheckMark.Pass, "No group-policy inactivity lock was found.");

    public static CheckResult Screensaver(bool policyForcesScreensaver) => policyForcesScreensaver
        ? new("saver", CheckMark.Warn, "Policy enables a screensaver. Display-required can ask it to wait, but policy still wins.")
        : new("saver", CheckMark.Pass, "No forced screensaver policy was found.");
}
