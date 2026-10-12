using System.Data.Common;

namespace MiniPdm.Infrastructure.Persistence.Connections;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken);

    Task<IDbSession> BeginSessionAsync(
        CancellationToken cancellationToken);
}