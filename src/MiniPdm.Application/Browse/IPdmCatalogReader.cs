using MiniPdm.Application.Browse.Models;

namespace MiniPdm.Application.Browse;

public interface IPdmCatalogReader
{
    Task<IReadOnlyList<PdmObjectListItem>> SearchAsync(
        string? searchText,
        CancellationToken cancellationToken);

    Task<PdmObjectDetails?> GetDetailsAsync(
        long objectId,
        CancellationToken cancellationToken);
}