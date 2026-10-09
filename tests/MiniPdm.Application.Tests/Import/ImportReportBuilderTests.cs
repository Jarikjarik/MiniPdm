using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Import;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Tests.Import;

public class ImportReportBuilderTests
{
    [Fact]
    public void Build_CombinesSuccessWarningErrorAndReadFailure()
    {
        var successDocument = new CadDocument(
            1,
            "Успешная деталь.m3d",
            ObjectType.Part,
            "РДЦЛ.304112.111",
            "Успешная деталь",
            "Сталь",
            1.25m,
            []);

        var warningDocument = new CadDocument(
            1,
            "Деталь без массы.m3d",
            ObjectType.Part,
            "РДЦЛ.304112.112",
            "Деталь без массы",
            "Сталь",
            null,
            []);

        var errorDocument = new CadDocument(
            1,
            "Ошибочная деталь.m3d",
            ObjectType.Part,
            "PДЦЛ.304112.113",
            "Ошибочная деталь",
            "Сталь",
            0.5m,
            []);

        var readResult = new CadFolderReadResult(
            [
                successDocument,
                warningDocument,
                errorDocument
            ],
            [
                new CadReadError(
                    "Битый файл.m3d",
                    "Invalid JSON.")
            ]);

        var validationResult = new CadValidationResult(
            [
                successDocument,
                warningDocument
            ],
            [
                new CadValidationIssue(
                    "Деталь без массы.m3d",
                    ImportIssueSeverity.Warning,
                    "Mass is not specified."),

                new CadValidationIssue(
                    "Ошибочная деталь.m3d",
                    ImportIssueSeverity.Error,
                    "Invalid designation.")
            ]);

        var builder = new ImportReportBuilder();

        var report = builder.Build(
            readResult,
            validationResult);

        Assert.Equal(4, report.Items.Count);

        Assert.Equal(2, report.AcceptedCount);
        Assert.Equal(2, report.RejectedCount);
        Assert.Equal(1, report.WarningCount);

        Assert.Contains(
            report.Items,
            item =>
                item.FileName == "Успешная деталь.m3d" &&
                item.Status == ImportItemStatus.Success);

        Assert.Contains(
            report.Items,
            item =>
                item.FileName == "Деталь без массы.m3d" &&
                item.Status == ImportItemStatus.Warning);

        Assert.Contains(
            report.Items,
            item =>
                item.FileName == "Ошибочная деталь.m3d" &&
                item.Status == ImportItemStatus.Error);

        Assert.Contains(
            report.Items,
            item =>
                item.FileName == "Битый файл.m3d" &&
                item.Status == ImportItemStatus.Error);
    }
}