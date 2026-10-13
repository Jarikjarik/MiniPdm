using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Queries;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.IntegrationTests.Persistence;

public class PdmCatalogReaderIntegrationTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;Username=minipdm;Password=minipdm_dev";

    [Fact]
    public async Task SearchAsync_ByName_ReturnsMatchingObject()
    {
        var factory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        var objectRepository =
            new PdmObjectRepository();

        var versionRepository =
            new ObjectVersionRepository();

        await using var session =
            await factory.BeginSessionAsync(
                CancellationToken.None);

        var objectId =
            await objectRepository.InsertAsync(
                session,
                ObjectType.Part,
                "ПОИС.900001.001",
                "Уникальный поисковый вал",
                CancellationToken.None);

        var versionId =
            await versionRepository.InsertAsync(
                session,
                objectId,
                1,
                VersionState.InWork,
                "Сталь",
                4.20m,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            versionId,
            CancellationToken.None);

        await session.CommitAsync(
            CancellationToken.None);

        try
        {
            var reader =
                new PostgresPdmCatalogReader(
                    factory);

            var results =
                await reader.SearchAsync(
                    "поисковый вал",
                    CancellationToken.None);

            var item =
                Assert.Single(
                    results.Where(
                        item => item.ObjectId == objectId));

            Assert.Equal(
                ObjectType.Part,
                item.ObjectType);

            Assert.Equal(
                "ПОИС.900001.001",
                item.Designation);

            Assert.Equal(
                "Уникальный поисковый вал",
                item.Name);

            Assert.Equal(
                1,
                item.CurrentVersionNumber);

            Assert.Equal(
                VersionState.InWork,
                item.CurrentState);
        }
        finally
        {
            await CleanupAsync(
                factory,
                objectId);
        }
    }

    [Fact]
    public async Task SearchAsync_ByDesignation_ReturnsMatchingObject()
    {
        var factory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        var objectRepository =
            new PdmObjectRepository();

        var versionRepository =
            new ObjectVersionRepository();

        await using var session =
            await factory.BeginSessionAsync(
                CancellationToken.None);

        var objectId =
            await objectRepository.InsertAsync(
                session,
                ObjectType.Part,
                "ПОИС.900002.001",
                "Деталь поиска",
                CancellationToken.None);

        var versionId =
            await versionRepository.InsertAsync(
                session,
                objectId,
                1,
                VersionState.InWork,
                "Сталь",
                1.10m,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            versionId,
            CancellationToken.None);

        await session.CommitAsync(
            CancellationToken.None);

        try
        {
            var reader =
                new PostgresPdmCatalogReader(
                    factory);

            var results =
                await reader.SearchAsync(
                    "900002",
                    CancellationToken.None);

            Assert.Contains(
                results,
                item =>
                    item.ObjectId == objectId &&
                    item.Designation == "ПОИС.900002.001");
        }
        finally
        {
            await CleanupAsync(
                factory,
                objectId);
        }
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsVersionHistoryAndCurrentVersion()
    {
        var factory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        var objectRepository =
            new PdmObjectRepository();

        var versionRepository =
            new ObjectVersionRepository();

        await using var session =
            await factory.BeginSessionAsync(
                CancellationToken.None);

        var objectId =
            await objectRepository.InsertAsync(
                session,
                ObjectType.Part,
                "ПОИС.900003.001",
                "Версионная деталь",
                CancellationToken.None);

        var version1Id =
            await versionRepository.InsertAsync(
                session,
                objectId,
                1,
                VersionState.InWork,
                "Сталь",
                1.00m,
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

        var version2Id =
            await versionRepository.InsertAsync(
                session,
                objectId,
                2,
                VersionState.InWork,
                "Сталь",
                1.20m,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            version2Id,
            CancellationToken.None);

        await session.CommitAsync(
            CancellationToken.None);

        try
        {
            var reader =
                new PostgresPdmCatalogReader(
                    factory);

            var details =
                await reader.GetDetailsAsync(
                    objectId,
                    CancellationToken.None);

            Assert.NotNull(
                details);

            Assert.Equal(
                objectId,
                details!.ObjectId);

            Assert.Equal(
                version2Id,
                details.CurrentVersionId);

            Assert.Equal(
                2,
                details.Versions.Count);

            Assert.NotNull(
                details.CurrentVersion);

            Assert.Equal(
                2,
                details.CurrentVersion!.VersionNumber);

            Assert.Equal(
                VersionState.InWork,
                details.CurrentVersion.State);

            var version1 =
                details.Versions.Single(
                    version =>
                        version.VersionNumber == 1);

            Assert.Equal(
                VersionState.Approved,
                version1.State);

            Assert.Equal(
                1.00m,
                version1.MassKg);
        }
        finally
        {
            await CleanupAsync(
                factory,
                objectId);
        }
    }

    [Fact]
    public async Task GetDetailsAsync_UnknownObject_ReturnsNull()
    {
        var factory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        var reader =
            new PostgresPdmCatalogReader(
                factory);

        var details =
            await reader.GetDetailsAsync(
                long.MaxValue,
                CancellationToken.None);

        Assert.Null(
            details);
    }

    private static async Task CleanupAsync(
        IDbConnectionFactory connectionFactory,
        long objectId)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                CancellationToken.None);

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            """
            DELETE FROM pdm_object
            WHERE id = @ObjectId;
            """;

        var parameter =
            command.CreateParameter();

        parameter.ParameterName =
            "ObjectId";

        parameter.Value =
            objectId;

        command.Parameters.Add(
            parameter);

        await command.ExecuteNonQueryAsync();
    }
}