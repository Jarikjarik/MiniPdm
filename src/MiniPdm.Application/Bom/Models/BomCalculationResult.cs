namespace MiniPdm.Application.Bom.Models;

public sealed class BomCalculationResult
{
    public BomCalculationResult(
        decimal? massKg,
        IReadOnlyList<FlatSpecificationItem> specification,
        IReadOnlyList<BomCalculationIssue> issues)
    {
        MassKg = massKg;
        Specification = specification;
        Issues = issues;
    }

    public decimal? MassKg { get; }

    public IReadOnlyList<FlatSpecificationItem> Specification { get; }

    public IReadOnlyList<BomCalculationIssue> Issues { get; }

    public bool HasIssues =>
        Issues.Count > 0;

    public bool HasMass =>
        MassKg.HasValue;
}