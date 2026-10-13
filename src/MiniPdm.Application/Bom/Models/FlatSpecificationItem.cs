using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Bom.Models;

public sealed record FlatSpecificationItem(
    long ObjectId,
    ObjectType ObjectType,
    string? Designation,
    string Name,
    string? Material,
    decimal? MassKg,
    long Quantity);