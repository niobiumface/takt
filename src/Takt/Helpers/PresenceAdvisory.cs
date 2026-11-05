using Takt.Models;

namespace Takt.Helpers;

public static class PresenceAdvisory
{
    public static PresenceWarning Evaluate(int? policyLockSeconds, bool sessionLocked, bool idleNotReset)
    {
        if (sessionLocked)
            return PresenceWarning.SessionLocked;
        if (policyLockSeconds is > 0)
            return PresenceWarning.PolicyLock;
        if (idleNotReset)
            return PresenceWarning.IdleNotReset;
        return PresenceWarning.None;
    }
}
