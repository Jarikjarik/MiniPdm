using MiniPdm.Application.Persistence.Models;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.IntegrationTests.Persistence;

public class PersistenceInvariantTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;Username=minipdm;Password=minipdm_dev";

    [Fact]
    public async Task SetCurrentVersionAsync_OlderLiveVersionExists_Throws()
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
                "ТЕСТ.888888.001",
                "Деталь с двумя версиями",
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
                1.10m,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            objectId,
            version2Id,
            CancellationToken.None);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => objectRepository.SetCurrentVersionAsync(
                    session,
                    objectId,
                    version1Id,
                    CancellationToken.None));

        Assert.Contains(
            "latest non-cancelled",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        var current =
            await versionRepository.GetCurrentAsync(
                session,
                objectId,
                CancellationToken.None);

        Assert.NotNull(
            current);

        Assert.Equal(
            version2Id,
            current!.Id);

        Assert.Equal(
            2,
            current.VersionNumber);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task ReplaceBomLinksAsync_ApprovedAssemblyVersion_ThrowsAndPreservesBom()
    {
        var factory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        var objectRepository =
            new PdmObjectRepository();

        var versionRepository =
            new ObjectVersionRepository();

        var bomRepository =
            new BomLinkRepository();

        await using var session =
            await factory.BeginSessionAsync(
                CancellationToken.None);

        var assemblyId =
            await objectRepository.InsertAsync(
                session,
                ObjectType.Assembly,
                "ТЕСТ.888888.002",
                "Утверждённая сборка",
                CancellationToken.None);

        var assemblyVersionId =
            await versionRepository.InsertAsync(
                session,
                assemblyId,
                1,
                VersionState.InWork,
                null,
                null,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            assemblyId,
            assemblyVersionId,
            CancellationToken.None);

        var partId =
            await objectRepository.InsertAsync(
                session,
                ObjectType.Part,
                "ТЕСТ.888888.003",
                "Дочерняя деталь",
                CancellationToken.None);

        var partVersionId =
            await versionRepository.InsertAsync(
                session,
                partId,
                1,
                VersionState.InWork,
                "Сталь",
                2.00m,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            partId,
            partVersionId,
            CancellationToken.None);

        await bomRepository.ReplaceBomLinksAsync(
            session,
            assemblyVersionId,
            new[]
            {
                new StoredBomLink(
                    partId,
                    1)
            },
            CancellationToken.None);

        await versionRepository.ChangeStateAsync(
            session,
            assemblyVersionId,
            VersionState.InWork,
            VersionState.Approved,
            CancellationToken.None);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => bomRepository.ReplaceBomLinksAsync(
                    session,
                    assemblyVersionId,
                    new[]
                    {
                        new StoredBomLink(
                            partId,
                            5)
                    },
                    CancellationToken.None));

        Assert.Contains(
            "InWork",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        var storedBom =
            await bomRepository.GetBomLinksAsync(
                session,
                assemblyVersionId,
                CancellationToken.None);

        var storedLink =
            Assert.Single(
                storedBom);

        Assert.Equal(
            partId,
            storedLink.ChildObjectId);

        Assert.Equal(
            1,
            storedLink.Quantity);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTreeAsync_ChildWithoutCurrentVersion_ReturnsNodeInsteadOfDroppingIt()
    {
        var factory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        var objectRepository =
            new PdmObjectRepository();

        var versionRepository =
            new ObjectVersionRepository();

        var bomRepository =
            new BomLinkRepository();

        await using var session =
            await factory.BeginSessionAsync(
                CancellationToken.None);

        var assemblyId =
            await objectRepository.InsertAsync(
                session,
                ObjectType.Assembly,
                "ТЕСТ.888888.004",
                "Корневая сборка",
                CancellationToken.None);

        var assemblyVersionId =
            await versionRepository.InsertAsync(
                session,
                assemblyId,
                1,
                VersionState.InWork,
                null,
                null,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            assemblyId,
            assemblyVersionId,
            CancellationToken.None);

        var partId =
            await objectRepository.InsertAsync(
                session,
                ObjectType.Part,
                "ТЕСТ.888888.005",
                "Отменённая деталь",
                CancellationToken.None);

        var partVersionId =
            await versionRepository.InsertAsync(
                session,
                partId,
                1,
                VersionState.InWork,
                "Сталь",
                3.00m,
                CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            partId,
            partVersionId,
            CancellationToken.None);

        await bomRepository.InsertAsync(
            session,
            assemblyVersionId,
            partId,
            2,
            CancellationToken.None);

        await versionRepository.ChangeStateAsync(
            session,
            partVersionId,
            VersionState.InWork,
            VersionState.Cancelled,
            CancellationToken.None);

        await objectRepository.RefreshCurrentVersionAsync(
            session,
            partId,
            CancellationToken.None);

        var currentPartVersion =
            await versionRepository.GetCurrentAsync(
                session,
                partId,
                CancellationToken.None);

        Assert.Null(
            currentPartVersion);

        var tree =
            await bomRepository.GetTreeAsync(
                session,
                assemblyId,
                CancellationToken.None);

        var partNode =
            Assert.Single(
                tree);

        Assert.Equal(
            partId,
            partNode.ObjectId);

        Assert.Equal(
            ObjectType.Part,
            partNode.ObjectType);

        Assert.Equal(
            2,
            partNode.Quantity);

        Assert.Equal(
            2,
            partNode.TotalQuantity);

        Assert.Null(
            partNode.VersionId);

        Assert.Null(
            partNode.State);

        Assert.Null(
            partNode.MassKg);

        Assert.False(
            partNode.HasCurrentVersion);

        await session.RollbackAsync(
            CancellationToken.None);
    }
}