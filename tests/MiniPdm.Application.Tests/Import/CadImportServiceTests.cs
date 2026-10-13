using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Import;
using MiniPdm.Application.Persistence;
using MiniPdm.Application.Persistence.Models;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Tests.Import;

public class CadImportServiceTests
{
    [Fact]
    public async Task ImportAsync_NewPart_CreatesObjectAndInitialVersion()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();
        var factory = new FakePdmImportUnitOfWorkFactory(unitOfWork);
        var service = new CadImportService(factory);

        var document = new CadDocument(
            1,
            "Колесо зубчатое.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.001",
            "Колесо зубчатое",
            "Сталь 20",
            5.86m,
            Array.Empty<CadComponent>());

        var result = await service.ImportAsync(
            new[] { document },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(0, result.NewVersionCount);
        Assert.Equal(0, result.UnchangedCount);

        var item = Assert.Single(result.Items);

        Assert.Equal(
            CadPersistenceAction.Created,
            item.Action);

        var storedObject = Assert.Single(
            unitOfWork.Objects);

        Assert.Equal(
            ObjectType.Part,
            storedObject.ObjectType);

        Assert.Equal(
            "ТЕСТ.123456.001",
            storedObject.Designation);

        Assert.Equal(
            "Колесо зубчатое",
            storedObject.Name);

        var storedVersion = Assert.Single(
            unitOfWork.Versions);

        Assert.Equal(
            storedObject.Id,
            storedVersion.ObjectId);

        Assert.Equal(
            1,
            storedVersion.VersionNumber);

        Assert.Equal(
            VersionState.InWork,
            storedVersion.State);

        Assert.Equal(
            "Сталь 20",
            storedVersion.Material);

        Assert.Equal(
            5.86m,
            storedVersion.MassKg);

        Assert.True(unitOfWork.Committed);
        Assert.False(unitOfWork.RolledBack);
    }

    [Fact]
    public async Task ImportAsync_UnchangedPart_DoesNotCreateNewVersion()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();
        var factory = new FakePdmImportUnitOfWorkFactory(unitOfWork);
        var service = new CadImportService(factory);

        var document = new CadDocument(
            1,
            "Колесо зубчатое.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.002",
            "Колесо зубчатое",
            "Сталь 20",
            5.86m,
            Array.Empty<CadComponent>());

        var firstResult = await service.ImportAsync(
            new[] { document },
            CancellationToken.None);

        var secondResult = await service.ImportAsync(
            new[] { document },
            CancellationToken.None);

        Assert.Equal(1, firstResult.CreatedCount);

        Assert.Equal(0, secondResult.CreatedCount);
        Assert.Equal(0, secondResult.UpdatedCount);
        Assert.Equal(0, secondResult.NewVersionCount);
        Assert.Equal(1, secondResult.UnchangedCount);

        var item = Assert.Single(
            secondResult.Items);

        Assert.Equal(
            CadPersistenceAction.Unchanged,
            item.Action);

        Assert.Single(unitOfWork.Objects);
        Assert.Single(unitOfWork.Versions);

