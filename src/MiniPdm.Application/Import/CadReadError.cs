namespace MiniPdm.Application.Import;

public sealed record CadReadError(
    string Path,
    string Reason);