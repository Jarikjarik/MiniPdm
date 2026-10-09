namespace MiniPdm.Application.Import;

public sealed class ImportReportBuilder
{
    public ImportReport Build(
        CadFolderReadResult readResult,
        CadValidationResult validationResult)
    {
        var items = new List<ImportReportItem>();

        var issuesByFileName = validationResult.Issues
            .GroupBy(
                issue => issue.FileName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var document in readResult.Documents)
        {
            if (!issuesByFileName.TryGetValue(
                    document.FileName,
                    out var documentIssues))
            {
                items.Add(new ImportReportItem(
                    document.FileName,
                    ImportItemStatus.Success,
                    "Imported successfully."));

                continue;
            }

            var status = documentIssues.Any(issue =>
                issue.Severity == ImportIssueSeverity.Error)
                    ? ImportItemStatus.Error
                    : ImportItemStatus.Warning;

            var message = string.Join(
                "; ",
                documentIssues.Select(issue => issue.Reason));

            items.Add(new ImportReportItem(
                document.FileName,
                status,
                message));
        }

        foreach (var readError in readResult.Errors)
        {
            items.Add(new ImportReportItem(
                readError.Path,
                ImportItemStatus.Error,
                readError.Reason));
        }

        var orderedItems = items
            .OrderBy(
                item => item.FileName,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ImportReport(orderedItems);
    }
}