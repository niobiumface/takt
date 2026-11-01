using Takt.Models;

namespace Takt.Helpers;

public sealed class HoldSession
{
    private ClickButton? _pressed;

    public bool IsHolding => _pressed is not null;

    public void Press(ClickButton button) => _pressed = button;

    public ClickButton? Release()
    {
        var pressed = _pressed;
        _pressed = null;
        return pressed;
    }
}
