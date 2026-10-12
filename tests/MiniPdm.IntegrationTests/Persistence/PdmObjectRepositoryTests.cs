using Dapper;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.IntegrationTests.Persistence;

public class PdmObjectRepositoryTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;Username=minipdm;Password=minipdm_dev";

    [Fact]
    public async Task InsertAsync_PersistsObjectInsideTransaction()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var repository = new PdmObjectRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var name = $"Test bearing {Guid.NewGuid():N}";

        var id = await repository.InsertAsync(
            session,
            ObjectType.StandardPart,
            null,
            name,
            CancellationToken.None);

        var storedObject = await session.Connection.QuerySingleAsync<StoredPdmObject>(
            new CommandDefinition(
                """
                SELECT
                    id,
                    object_type AS ObjectType,
                    designation,
                    name
                FROM pdm_object
                WHERE id = @Id;
                """,
                new { Id = id },
                transaction: session.Transaction,
                cancellationToken: CancellationToken.None));

        Assert.Equal(id, storedObject.Id);
        Assert.Equal("StandardPart", storedObject.ObjectType);
        Assert.Null(storedObject.Designation);
        Assert.Equal(name, storedObject.Name);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task FindIdAsync_ForAssemblyOrPart_FindsByDesignation()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var repository = new PdmObjectRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        const string designation = "ТЕСТ.123456.001";

        var insertedId = await repository.InsertAsync(
            session,
            ObjectType.Part,
            designation,
            "Тестовая деталь",
            CancellationToken.None);

        var foundId = await repository.FindIdAsync(
            session,
            ObjectType.Assembly,
            designation,
            "Другое имя не имеет значения",
            CancellationToken.None);

        Assert.Equal(insertedId, foundId);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    [Fact]
    public async Task FindIdAsync_ForStandardPart_FindsByName()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        var repository = new PdmObjectRepository();

        await using var session = await factory.BeginSessionAsync(
            CancellationToken.None);

        var name = $"Test standard part {Guid.NewGuid():N}";

        var insertedId = await repository.InsertAsync(
            session,
            ObjectType.StandardPart,
            null,
            name,
            CancellationToken.None);

        var foundId = await repository.FindIdAsync(
            session,
            ObjectType.StandardPart,
            null,
            name,
            CancellationToken.None);

        Assert.Equal(insertedId, foundId);

        await session.RollbackAsync(
            CancellationToken.None);
    }

    private sealed class StoredPdmObject
    {
        public long Id { get; init; }

        public string ObjectType { get; init; } = string.Empty;

        public string? Designation { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}