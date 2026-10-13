using MiniPdm.Application.Bom.Models;

namespace MiniPdm.Application.Bom;

public interface IBomStructureReader
{
    Task<BomStructure?> ReadAsync(
        long rootObjectId,
        CancellationToken cancellationToken);
}