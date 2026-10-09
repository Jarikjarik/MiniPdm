namespace MiniPdm.Application.Import;

public sealed class ImportReport
{
    public ImportReport(
        IReadOnlyCollection<ImportReportItem> items)
    {
        Items = items;
    }

    public IReadOnlyCollection<ImportReportItem> Items { get; }

    public int AcceptedCount =>
        Items.Count(item =>
            item.Status is ImportItemStatus.Success or ImportItemStatus.Warning);

    public int RejectedCount =>
        Items.Count(item =>
            item.Status == ImportItemStatus.Error);

    public int WarningCount =>
        Items.Count(item =>
            item.Status == ImportItemStatus.Warning);
}