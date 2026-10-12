using Dapper;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Models;

namespace MiniPdm.Infrastructure.Persistence.Repositories;

public sealed class PdmObjectRepository
{
    public async Task<long> InsertAsync(
        IDbSession session,
        ObjectType objectType,
        string? designation,
        string name,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        const string sql =
            """
            INSERT INTO pdm_object (
                object_type,
                designation,
                name
            )
            VALUES (
                @ObjectType,
                @Designation,
                @Name
            )
            RETURNING id;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectType = objectType.ToString(),
                Designation = designation,
                Name = name
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QuerySingleAsync<long>(
            command);
    }

    public async Task<long?> FindIdAsync(
        IDbSession session,
        ObjectType objectType,
        string? designation,
        string name,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        const string sql =
            """
            SELECT id
            FROM pdm_object
            WHERE
                (
                    @ObjectType IN ('Assembly', 'Part')
                    AND designation = @Designation
                )
                OR
                (
                    @ObjectType = 'StandardPart'
                    AND object_type = 'StandardPart'
                    AND name = @Name
                )
            LIMIT 1;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectType = objectType.ToString(),
                Designation = designation,
                Name = name
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QuerySingleOrDefaultAsync<long?>(
            command);
    }

    public async Task<PdmObjectSnapshot?> FindAsync(
        IDbSession session,
        ObjectType objectType,
        string? designation,
        string name,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        const string sql =
            """
            SELECT
                id,
                object_type AS ObjectType,
                designation AS Designation,
                name AS Name
            FROM pdm_object
            WHERE
                (
                    @ObjectType IN ('Assembly', 'Part')
                    AND designation = @Designation
                )
                OR
                (
                    @ObjectType = 'StandardPart'
                    AND object_type = 'StandardPart'
                    AND name = @Name
                )
            LIMIT 1;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectType = objectType.ToString(),
                Designation = designation,
                Name = name
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection
            .QuerySingleOrDefaultAsync<PdmObjectSnapshot>(
                command);
    }

    public async Task UpdateNameAsync(
        IDbSession session,
        long objectId,
        string name,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        const string sql =
            """
            UPDATE pdm_object
            SET name = @Name
            WHERE id = @ObjectId;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectId = objectId,
                Name = name
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var affectedRows = await session.Connection.ExecuteAsync(
            command);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"PDM object with id {objectId} was not found.");
        }
    }

    public async Task SetCurrentVersionAsync(
        IDbSession session,
        long objectId,
        long versionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            UPDATE pdm_object
            SET current_version_id = @VersionId
            WHERE id = @ObjectId
              AND EXISTS (
                  SELECT 1
                  FROM object_version
                  WHERE id = @VersionId
                    AND object_id = @ObjectId
              );
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectId = objectId,
                VersionId = versionId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var affectedRows = await session.Connection.ExecuteAsync(
            command);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"PDM object with id {objectId} was not found, " +
                $"or version {versionId} does not belong to it.");
        }
    }
}