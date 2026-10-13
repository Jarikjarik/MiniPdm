using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Bom.Models;

public sealed record BomTreeNode(
    long ParentObjectId,
    long ParentVersionId,
    long ObjectId,
    long? VersionId,
    ObjectType ObjectType,
    string? Designation,
    string Name,
    VersionState? State,
    string? Material,
    decimal? MassKg,
    int Quantity,
    long TotalQuantity,
    int Depth,
    bool IsCycle)
{
    public bool HasCurrentVersion =>
        VersionId is not null;
}