namespace MiniPdm.Application.Persistence.Models;

public sealed record StoredBomLink(
    long ChildObjectId,
    int Quantity);