        var version = Assert.Single(
            unitOfWork.Versions);

        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(VersionState.InWork, version.State);
        Assert.Equal(5.86m, version.MassKg);
    }

    [Fact]
    public async Task ImportAsync_ChangedInWorkPart_UpdatesExistingVersion()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();
        var factory = new FakePdmImportUnitOfWorkFactory(unitOfWork);
        var service = new CadImportService(factory);

        var originalDocument = new CadDocument(
            1,
            "Колесо зубчатое.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.003",
            "Колесо зубчатое",
            "Сталь 20",
            5.86m,
            Array.Empty<CadComponent>());

        var firstResult = await service.ImportAsync(
            new[] { originalDocument },
            CancellationToken.None);

        var firstItem = Assert.Single(
            firstResult.Items);

        var updatedDocument = new CadDocument(
            1,
            "Колесо зубчатое.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.003",
            "Колесо зубчатое",
            "Сталь 20",
            5.92m,
            Array.Empty<CadComponent>());

        var secondResult = await service.ImportAsync(
            new[] { updatedDocument },
            CancellationToken.None);

        Assert.Equal(0, secondResult.CreatedCount);
        Assert.Equal(1, secondResult.UpdatedCount);
        Assert.Equal(0, secondResult.NewVersionCount);
        Assert.Equal(0, secondResult.UnchangedCount);

        var secondItem = Assert.Single(
            secondResult.Items);

        Assert.Equal(
            CadPersistenceAction.Updated,
            secondItem.Action);

        Assert.Equal(
            firstItem.ObjectId,
            secondItem.ObjectId);

        Assert.Equal(
            firstItem.VersionId,
            secondItem.VersionId);

        Assert.Single(unitOfWork.Objects);
        Assert.Single(unitOfWork.Versions);

        var version = Assert.Single(
            unitOfWork.Versions);

        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(VersionState.InWork, version.State);
        Assert.Equal("Сталь 20", version.Material);
        Assert.Equal(5.92m, version.MassKg);
    }

    [Fact]
    public async Task ImportAsync_ChangedApprovedPart_CreatesNewInWorkVersion()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();
        var factory = new FakePdmImportUnitOfWorkFactory(unitOfWork);
        var service = new CadImportService(factory);

        var originalDocument = new CadDocument(
            1,
            "Колесо зубчатое.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.004",
            "Колесо зубчатое",
            "Сталь 20",
            5.86m,
            Array.Empty<CadComponent>());

        var firstResult = await service.ImportAsync(
            new[] { originalDocument },
            CancellationToken.None);

        var firstItem = Assert.Single(
            firstResult.Items);

        unitOfWork.SetVersionState(
            firstItem.VersionId,
            VersionState.Approved);

        var updatedDocument = new CadDocument(
            1,
            "Колесо зубчатое.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.004",
            "Колесо зубчатое",
            "Сталь 20",
            5.92m,
            Array.Empty<CadComponent>());

        var secondResult = await service.ImportAsync(
            new[] { updatedDocument },
            CancellationToken.None);

        Assert.Equal(0, secondResult.CreatedCount);
        Assert.Equal(0, secondResult.UpdatedCount);
        Assert.Equal(1, secondResult.NewVersionCount);
        Assert.Equal(0, secondResult.UnchangedCount);

        var secondItem = Assert.Single(
            secondResult.Items);

        Assert.Equal(
            CadPersistenceAction.NewVersion,
            secondItem.Action);

        Assert.Equal(
            firstItem.ObjectId,
            secondItem.ObjectId);

        Assert.NotEqual(
            firstItem.VersionId,
            secondItem.VersionId);

        Assert.Single(unitOfWork.Objects);
        Assert.Equal(2, unitOfWork.Versions.Count);

        var version1 = unitOfWork.Versions
            .Single(version => version.VersionNumber == 1);

        var version2 = unitOfWork.Versions
            .Single(version => version.VersionNumber == 2);

        Assert.Equal(
            VersionState.Approved,
            version1.State);

        Assert.Equal(
            5.86m,
            version1.MassKg);

        Assert.Equal(
            VersionState.InWork,
            version2.State);

        Assert.Equal(
            5.92m,
            version2.MassKg);

        Assert.Equal(
            secondItem.VersionId,
            version2.Id);
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
            return Task.FromResult(_unitOfWork);
        }
    }

    private sealed class FakePdmImportUnitOfWork
        : IPdmImportUnitOfWork
    {
        private readonly Dictionary<long, StoredPdmObject> _objects = [];

        private readonly Dictionary<long, List<StoredObjectVersion>>
            _versionsByObject = [];

        private readonly Dictionary<long, long>
            _currentVersionByObject = [];

        private readonly Dictionary<long, List<StoredBomLink>>
            _bomByVersion = [];

        private long _nextObjectId = 1;
        private long _nextVersionId = 1;

        public IReadOnlyCollection<StoredPdmObject> Objects =>
            _objects.Values.ToArray();

        public IReadOnlyCollection<StoredObjectVersion> Versions =>
            _versionsByObject.Values
                .SelectMany(versions => versions)
                .ToArray();

        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public Task<StoredPdmObject?> FindObjectAsync(
            ObjectType objectType,
            string? designation,
            string name,
            CancellationToken cancellationToken)
        {
            StoredPdmObject? result;

            if (objectType == ObjectType.StandardPart)
            {
                result = _objects.Values.FirstOrDefault(
                    item =>
                        item.ObjectType == ObjectType.StandardPart &&
                        item.Name == name);
            }
            else
            {
                result = _objects.Values.FirstOrDefault(
                    item =>
                        item.Designation == designation);
            }

            return Task.FromResult(result);
        }

        public Task<long> InsertObjectAsync(
            ObjectType objectType,
            string? designation,
            string name,
            CancellationToken cancellationToken)
        {
            var id = _nextObjectId++;

            _objects.Add(
                id,
                new StoredPdmObject(
                    id,
                    objectType,
                    designation,
                    name));

            return Task.FromResult(id);
        }

        public Task UpdateObjectNameAsync(
            long objectId,
            string name,
            CancellationToken cancellationToken)
        {
            var current = _objects[objectId];

            _objects[objectId] =
                current with
                {
                    Name = name
                };

            return Task.CompletedTask;
        }

        public Task<StoredObjectVersion?> GetCurrentVersionAsync(
            long objectId,
            CancellationToken cancellationToken)
        {
            if (!_currentVersionByObject.TryGetValue(
                    objectId,
                    out var versionId))
            {
                return Task.FromResult<StoredObjectVersion?>(
                    null);
            }

            var version = _versionsByObject[objectId]
                .Single(item => item.Id == versionId);

            return Task.FromResult<StoredObjectVersion?>(
                version);
        }

        public Task<int> GetNextVersionNumberAsync(
            long objectId,
            CancellationToken cancellationToken)
        {
            if (!_versionsByObject.TryGetValue(
                    objectId,
                    out var versions) ||
                versions.Count == 0)
            {
                return Task.FromResult(1);
            }

            return Task.FromResult(
                versions.Max(
                    item => item.VersionNumber) + 1);
        }

        public Task<long> InsertVersionAsync(
            long objectId,
            int versionNumber,
            VersionState state,
            string? material,
            decimal? massKg,
            CancellationToken cancellationToken)
        {
            var id = _nextVersionId++;

            var version = new StoredObjectVersion(
                id,
                objectId,
                versionNumber,
                state,
                material,
                massKg);

            if (!_versionsByObject.TryGetValue(
                    objectId,
                    out var versions))
            {
                versions = [];

                _versionsByObject.Add(
                    objectId,
                    versions);
            }

            versions.Add(version);

            return Task.FromResult(id);
        }

        public Task UpdateVersionContentAsync(
            long versionId,
            string? material,
            decimal? massKg,
            CancellationToken cancellationToken)
        {
            foreach (var versions in _versionsByObject.Values)
            {
                var index = versions.FindIndex(
                    item => item.Id == versionId);

                if (index < 0)
                {
                    continue;
                }

                var version = versions[index];

                if (version.State != VersionState.InWork)
                {
                    throw new InvalidOperationException(
                        $"Version {versionId} is not InWork.");
                }

                versions[index] =
                    version with
                    {
                        Material = material,
                        MassKg = massKg
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
            _currentVersionByObject[objectId] =
                versionId;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StoredBomLink>>
            GetBomLinksAsync(
                long parentVersionId,
                CancellationToken cancellationToken)
        {
            if (!_bomByVersion.TryGetValue(
                    parentVersionId,
                    out var links))
            {
                return Task.FromResult<IReadOnlyList<StoredBomLink>>(
                    Array.Empty<StoredBomLink>());
            }

            return Task.FromResult<IReadOnlyList<StoredBomLink>>(
                links.ToArray());
        }

        public Task ReplaceBomLinksAsync(
            long parentVersionId,
            IReadOnlyCollection<StoredBomLink> links,
            CancellationToken cancellationToken)
        {
            _bomByVersion[parentVersionId] =
                links.ToList();

            return Task.CompletedTask;
        }

        public void SetVersionState(
            long versionId,
            VersionState state)
        {
            foreach (var versions in _versionsByObject.Values)
            {
                var index = versions.FindIndex(
                    item => item.Id == versionId);

                if (index < 0)
                {
                    continue;
                }

                versions[index] =
                    versions[index] with
                    {
                        State = state
                    };

                return;
            }

            throw new InvalidOperationException(
                $"Version {versionId} was not found.");
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

    [Fact]
    public async Task ImportAsync_NewAssemblyWithPart_CreatesBomLink()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();
        var factory = new FakePdmImportUnitOfWorkFactory(unitOfWork);
        var service = new CadImportService(factory);

        var partDocument = new CadDocument(
            1,
            "Вал.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.010",
            "Вал",
            "Сталь 20",
            2.50m,
            Array.Empty<CadComponent>());

        var assemblyDocument = new CadDocument(
            1,
            "Редуктор.a3d",
            ObjectType.Assembly,
            "ТЕСТ.123456.011",
            "Редуктор",
            null,
            null,
            new[]
            {
            new CadComponent(
                "Вал.m3d",
                3)
            });

        var result = await service.ImportAsync(
            new[]
            {
            assemblyDocument,
            partDocument
            },
            CancellationToken.None);

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(0, result.NewVersionCount);
        Assert.Equal(0, result.UnchangedCount);

        var assemblyItem = result.Items.Single(
            item => item.FileName == "Редуктор.a3d");

        var partItem = result.Items.Single(
            item => item.FileName == "Вал.m3d");

        var bomLinks = await unitOfWork.GetBomLinksAsync(
            assemblyItem.VersionId,
            CancellationToken.None);

        var bomLink = Assert.Single(bomLinks);

        Assert.Equal(
            partItem.ObjectId,
            bomLink.ChildObjectId);

        Assert.Equal(
            3,
            bomLink.Quantity);

        Assert.Equal(2, unitOfWork.Objects.Count);
        Assert.Equal(2, unitOfWork.Versions.Count);
    }

    [Fact]
    public async Task ImportAsync_ChangedBomOfInWorkAssembly_UpdatesSameVersion()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();
        var factory = new FakePdmImportUnitOfWorkFactory(unitOfWork);
        var service = new CadImportService(factory);

        var partDocument = new CadDocument(
            1,
            "Прокладка.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.020",
            "Прокладка",
            "Паронит",
            0.10m,
            Array.Empty<CadComponent>());

        var originalAssembly = new CadDocument(
            1,
            "Крышка.a3d",
            ObjectType.Assembly,
            "ТЕСТ.123456.021",
            "Крышка в сборе",
            null,
            null,
            new[]
            {
            new CadComponent(
                "Прокладка.m3d",
                3)
            });

        var firstResult = await service.ImportAsync(
            new[]
            {
            originalAssembly,
            partDocument
            },
            CancellationToken.None);

        var firstAssemblyItem = firstResult.Items.Single(
            item => item.FileName == "Крышка.a3d");

        var updatedAssembly = new CadDocument(
            1,
            "Крышка.a3d",
            ObjectType.Assembly,
            "ТЕСТ.123456.021",
            "Крышка в сборе",
            null,
            null,
            new[]
            {
            new CadComponent(
                "Прокладка.m3d",
                4)
            });

        var secondResult = await service.ImportAsync(
            new[]
            {
            updatedAssembly,
            partDocument
            },
            CancellationToken.None);

        var secondAssemblyItem = secondResult.Items.Single(
            item => item.FileName == "Крышка.a3d");

        var secondPartItem = secondResult.Items.Single(
            item => item.FileName == "Прокладка.m3d");

        Assert.Equal(
            CadPersistenceAction.Updated,
            secondAssemblyItem.Action);

        Assert.Equal(
            CadPersistenceAction.Unchanged,
            secondPartItem.Action);

        Assert.Equal(
            firstAssemblyItem.VersionId,
            secondAssemblyItem.VersionId);

        var assemblyVersions = unitOfWork.Versions
            .Where(version =>
                version.ObjectId == secondAssemblyItem.ObjectId)
            .ToArray();

        var assemblyVersion = Assert.Single(
            assemblyVersions);

        Assert.Equal(
            1,
            assemblyVersion.VersionNumber);

        Assert.Equal(
            VersionState.InWork,
            assemblyVersion.State);

        var bomLinks = await unitOfWork.GetBomLinksAsync(
            secondAssemblyItem.VersionId,
            CancellationToken.None);

        var bomLink = Assert.Single(
            bomLinks);

        Assert.Equal(
            4,
            bomLink.Quantity);
    }

    [Fact]
    public async Task ImportAsync_ChangedBomOfApprovedAssembly_CreatesNewVersion()
    {
        var unitOfWork = new FakePdmImportUnitOfWork();
        var factory = new FakePdmImportUnitOfWorkFactory(unitOfWork);
        var service = new CadImportService(factory);

        var partDocument = new CadDocument(
            1,
            "Прокладка регулировочная.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.030",
            "Прокладка регулировочная",
            "Паронит",
            0.15m,
            Array.Empty<CadComponent>());

        var originalAssembly = new CadDocument(
            1,
            "Крышка подшипника.a3d",
            ObjectType.Assembly,
            "ТЕСТ.123456.031",
            "Крышка подшипника в сборе",
            null,
            null,
            new[]
            {
            new CadComponent(
                "Прокладка регулировочная.m3d",
                3)
            });

        var firstResult = await service.ImportAsync(
            new[]
            {
            originalAssembly,
            partDocument
            },
            CancellationToken.None);

        var firstAssemblyItem = firstResult.Items.Single(
            item => item.FileName == "Крышка подшипника.a3d");

        unitOfWork.SetVersionState(
            firstAssemblyItem.VersionId,
            VersionState.Approved);

        var updatedAssembly = new CadDocument(
            1,
            "Крышка подшипника.a3d",
            ObjectType.Assembly,
            "ТЕСТ.123456.031",
            "Крышка подшипника в сборе",
            null,
            null,
            new[]
            {
            new CadComponent(
                "Прокладка регулировочная.m3d",
                4)
            });

        var secondResult = await service.ImportAsync(
            new[]
            {
            updatedAssembly,
            partDocument
            },
            CancellationToken.None);

        var secondAssemblyItem = secondResult.Items.Single(
            item => item.FileName == "Крышка подшипника.a3d");

        Assert.Equal(
            CadPersistenceAction.NewVersion,
            secondAssemblyItem.Action);

        Assert.NotEqual(
            firstAssemblyItem.VersionId,
            secondAssemblyItem.VersionId);

        var assemblyVersions = unitOfWork.Versions
            .Where(version =>
                version.ObjectId == secondAssemblyItem.ObjectId)
            .OrderBy(version =>
                version.VersionNumber)
            .ToArray();

        Assert.Equal(
            2,
            assemblyVersions.Length);

        Assert.Equal(
            1,
            assemblyVersions[0].VersionNumber);

        Assert.Equal(
            VersionState.Approved,
            assemblyVersions[0].State);

        Assert.Equal(
            2,
            assemblyVersions[1].VersionNumber);

        Assert.Equal(
            VersionState.InWork,
            assemblyVersions[1].State);

        var oldBom = await unitOfWork.GetBomLinksAsync(
            firstAssemblyItem.VersionId,
            CancellationToken.None);

        var newBom = await unitOfWork.GetBomLinksAsync(
            secondAssemblyItem.VersionId,
            CancellationToken.None);

        Assert.Equal(
            3,
            Assert.Single(oldBom).Quantity);

        Assert.Equal(
            4,
            Assert.Single(newBom).Quantity);
    }
}