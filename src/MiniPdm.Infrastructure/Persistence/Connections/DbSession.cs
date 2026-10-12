using System.Data.Common;

namespace MiniPdm.Infrastructure.Persistence.Connections;

public sealed class DbSession : IDbSession
{
    public DbConnection Connection { get; }

    public DbTransaction Transaction { get; }

    public DbSession(
        DbConnection connection,
        DbTransaction transaction)
    {
        Connection = connection
            ?? throw new ArgumentNullException(nameof(connection));

        Transaction = transaction
            ?? throw new ArgumentNullException(nameof(transaction));
    }

    public Task CommitAsync(
        CancellationToken cancellationToken)
    {
        return Transaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken)
    {
        return Transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}