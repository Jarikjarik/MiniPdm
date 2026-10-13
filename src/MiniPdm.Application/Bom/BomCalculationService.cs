using MiniPdm.Application.Bom.Models;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Bom;

public sealed class BomCalculationService
{
    private readonly IBomStructureReader _structureReader;

    public BomCalculationService(
        IBomStructureReader structureReader)
    {
        _structureReader = structureReader
            ?? throw new ArgumentNullException(
                nameof(structureReader));
    }

    public async Task<BomCalculationResult> CalculateAsync(
        long rootObjectId,
        CancellationToken cancellationToken)
    {
        var structure =
            await _structureReader.ReadAsync(
                rootObjectId,
                cancellationToken);

        if (structure is null)
        {
            return new BomCalculationResult(
                null,
                Array.Empty<FlatSpecificationItem>(),
                new[]
                {
                    new BomCalculationIssue(
                        BomCalculationIssueCode.ObjectNotFound,
                        rootObjectId,
                        null,
                        $"Object {rootObjectId} was not found.")
                });
        }

        var root = structure.Root;

        if (!root.HasCurrentVersion)
        {
            return new BomCalculationResult(
                null,
                Array.Empty<FlatSpecificationItem>(),
                new[]
                {
                    new BomCalculationIssue(
                        BomCalculationIssueCode.MissingCurrentVersion,
                        root.ObjectId,
                        root.Name,
                        $"Object '{root.Name}' has no current version.")
                });
        }

        var cycleNodes = structure.Nodes
            .Where(node =>
                node.IsCycle)
            .GroupBy(node =>
                node.ObjectId)
            .Select(group =>
                group.First())
            .ToArray();

        if (cycleNodes.Length > 0)
        {
            var issues = cycleNodes
                .Select(node =>
                    new BomCalculationIssue(
                        BomCalculationIssueCode.CycleDetected,
                        node.ObjectId,
                        node.Name,
                        $"Cycle detected at object '{node.Name}'."))
                .ToArray();

            return new BomCalculationResult(
                null,
                Array.Empty<FlatSpecificationItem>(),
                issues);
        }

        var nodesWithoutCurrentVersion = structure.Nodes
            .Where(node =>
                !node.HasCurrentVersion)
            .GroupBy(node =>
                node.ObjectId)
            .Select(group =>
                group.First())
            .ToArray();

        if (nodesWithoutCurrentVersion.Length > 0)
        {
            var issues = nodesWithoutCurrentVersion
                .Select(node =>
                    new BomCalculationIssue(
                        BomCalculationIssueCode.MissingCurrentVersion,
                        node.ObjectId,
                        node.Name,
                        $"Object '{node.Name}' has no current version."))
                .ToArray();

            return new BomCalculationResult(
                null,
                Array.Empty<FlatSpecificationItem>(),
                issues);
        }

        if (root.ObjectType != ObjectType.Assembly)
        {
            return CalculateLeafRoot(
                root);
        }

        return CalculateAssembly(
            structure);
    }

    private static BomCalculationResult CalculateLeafRoot(
        BomObject root)
    {
        var specification = new[]
        {
            new FlatSpecificationItem(
                root.ObjectId,
                root.ObjectType,
                root.Designation,
                root.Name,
                root.Material,
                root.MassKg,
                1)
        };

        if (root.MassKg is null)
        {
            return new BomCalculationResult(
                null,
                specification,
                new[]
                {
                    new BomCalculationIssue(
                        BomCalculationIssueCode.MissingMass,
                        root.ObjectId,
                        root.Name,
                        $"Mass is not specified for '{root.Name}'.")
                });
        }

        return new BomCalculationResult(
            root.MassKg,
            specification,
            Array.Empty<BomCalculationIssue>());
    }

    private static BomCalculationResult CalculateAssembly(
        BomStructure structure)
    {
        var leafNodes = structure.Nodes
            .Where(node =>
                node.ObjectType is
                    ObjectType.Part or
                    ObjectType.StandardPart)
            .ToArray();

        var specification = leafNodes
            .GroupBy(node =>
                node.ObjectId)
            .Select(group =>
            {
                var first = group.First();

                return new FlatSpecificationItem(
                    first.ObjectId,
                    first.ObjectType,
                    first.Designation,
                    first.Name,
                    first.Material,
                    first.MassKg,
                    group.Sum(node =>
                        node.TotalQuantity));
            })
            .OrderBy(item =>
                item.ObjectType)
            .ThenBy(item =>
                item.Designation)
            .ThenBy(item =>
                item.Name)
            .ToArray();

        var nodesWithoutMass = leafNodes
            .Where(node =>
                node.MassKg is null)
            .GroupBy(node =>
                node.ObjectId)
            .Select(group =>
                group.First())
            .ToArray();

        if (nodesWithoutMass.Length > 0)
        {
            var issues = nodesWithoutMass
                .Select(node =>
                    new BomCalculationIssue(
                        BomCalculationIssueCode.MissingMass,
                        node.ObjectId,
                        node.Name,
                        $"Mass is not specified for '{node.Name}'."))
                .ToArray();

            return new BomCalculationResult(
                null,
                specification,
                issues);
        }

        var totalMass = leafNodes.Sum(node =>
            node.MassKg!.Value *
            node.TotalQuantity);

        return new BomCalculationResult(
            totalMass,
            specification,
            Array.Empty<BomCalculationIssue>());
    }
}