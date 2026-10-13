namespace MiniPdm.Application.Bom.Models;

public enum BomCalculationIssueCode
{
    ObjectNotFound,
    MissingCurrentVersion,
    MissingMass,
    CycleDetected
}

public sealed record BomCalculationIssue(
    BomCalculationIssueCode Code,
    long ObjectId,
    string? Name,
    string Message);