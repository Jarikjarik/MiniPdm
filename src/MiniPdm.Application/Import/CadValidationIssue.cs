namespace MiniPdm.Application.Import;

public sealed record CadValidationIssue(
    string FileName,
    ImportIssueSeverity Severity,
    string Reason);