using MiniPdm.Application.Persistence.Models;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence;
using MiniPdm.Infrastructure.Persistence.Connections;

namespace MiniPdm.IntegrationTests.Persistence;

public class PdmImportUnitOfWorkTests
{
    private static string ConnectionString =>
        TestDatabase.ConnectionString;

    [Fact]
    public async Task RollbackAsync_RollsBackWholeImportTransaction()
    {
        var connectionFactory =
            new NpgsqlConnectionFactory(ConnectionString);

        var unitOfWorkFactory =
            new PdmImportUnitOfWorkFactory(connectionFactory);

        await using (var unitOfWork =
                     await unitOfWorkFactory.BeginAsync(
                         CancellationToken.None))
        {
            var assemblyId = await unitOfWork.InsertObjectAsync(
                ObjectType.Assembly,
                "ТЕСТ.654321.020",
                "Транзакционная сборка",
                CancellationToken.None);

            var assemblyVersionId =
                await unitOfWork.InsertVersionAsync(
                    assemblyId,
                    1,
                    VersionState.InWork,
                    null,
                    null,
                    CancellationToken.None);

            await unitOfWork.SetCurrentVersionAsync(
                assemblyId,
                assemblyVersionId,
                CancellationToken.None);

            var partId = await unitOfWork.InsertObjectAsync(
                ObjectType.Part,
                "ТЕСТ.654321.021",
                "Транзакционная деталь",
                CancellationToken.None);

            var partVersionId =
                await unitOfWork.InsertVersionAsync(
                    partId,
                    1,
                    VersionState.InWork,
                    "Сталь 20",
                    2.50m,
                    CancellationToken.None);

            await unitOfWork.SetCurrentVersionAsync(
                partId,
                partVersionId,
                CancellationToken.None);

            await unitOfWork.ReplaceBomLinksAsync(
                assemblyVersionId,
                new[]
                {
                    new StoredBomLink(
                        partId,
                        3)
                },
                CancellationToken.None);

            await unitOfWork.RollbackAsync(
                CancellationToken.None);
        }

        await using var verificationUnitOfWork =
            await unitOfWorkFactory.BeginAsync(
                CancellationToken.None);

        var assembly = await verificationUnitOfWork.FindObjectAsync(
            ObjectType.Assembly,
            "ТЕСТ.654321.020",
            "Транзакционная сборка",
            CancellationToken.None);

        var part = await verificationUnitOfWork.FindObjectAsync(
            ObjectType.Part,
            "ТЕСТ.654321.021",
            "Транзакционная деталь",
            CancellationToken.None);

        Assert.Null(assembly);
        Assert.Null(part);

        await verificationUnitOfWork.RollbackAsync(
            CancellationToken.None);
    }
}
