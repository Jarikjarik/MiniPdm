using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Persistence.Models;

public sealed record StoredPdmObject(
    long Id,
    ObjectType ObjectType,
    string? Designation,
    string Name);