using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Browse.Models;

public sealed record PdmObjectDetails(
    long ObjectId,
    ObjectType ObjectType,
    string? Designation,
    string Name,
    long? CurrentVersionId,
    IReadOnlyList<PdmVersionListItem> Versions)
{
    public PdmVersionListItem? CurrentVersion =>
        CurrentVersionId is null
            ? null
            : Versions.FirstOrDefault(
                version => version.VersionId == CurrentVersionId);
}