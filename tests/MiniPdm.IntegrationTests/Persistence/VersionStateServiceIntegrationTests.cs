using Dapper;
using MiniPdm.Application.Versions;
using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Persistence;
using MiniPdm.Infrastructure.Persistence.Connections;

namespace MiniPdm.IntegrationTests.Persistence;

public class VersionStateServiceIntegrationTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;Username=minipdm;Password=minipdm_dev";

    private const string TestDesignation =
        "ТЕСТ.777777.001";

    [Fact]
    public async Task ChangeStateAsync_CancelCurrentV2_FallsBackToApprovedV1()
    {
        var connectionFactory =
            new NpgsqlConnectionFactory(ConnectionString);

        var unitOfWorkFactory =
            new PdmImportUnitOfWorkFactory(connectionFactory);

        await CleanupAsync(
            connectionFactory);

        try
        {
            long objectId;
            long version1Id;
            long version2Id;

            await using (var setupUnitOfWork =
                         await unitOfWorkFactory.BeginAsync(
                             CancellationToken.None))
            {
                objectId =
                    await setupUnitOfWork.InsertObjectAsync(
                        ObjectType.Part,
                        TestDesignation,
                        "Интеграционная деталь",
                        CancellationToken.None);

                version1Id =
                    await setupUnitOfWork.InsertVersionAsync(
                        objectId,
                        1,
                        VersionState.InWork,
                        "Сталь 45",
                        1.50m,
                        CancellationToken.None);

                await setupUnitOfWork.SetCurrentVersionAsync(
                    objectId,
                    version1Id,
                    CancellationToken.None);

                await setupUnitOfWork.ChangeVersionStateAsync(
                    version1Id,
                    VersionState.InWork,
                    VersionState.Approved,
                    CancellationToken.None);

                version2Id =
                    await setupUnitOfWork.InsertVersionAsync(
                        objectId,
                        2,
                        VersionState.InWork,
                        "Сталь 45",
                        1.60m,
                        CancellationToken.None);

                await setupUnitOfWork.SetCurrentVersionAsync(
                    objectId,
                    version2Id,
                    CancellationToken.None);

                await setupUnitOfWork.CommitAsync(
                    CancellationToken.None);
            }

            var service =
                new VersionStateService(
                    unitOfWorkFactory);

            var currentAfterCancellation =
                await service.ChangeStateAsync(
                    version2Id,
                    VersionState.Cancelled,
                    CancellationToken.None);

            Assert.NotNull(
                currentAfterCancellation);

            Assert.Equal(
                version1Id,
                currentAfterCancellation!.Id);

            Assert.Equal(
                1,
                currentAfterCancellation.VersionNumber);

            Assert.Equal(
                VersionState.Approved,
                currentAfterCancellation.State);

            await using var verificationUnitOfWork =
                await unitOfWorkFactory.BeginAsync(
                    CancellationToken.None);

            var storedCurrent =
                await verificationUnitOfWork.GetCurrentVersionAsync(
                    objectId,
                    CancellationToken.None);

            var cancelledVersion =
                await verificationUnitOfWork.GetVersionAsync(
                    version2Id,
                    CancellationToken.None);

            Assert.NotNull(
                storedCurrent);

            Assert.Equal(
                version1Id,
                storedCurrent!.Id);

            Assert.Equal(
                VersionState.Approved,
                storedCurrent.State);

            Assert.NotNull(
                cancelledVersion);

            Assert.Equal(
                VersionState.Cancelled,
                cancelledVersion!.State);

            await verificationUnitOfWork.RollbackAsync(
                CancellationToken.None);
        }
        finally
        {
            await CleanupAsync(
                connectionFactory);
        }
    }

    private static async Task CleanupAsync(
        IDbConnectionFactory connectionFactory)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                CancellationToken.None);

        const string sql =
            """
            DELETE FROM pdm_object
            WHERE designation = @Designation;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                Designation = TestDesignation
            },
            cancellationToken: CancellationToken.None);

        await connection.ExecuteAsync(
            command);
    }
}