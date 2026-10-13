using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Validation;

namespace MiniPdm.Application.Import;

public sealed class CadDocumentValidator
{
    public CadValidationResult Validate(
        IReadOnlyCollection<CadDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var acceptedDocuments = new List<CadDocument>();
        var issues = new List<CadValidationIssue>();

        var availableFileNames = documents
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.FileName))
            .Select(document =>
                document.FileName)
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var duplicateDesignations = documents
            .Where(document =>
                document.Type is ObjectType.Assembly or ObjectType.Part)
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.Designation))
            .GroupBy(
                document => document.Designation!,
                StringComparer.Ordinal)
            .Where(group =>
                group.Count() > 1)
            .Select(group =>
                group.Key)
            .ToHashSet(
                StringComparer.Ordinal);

        var duplicateStandardPartNames = documents
            .Where(document =>
                document.Type == ObjectType.StandardPart)
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.Name))
            .GroupBy(
                document => document.Name,
                StringComparer.Ordinal)
            .Where(group =>
                group.Count() > 1)
            .Select(group =>
                group.Key)
            .ToHashSet(
                StringComparer.Ordinal);

        foreach (var document in documents)
        {
            var hasError = false;

            if (string.IsNullOrWhiteSpace(
                    document.FileName))
            {
                issues.Add(
                    new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        "File name is required."));

                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(
                    document.Name))
            {
                issues.Add(
                    new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        "Object name is required."));

                hasError = true;
            }

            if (document.MassKg is < 0)
            {
                issues.Add(
                    new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        "Mass must not be negative."));

                hasError = true;
            }

            switch (document.Type)
            {
                case ObjectType.Assembly:
                    ValidateAssembly(
                        document,
                        availableFileNames,
                        issues,
                        ref hasError);

                    break;

                case ObjectType.Part:
                    ValidatePart(
                        document,
                        issues,
                        ref hasError);

                    break;

                case ObjectType.StandardPart:
                    ValidateStandardPart(
                        document,
                        duplicateStandardPartNames,
                        issues,
                        ref hasError);

                    break;

                default:
                    issues.Add(
                        new CadValidationIssue(
                            document.FileName,
                            ImportIssueSeverity.Error,
                            $"Unsupported object type '{document.Type}'."));

                    hasError = true;

                    break;
            }

            if (document.Type is ObjectType.Assembly or ObjectType.Part)
            {
                if (!DesignationValidator.IsValid(
                        document.Designation))
                {
                    issues.Add(
                        new CadValidationIssue(
                            document.FileName,
                            ImportIssueSeverity.Error,
                            "Invalid designation."));

                    hasError = true;
                }
                else if (duplicateDesignations.Contains(
                             document.Designation!))
                {
                    issues.Add(
                        new CadValidationIssue(
                            document.FileName,
                            ImportIssueSeverity.Error,
                            $"Duplicate designation " +
                            $"'{document.Designation}'."));

                    hasError = true;
                }
            }

            if (!hasError)
            {
                acceptedDocuments.Add(
                    document);
            }
        }

        RejectAssembliesWithRejectedChildren(
            acceptedDocuments,
            issues);

        return new CadValidationResult(
            acceptedDocuments,
            issues);
    }

    private static void ValidateAssembly(
        CadDocument document,
        IReadOnlySet<string> availableFileNames,
        ICollection<CadValidationIssue> issues,
        ref bool hasError)
    {
        if (!string.IsNullOrWhiteSpace(
                document.Material))
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Assembly must not have material."));

            hasError = true;
        }

        if (document.MassKg is not null)
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Assembly mass must be calculated from BOM."));

            hasError = true;
        }

        var duplicateComponents = document.Components
            .GroupBy(
                component => component.FileName,
                StringComparer.OrdinalIgnoreCase)
            .Where(group =>
                group.Count() > 1)
            .Select(group =>
                group.Key)
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateComponent in duplicateComponents)
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    $"Component '{duplicateComponent}' " +
                    "occurs more than once in BOM."));

            hasError = true;
        }

        foreach (var component in document.Components)
        {
            if (string.IsNullOrWhiteSpace(
                    component.FileName))
            {
                issues.Add(
                    new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        "Component file name is required."));

                hasError = true;

                continue;
            }

            if (component.Quantity <= 0)
            {
                issues.Add(
                    new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        $"Component '{component.FileName}' " +
                        $"has invalid quantity {component.Quantity}."));

                hasError = true;
            }

            if (!availableFileNames.Contains(
                    component.FileName))
            {
                issues.Add(
                    new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        $"Referenced component '{component.FileName}' " +
                        "was not found."));

                hasError = true;
            }
        }
    }

    private static void ValidatePart(
        CadDocument document,
        ICollection<CadValidationIssue> issues,
        ref bool hasError)
    {
        if (string.IsNullOrWhiteSpace(
                document.Material))
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Material is required for part."));

            hasError = true;
        }

        if (document.Components.Count > 0)
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Part must not contain BOM components."));

            hasError = true;
        }

        if (document.MassKg is null)
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Warning,
                    "Mass is not specified."));
        }
    }

    private static void ValidateStandardPart(
        CadDocument document,
        IReadOnlySet<string> duplicateStandardPartNames,
        ICollection<CadValidationIssue> issues,
        ref bool hasError)
    {
        if (duplicateStandardPartNames.Contains(
                document.Name))
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    $"Duplicate standard part name '{document.Name}'."));

            hasError = true;
        }

        if (document.Designation is not null)
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Standard part must not have a designation."));

            hasError = true;
        }

        if (document.MassKg is null)
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Mass is required for standard part."));

            hasError = true;
        }

        if (!string.IsNullOrWhiteSpace(
                document.Material))
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Standard part must not have material."));

            hasError = true;
        }

        if (document.Components.Count > 0)
        {
            issues.Add(
                new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    "Standard part must not contain BOM components."));

            hasError = true;
        }
    }

    private static void RejectAssembliesWithRejectedChildren(
        List<CadDocument> acceptedDocuments,
        ICollection<CadValidationIssue> issues)
    {
        var rejectedFileNames = issues
            .Where(issue =>
                issue.Severity == ImportIssueSeverity.Error)
            .Select(issue =>
                issue.FileName)
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var rejectedSomething = true;

        while (rejectedSomething)
        {
            rejectedSomething = false;

            var assemblies = acceptedDocuments
                .Where(document =>
                    document.Type == ObjectType.Assembly)
                .ToArray();

            foreach (var assembly in assemblies)
            {
                var rejectedComponent = assembly.Components
                    .FirstOrDefault(component =>
                        rejectedFileNames.Contains(
                            component.FileName));

                if (rejectedComponent is null)
                {
                    continue;
                }

                acceptedDocuments.Remove(
                    assembly);

                rejectedFileNames.Add(
                    assembly.FileName);

                issues.Add(
                    new CadValidationIssue(
                        assembly.FileName,
                        ImportIssueSeverity.Error,
                        $"Referenced component " +
                        $"'{rejectedComponent.FileName}' " +
                        "was rejected."));

                rejectedSomething = true;
            }
        }
    }
}