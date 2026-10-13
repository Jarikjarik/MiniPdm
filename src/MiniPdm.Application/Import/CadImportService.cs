using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Persistence;
using MiniPdm.Application.Persistence.Models;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Import;

public sealed class CadImportService : ICadPersistenceImporter
{
    private readonly IPdmImportUnitOfWorkFactory _unitOfWorkFactory;

    public CadImportService(
        IPdmImportUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory
            ?? throw new ArgumentNullException(
                nameof(unitOfWorkFactory));
    }

    public async Task<CadPersistenceImportResult> ImportAsync(
        IReadOnlyCollection<CadDocument> documents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documents);

        await using var unitOfWork =
            await _unitOfWorkFactory.BeginAsync(
                cancellationToken);

        try
        {
            var pendingDocuments =
                await PrepareObjectsAsync(
                    unitOfWork,
                    documents,
                    cancellationToken);

            var results =
                await PersistVersionsAndBomAsync(
                    unitOfWork,
                    pendingDocuments,
                    cancellationToken);

            await unitOfWork.CommitAsync(
                cancellationToken);

            return new CadPersistenceImportResult(
                results);
        }
        catch
        {
            await unitOfWork.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private static async Task<
        Dictionary<string, PendingDocument>> PrepareObjectsAsync(
        IPdmImportUnitOfWork unitOfWork,
        IReadOnlyCollection<CadDocument> documents,
        CancellationToken cancellationToken)
    {
        var result =
            new Dictionary<string, PendingDocument>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var document in documents)
        {
            if (result.ContainsKey(document.FileName))
            {
                throw new InvalidOperationException(
                    $"Document '{document.FileName}' occurs more than once.");
            }

            var existingObject =
                await unitOfWork.FindObjectAsync(
                    document.Type,
                    document.Designation,
                    document.Name,
                    cancellationToken);

            if (existingObject is not null &&
                existingObject.ObjectType != document.Type)
            {
                throw new InvalidOperationException(
                    $"Object '{document.FileName}' already exists " +
                    $"with type {existingObject.ObjectType}, " +
                    $"but imported document has type {document.Type}.");
            }

            long objectId;
            StoredObjectVersion? currentVersion;

            if (existingObject is null)
            {
                objectId = await unitOfWork.InsertObjectAsync(
                    document.Type,
                    document.Designation,
                    document.Name,
                    cancellationToken);

                currentVersion = null;
            }
            else
            {
                objectId = existingObject.Id;

                currentVersion =
                    await unitOfWork.GetCurrentVersionAsync(
                        objectId,
                        cancellationToken);
            }

            result.Add(
                document.FileName,
                new PendingDocument(
                    document,
                    existingObject,
                    objectId,
                    currentVersion));
        }

        return result;
    }

