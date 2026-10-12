using MiniPdm.Application.Persistence;
using MiniPdm.Infrastructure.Persistence.Connections;
using MiniPdm.Infrastructure.Persistence.Repositories;

namespace MiniPdm.Infrastructure.Persistence;

public sealed class PdmImportUnitOfWorkFactory
    : IPdmImportUnitOfWorkFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PdmImportUnitOfWorkFactory(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(
                nameof(connectionFactory));
    }

    public async Task<IPdmImportUnitOfWork> BeginAsync(
        CancellationToken cancellationToken)
    {
        var session = await _connectionFactory.BeginSessionAsync(
            cancellationToken);

        return new PdmImportUnitOfWork(
            session,
            new PdmObjectRepository(),
            new ObjectVersionRepository(),
            new BomLinkRepository());
    }
}