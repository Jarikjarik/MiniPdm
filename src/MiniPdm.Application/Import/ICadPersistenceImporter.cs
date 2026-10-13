using MiniPdm.Application.Abstractions.Cad;

namespace MiniPdm.Application.Import;

public interface ICadPersistenceImporter
{
    Task<CadPersistenceImportResult> ImportAsync(
        IReadOnlyCollection<CadDocument> documents,
        CancellationToken cancellationToken);
}