    private static async Task<
        IReadOnlyCollection<CadPersistenceImportItem>>
        PersistVersionsAndBomAsync(
            IPdmImportUnitOfWork unitOfWork,
            IReadOnlyDictionary<string, PendingDocument> pendingDocuments,
            CancellationToken cancellationToken)
    {
        var results =
            new List<CadPersistenceImportItem>();

        foreach (var pending in pendingDocuments.Values)
        {
            var document = pending.Document;

            var desiredBom =
                BuildDesiredBom(
                    document,
                    pendingDocuments);

            var currentBom =
                pending.CurrentVersion is null
                    ? Array.Empty<StoredBomLink>()
                    : await unitOfWork.GetBomLinksAsync(
                        pending.CurrentVersion.Id,
                        cancellationToken);

            var desiredMaterial =
                document.Type == ObjectType.Part
                    ? document.Material
                    : null;

            var desiredMass =
                document.Type is ObjectType.Part
                    or ObjectType.StandardPart
                    ? document.MassKg
                    : null;

            var nameChanged =
                pending.ExistingObject is not null &&
                !string.Equals(
                    pending.ExistingObject.Name,
                    document.Name,
                    StringComparison.Ordinal);

            var contentChanged =
                pending.CurrentVersion is null ||
                !string.Equals(
                    pending.CurrentVersion.Material,
                    desiredMaterial,
                    StringComparison.Ordinal) ||
                pending.CurrentVersion.MassKg != desiredMass;

            var bomChanged =
                !BomEquals(
                    currentBom,
                    desiredBom);

            var hasChanges =
                pending.ExistingObject is null ||
                nameChanged ||
                contentChanged ||
                bomChanged;

            if (!hasChanges &&
                pending.CurrentVersion is not null)
            {
                results.Add(
                    new CadPersistenceImportItem(
                        document.FileName,
                        pending.ObjectId,
                        pending.CurrentVersion.Id,
                        CadPersistenceAction.Unchanged));

                continue;
            }

            if (nameChanged)
            {
                await unitOfWork.UpdateObjectNameAsync(
                    pending.ObjectId,
                    document.Name,
                    cancellationToken);
            }

            if (pending.CurrentVersion is null)
            {
                var versionId =
                    await unitOfWork.InsertVersionAsync(
                        pending.ObjectId,
                        1,
                        VersionState.InWork,
                        desiredMaterial,
                        desiredMass,
                        cancellationToken);

                await unitOfWork.SetCurrentVersionAsync(
                    pending.ObjectId,
                    versionId,
                    cancellationToken);

                await unitOfWork.ReplaceBomLinksAsync(
                    versionId,
                    desiredBom,
                    cancellationToken);

                results.Add(
                    new CadPersistenceImportItem(
                        document.FileName,
                        pending.ObjectId,
                        versionId,
                        CadPersistenceAction.Created));

                continue;
            }

            if (pending.CurrentVersion.State ==
                VersionState.InWork)
            {
                await unitOfWork.UpdateVersionContentAsync(
                    pending.CurrentVersion.Id,
                    desiredMaterial,
                    desiredMass,
                    cancellationToken);

                await unitOfWork.ReplaceBomLinksAsync(
                    pending.CurrentVersion.Id,
                    desiredBom,
                    cancellationToken);

                results.Add(
                    new CadPersistenceImportItem(
                        document.FileName,
                        pending.ObjectId,
                        pending.CurrentVersion.Id,
                        CadPersistenceAction.Updated));

                continue;
            }

            if (pending.CurrentVersion.State ==
                VersionState.Approved)
            {
                var nextVersionNumber =
                    await unitOfWork.GetNextVersionNumberAsync(
                        pending.ObjectId,
                        cancellationToken);

                var newVersionId =
                    await unitOfWork.InsertVersionAsync(
                        pending.ObjectId,
                        nextVersionNumber,
                        VersionState.InWork,
                        desiredMaterial,
                        desiredMass,
                        cancellationToken);

                await unitOfWork.SetCurrentVersionAsync(
                    pending.ObjectId,
                    newVersionId,
                    cancellationToken);

                await unitOfWork.ReplaceBomLinksAsync(
                    newVersionId,
                    desiredBom,
                    cancellationToken);

                results.Add(
                    new CadPersistenceImportItem(
                        document.FileName,
                        pending.ObjectId,
                        newVersionId,
                        CadPersistenceAction.NewVersion));

                continue;
            }

            throw new InvalidOperationException(
                $"Current version {pending.CurrentVersion.Id} " +
                $"of object {pending.ObjectId} is cancelled.");
        }

        return results;
    }

    private static IReadOnlyCollection<StoredBomLink>
        BuildDesiredBom(
            CadDocument document,
            IReadOnlyDictionary<string, PendingDocument> pendingDocuments)
    {
        if (document.Type != ObjectType.Assembly)
        {
            return Array.Empty<StoredBomLink>();
        }

        var links = new List<StoredBomLink>();

        foreach (var component in document.Components)
        {
            if (!pendingDocuments.TryGetValue(
                    component.FileName,
                    out var child))
            {
                throw new InvalidOperationException(
                    $"Referenced document '{component.FileName}' " +
                    $"was not provided for import.");
            }

            links.Add(
                new StoredBomLink(
                    child.ObjectId,
                    component.Quantity));
        }

        return links;
    }

    private static bool BomEquals(
        IReadOnlyCollection<StoredBomLink> current,
        IReadOnlyCollection<StoredBomLink> desired)
    {
        if (current.Count != desired.Count)
        {
            return false;
        }

        var currentOrdered = current
            .OrderBy(link => link.ChildObjectId)
            .ThenBy(link => link.Quantity)
            .ToArray();

        var desiredOrdered = desired
            .OrderBy(link => link.ChildObjectId)
            .ThenBy(link => link.Quantity)
            .ToArray();

        return currentOrdered.SequenceEqual(
            desiredOrdered);
    }

    private sealed record PendingDocument(
        CadDocument Document,
        StoredPdmObject? ExistingObject,
        long ObjectId,
        StoredObjectVersion? CurrentVersion);
}