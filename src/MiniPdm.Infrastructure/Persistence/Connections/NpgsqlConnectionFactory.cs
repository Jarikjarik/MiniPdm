using System.Data.Common;
using Npgsql;

namespace MiniPdm.Infrastructure.Persistence.Connections;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionString = connectionString;
    }

    public async Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        return connection;
    }

    public async Task<IDbSession> BeginSessionAsync(
        CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(
            cancellationToken);

        try
        {
            var transaction = await connection.BeginTransactionAsync(
                cancellationToken);

            return new DbSession(
                connection,
                transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}