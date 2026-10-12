using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Persistence.Models;

public sealed record StoredObjectVersion(
    long Id,
    long ObjectId,
    int VersionNumber,
    VersionState State,
    string? Material,
    decimal? MassKg);