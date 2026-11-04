using Takt.Models;

namespace Takt.Helpers;

public readonly record struct SequenceStep(ClickButton Button, bool UsePointDelay, int PointDelayMs);

public static class SequencePlan
{
    public static SequenceStep For(ClickPoint? point, ClickButton fallback)
    {
        if (point is null)
            return new SequenceStep(fallback, false, 0);
        var button = point.CustomButton ? point.Button : fallback;
        return new SequenceStep(button, point.DelayMs > 0, Math.Max(1, point.DelayMs));
    }
}
