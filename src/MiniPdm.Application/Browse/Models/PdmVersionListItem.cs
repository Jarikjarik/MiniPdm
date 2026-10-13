using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Browse.Models;

public sealed record PdmVersionListItem(
    long VersionId,
    int VersionNumber,
    VersionState State,
    string? Material,
    decimal? MassKg,
    DateTime CreatedAt);