using System.Data.Common;

namespace MiniPdm.Infrastructure.Persistence.Connections;

public interface IDbSession : IAsyncDisposable
{
    DbConnection Connection { get; }

    DbTransaction Transaction { get; }

    Task CommitAsync(
        CancellationToken cancellationToken);

    Task RollbackAsync(
        CancellationToken cancellationToken);
}