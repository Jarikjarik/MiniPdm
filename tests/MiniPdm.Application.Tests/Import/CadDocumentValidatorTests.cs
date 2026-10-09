using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Import;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Tests.Import;

public class CadDocumentValidatorTests
{
    [Fact]
    public void Validate_WithInvalidDesignation_RejectsDocument()
    {
        var document = new CadDocument(
            1,
            "Втулка распорная.m3d",
            ObjectType.Part,
            "PДЦЛ.304112.601",
            "Втулка распорная",
            "Сталь 45",
            0.42m,
            []);

        var validator = new CadDocumentValidator();

        var result = validator.Validate([document]);

        Assert.Empty(result.AcceptedDocuments);
        Assert.Single(result.Issues);

        var issue = result.Issues.Single();

        Assert.Equal(
            ImportIssueSeverity.Error,
            issue.Severity);

        Assert.Equal(
            "Втулка распорная.m3d",
            issue.FileName);
    }

    [Fact]
    public void Validate_WithPartWithoutMass_AcceptsDocumentWithWarning()
    {
        var document = new CadDocument(
            1,
            "Прокладка маслоуказателя.m3d",
            ObjectType.Part,
            "РДЦЛ.304112.801",
            "Прокладка маслоуказателя",
            "Паронит",
            null,
            []);

        var validator = new CadDocumentValidator();

        var result = validator.Validate([document]);

        Assert.Single(result.AcceptedDocuments);
        Assert.Single(result.Issues);

        var issue = result.Issues.Single();

        Assert.Equal(
            ImportIssueSeverity.Warning,
            issue.Severity);

        Assert.Equal(
            "Прокладка маслоуказателя.m3d",
            issue.FileName);
    }

    [Fact]
    public void Validate_WithDuplicateDesignations_RejectsAllDuplicates()
    {
        var firstDocument = new CadDocument(
            1,
            "Шайба стопорная.m3d",
            ObjectType.Part,
            "РДЦЛ.304112.710",
            "Шайба стопорная",
            "Сталь",
            0.15m,
            []);

        var secondDocument = new CadDocument(
            1,
            "Шайба упорная.m3d",
            ObjectType.Part,
            "РДЦЛ.304112.710",
            "Шайба упорная",
            "Сталь",
            0.12m,
            []);

        var validator = new CadDocumentValidator();

        var result = validator.Validate(
            [firstDocument, secondDocument]);

        Assert.Empty(result.AcceptedDocuments);

        Assert.Equal(
            2,
            result.Issues.Count(issue =>
                issue.Severity == ImportIssueSeverity.Error));

        Assert.Contains(
            result.Issues,
            issue => issue.FileName == "Шайба стопорная.m3d");

        Assert.Contains(
            result.Issues,
            issue => issue.FileName == "Шайба упорная.m3d");
    }

