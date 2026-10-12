using MiniPdm.Domain.Enums;

namespace MiniPdm.Infrastructure.Persistence.Models;

public sealed class PdmObjectSnapshot
{
    public long Id { get; init; }

    public ObjectType ObjectType { get; init; }

    public string? Designation { get; init; }

    public string Name { get; init; } = string.Empty;
}