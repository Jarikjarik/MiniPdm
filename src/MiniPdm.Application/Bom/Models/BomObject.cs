using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Bom.Models;

public sealed record BomObject(
    long ObjectId,
    ObjectType ObjectType,
    string? Designation,
    string Name,
    long? VersionId,
    int? VersionNumber,
    VersionState? State,
    string? Material,
    decimal? MassKg)
{
    public bool HasCurrentVersion =>
        VersionId is not null;
}