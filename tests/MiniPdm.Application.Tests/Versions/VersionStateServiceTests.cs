using MiniPdm.Application.Persistence;
using MiniPdm.Application.Persistence.Models;
using MiniPdm.Application.Versions;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Rules;

namespace MiniPdm.Application.Tests.Versions;

public class VersionStateServiceTests
{
    [Fact]
    public async Task ChangeStateAsync_InWorkToApproved_ChangesStateAndKeepsCurrentVersion()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();

        var objectId = unitOfWork.AddObject(
            ObjectType.Part,
            "ТЕСТ.111111.001",
            "Деталь");

        var versionId = unitOfWork.AddVersion(
            objectId,
            1,
            VersionState.InWork,
            "Сталь",
            1.50m,
            isCurrent: true);

        var service = CreateService(
            unitOfWork);

        var current = await service.ChangeStateAsync(
            versionId,
            VersionState.Approved,
            CancellationToken.None);

        Assert.NotNull(current);

        Assert.Equal(
            versionId,
            current!.Id);

        Assert.Equal(
            VersionState.Approved,
            current.State);

        Assert.Equal(
            VersionState.Approved,
            unitOfWork.GetStoredVersion(versionId).State);

        Assert.True(
            unitOfWork.Committed);

        Assert.False(
            unitOfWork.RolledBack);
    }

    [Fact]
    public async Task ChangeStateAsync_InWorkToCancelled_WhenOnlyVersionExists_CurrentBecomesNull()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();

        var objectId = unitOfWork.AddObject(
            ObjectType.Part,
            "ТЕСТ.111111.002",
            "Деталь");

        var versionId = unitOfWork.AddVersion(
            objectId,
            1,
            VersionState.InWork,
            "Сталь",
            1.50m,
            isCurrent: true);

        var service = CreateService(
            unitOfWork);

        var current = await service.ChangeStateAsync(
            versionId,
            VersionState.Cancelled,
            CancellationToken.None);

        Assert.Null(
            current);

        Assert.Equal(
            VersionState.Cancelled,
            unitOfWork.GetStoredVersion(versionId).State);

        Assert.True(
            unitOfWork.Committed);
    }

    [Fact]
    public async Task ChangeStateAsync_ApprovedToCancelled_WhenOnlyVersionExists_CurrentBecomesNull()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();

        var objectId = unitOfWork.AddObject(
            ObjectType.Part,
            "ТЕСТ.111111.003",
            "Деталь");

        var versionId = unitOfWork.AddVersion(
            objectId,
            1,
            VersionState.Approved,
            "Сталь",
            1.50m,
            isCurrent: true);

        var service = CreateService(
            unitOfWork);

        var current = await service.ChangeStateAsync(
            versionId,
            VersionState.Cancelled,
            CancellationToken.None);

        Assert.Null(
            current);

        Assert.Equal(
            VersionState.Cancelled,
            unitOfWork.GetStoredVersion(versionId).State);

        Assert.True(
            unitOfWork.Committed);
    }

    [Fact]
    public async Task ChangeStateAsync_CancelCurrentV2_FallsBackToApprovedV1()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();

        var objectId = unitOfWork.AddObject(
            ObjectType.Part,
            "ТЕСТ.111111.004",
            "Деталь");

        var version1Id = unitOfWork.AddVersion(
            objectId,
            1,
            VersionState.Approved,
            "Сталь",
            1.50m);

        var version2Id = unitOfWork.AddVersion(
            objectId,
            2,
            VersionState.InWork,
            "Сталь",
            1.60m,
            isCurrent: true);

        var service = CreateService(
            unitOfWork);

        var current = await service.ChangeStateAsync(
            version2Id,
            VersionState.Cancelled,
            CancellationToken.None);

        Assert.NotNull(
            current);

        Assert.Equal(
            version1Id,
            current!.Id);

        Assert.Equal(
            1,
            current.VersionNumber);

        Assert.Equal(
            VersionState.Approved,
            current.State);

        Assert.Equal(
            VersionState.Cancelled,
            unitOfWork.GetStoredVersion(version2Id).State);

        Assert.Equal(
            VersionState.Approved,
            unitOfWork.GetStoredVersion(version1Id).State);
    }

    [Fact]
    public async Task ChangeStateAsync_ApprovedToInWork_ThrowsAndRollsBack()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();

        var objectId = unitOfWork.AddObject(
            ObjectType.Part,
            "ТЕСТ.111111.005",
            "Деталь");

        var versionId = unitOfWork.AddVersion(
            objectId,
            1,
            VersionState.Approved,
            "Сталь",
            1.50m,
            isCurrent: true);

        var service = CreateService(
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangeStateAsync(
                versionId,
                VersionState.InWork,
                CancellationToken.None));

        Assert.Equal(
            VersionState.Approved,
            unitOfWork.GetStoredVersion(versionId).State);

        Assert.False(
            unitOfWork.Committed);

        Assert.True(
            unitOfWork.RolledBack);
    }

    [Fact]
    public async Task ChangeStateAsync_CancelledToApproved_ThrowsAndRollsBack()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();

        var objectId = unitOfWork.AddObject(
            ObjectType.Part,
            "ТЕСТ.111111.006",
            "Деталь");

        var versionId = unitOfWork.AddVersion(
            objectId,
            1,
            VersionState.Cancelled,
            "Сталь",
            1.50m);

        var service = CreateService(
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangeStateAsync(
                versionId,
                VersionState.Approved,
                CancellationToken.None));

        Assert.Equal(
            VersionState.Cancelled,
            unitOfWork.GetStoredVersion(versionId).State);

        Assert.False(
            unitOfWork.Committed);

        Assert.True(
            unitOfWork.RolledBack);
    }

    [Fact]
    public async Task ChangeStateAsync_UnknownVersion_ThrowsAndRollsBack()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();

        var service = CreateService(
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChangeStateAsync(
                999,
                VersionState.Approved,
                CancellationToken.None));

        Assert.False(
            unitOfWork.Committed);

        Assert.True(
            unitOfWork.RolledBack);
    }

    private static VersionStateService CreateService(
        FakePdmImportUnitOfWork unitOfWork)
    {
        return new VersionStateService(
            new FakePdmImportUnitOfWorkFactory(
                unitOfWork));
    }

    private sealed class FakePdmImportUnitOfWorkFactory
        : IPdmImportUnitOfWorkFactory
    {
        private readonly IPdmImportUnitOfWork _unitOfWork;

        public FakePdmImportUnitOfWorkFactory(
            IPdmImportUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<IPdmImportUnitOfWork> BeginAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                _unitOfWork);
        }
    }

    private sealed class FakePdmImportUnitOfWork
        : IPdmImportUnitOfWork
    {
        private readonly Dictionary<long, StoredPdmObject>
            _objects = [];

        private readonly Dictionary<long, List<StoredObjectVersion>>
            _versionsByObject = [];

        private readonly Dictionary<long, long>
            _currentVersionByObject = [];

        private long _nextObjectId = 1;
        private long _nextVersionId = 1;

        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public long AddObject(
            ObjectType objectType,
            string? designation,
            string name)
        {
            var id = _nextObjectId++;

            _objects[id] = new StoredPdmObject(
                id,
                objectType,
                designation,
                name);

            return id;
        }

        public long AddVersion(
            long objectId,
            int versionNumber,
            VersionState state,
            string? material,
            decimal? massKg,
            bool isCurrent = false)
        {
            var id = _nextVersionId++;

            if (!_versionsByObject.TryGetValue(
                    objectId,
                    out var versions))
            {
                versions = [];
                _versionsByObject[objectId] = versions;
            }

            versions.Add(
                new StoredObjectVersion(
                    id,
                    objectId,
                    versionNumber,
                    state,
                    material,
                    massKg));

            if (isCurrent)
            {
                if (state == VersionState.Cancelled)
                {
                    throw new InvalidOperationException(
                        "Cancelled version cannot be current.");
                }

                _currentVersionByObject[objectId] = id;
            }

            return id;
        }

        public StoredObjectVersion GetStoredVersion(
            long versionId)
        {
            return _versionsByObject.Values
                .SelectMany(versions => versions)
                .Single(version =>
                    version.Id == versionId);
        }

        public Task<StoredPdmObject?> FindObjectAsync(
            ObjectType objectType,
            string? designation,
            string name,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<long> InsertObjectAsync(
            ObjectType objectType,
            string? designation,
            string name,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateObjectNameAsync(
            long objectId,
            string name,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StoredObjectVersion?> GetVersionAsync(
            long versionId,
            CancellationToken cancellationToken)
        {
            var version = _versionsByObject.Values
                .SelectMany(versions => versions)
                .FirstOrDefault(version =>
                    version.Id == versionId);

            return Task.FromResult<StoredObjectVersion?>(
                version);
        }

        public Task<StoredObjectVersion?> GetCurrentVersionAsync(
            long objectId,
            CancellationToken cancellationToken)
        {
            if (!_currentVersionByObject.TryGetValue(
                    objectId,
                    out var currentVersionId))
            {
                return Task.FromResult<StoredObjectVersion?>(
                    null);
            }

            var version = _versionsByObject[objectId]
                .Single(item =>
                    item.Id == currentVersionId);

            return Task.FromResult<StoredObjectVersion?>(
                version);
        }

        public Task<int> GetNextVersionNumberAsync(
            long objectId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<long> InsertVersionAsync(
            long objectId,
            int versionNumber,
            VersionState state,
            string? material,
            decimal? massKg,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateVersionContentAsync(
            long versionId,
            string? material,
            decimal? massKg,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ChangeVersionStateAsync(
            long versionId,
            VersionState currentState,
            VersionState newState,
            CancellationToken cancellationToken)
        {
            if (!VersionStateTransitionRules.CanTransition(
                    currentState,
                    newState))
            {
                throw new InvalidOperationException(
                    $"Transition from {currentState} " +
                    $"to {newState} is not allowed.");
            }

            foreach (var versions in _versionsByObject.Values)
            {
                var index = versions.FindIndex(
                    version =>
                        version.Id == versionId);

                if (index < 0)
                {
                    continue;
                }

                var version = versions[index];

                if (version.State != currentState)
                {
                    throw new InvalidOperationException(
                        $"Version {versionId} is not in state " +
                        $"{currentState}.");
                }

                versions[index] =
                    version with
                    {
                        State = newState
                    };

                return Task.CompletedTask;
            }

            throw new InvalidOperationException(
                $"Version {versionId} was not found.");
        }

        public Task SetCurrentVersionAsync(
            long objectId,
            long versionId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RefreshCurrentVersionAsync(
            long objectId,
            CancellationToken cancellationToken)
        {
            if (!_versionsByObject.TryGetValue(
                    objectId,
                    out var versions))
            {
                _currentVersionByObject.Remove(
                    objectId);

                return Task.CompletedTask;
            }

            var currentVersion = versions
                .Where(version =>
                    version.State != VersionState.Cancelled)
                .OrderByDescending(version =>
                    version.VersionNumber)
                .FirstOrDefault();

            if (currentVersion is null)
            {
                _currentVersionByObject.Remove(
                    objectId);
            }
            else
            {
                _currentVersionByObject[objectId] =
                    currentVersion.Id;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StoredBomLink>>
            GetBomLinksAsync(
                long parentVersionId,
                CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ReplaceBomLinksAsync(
            long parentVersionId,
            IReadOnlyCollection<StoredBomLink> links,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CommitAsync(
            CancellationToken cancellationToken)
        {
            Committed = true;

            return Task.CompletedTask;
        }

        public Task RollbackAsync(
            CancellationToken cancellationToken)
        {
            RolledBack = true;

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}