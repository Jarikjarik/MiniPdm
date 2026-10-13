using Dapper;
using MiniPdm.Application.Browse;
using MiniPdm.Application.Browse.Models;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence.Connections;

namespace MiniPdm.Infrastructure.Persistence.Queries;

public sealed class PostgresPdmCatalogReader
    : IPdmCatalogReader
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PostgresPdmCatalogReader(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(
                nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<PdmObjectListItem>> SearchAsync(
        string? searchText,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var normalizedSearchText =
            string.IsNullOrWhiteSpace(searchText)
                ? null
                : searchText.Trim();

        const string sql =
            """
            SELECT
                o.id AS ObjectId,
                o.object_type AS ObjectType,
                o.designation AS Designation,
                o.name AS Name,
                v.version_no AS CurrentVersionNumber,
                v.state AS CurrentState
            FROM pdm_object o
            LEFT JOIN object_version v
                ON v.id = o.current_version_id
            WHERE
                @SearchText IS NULL
                OR o.name ILIKE '%' || @SearchText || '%'
                OR o.designation ILIKE '%' || @SearchText || '%'
            ORDER BY
                o.object_type,
                o.name,
                o.designation;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                SearchText = normalizedSearchText
            },
            cancellationToken: cancellationToken);

        var rows =
            await connection.QueryAsync<ObjectListRow>(
                command);

        return rows
            .Select(row =>
                new PdmObjectListItem(
                    row.ObjectId,
                    row.ObjectType,
                    row.Designation,
                    row.Name,
                    row.CurrentVersionNumber,
                    row.CurrentState))
            .ToArray();
    }

    public async Task<PdmObjectDetails?> GetDetailsAsync(
        long objectId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(
                cancellationToken);

        const string sql =
            """
            SELECT
                id AS ObjectId,
                object_type AS ObjectType,
                designation AS Designation,
                name AS Name,
                current_version_id AS CurrentVersionId
            FROM pdm_object
            WHERE id = @ObjectId;

            SELECT
                id AS VersionId,
                version_no AS VersionNumber,
                state AS State,
                material AS Material,
                mass_kg AS MassKg,
                created_at AS CreatedAt
            FROM object_version
            WHERE object_id = @ObjectId
            ORDER BY version_no DESC;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ObjectId = objectId
            },
            cancellationToken: cancellationToken);

        using var grid =
            await connection.QueryMultipleAsync(
                command);

        var objectRow =
            await grid.ReadSingleOrDefaultAsync<ObjectDetailsRow>();

        if (objectRow is null)
        {
            return null;
        }

        var versionRows =
            (await grid.ReadAsync<VersionRow>())
            .ToArray();

        var versions = versionRows
            .Select(row =>
                new PdmVersionListItem(
                    row.VersionId,
                    row.VersionNumber,
                    row.State,
                    row.Material,
                    row.MassKg,
                    row.CreatedAt))
            .ToArray();

        return new PdmObjectDetails(
            objectRow.ObjectId,
            objectRow.ObjectType,
            objectRow.Designation,
            objectRow.Name,
            objectRow.CurrentVersionId,
            versions);
    }

    private sealed class ObjectListRow
    {
        public long ObjectId { get; init; }

        public ObjectType ObjectType { get; init; }

        public string? Designation { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public int? CurrentVersionNumber { get; init; }

        public VersionState? CurrentState { get; init; }
    }

    private sealed class ObjectDetailsRow
    {
        public long ObjectId { get; init; }

        public ObjectType ObjectType { get; init; }

        public string? Designation { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public long? CurrentVersionId { get; init; }
    }

    private sealed class VersionRow
    {
        public long VersionId { get; init; }

        public int VersionNumber { get; init; }

        public VersionState State { get; init; }

        public string? Material { get; init; }

        public decimal? MassKg { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}