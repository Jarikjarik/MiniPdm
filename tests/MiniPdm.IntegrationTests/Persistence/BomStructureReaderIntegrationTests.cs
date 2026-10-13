using Dapper;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Queries;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.IntegrationTests.Persistence;

public class BomStructureReaderIntegrationTests
{
    private static string ConnectionString =>
        TestDatabase.ConnectionString;

    [Fact]
    public async Task ReadAsync_NestedAssembly_ReturnsRootAndRecursiveNodes()
    {
        var designations = new[]
        {
            "КАЛК.900001.001",
            "КАЛК.900001.002",
            "КАЛК.900001.003"
        };

        var connectionFactory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        await CleanupAsync(
            connectionFactory,
            designations);

        try
        {
            var objectRepository =
                new PdmObjectRepository();

            var versionRepository =
                new ObjectVersionRepository();

            var bomRepository =
                new BomLinkRepository();

            long rootId;
            long childAssemblyId;
            long partId;

            await using (var session =
                         await connectionFactory.BeginSessionAsync(
                             CancellationToken.None))
            {
                rootId =
                    await objectRepository.InsertAsync(
                        session,
                        ObjectType.Assembly,
                        designations[0],
                        "Корневая сборка",
                        CancellationToken.None);

                var rootVersionId =
                    await versionRepository.InsertAsync(
                        session,
                        rootId,
                        1,
                        VersionState.InWork,
                        null,
                        null,
                        CancellationToken.None);

                await objectRepository.SetCurrentVersionAsync(
                    session,
                    rootId,
                    rootVersionId,
                    CancellationToken.None);

                childAssemblyId =
                    await objectRepository.InsertAsync(
                        session,
                        ObjectType.Assembly,
                        designations[1],
                        "Дочерняя сборка",
                        CancellationToken.None);

                var childAssemblyVersionId =
                    await versionRepository.InsertAsync(
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

                partId =
                    await objectRepository.InsertAsync(
                        session,
                        ObjectType.Part,
                        designations[2],
                        "Вал",
                        CancellationToken.None);

                var partVersionId =
                    await versionRepository.InsertAsync(
                        session,
                        partId,
                        1,
                        VersionState.InWork,
                        "Сталь 45",
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
                    2,
                    CancellationToken.None);

                await bomRepository.InsertAsync(
                    session,
                    childAssemblyVersionId,
                    partId,
                    3,
                    CancellationToken.None);

                await session.CommitAsync(
                    CancellationToken.None);
            }

            var reader =
                new PostgresBomStructureReader(
                    connectionFactory,
                    new BomLinkRepository());

            var structure =
                await reader.ReadAsync(
                    rootId,
                    CancellationToken.None);

            Assert.NotNull(
                structure);

            Assert.Equal(
                rootId,
                structure!.Root.ObjectId);

            Assert.Equal(
                ObjectType.Assembly,
                structure.Root.ObjectType);

            Assert.Equal(
                1,
                structure.Root.VersionNumber);

            Assert.Equal(
                VersionState.InWork,
                structure.Root.State);

            Assert.Equal(
                2,
                structure.Nodes.Count);

            var assemblyNode =
                structure.Nodes.Single(
                    node =>
                        node.ObjectId ==
                        childAssemblyId);

            Assert.Equal(
                1,
                assemblyNode.Depth);

            Assert.Equal(
                2,
                assemblyNode.Quantity);

            Assert.Equal(
                2,
                assemblyNode.TotalQuantity);

            Assert.True(
                assemblyNode.HasCurrentVersion);

            var partNode =
                structure.Nodes.Single(
                    node =>
                        node.ObjectId == partId);

            Assert.Equal(
                2,
                partNode.Depth);

            Assert.Equal(
                3,
                partNode.Quantity);

            Assert.Equal(
                6,
                partNode.TotalQuantity);

            Assert.Equal(
                2.50m,
                partNode.MassKg);

            Assert.True(
                partNode.HasCurrentVersion);
        }
        finally
        {
            await CleanupAsync(
                connectionFactory,
                designations);
        }
    }

    [Fact]
    public async Task ReadAsync_RootWithoutCurrentVersion_ReturnsRootInsteadOfNull()
    {
        var designations = new[]
        {
            "КАЛК.900002.001"
        };

        var connectionFactory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        await CleanupAsync(
            connectionFactory,
            designations);

        try
        {
            var objectRepository =
                new PdmObjectRepository();

            var versionRepository =
                new ObjectVersionRepository();

            long objectId;

            await using (var session =
                         await connectionFactory.BeginSessionAsync(
                             CancellationToken.None))
            {
                objectId =
                    await objectRepository.InsertAsync(
                        session,
                        ObjectType.Part,
                        designations[0],
                        "Отменённая деталь",
                        CancellationToken.None);

                var versionId =
                    await versionRepository.InsertAsync(
                        session,
                        objectId,
                        1,
                        VersionState.InWork,
                        "Сталь",
                        1.25m,
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
                    VersionState.Cancelled,
                    CancellationToken.None);

                await objectRepository.RefreshCurrentVersionAsync(
                    session,
                    objectId,
                    CancellationToken.None);

                await session.CommitAsync(
                    CancellationToken.None);
            }

            var reader =
                new PostgresBomStructureReader(
                    connectionFactory,
                    new BomLinkRepository());

            var structure =
                await reader.ReadAsync(
                    objectId,
                    CancellationToken.None);

            Assert.NotNull(
                structure);

            Assert.Equal(
                objectId,
                structure!.Root.ObjectId);

            Assert.False(
                structure.Root.HasCurrentVersion);

            Assert.Null(
                structure.Root.VersionId);

            Assert.Null(
                structure.Root.VersionNumber);

            Assert.Null(
                structure.Root.State);

            Assert.Null(
                structure.Root.MassKg);

            Assert.Empty(
                structure.Nodes);
        }
        finally
        {
            await CleanupAsync(
                connectionFactory,
                designations);
        }
    }

    [Fact]
    public async Task ReadAsync_UnknownObject_ReturnsNull()
    {
        var connectionFactory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        var reader =
            new PostgresBomStructureReader(
                connectionFactory,
                new BomLinkRepository());

        var structure =
            await reader.ReadAsync(
                long.MaxValue,
                CancellationToken.None);

        Assert.Null(
            structure);
    }

    private static async Task CleanupAsync(
        IDbConnectionFactory connectionFactory,
        IReadOnlyCollection<string> designations)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                CancellationToken.None);

        const string sql =
            """
            DELETE FROM bom_link
            WHERE parent_version_id IN
            (
                SELECT v.id
                FROM object_version v
                INNER JOIN pdm_object o
                    ON o.id = v.object_id
                WHERE o.designation = ANY(@Designations)
            )
            OR child_object_id IN
            (
                SELECT o.id
                FROM pdm_object o
                WHERE o.designation = ANY(@Designations)
            );

            DELETE FROM pdm_object
            WHERE designation = ANY(@Designations);
            """;

        var command =
            new CommandDefinition(
                sql,
                new
                {
                    Designations =
                        designations.ToArray()
                },
                cancellationToken:
                    CancellationToken.None);

        await connection.ExecuteAsync(
            command);
    }
}
