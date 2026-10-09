using MiniPdm.Application.Abstractions.Cad;

namespace MiniPdm.Application.Import;

public sealed class CadFolderReader
{
    private readonly ICadDocumentLocator _documentLocator;
    private readonly ICadDocumentReader _documentReader;

    public CadFolderReader(
        ICadDocumentLocator documentLocator,
        ICadDocumentReader documentReader)
    {
        _documentLocator = documentLocator;
        _documentReader = documentReader;
    }

    public async Task<CadFolderReadResult> ReadAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        var paths = await _documentLocator.GetDocumentPathsAsync(
            folderPath,
            cancellationToken);

        var documents = new List<CadDocument>();
        var errors = new List<CadReadError>();

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var document = await _documentReader.ReadAsync(
                    path,
                    cancellationToken);

                documents.Add(document);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                errors.Add(new CadReadError(
                    path,
                    exception.Message));
            }
        }

        return new CadFolderReadResult(
            documents,
            errors);
    }
}