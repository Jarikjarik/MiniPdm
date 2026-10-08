using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Rules;

namespace MiniPdm.Domain.Tests.Rules;

public class VersionStateTransitionRulesTests
{
    [Theory]
    [InlineData(VersionState.InWork, VersionState.Approved)]
    [InlineData(VersionState.InWork, VersionState.Cancelled)]
    [InlineData(VersionState.Approved, VersionState.Cancelled)]
    public void CanTransition_WithAllowedTransition_ReturnsTrue(
        VersionState from,
        VersionState to)
    {
        var result = VersionStateTransitionRules.CanTransition(from, to);

        Assert.True(result);
    }

    [Theory]
    [InlineData(VersionState.InWork, VersionState.InWork)]
    [InlineData(VersionState.Approved, VersionState.InWork)]
    [InlineData(VersionState.Approved, VersionState.Approved)]
    [InlineData(VersionState.Cancelled, VersionState.InWork)]
    [InlineData(VersionState.Cancelled, VersionState.Approved)]
    [InlineData(VersionState.Cancelled, VersionState.Cancelled)]
    public void CanTransition_WithForbiddenTransition_ReturnsFalse(
        VersionState from,
        VersionState to)
    {
        var result = VersionStateTransitionRules.CanTransition(from, to);

        Assert.False(result);
    }
}