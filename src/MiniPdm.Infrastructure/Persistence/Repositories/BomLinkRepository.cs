using Dapper;
using MiniPdm.Application.Persistence.Models;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Models;

namespace MiniPdm.Infrastructure.Persistence.Repositories;

public sealed class BomLinkRepository
{
    public async Task<long> InsertAsync(
        IDbSession session,
        long parentVersionId,
        long childObjectId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        await EnsureEditableAssemblyVersionAsync(
            session,
            parentVersionId,
            cancellationToken);

        return await InsertCoreAsync(
            session,
            parentVersionId,
            childObjectId,
            quantity,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StoredBomLink>> GetBomLinksAsync(
        IDbSession session,
        long parentVersionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            SELECT
                child_object_id AS ChildObjectId,
                quantity AS Quantity
            FROM bom_link
            WHERE parent_version_id = @ParentVersionId
            ORDER BY child_object_id;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ParentVersionId = parentVersionId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var links = await session.Connection
            .QueryAsync<StoredBomLink>(
                command);

        return links.AsList();
    }

    public async Task ReplaceBomLinksAsync(
        IDbSession session,
        long parentVersionId,
        IReadOnlyCollection<StoredBomLink> links,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(links);

        if (links.Any(link =>
                link.Quantity <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(links),
                "All BOM quantities must be greater than zero.");
        }

        var duplicateChild = links
            .GroupBy(link =>
                link.ChildObjectId)
            .FirstOrDefault(group =>
                group.Count() > 1);

        if (duplicateChild is not null)
        {
            throw new InvalidOperationException(
                $"Child object {duplicateChild.Key} " +
                "occurs more than once in BOM.");
        }

        await EnsureEditableAssemblyVersionAsync(
            session,
            parentVersionId,
            cancellationToken);

        const string deleteSql =
            """
            DELETE FROM bom_link
            WHERE parent_version_id = @ParentVersionId;
            """;

        var deleteCommand = new CommandDefinition(
            deleteSql,
            new
            {
                ParentVersionId = parentVersionId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        await session.Connection.ExecuteAsync(
            deleteCommand);

        foreach (var link in links)
        {
            await InsertCoreAsync(
                session,
                parentVersionId,
                link.ChildObjectId,
                link.Quantity,
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<BomTreeNodeSnapshot>> GetTreeAsync(
        IDbSession session,
        long rootObjectId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        const string sql =
            """
            WITH RECURSIVE bom_tree AS
            (
                SELECT
                    root.id AS parent_object_id,
                    root.current_version_id AS parent_version_id,
                    child.id AS object_id,
                    child.current_version_id AS version_id,
                    child.object_type,
                    child.designation,
                    child.name,
                    child_version.state,
                    child_version.material,
                    child_version.mass_kg,
                    link.quantity,
                    link.quantity::BIGINT AS total_quantity,
                    1 AS depth,
                    ARRAY[root.id, child.id]::BIGINT[] AS path,
                    child.id = root.id AS is_cycle
                FROM pdm_object root
                INNER JOIN bom_link link
                    ON link.parent_version_id = root.current_version_id
                INNER JOIN pdm_object child
                    ON child.id = link.child_object_id
                LEFT JOIN object_version child_version
                    ON child_version.id = child.current_version_id
                WHERE root.id = @RootObjectId

                UNION ALL

                SELECT
                    parent.object_id AS parent_object_id,
                    parent.version_id AS parent_version_id,
                    child.id AS object_id,
                    child.current_version_id AS version_id,
                    child.object_type,
                    child.designation,
                    child.name,
                    child_version.state,
                    child_version.material,
                    child_version.mass_kg,
                    link.quantity,
                    parent.total_quantity * link.quantity::BIGINT,
                    parent.depth + 1,
                    parent.path || child.id,
                    child.id = ANY(parent.path)
                FROM bom_tree parent
                INNER JOIN bom_link link
                    ON link.parent_version_id = parent.version_id
                INNER JOIN pdm_object child
                    ON child.id = link.child_object_id
                LEFT JOIN object_version child_version
                    ON child_version.id = child.current_version_id
                WHERE NOT parent.is_cycle
            )
            SELECT
                parent_object_id AS ParentObjectId,
                parent_version_id AS ParentVersionId,
                object_id AS ObjectId,
                version_id AS VersionId,
                object_type AS ObjectType,
                designation AS Designation,
                name AS Name,
                state AS State,
                material AS Material,
                mass_kg AS MassKg,
                quantity AS Quantity,
                total_quantity AS TotalQuantity,
                depth AS Depth,
                is_cycle AS IsCycle
            FROM bom_tree
            ORDER BY
                depth,
                parent_object_id,
                object_id;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                RootObjectId = rootObjectId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var nodes = await session.Connection
            .QueryAsync<BomTreeNodeSnapshot>(
                command);

        return nodes.AsList();
    }

    private static async Task<long> InsertCoreAsync(
        IDbSession session,
        long parentVersionId,
        long childObjectId,
        int quantity,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO bom_link (
                parent_version_id,
                child_object_id,
                quantity
            )
            VALUES (
                @ParentVersionId,
                @ChildObjectId,
                @Quantity
            )
            RETURNING id;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ParentVersionId = parentVersionId,
                ChildObjectId = childObjectId,
                Quantity = quantity
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        return await session.Connection.QuerySingleAsync<long>(
            command);
    }

    private static async Task EnsureEditableAssemblyVersionAsync(
        IDbSession session,
        long parentVersionId,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT COUNT(*)
            FROM object_version version
            INNER JOIN pdm_object object
                ON object.id = version.object_id
            WHERE version.id = @ParentVersionId
              AND version.state = 'InWork'
              AND object.object_type = 'Assembly';
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ParentVersionId = parentVersionId
            },
            transaction: session.Transaction,
            cancellationToken: cancellationToken);

        var count =
            await session.Connection.QuerySingleAsync<int>(
                command);

        if (count != 1)
        {
            throw new InvalidOperationException(
                $"Version {parentVersionId} cannot be used " +
                "as an editable BOM parent. " +
                "The version must exist, belong to an Assembly, " +
                "and be in InWork state.");
        }
    }
}