using Dapper;
using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Import;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence;
using MiniPdm.Infrastructure.Persistence.Connections;

namespace MiniPdm.IntegrationTests.Persistence;

public class CadImportServicePersistenceIntegrationTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;" +
        "Username=minipdm;Password=minipdm_dev";

    private const string TestDesignation =
        "ТЕСТ.991001.001";

    [Fact]
    public async Task ImportAsync_NewPart_DoesNotAttemptToWriteBom()
    {
        var connectionFactory =
            new NpgsqlConnectionFactory(
                ConnectionString);

        await CleanupAsync(
            connectionFactory);

        try
        {
            var unitOfWorkFactory =
                new PdmImportUnitOfWorkFactory(
                    connectionFactory);

            var service =
                new CadImportService(
                    unitOfWorkFactory);

            var document =
                new CadDocument(
                    1,
                    "Интеграционная деталь.m3d",
                    ObjectType.Part,
                    TestDesignation,
                    "Интеграционная деталь",
                    "Сталь 45",
                    2.50m,
                    Array.Empty<CadComponent>());

            var result =
                await service.ImportAsync(
                    new[] { document },
                    CancellationToken.None);

            var item =
                Assert.Single(
                    result.Items);

            Assert.Equal(
                CadPersistenceAction.Created,
                item.Action);

            Assert.Equal(
                1,
                result.CreatedCount);

            Assert.Equal(
                0,
                result.UpdatedCount);

            Assert.Equal(
                0,
                result.NewVersionCount);

            Assert.Equal(
                0,
                result.UnchangedCount);

            await using var connection =
                await connectionFactory.OpenConnectionAsync(
                    CancellationToken.None);

            const string sql =
                """
                SELECT COUNT(*)
                FROM bom_link link
                INNER JOIN object_version version
                    ON version.id = link.parent_version_id
                INNER JOIN pdm_object object
                    ON object.id = version.object_id
                WHERE object.designation = @Designation;
                """;

            var bomLinkCount =
                await connection.QuerySingleAsync<int>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            Designation =
                                TestDesignation
                        },
                        cancellationToken:
                            CancellationToken.None));

            Assert.Equal(
                0,
                bomLinkCount);
        }
        finally
        {
            await CleanupAsync(
                connectionFactory);
        }
    }

    private static async Task CleanupAsync(
        IDbConnectionFactory connectionFactory)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                CancellationToken.None);

        const string sql =
            """
            DELETE FROM pdm_object
            WHERE designation = @Designation;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Designation =
                        TestDesignation
                },
                cancellationToken:
                    CancellationToken.None));
    }
}