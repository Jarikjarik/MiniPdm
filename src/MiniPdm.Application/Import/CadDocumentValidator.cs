using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Validation;

namespace MiniPdm.Application.Import;

public sealed class CadDocumentValidator
{
    public CadValidationResult Validate(
        IReadOnlyCollection<CadDocument> documents)
    {
        var acceptedDocuments = new List<CadDocument>();
        var issues = new List<CadValidationIssue>();

        var availableFileNames = documents
            .Select(document => document.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var duplicateDesignations = documents
            .Where(document =>
                document.Type is ObjectType.Assembly or ObjectType.Part)
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.Designation))
            .GroupBy(
                document => document.Designation!,
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        var duplicateStandardPartNames = documents
            .Where(document =>
                document.Type == ObjectType.StandardPart)
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.Name))
            .GroupBy(
                document => document.Name,
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var document in documents)
        {
            var hasError = false;

            if (document.Type == ObjectType.Assembly)
            {
                var invalidComponent = document.Components
                    .FirstOrDefault(component => component.Quantity <= 0);

                if (invalidComponent is not null)
                {
                    issues.Add(new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        $"Component '{invalidComponent.FileName}' has invalid quantity {invalidComponent.Quantity}."));

                    hasError = true;
                }

                foreach (var component in document.Components)
                {
                    if (availableFileNames.Contains(component.FileName))
                    {
                        continue;
                    }

                    issues.Add(new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        $"Referenced component '{component.FileName}' was not found."));

                    hasError = true;
                }
            }

            if (document.Type == ObjectType.StandardPart &&
                duplicateStandardPartNames.Contains(document.Name))
            {
                issues.Add(new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Error,
                    $"Duplicate standard part name '{document.Name}'."));

                hasError = true;
            }

            if (document.Type is ObjectType.Assembly or ObjectType.Part)
            {
                if (!DesignationValidator.IsValid(document.Designation))
                {
                    issues.Add(new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        "Invalid designation."));

                    hasError = true;
                }
                else if (duplicateDesignations.Contains(document.Designation!))
                {
                    issues.Add(new CadValidationIssue(
                        document.FileName,
                        ImportIssueSeverity.Error,
                        $"Duplicate designation '{document.Designation}'."));

                    hasError = true;
                }
            }

            if (document.Type == ObjectType.Part &&
                document.MassKg is null)
            {
                issues.Add(new CadValidationIssue(
                    document.FileName,
                    ImportIssueSeverity.Warning,
                    "Mass is not specified."));
            }

            if (!hasError)
            {
                acceptedDocuments.Add(document);
            }
        }

        var rejectedFileNames = issues
            .Where(issue => issue.Severity == ImportIssueSeverity.Error)
            .Select(issue => issue.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rejectedSomething = true;

        while (rejectedSomething)
        {
            rejectedSomething = false;

            var assemblies = acceptedDocuments
                .Where(document => document.Type == ObjectType.Assembly)
                .ToArray();

            foreach (var assembly in assemblies)
            {
                var rejectedComponent = assembly.Components
                    .FirstOrDefault(component =>
                        rejectedFileNames.Contains(component.FileName));

                if (rejectedComponent is null)
                {
                    continue;
                }

                acceptedDocuments.Remove(assembly);

                rejectedFileNames.Add(assembly.FileName);

                issues.Add(new CadValidationIssue(
                    assembly.FileName,
                    ImportIssueSeverity.Error,
                    $"Referenced component '{rejectedComponent.FileName}' was rejected."));

                rejectedSomething = true;
            }
        }

        return new CadValidationResult(
            acceptedDocuments,
            issues);
    }
}