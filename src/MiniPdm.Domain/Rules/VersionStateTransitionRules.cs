using MiniPdm.Domain.Enums;

namespace MiniPdm.Domain.Rules;

public static class VersionStateTransitionRules
{
    public static bool CanTransition(VersionState from, VersionState to)
    {
        return (from, to) switch
        {
            (VersionState.InWork, VersionState.Approved) => true,
            (VersionState.InWork, VersionState.Cancelled) => true,
            (VersionState.Approved, VersionState.Cancelled) => true,
            _ => false
        };
    }
}