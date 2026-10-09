namespace MiniPdm.Application.Abstractions.Cad;

public interface ICadDocumentLocator
{
    Task<IReadOnlyCollection<string>> GetDocumentPathsAsync(
        string folderPath,
        CancellationToken cancellationToken);
}