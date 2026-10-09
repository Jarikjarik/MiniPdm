using MiniPdm.Application.Import;
using MiniPdm.Infrastructure.Cad;

namespace MiniPdm.IntegrationTests.Cad;

public class CadFolderReaderIntegrationTests
{
    [Fact]
    public async Task ReadAsync_ReadsRealCadExportFolder()
    {
        var folderPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "cad-export");

        var locator = new FileSystemCadDocumentLocator();
        var reader = new JsonCadDocumentReader();

        var service = new CadFolderReader(
            locator,
            reader);

        var result = await service.ReadAsync(
            folderPath,
            CancellationToken.None);

        Assert.NotEmpty(result.Documents);

        Assert.Contains(
            result.Errors,
            error => error.Path.Contains(
                "Отдушина.m3d",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_RealCadExport_DetectsKnownProblems()
    {
        var folderPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "cad-export");

        var locator = new FileSystemCadDocumentLocator();
        var reader = new JsonCadDocumentReader();

        var folderReader = new CadFolderReader(
            locator,
            reader);

        var readResult = await folderReader.ReadAsync(
            folderPath,
            CancellationToken.None);

        var validator = new CadDocumentValidator();

        var validationResult = validator.Validate(
            readResult.Documents);

        Assert.Contains(
            readResult.Errors,
            error => error.Path.Contains(
                "Отдушина.m3d",
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            validationResult.Issues,
            issue =>
                issue.FileName == "Втулка распорная.m3d" &&
                issue.Severity == ImportIssueSeverity.Error);

        Assert.Contains(
            validationResult.Issues,
            issue =>
                issue.FileName == "Кольцо распорное.m3d" &&
                issue.Severity == ImportIssueSeverity.Error);

        Assert.Contains(
            validationResult.Issues,
            issue =>
                issue.FileName == "Шайба стопорная.m3d" &&
                issue.Severity == ImportIssueSeverity.Error);

        Assert.Contains(
            validationResult.Issues,
            issue =>
                issue.FileName == "Шайба упорная.m3d" &&
                issue.Severity == ImportIssueSeverity.Error);

        Assert.Contains(
            validationResult.Issues,
            issue =>
                issue.FileName == "Пробка сливная в сборе.a3d" &&
                issue.Severity == ImportIssueSeverity.Error);

        Assert.Contains(
            validationResult.Issues,
            issue =>
                issue.FileName == "Привод масляного насоса.a3d" &&
                issue.Severity == ImportIssueSeverity.Error);

        Assert.Contains(
            validationResult.Issues,
            issue =>
                issue.FileName == "Прокладка маслоуказателя.m3d" &&
                issue.Severity == ImportIssueSeverity.Warning);
    }
}