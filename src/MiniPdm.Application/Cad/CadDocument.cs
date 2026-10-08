using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Abstractions.Cad;

public sealed record CadDocument(
    int FormatVersion,
    string FileName,
    ObjectType Type,
    string? Designation,
    string Name,
    string? Material,
    decimal? MassKg,
    IReadOnlyCollection<CadComponent> Components);