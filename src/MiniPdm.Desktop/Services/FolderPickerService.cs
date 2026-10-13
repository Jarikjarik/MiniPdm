using Microsoft.Win32;

namespace MiniPdm.Desktop.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    public string? PickFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите папку с CAD-файлами"
        };

        return dialog.ShowDialog() == true
            ? dialog.FolderName
            : null;
    }
}