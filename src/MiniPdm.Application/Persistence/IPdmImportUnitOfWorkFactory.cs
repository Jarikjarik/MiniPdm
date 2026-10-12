namespace MiniPdm.Application.Persistence;

public interface IPdmImportUnitOfWorkFactory
{
    Task<IPdmImportUnitOfWork> BeginAsync(
        CancellationToken cancellationToken);
}