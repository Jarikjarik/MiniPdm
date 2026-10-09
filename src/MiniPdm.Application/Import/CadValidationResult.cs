using MiniPdm.Application.Abstractions.Cad;

namespace MiniPdm.Application.Import;

public sealed record CadValidationResult(
    IReadOnlyCollection<CadDocument> AcceptedDocuments,
    IReadOnlyCollection<CadValidationIssue> Issues);