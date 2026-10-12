namespace MiniPdm.Application.Import;

public enum CadPersistenceAction
{
    Created,
    Updated,
    NewVersion,
    Unchanged
}

public sealed record CadPersistenceImportItem(
    string FileName,
    long ObjectId,
    long VersionId,
    CadPersistenceAction Action);

public sealed class CadPersistenceImportResult
{
    public CadPersistenceImportResult(
        IReadOnlyCollection<CadPersistenceImportItem> items)
    {
        Items = items;
    }

    public IReadOnlyCollection<CadPersistenceImportItem> Items { get; }

    public int CreatedCount =>
        Items.Count(item => item.Action == CadPersistenceAction.Created);

    public int UpdatedCount =>
        Items.Count(item => item.Action == CadPersistenceAction.Updated);

    public int NewVersionCount =>
        Items.Count(item => item.Action == CadPersistenceAction.NewVersion);

    public int UnchangedCount =>
        Items.Count(item => item.Action == CadPersistenceAction.Unchanged);
}