using Dapper;
using MiniPdm.Application.Bom;
using MiniPdm.Application.Bom.Models;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.Infrastructure.Persistence.Queries;

public sealed class PostgresBomStructureReader
    : IBomStructureReader
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly BomLinkRepository _bomRepository;

    public PostgresBomStructureReader(
        IDbConnectionFactory connectionFactory,
        BomLinkRepository bomRepository)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(
                nameof(connectionFactory));

        _bomRepository = bomRepository
            ?? throw new ArgumentNullException(
                nameof(bomRepository));
    }

    public async Task<BomStructure?> ReadAsync(
        long rootObjectId,
        CancellationToken cancellationToken)
    {
        await using var session =
            await _connectionFactory.BeginSessionAsync(
                cancellationToken);

        try
        {
            var root = await ReadRootAsync(
                session,
                rootObjectId,
                cancellationToken);

            if (root is null)
            {
                await session.RollbackAsync(
                    cancellationToken);

                return null;
            }

            var snapshots =
                await _bomRepository.GetTreeAsync(
                    session,
                    rootObjectId,
                    cancellationToken);

            var nodes = snapshots
                .Select(snapshot =>
                    new BomTreeNode(
                        snapshot.ParentObjectId,
                        snapshot.ParentVersionId,
                        snapshot.ObjectId,
                        snapshot.VersionId,
                        snapshot.ObjectType,
                        snapshot.Designation,
                        snapshot.Name,
                        snapshot.State,
                        snapshot.Material,
                        snapshot.MassKg,
                        snapshot.Quantity,
                        snapshot.TotalQuantity,
                        snapshot.Depth,
                        snapshot.IsCycle))
                .ToArray();

            await session.RollbackAsync(
                cancellationToken);

            return new BomStructure(
                root,
                nodes);
        }
        catch
        {
            await session.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private static async Task<BomObject?> ReadRootAsync(
        IDbSession session,
        long rootObjectId,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                o.id AS ObjectId,
                o.object_type AS ObjectType,
                o.designation AS Designation,
                o.name AS Name,
                v.id AS VersionId,
                v.version_no AS VersionNumber,
                v.state AS State,
                v.material AS Material,
                v.mass_kg AS MassKg
            FROM pdm_object o
            LEFT JOIN object_version v
                ON v.id = o.current_version_id
            WHERE o.id = @RootObjectId
            LIMIT 1;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                RootObjectId = rootObjectId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var row = await session.Connection
            .QuerySingleOrDefaultAsync<RootRow>(
                command);

        if (row is null)
        {
            return null;
        }

        return new BomObject(
            row.ObjectId,
            row.ObjectType,
            row.Designation,
            row.Name,
            row.VersionId,
            row.VersionNumber,
            row.State,
            row.Material,
            row.MassKg);
    }

    private sealed class RootRow
    {
        public long ObjectId { get; init; }

        public ObjectType ObjectType { get; init; }

        public string? Designation { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public long? VersionId { get; init; }

        public int? VersionNumber { get; init; }

        public VersionState? State { get; init; }

        public string? Material { get; init; }

        public decimal? MassKg { get; init; }
    }
}