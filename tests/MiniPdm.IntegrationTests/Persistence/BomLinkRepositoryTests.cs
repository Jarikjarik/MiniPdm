using Dapper;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.IntegrationTests.Persistence;

public class BomLinkRepositoryTests
{
    private static string ConnectionString =>
        TestDatabase.ConnectionString;

    [Fact]
    public async Task InsertAsync_PersistsBomLinkInsideTransaction()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);

        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();
        var bomRepository = new BomLinkRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var assemblyId = await objectRepository.InsertAsync(
            session,
            ObjectType.Assembly,
            "ТЕСТ.654321.008",
            "Тестовая сборка",
            CancellationToken.None);

        var assemblyVersionId = await versionRepository.InsertAsync(
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

        var partId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.009",
            "Тестовая дочерняя деталь",
            CancellationToken.None);

        var partVersionId = await versionRepository.InsertAsync(
            session,
            partId,
            1,
            VersionState.InWork,
            "Сталь 20",
            2.50m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            partId,
            partVersionId,
            CancellationToken.None);

        var bomLinkId = await bomRepository.InsertAsync(
            session,
            assemblyVersionId,
            partId,
            3,
            CancellationToken.None);

        var storedLink =
            await session.Connection.QuerySingleAsync<StoredBomLink>(
                new CommandDefinition(
                    """
                    SELECT
                        id,
                        parent_version_id AS ParentVersionId,
                        child_object_id AS ChildObjectId,
                        quantity
                    FROM bom_link
                    WHERE id = @Id;
                    """,
                    new { Id = bomLinkId },
                    transaction: session.Transaction,
                    cancellationToken: CancellationToken.None));

        Assert.Equal(bomLinkId, storedLink.Id);
        Assert.Equal(assemblyVersionId, storedLink.ParentVersionId);
        Assert.Equal(partId, storedLink.ChildObjectId);
        Assert.Equal(3, storedLink.Quantity);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task InsertAsync_ThrowsWhenQuantityIsNotPositive()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var bomRepository = new BomLinkRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => bomRepository.InsertAsync(
                session,
                parentVersionId: 1,
                childObjectId: 1,
                quantity: 0,
                CancellationToken.None));

        Assert.Equal("quantity", exception.ParamName);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTreeAsync_ReturnsRecursiveTreeWithTotalQuantity()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);

        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();
        var bomRepository = new BomLinkRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var rootAssemblyId = await objectRepository.InsertAsync(
            session,
            ObjectType.Assembly,
            "ТЕСТ.654321.010",
            "Сборка A",
            CancellationToken.None);

        var rootVersionId = await versionRepository.InsertAsync(
            session,
            rootAssemblyId,
            1,
            VersionState.InWork,
            null,
            null,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            rootAssemblyId,
            rootVersionId,
            CancellationToken.None);

        var childAssemblyId = await objectRepository.InsertAsync(
            session,
            ObjectType.Assembly,
            "ТЕСТ.654321.011",
            "Сборка B",
            CancellationToken.None);

        var childAssemblyVersionId = await versionRepository.InsertAsync(
            session,
            childAssemblyId,
            1,
            VersionState.InWork,
            null,
            null,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            childAssemblyId,
            childAssemblyVersionId,
            CancellationToken.None);

        var partId = await objectRepository.InsertAsync(
            session,
            ObjectType.Part,
            "ТЕСТ.654321.012",
            "Деталь C",
            CancellationToken.None);

        var partVersionId = await versionRepository.InsertAsync(
            session,
            partId,
            1,
            VersionState.InWork,
            "Сталь 20",
            2.50m,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            partId,
            partVersionId,
            CancellationToken.None);

        await bomRepository.InsertAsync(
            session,
            rootVersionId,
            childAssemblyId,
            3,
            CancellationToken.None);

        await bomRepository.InsertAsync(
            session,
            childAssemblyVersionId,
            partId,
            4,
            CancellationToken.None);

        var tree = await bomRepository.GetTreeAsync(
            session,
            rootAssemblyId,
            CancellationToken.None);

        Assert.Equal(2, tree.Count);

        var assemblyNode = tree.Single(
            node => node.ObjectId == childAssemblyId);

        Assert.Equal(1, assemblyNode.Depth);
        Assert.Equal(3, assemblyNode.Quantity);
        Assert.Equal(3, assemblyNode.TotalQuantity);
        Assert.Equal(ObjectType.Assembly, assemblyNode.ObjectType);
        Assert.False(assemblyNode.IsCycle);

        var partNode = tree.Single(
            node => node.ObjectId == partId);

        Assert.Equal(2, partNode.Depth);
        Assert.Equal(4, partNode.Quantity);
        Assert.Equal(12, partNode.TotalQuantity);
        Assert.Equal(ObjectType.Part, partNode.ObjectType);
        Assert.Equal(2.50m, partNode.MassKg);
        Assert.False(partNode.IsCycle);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task GetTreeAsync_DetectsCycleAndStopsRecursion()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);

        var objectRepository = new PdmObjectRepository();
        var versionRepository = new ObjectVersionRepository();
        var bomRepository = new BomLinkRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var assemblyAId = await objectRepository.InsertAsync(
            session,
            ObjectType.Assembly,
            "ТЕСТ.654321.013",
            "Циклическая сборка A",
            CancellationToken.None);

        var assemblyAVersionId = await versionRepository.InsertAsync(
            session,
            assemblyAId,
            1,
            VersionState.InWork,
            null,
            null,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            assemblyAId,
            assemblyAVersionId,
            CancellationToken.None);

        var assemblyBId = await objectRepository.InsertAsync(
            session,
            ObjectType.Assembly,
            "ТЕСТ.654321.014",
            "Циклическая сборка B",
            CancellationToken.None);

        var assemblyBVersionId = await versionRepository.InsertAsync(
            session,
            assemblyBId,
            1,
            VersionState.InWork,
            null,
            null,
            CancellationToken.None);

        await objectRepository.SetCurrentVersionAsync(
            session,
            assemblyBId,
            assemblyBVersionId,
            CancellationToken.None);

        await bomRepository.InsertAsync(
            session,
            assemblyAVersionId,
            assemblyBId,
            2,
            CancellationToken.None);

        await bomRepository.InsertAsync(
            session,
            assemblyBVersionId,
            assemblyAId,
            3,
            CancellationToken.None);

        var tree = await bomRepository.GetTreeAsync(
            session,
            assemblyAId,
            CancellationToken.None);

        Assert.Equal(2, tree.Count);

        var assemblyBNode = tree.Single(
            node => node.ObjectId == assemblyBId);

        Assert.Equal(1, assemblyBNode.Depth);
        Assert.Equal(2, assemblyBNode.Quantity);
        Assert.Equal(2, assemblyBNode.TotalQuantity);
        Assert.False(assemblyBNode.IsCycle);

        var cycleNode = tree.Single(
            node => node.ObjectId == assemblyAId);

        Assert.Equal(2, cycleNode.Depth);
        Assert.Equal(3, cycleNode.Quantity);
        Assert.Equal(6, cycleNode.TotalQuantity);
        Assert.True(cycleNode.IsCycle);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    private sealed class StoredBomLink
    {
        public long Id { get; init; }

        public long ParentVersionId { get; init; }

        public long ChildObjectId { get; init; }

        public int Quantity { get; init; }
    }
}
