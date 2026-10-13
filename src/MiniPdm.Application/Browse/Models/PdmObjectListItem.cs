using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Browse.Models;

public sealed record PdmObjectListItem(
    long ObjectId,
    ObjectType ObjectType,
    string? Designation,
    string Name,
    int? CurrentVersionNumber,
    VersionState? CurrentState);