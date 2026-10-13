namespace MiniPdm.Application.Import;

public sealed class FolderImportService
{
    private readonly ICadFolderReader _folderReader;
    private readonly CadDocumentValidator _validator;
    private readonly ICadPersistenceImporter _persistenceImporter;
    private readonly ImportReportBuilder _reportBuilder;

    public FolderImportService(
        ICadFolderReader folderReader,
        CadDocumentValidator validator,
        ICadPersistenceImporter persistenceImporter,
        ImportReportBuilder reportBuilder)
    {
        _folderReader = folderReader
            ?? throw new ArgumentNullException(
                nameof(folderReader));

        _validator = validator
            ?? throw new ArgumentNullException(
                nameof(validator));

        _persistenceImporter = persistenceImporter
            ?? throw new ArgumentNullException(
                nameof(persistenceImporter));

        _reportBuilder = reportBuilder
            ?? throw new ArgumentNullException(
                nameof(reportBuilder));
    }

    public async Task<FolderImportResult> ImportAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException(
                "Folder path must not be empty.",
                nameof(folderPath));
        }

        var readResult =
            await _folderReader.ReadAsync(
                folderPath,
                cancellationToken);

        var validationResult =
            _validator.Validate(
                readResult.Documents);

        CadPersistenceImportResult persistenceResult;

        if (validationResult.AcceptedDocuments.Count == 0)
        {
            persistenceResult =
                new CadPersistenceImportResult(
                    Array.Empty<CadPersistenceImportItem>());
        }
        else
        {
            persistenceResult =
                await _persistenceImporter.ImportAsync(
                    validationResult.AcceptedDocuments,
                    cancellationToken);
        }

        var report =
            _reportBuilder.Build(
                readResult,
                validationResult);

        return new FolderImportResult(
            readResult,
            validationResult,
            persistenceResult,
            report);
    }
}