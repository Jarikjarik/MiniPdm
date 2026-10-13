using System.IO;
using MiniPdm.Application.Import;
using MiniPdm.Desktop.Services;

namespace MiniPdm.Desktop.ViewModels;

public sealed class ImportReportItemViewModel
{
    public ImportReportItemViewModel(
        ImportReportItem item)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        FileName =
            Path.GetFileName(
                item.FileName);

        Status =
            UiTextFormatter.FormatImportStatus(
                item.Status);

        Message =
            UiTextFormatter.FormatImportMessage(
                item.Message);
    }

    public string FileName { get; }

    public string Status { get; }

    public string Message { get; }
}