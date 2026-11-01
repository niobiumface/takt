namespace Takt.Helpers;

public static class IdleVerification
{
    public const int ResetWithinMs = 2_000;

    public static bool InputRegistered(TimeSpan idleAfterInput) =>
        idleAfterInput.TotalMilliseconds <= ResetWithinMs;
}
