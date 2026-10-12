using Dapper;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.IntegrationTests.Persistence;

public class ObjectVersionRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;Username=minipdm;Password=minipdm_dev";

    [Fact]
    public async Task InsertAsync_PersistsObjectVersionInsideTransaction()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var objectId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.001",
            "Тестовая деталь версии",
            CancellationToken.None);

        var versionId = await versionRepository.InsertAsync(
            session,
            objectId,
            1,
            VersionState.InWork,
            "Сталь 40Х",
            5.86m,
            CancellationToken.None);

        var storedVersion =
            await session.Connection.QuerySingleAsync<StoredObjectVersion>(
                new CommandDefinition(
                    """
                    SELECT
                        id,
                        object_id AS ObjectId,
                        version_no AS VersionNumber,
                        state,
                        material,
                        mass_kg AS MassKg
                    FROM object_version
                    WHERE id = @Id;
                    """,
                    new { Id = versionId },
                    transaction: session.Transaction,
                    cancellationToken: CancellationToken.None));

        Assert.Equal(versionId, storedVersion.Id);
        Assert.Equal(objectId, storedVersion.ObjectId);
        Assert.Equal(1, storedVersion.VersionNumber);
        Assert.Equal("InWork", storedVersion.State);
        Assert.Equal("Сталь 40Х", storedVersion.Material);
        Assert.Equal(5.86m, storedVersion.MassKg);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task SetCurrentVersionAsync_SetsCurrentVersionForObject()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var objectId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.002",
            "Тестовая деталь с текущей версией",
            CancellationToken.None);

        var versionId = await versionRepository.InsertAsync(
            session,
            objectId,
            1,
            VersionState.InWork,
            "Сталь 40Х",
            3.25m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            versionId,
            CancellationToken.None);

        var currentVersionId =
            await session.Connection.QuerySingleAsync<long?>(
                new CommandDefinition(
                    """
                    SELECT current_version_id
                    FROM pdm_object
                    WHERE id = @ObjectId;
                    """,
                    new { ObjectId = objectId },
                    transaction: session.Transaction,
                    cancellationToken: CancellationToken.None));

        Assert.Equal(versionId, currentVersionId);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsCurrentVersionSnapshot()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var objectId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.003",
            "Тестовая деталь для чтения версии",
            CancellationToken.None);

        var versionId = await versionRepository.InsertAsync(
            session,
            objectId,
            1,
            VersionState.InWork,
            "Сталь 20",
            7.45m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            versionId,
            CancellationToken.None);

        var currentVersion = await versionRepository.GetCurrentAsync(
            session,
            objectId,
            CancellationToken.None);

        Assert.NotNull(currentVersion);
        Assert.Equal(versionId, currentVersion.Id);
        Assert.Equal(objectId, currentVersion.ObjectId);
        Assert.Equal(1, currentVersion.VersionNumber);
        Assert.Equal(VersionState.InWork, currentVersion.State);
        Assert.Equal("Сталь 20", currentVersion.Material);
        Assert.Equal(7.45m, currentVersion.MassKg);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task UpdateContentAsync_UpdatesExistingInWorkVersion()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var objectId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.004",
            "Тестовая деталь для обновления",
            CancellationToken.None);

        var versionId = await versionRepository.InsertAsync(
            session,
            objectId,
            1,
            VersionState.InWork,
            "Сталь 20",
            5.86m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            versionId,
            CancellationToken.None);

        await versionRepository.UpdateContentAsync(
            session,
            versionId,
            "Сталь 20",
            5.92m,
            CancellationToken.None);

        var currentVersion = await versionRepository.GetCurrentAsync(
            session,
            objectId,
            CancellationToken.None);

        Assert.NotNull(currentVersion);
        Assert.Equal(versionId, currentVersion.Id);
        Assert.Equal(1, currentVersion.VersionNumber);
        Assert.Equal(VersionState.InWork, currentVersion.State);
        Assert.Equal("Сталь 20", currentVersion.Material);
        Assert.Equal(5.92m, currentVersion.MassKg);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task ChangeStateAsync_ChangesInWorkToApproved()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var objectId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.005",
            "Тестовая деталь для смены состояния",
            CancellationToken.None);

        var versionId = await versionRepository.InsertAsync(
            session,
            objectId,
            1,
            VersionState.InWork,
            "Сталь 20",
            4.50m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            versionId,
            CancellationToken.None);

        await versionRepository.ChangeStateAsync(
            session,
            versionId,
            VersionState.InWork,
            VersionState.Approved,
            CancellationToken.None);

        var currentVersion = await versionRepository.GetCurrentAsync(
            session,
            objectId,
            CancellationToken.None);

        Assert.NotNull(currentVersion);
        Assert.Equal(VersionState.Approved, currentVersion.State);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task UpdateContentAsync_ThrowsForApprovedVersion()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var objectId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.006",
            "Утверждённая тестовая деталь",
            CancellationToken.None);

        var versionId = await versionRepository.InsertAsync(
            session,
            objectId,
            1,
            VersionState.InWork,
            "Сталь 20",
            5.86m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            versionId,
            CancellationToken.None);

        await versionRepository.ChangeStateAsync(
            session,
            versionId,
            VersionState.InWork,
            VersionState.Approved,
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => versionRepository.UpdateContentAsync(
                session,
                versionId,
                "Сталь 20",
                5.92m,
                CancellationToken.None));

        Assert.Contains("is not InWork", exception.Message);

        var currentVersion = await versionRepository.GetCurrentAsync(
            session,
            objectId,
            CancellationToken.None);

        Assert.NotNull(currentVersion);
        Assert.Equal(VersionState.Approved, currentVersion.State);
        Assert.Equal(5.86m, currentVersion.MassKg);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task ReimportApprovedVersion_CreatesNewInWorkVersionAndMakesItCurrent()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var objectId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.007",
            "Тестовая деталь с новой версией",
            CancellationToken.None);

        var version1Id = await versionRepository.InsertAsync(
            session,
            objectId,
            1,
            VersionState.InWork,
            "Сталь 20",
            5.86m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            version1Id,
            CancellationToken.None);

        await versionRepository.ChangeStateAsync(
            session,
            version1Id,
            VersionState.InWork,
            VersionState.Approved,
            CancellationToken.None);

        var approvedVersion = await versionRepository.GetCurrentAsync(
            session,
            objectId,
            CancellationToken.None);

        Assert.NotNull(approvedVersion);
        Assert.Equal(VersionState.Approved, approvedVersion.State);

        var version2Id = await versionRepository.InsertAsync(
            session,
            objectId,
            approvedVersion.VersionNumber + 1,
            VersionState.InWork,
            "Сталь 20",
            5.92m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            version2Id,
            CancellationToken.None);

        var currentVersion = await versionRepository.GetCurrentAsync(
            session,
            objectId,
            CancellationToken.None);

        Assert.NotNull(currentVersion);
        Assert.Equal(version2Id, currentVersion.Id);
        Assert.Equal(2, currentVersion.VersionNumber);
        Assert.Equal(VersionState.InWork, currentVersion.State);
        Assert.Equal("Сталь 20", currentVersion.Material);
        Assert.Equal(5.92m, currentVersion.MassKg);

        var versionCount = await session.Connection.QuerySingleAsync<int>(
            new CommandDefinition(
                """
            SELECT COUNT(*)
            FROM object_version
            WHERE object_id = @ObjectId;
            """,
                new { ObjectId = objectId },
                transaction: session.Transaction,
                cancellationToken: CancellationToken.None));

        Assert.Equal(2, versionCount);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    private sealed class StoredObjectVersion
    {
        public long Id { get; init; }

        public long ObjectId { get; init; }

        public int VersionNumber { get; init; }

        public string State { get; init; } = string.Empty;

        public string? Material { get; init; }

        public decimal? MassKg { get; init; }
    }
}