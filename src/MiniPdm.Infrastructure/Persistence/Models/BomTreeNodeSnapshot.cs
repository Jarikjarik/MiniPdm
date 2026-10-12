using MiniPdm.Domain.Enums;

namespace MiniPdm.Infrastructure.Persistence.Models;

public sealed class BomTreeNodeSnapshot
{
    public long ParentObjectId { get; init; }

    public long ParentVersionId { get; init; }

    public long ObjectId { get; init; }

    public long VersionId { get; init; }

    public ObjectType ObjectType { get; init; }

    public string? Designation { get; init; }

    public string Name { get; init; } = string.Empty;

    public VersionState State { get; init; }

    public string? Material { get; init; }

    public decimal? MassKg { get; init; }

    public int Quantity { get; init; }

    public long TotalQuantity { get; init; }

    public int Depth { get; init; }

    public bool IsCycle { get; init; }
}