using MiniPdm.Application.Persistence.Models;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Persistence;

public interface IPdmImportUnitOfWork : IAsyncDisposable
{
    Task<StoredPdmObject?> FindObjectAsync(
        ObjectType objectType,
        string? designation,
        string name,
        CancellationToken cancellationToken);

    Task<long> InsertObjectAsync(
        ObjectType objectType,
        string? designation,
        string name,
        CancellationToken cancellationToken);

    Task UpdateObjectNameAsync(
        long objectId,
        string name,
        CancellationToken cancellationToken);

    Task<StoredObjectVersion?> GetCurrentVersionAsync(
        long objectId,
        CancellationToken cancellationToken);

    Task<int> GetNextVersionNumberAsync(
        long objectId,
        CancellationToken cancellationToken);

    Task<long> InsertVersionAsync(
        long objectId,
        int versionNumber,
        VersionState state,
        string? material,
        decimal? massKg,
        CancellationToken cancellationToken);

    Task UpdateVersionContentAsync(
        long versionId,
        string? material,
        decimal? massKg,
        CancellationToken cancellationToken);

    Task SetCurrentVersionAsync(
        long objectId,
        long versionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredBomLink>> GetBomLinksAsync(
        long parentVersionId,
        CancellationToken cancellationToken);

    Task ReplaceBomLinksAsync(
        long parentVersionId,
        IReadOnlyCollection<StoredBomLink> links,
        CancellationToken cancellationToken);

    Task CommitAsync(
        CancellationToken cancellationToken);

    Task RollbackAsync(
        CancellationToken cancellationToken);
}