using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Rules;

namespace MiniPdm.Domain.Entities;

public sealed class ObjectVersion
{
    public long Id { get; private set; }

    public int VersionNumber { get; }

    public VersionState State { get; private set; }

    public string? Material { get; }

    public decimal? MassKg { get; }

    public DateTimeOffset CreatedAt { get; }

    public ObjectVersion(
        int versionNumber,
        VersionState state,
        string? material,
        decimal? massKg,
        DateTimeOffset createdAt)
    {
        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "Version number must be greater than zero.");
        }

        if (massKg < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(massKg),
                "Mass cannot be negative.");
        }

        VersionNumber = versionNumber;
        State = state;
        Material = material;
        MassKg = massKg;
        CreatedAt = createdAt;
    }

    public void ChangeState(VersionState newState)
    {
        if (!VersionStateTransitionRules.CanTransition(State, newState))
        {
            throw new InvalidOperationException(
                $"Transition from {State} to {newState} is not allowed.");
        }

        State = newState;
    }
}