    [Fact]
    public void Validate_WithNonPositiveComponentQuantity_RejectsAssembly()
    {
        var component = new CadDocument(
            1,
            "Пробка сливная.m3d",
            ObjectType.Part,
            "РДЦЛ.304112.901",
            "Пробка сливная",
            "Сталь",
            0.25m,
            []);

        var assembly = new CadDocument(
            1,
            "Пробка сливная в сборе.a3d",
            ObjectType.Assembly,
            "РДЦЛ.304112.900",
            "Пробка сливная в сборе",
            null,
            null,
            [
                new CadComponent(
                "Пробка сливная.m3d",
                0)
            ]);

        var validator = new CadDocumentValidator();

        var result = validator.Validate(
            [assembly, component]);

        Assert.DoesNotContain(
            result.AcceptedDocuments,
            document =>
                document.FileName == "Пробка сливная в сборе.a3d");

        Assert.Contains(
            result.AcceptedDocuments,
            document =>
                document.FileName == "Пробка сливная.m3d");

        Assert.Single(result.Issues);

        var issue = result.Issues.Single();

        Assert.Equal(
            ImportIssueSeverity.Error,
            issue.Severity);

        Assert.Equal(
            "Пробка сливная в сборе.a3d",
            issue.FileName);

        Assert.Contains(
            "invalid quantity",
            issue.Reason,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WhenAssemblyReferencesMissingComponent_RejectsAssembly()
    {
        var assembly = new CadDocument(
            1,
            "Привод масляного насоса.a3d",
            ObjectType.Assembly,
            "РДЦЛ.304112.950",
            "Привод масляного насоса",
            null,
            null,
            [
                new CadComponent(
                "Насос шестерённый НШ-10.m3d",
                1)
            ]);

        var validator = new CadDocumentValidator();

        var result = validator.Validate([assembly]);

        Assert.Empty(result.AcceptedDocuments);
        Assert.Single(result.Issues);

        var issue = result.Issues.Single();

        Assert.Equal(
            ImportIssueSeverity.Error,
            issue.Severity);

        Assert.Equal(
            "Привод масляного насоса.a3d",
            issue.FileName);

        Assert.Contains(
            "was not found",
            issue.Reason,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WhenNestedComponentIsRejected_RejectsParentAssembliesTransitively()
    {
        var invalidPart = new CadDocument(
            1,
            "Невалидная деталь.m3d",
            ObjectType.Part,
            "PДЦЛ.304112.001",
            "Невалидная деталь",
            "Сталь",
            1.5m,
            []);

        var childAssembly = new CadDocument(
            1,
            "Дочерняя сборка.a3d",
            ObjectType.Assembly,
            "РДЦЛ.304112.002",
            "Дочерняя сборка",
            null,
            null,
            [
                new CadComponent(
                "Невалидная деталь.m3d",
                1)
            ]);

        var rootAssembly = new CadDocument(
            1,
            "Главная сборка.a3d",
            ObjectType.Assembly,
            "РДЦЛ.304112.003",
            "Главная сборка",
            null,
            null,
            [
                new CadComponent(
                "Дочерняя сборка.a3d",
                1)
            ]);

        var validator = new CadDocumentValidator();

        var result = validator.Validate(
            [
                rootAssembly,
            childAssembly,
            invalidPart
            ]);

        Assert.Empty(result.AcceptedDocuments);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.FileName == "Невалидная деталь.m3d" &&
                issue.Severity == ImportIssueSeverity.Error);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.FileName == "Дочерняя сборка.a3d" &&
                issue.Reason.Contains(
                    "was rejected",
                    StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            result.Issues,
            issue =>
                issue.FileName == "Главная сборка.a3d" &&
                issue.Reason.Contains(
                    "was rejected",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithDuplicateStandardPartNames_RejectsAllDuplicates()
    {
        var firstDocument = new CadDocument(
            1,
            "Подшипник первый.m3d",
            ObjectType.StandardPart,
            null,
            "Подшипник 210",
            null,
            0.37m,
            []);

        var secondDocument = new CadDocument(
            1,
            "Подшипник второй.m3d",
            ObjectType.StandardPart,
            null,
            "Подшипник 210",
            null,
            0.37m,
            []);

        var validator = new CadDocumentValidator();

        var result = validator.Validate(
            [firstDocument, secondDocument]);

        Assert.Empty(result.AcceptedDocuments);

        Assert.Equal(
            2,
            result.Issues.Count(issue =>
                issue.Severity == ImportIssueSeverity.Error));

        Assert.Contains(
            result.Issues,
            issue =>
                issue.FileName == "Подшипник первый.m3d" &&
                issue.Reason.Contains(
                    "Duplicate standard part name",
                    StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            result.Issues,
            issue =>
                issue.FileName == "Подшипник второй.m3d" &&
                issue.Reason.Contains(
                    "Duplicate standard part name",
                    StringComparison.OrdinalIgnoreCase));
    }
}