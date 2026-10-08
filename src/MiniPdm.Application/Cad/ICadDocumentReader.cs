namespace MiniPdm.Application.Abstractions.Cad;

public interface ICadDocumentReader
{
    Task<CadDocument> ReadAsync(
        string path,
        CancellationToken cancellationToken);
}