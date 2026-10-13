using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Import;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Tests.Import;

public class FolderImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ReadsValidatesPersistsAcceptedDocumentsAndBuildsReport()
    {
        var part = new CadDocument(
            1,
            "Вал.m3d",
            ObjectType.Part,
            "ТЕСТ.654321.001",
            "Вал",
            "Сталь 20",
            null,
            Array.Empty<CadComponent>());

        var invalidAssembly = new CadDocument(
            1,
            "Редуктор.a3d",
            ObjectType.Assembly,
            "ТЕСТ.654321.002",
            "Редуктор",
            null,
            null,
            new[]
            {
                new CadComponent(
                    "Отсутствующая деталь.m3d",
                    1)
            });

        var readResult = new CadFolderReadResult(
            new[]
            {
                part,
                invalidAssembly
            },
            new[]
            {
                new CadReadError(
                    "Поврежденная деталь.m3d",
                    "Invalid JSON.")
            });

        var folderReader =
            new FakeCadFolderReader(
                readResult);

        var persistenceImporter =
            new FakeCadPersistenceImporter();

        var service = new FolderImportService(
            folderReader,
            new CadDocumentValidator(),
            persistenceImporter,
            new ImportReportBuilder());

        var result = await service.ImportAsync(
            @"C:\cad-export",
            CancellationToken.None);

        Assert.Equal(
            @"C:\cad-export",
            folderReader.LastFolderPath);

        var persistedDocument = Assert.Single(
            persistenceImporter.LastDocuments);

        Assert.Equal(
            "Вал.m3d",
            persistedDocument.FileName);

        Assert.Equal(
            1,
            result.PersistenceResult.CreatedCount);

        Assert.Equal(
            1,
            result.Report.AcceptedCount);

        Assert.Equal(
            2,
            result.Report.RejectedCount);

        Assert.Equal(
            1,
            result.Report.WarningCount);
    }

    private sealed class FakeCadFolderReader
        : ICadFolderReader
    {
        private readonly CadFolderReadResult _result;

        public FakeCadFolderReader(
            CadFolderReadResult result)
        {
            _result = result;
        }

        public string? LastFolderPath { get; private set; }

        public Task<CadFolderReadResult> ReadAsync(
            string folderPath,
            CancellationToken cancellationToken)
        {
            LastFolderPath = folderPath;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class FakeCadPersistenceImporter
        : ICadPersistenceImporter
    {
        public IReadOnlyCollection<CadDocument> LastDocuments
        {
            get;
            private set;
        } = Array.Empty<CadDocument>();

        public Task<CadPersistenceImportResult> ImportAsync(
            IReadOnlyCollection<CadDocument> documents,
            CancellationToken cancellationToken)
        {
            LastDocuments = documents.ToArray();

            var items = documents
                .Select(
                    (document, index) =>
                        new CadPersistenceImportItem(
                            document.FileName,
                            index + 1,
                            index + 1,
                            CadPersistenceAction.Created))
                .ToArray();

            return Task.FromResult(
                new CadPersistenceImportResult(
                    items));
        }
    }
}