using MiniPdm.Application.Import;
using MiniPdm.Infrastructure.Cad;
using Xunit;
using Xunit.Abstractions;

namespace MiniPdm.IntegrationTests.Cad;

public class RealCadImportReportTests
{
    private readonly ITestOutputHelper _output;

    public RealCadImportReportTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task BuildReport_ForRealCadExport_PrintsImportSummary()
    {
        var folderPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "cad-export");

        var locator = new FileSystemCadDocumentLocator();
        var reader = new JsonCadDocumentReader();

        var paths = await locator.GetDocumentPathsAsync(
            folderPath,
            CancellationToken.None);

        var folderReader = new CadFolderReader(
            locator,
            reader);

        var readResult = await folderReader.ReadAsync(
            folderPath,
            CancellationToken.None);

        var validator = new CadDocumentValidator();

        var validationResult = validator.Validate(
            readResult.Documents);

        var reportBuilder = new ImportReportBuilder();

        var report = reportBuilder.Build(
            readResult,
            validationResult);

        _output.WriteLine($"CAD files found: {paths.Count}");
        _output.WriteLine($"Documents read: {readResult.Documents.Count}");
        _output.WriteLine($"Read errors: {readResult.Errors.Count}");
        _output.WriteLine($"Accepted: {report.AcceptedCount}");
        _output.WriteLine($"Rejected: {report.RejectedCount}");
        _output.WriteLine($"Warnings: {report.WarningCount}");
        _output.WriteLine("");

        foreach (var item in report.Items
                     .Where(item => item.Status != ImportItemStatus.Success))
        {
            _output.WriteLine(
                $"{item.Status} | {Path.GetFileName(item.FileName)} | {item.Message}");
        }

        Assert.Equal(45, paths.Count);
        Assert.Equal(44, readResult.Documents.Count);
        Assert.Single(readResult.Errors);

        Assert.Equal(45, report.Items.Count);
        Assert.Equal(38, report.AcceptedCount);
        Assert.Equal(7, report.RejectedCount);
        Assert.Equal(1, report.WarningCount);
    }
}