namespace MiniPdm.Application.Import;

public interface ICadFolderReader
{
    Task<CadFolderReadResult> ReadAsync(
        string folderPath,
        CancellationToken cancellationToken);
}