using MiniPdm.Application.Abstractions.Cad;

namespace MiniPdm.Application.Import;

public sealed record CadFolderReadResult(
    IReadOnlyCollection<CadDocument> Documents,
    IReadOnlyCollection<CadReadError> Errors);