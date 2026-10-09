namespace MiniPdm.Application.Import;

public sealed record ImportReportItem(
    string FileName,
    ImportItemStatus Status,
    string Message);