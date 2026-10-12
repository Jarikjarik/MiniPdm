using Dapper;
using MiniPdm.Infrastructure.Persistence.Connections;

namespace MiniPdm.IntegrationTests.Persistence;

public class NpgsqlConnectionFactoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;Username=minipdm;Password=minipdm_dev";

    [Fact]
    public async Task OpenConnectionAsync_CanExecuteQuery()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);

        await using var connection = await factory.OpenConnectionAsync(
            CancellationToken.None);

        var result = await connection.QuerySingleAsync<int>(
            "SELECT 1;");

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task BeginSessionAsync_RollbackDoesNotPersistChanges()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var objectName = $"Rollback test {Guid.NewGuid():N}";

        long objectId;

        await using (var session = await factory.BeginSessionAsync(
                         CancellationToken.None))
        {
            var insertCommand = new CommandDefinition(
                """
            INSERT INTO pdm_object (
                object_type,
                designation,
                name
            )
            VALUES (
                'StandardPart',
                NULL,
                @Name
            )
            RETURNING id;
            """,
                new { Name = objectName },
                transaction: session.Transaction,
                cancellationToken: CancellationToken.None);

            objectId = await session.Connection.QuerySingleAsync<long>(
                insertCommand);

            await session.RollbackAsync(
                CancellationToken.None);
        }

        await using var verificationConnection =
            await factory.OpenConnectionAsync(
                CancellationToken.None);

        var count = await verificationConnection.QuerySingleAsync<int>(
            """
        SELECT COUNT(*)
        FROM pdm_object
        WHERE id = @Id;
        """,
            new { Id = objectId });

        Assert.Equal(0, count);
    }
}