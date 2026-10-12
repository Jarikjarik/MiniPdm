using MiniPdm.Application.Persistence;
using MiniPdm.Application.Persistence.Models;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.Infrastructure.Persistence;

public sealed class PdmImportUnitOfWork : IPdmImportUnitOfWork
{
    private readonly IDbSession _session;
    private readonly PdmObjectRepository _objectRepository;
    private readonly ObjectVersionRepository _versionRepository;
    private readonly BomLinkRepository _bomRepository;

    public PdmImportUnitOfWork(
        IDbSession session,
        PdmObjectRepository objectRepository,
        ObjectVersionRepository versionRepository,
        BomLinkRepository bomRepository)
    {
        _session = session
            ?? throw new ArgumentNullException(nameof(session));

        _objectRepository = objectRepository
            ?? throw new ArgumentNullException(nameof(objectRepository));

        _versionRepository = versionRepository
            ?? throw new ArgumentNullException(nameof(versionRepository));

        _bomRepository = bomRepository
            ?? throw new ArgumentNullException(nameof(bomRepository));
    }

    public async Task<StoredPdmObject?> FindObjectAsync(
        ObjectType objectType,
        string? designation,
        string name,
        CancellationToken cancellationToken)
    {
        var snapshot = await _objectRepository.FindAsync(
            _session,
            objectType,
            designation,
            name,
            cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        return new StoredPdmObject(
            snapshot.Id,
            snapshot.ObjectType,
            snapshot.Designation,
            snapshot.Name);
    }

    public Task<long> InsertObjectAsync(
        ObjectType objectType,
        string? designation,
        string name,
        CancellationToken cancellationToken)
    {
        return _objectRepository.InsertAsync(
            _session,
            objectType,
            designation,
            name,
            cancellationToken);
    }

    public Task UpdateObjectNameAsync(
        long objectId,
        string name,
        CancellationToken cancellationToken)
    {
        return _objectRepository.UpdateNameAsync(
            _session,
            objectId,
            name,
            cancellationToken);
    }

    public async Task<StoredObjectVersion?> GetCurrentVersionAsync(
        long objectId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _versionRepository.GetCurrentAsync(
            _session,
            objectId,
            cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        return new StoredObjectVersion(
            snapshot.Id,
            snapshot.ObjectId,
            snapshot.VersionNumber,
            snapshot.State,
            snapshot.Material,
            snapshot.MassKg);
    }

    public Task<int> GetNextVersionNumberAsync(
        long objectId,
    CancellationToken cancellationToken)
    {
        return _versionRepository.GetNextVersionNumberAsync(
            _session,
            objectId,
            cancellationToken);
    }

    public Task<long> InsertVersionAsync(
        long objectId,
        int versionNumber,
        VersionState state,
        string? material,
        decimal? massKg,
        CancellationToken cancellationToken)
    {
        return _versionRepository.InsertAsync(
            _session,
            objectId,
            versionNumber,
            state,
            material,
            massKg,
            cancellationToken);
    }

    public Task UpdateVersionContentAsync(
        long versionId,
        string? material,
        decimal? massKg,
        CancellationToken cancellationToken)
    {
        return _versionRepository.UpdateContentAsync(
            _session,
            versionId,
            material,
            massKg,
            cancellationToken);
    }

    public Task SetCurrentVersionAsync(
        long objectId,
        long versionId,
        CancellationToken cancellationToken)
    {
        return _objectRepository.SetCurrentVersionAsync(
            _session,
            objectId,
            versionId,
            cancellationToken);
    }

    public Task<IReadOnlyList<StoredBomLink>> GetBomLinksAsync(
        long parentVersionId,
        CancellationToken cancellationToken)
    {
        return _bomRepository.GetBomLinksAsync(
            _session,
            parentVersionId,
            cancellationToken);
    }

    public Task ReplaceBomLinksAsync(
        long parentVersionId,
        IReadOnlyCollection<StoredBomLink> links,
        CancellationToken cancellationToken)
    {
        return _bomRepository.ReplaceBomLinksAsync(
            _session,
            parentVersionId,
            links,
            cancellationToken);
    }

    public Task CommitAsync(
        CancellationToken cancellationToken)
    {
        return _session.CommitAsync(
            cancellationToken);
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken)
    {
        return _session.RollbackAsync(
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _session.DisposeAsync();
    }
}