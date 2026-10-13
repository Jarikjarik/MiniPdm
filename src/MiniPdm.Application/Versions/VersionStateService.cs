using MiniPdm.Application.Persistence;
using MiniPdm.Application.Persistence.Models;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Rules;

namespace MiniPdm.Application.Versions;

public sealed class VersionStateService
{
    private readonly IPdmImportUnitOfWorkFactory _unitOfWorkFactory;

    public VersionStateService(
        IPdmImportUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory
            ?? throw new ArgumentNullException(
                nameof(unitOfWorkFactory));
    }

    public async Task<StoredObjectVersion?> ChangeStateAsync(
        long versionId,
        VersionState newState,
        CancellationToken cancellationToken)
    {
        await using var unitOfWork =
            await _unitOfWorkFactory.BeginAsync(
                cancellationToken);

        try
        {
            var version =
                await unitOfWork.GetVersionAsync(
                    versionId,
                    cancellationToken);

            if (version is null)
            {
                throw new InvalidOperationException(
                    $"Version {versionId} was not found.");
            }

            if (!VersionStateTransitionRules.CanTransition(
                    version.State,
                    newState))
            {
                throw new InvalidOperationException(
                    $"Transition from {version.State} " +
                    $"to {newState} is not allowed.");
            }

            await unitOfWork.ChangeVersionStateAsync(
                version.Id,
                version.State,
                newState,
                cancellationToken);

            await unitOfWork.RefreshCurrentVersionAsync(
                version.ObjectId,
                cancellationToken);

            var currentVersion =
                await unitOfWork.GetCurrentVersionAsync(
                    version.ObjectId,
                    cancellationToken);

            await unitOfWork.CommitAsync(
                cancellationToken);

            return currentVersion;
        }
        catch
        {
            await unitOfWork.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }
}