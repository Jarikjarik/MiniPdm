using Dapper;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Models;

namespace MiniPdm.Infrastructure.Persistence.Repositories;

public sealed class ObjectVersionRepository
{
    public async Task<long> InsertAsync(
        IDbSession session,
        long objectId,
        int versionNumber,
        VersionState state,
        string? material,
        decimal? massKg,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            INSERT INTO object_version (
                object_id,
                version_no,
                state,
                material,
                mass_kg
            )
            VALUES (
                @ObjectId,
                @VersionNumber,
                @State,
                @Material,
                @MassKg
            )
            RETURNING id;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectId = objectId,
                VersionNumber = versionNumber,
                State = state.ToString(),
                Material = material,
                MassKg = massKg
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QuerySingleAsync<long>(
            command);
    }

    public async Task<ObjectVersionSnapshot?> GetCurrentAsync(
        IDbSession session,
        long objectId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            SELECT
                v.id,
                v.object_id AS ObjectId,
                v.version_no AS VersionNumber,
                v.state,
                v.material,
                v.mass_kg AS MassKg,
                v.created_at AS CreatedAt
            FROM pdm_object o
            INNER JOIN object_version v
                ON v.id = o.current_version_id
            WHERE o.id = @ObjectId
            LIMIT 1;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectId = objectId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection
            .QuerySingleOrDefaultAsync<ObjectVersionSnapshot>(
                command);
    }

    public async Task<int> GetNextVersionNumberAsync(
        IDbSession session,
        long objectId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            SELECT COALESCE(MAX(version_no), 0) + 1
            FROM object_version
            WHERE object_id = @ObjectId;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectId = objectId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QuerySingleAsync<int>(
            command);
    }

    public async Task UpdateContentAsync(
        IDbSession session,
        long versionId,
        string? material,
        decimal? massKg,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            UPDATE object_version
            SET
                material = @Material,
                mass_kg = @MassKg
            WHERE id = @VersionId
              AND state = 'InWork';
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                VersionId = versionId,
                Material = material,
                MassKg = massKg
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var affectedRows = await session.Connection.ExecuteAsync(
            command);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"Version {versionId} was not found or is not InWork.");
        }
    }

    public async Task ChangeStateAsync(
        IDbSession session,
        long versionId,
        VersionState currentState,
        VersionState newState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            UPDATE object_version
            SET state = @NewState
            WHERE id = @VersionId
              AND state = @CurrentState;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                VersionId = versionId,
                CurrentState = currentState.ToString(),
                NewState = newState.ToString()
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var affectedRows = await session.Connection.ExecuteAsync(
            command);

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"Version {versionId} was not found " +
                $"or is not in state {currentState}.");
        }
    }
}