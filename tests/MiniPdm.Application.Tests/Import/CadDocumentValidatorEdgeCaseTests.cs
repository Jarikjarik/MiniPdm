using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Import;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Tests.Import;

public class CadDocumentValidatorEdgeCaseTests
{
    [Fact]
    public void Validate_DesignationWithUnicodeDigits_RejectsDocument()
    {
        var document = new CadDocument(
            1,
            "Деталь.m3d",
            ObjectType.Part,
            "ТЕСТ.１２３４５６.001",
            "Деталь",
            "Сталь",
            1.0m,
            []);

        var validator =
            new CadDocumentValidator();

        var result =
            validator.Validate([document]);

        Assert.Empty(
            result.AcceptedDocuments);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Severity == ImportIssueSeverity.Error &&
                issue.Reason == "Invalid designation.");
    }

    [Fact]
    public void Validate_StandardPartWithEmptyDesignation_RejectsDocument()
    {
        var document = new CadDocument(
            1,
            "Подшипник.m3d",
            ObjectType.StandardPart,
            "",
            "Подшипник 210",
            null,
            0.37m,
            []);

        var validator =
            new CadDocumentValidator();

        var result =
            validator.Validate([document]);

        Assert.Empty(
            result.AcceptedDocuments);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Severity == ImportIssueSeverity.Error &&
                issue.Reason.Contains(
                    "must not have a designation",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithNegativeMass_RejectsDocument()
    {
        var document = new CadDocument(
            1,
            "Деталь.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.501",
            "Деталь",
            "Сталь",
            -1.0m,
            []);

        var validator =
            new CadDocumentValidator();

        var result =
            validator.Validate([document]);

        Assert.Empty(
            result.AcceptedDocuments);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Severity == ImportIssueSeverity.Error &&
                issue.Reason.Contains(
                    "must not be negative",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AssemblyWithDuplicateComponents_RejectsDocument()
    {
        var part = new CadDocument(
            1,
            "Вал.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.502",
            "Вал",
            "Сталь",
            1.0m,
            []);

        var assembly = new CadDocument(
            1,
            "Редуктор.a3d",
            ObjectType.Assembly,
            "ТЕСТ.123456.503",
            "Редуктор",
            null,
            null,
            [
                new CadComponent(
                    "Вал.m3d",
                    1),

                new CadComponent(
                    "Вал.m3d",
                    2)
            ]);

        var validator =
            new CadDocumentValidator();

        var result =
            validator.Validate(
                [
                    assembly,
                    part
                ]);

        Assert.DoesNotContain(
            result.AcceptedDocuments,
            document =>
                document.FileName == "Редуктор.a3d");

        Assert.Contains(
            result.Issues,
            issue =>
                issue.FileName == "Редуктор.a3d" &&
                issue.Reason.Contains(
                    "occurs more than once",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_PartWithBomComponents_RejectsDocument()
    {
        var part = new CadDocument(
            1,
            "Деталь.m3d",
            ObjectType.Part,
            "ТЕСТ.123456.504",
            "Деталь",
            "Сталь",
            1.0m,
            [
                new CadComponent(
                    "Что-то.m3d",
                    1)
            ]);

        var validator =
            new CadDocumentValidator();

        var result =
            validator.Validate([part]);

        Assert.Empty(
            result.AcceptedDocuments);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Reason.Contains(
                    "must not contain BOM components",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AssemblyWithOwnMass_RejectsDocument()
    {
        var assembly = new CadDocument(
            1,
            "Сборка.a3d",
            ObjectType.Assembly,
            "ТЕСТ.123456.505",
            "Сборка",
            null,
            10.0m,
            []);

        var validator =
            new CadDocumentValidator();

        var result =
            validator.Validate([assembly]);

        Assert.Empty(
            result.AcceptedDocuments);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Reason.Contains(
                    "calculated from BOM",
                    StringComparison.OrdinalIgnoreCase));
    }
}