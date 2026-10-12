using MiniPdm.Domain.Enums;

namespace MiniPdm.Infrastructure.Persistence.Models;

public sealed class ObjectVersionSnapshot
{
    public long Id { get; init; }

    public long ObjectId { get; init; }

    public int VersionNumber { get; init; }

    public VersionState State { get; init; }

    public string? Material { get; init; }

    public decimal? MassKg { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}