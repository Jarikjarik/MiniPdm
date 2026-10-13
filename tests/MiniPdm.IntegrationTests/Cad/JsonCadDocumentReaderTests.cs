using MiniPdm.Domain.Enums;
using MiniPdm.Infrastructure.Cad;

namespace MiniPdm.IntegrationTests.Cad;

public class JsonCadDocumentReaderTests
{
    [Fact]
    public async Task ReadAsync_WithValidPartJson_ReturnsCadDocument()
    {
        var folderPath = CreateTempDirectory();

        var path = Path.Combine(
            folderPath,
            "Колесо зубчатое.m3d");

        try
        {
            var json = """
            {
              "formatVersion": 1,
              "fileName": "Колесо зубчатое.m3d",
              "type": "Part",
              "designation": "РДЦЛ.304112.302",
              "name": "Колесо зубчатое",
              "properties": {
                "material": "Сталь 40Х",
                "mass": 5.86
              },
              "components": []
            }
            """;

            await File.WriteAllTextAsync(
                path,
                json);

            var reader =
                new JsonCadDocumentReader();

            var document =
                await reader.ReadAsync(
                    path,
                    CancellationToken.None);

            Assert.Equal(
                1,
                document.FormatVersion);

            Assert.Equal(
                "Колесо зубчатое.m3d",
                document.FileName);

            Assert.Equal(
                ObjectType.Part,
                document.Type);

            Assert.Equal(
                "РДЦЛ.304112.302",
                document.Designation);

            Assert.Equal(
                "Колесо зубчатое",
                document.Name);

            Assert.Equal(
                "Сталь 40Х",
                document.Material);

            Assert.Equal(
                5.86m,
                document.MassKg);

            Assert.Empty(
                document.Components);
        }
        finally
        {
            Directory.Delete(
                folderPath,
                recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_WithAssemblyJson_ReturnsComponents()
    {
        var folderPath = CreateTempDirectory();

        var path = Path.Combine(
            folderPath,
            "Вал промежуточный в сборе.a3d");

        try
        {
            var json = """
            {
              "formatVersion": 1,
              "fileName": "Вал промежуточный в сборе.a3d",
              "type": "Assembly",
              "designation": "РДЦЛ.304112.300",
              "name": "Вал промежуточный в сборе",
              "properties": {
                "material": null,
                "mass": null
              },
              "components": [
                {
                  "file": "Вал промежуточный.m3d",
                  "count": 1
                },
                {
                  "file": "Колесо зубчатое.m3d",
                  "count": 1
                },
                {
                  "file": "Подшипник 208 ГОСТ 8338-75.m3d",
                  "count": 2
                }
              ]
            }
            """;

            await File.WriteAllTextAsync(
                path,
                json);

            var reader =
                new JsonCadDocumentReader();

            var document =
                await reader.ReadAsync(
                    path,
                    CancellationToken.None);

            Assert.Equal(
                ObjectType.Assembly,
                document.Type);

            Assert.Null(
                document.Material);

            Assert.Null(
                document.MassKg);

            Assert.Equal(
                3,
                document.Components.Count);

            Assert.Contains(
                document.Components,
                component =>
                    component.FileName ==
                    "Вал промежуточный.m3d" &&
                    component.Quantity == 1);

            Assert.Contains(
                document.Components,
                component =>
                    component.FileName ==
                    "Колесо зубчатое.m3d" &&
                    component.Quantity == 1);

            Assert.Contains(
                document.Components,
                component =>
                    component.FileName ==
                    "Подшипник 208 ГОСТ 8338-75.m3d" &&
                    component.Quantity == 2);
        }
        finally
        {
            Directory.Delete(
                folderPath,
                recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_WithRealCadExportFile_ReturnsExpectedDocument()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Колесо зубчатое.m3d");

        var reader =
            new JsonCadDocumentReader();

        var document =
            await reader.ReadAsync(
                path,
                CancellationToken.None);

        Assert.Equal(
            "Колесо зубчатое.m3d",
            document.FileName);

        Assert.Equal(
            ObjectType.Part,
            document.Type);

        Assert.Equal(
            "РДЦЛ.304112.302",
            document.Designation);

        Assert.Equal(
            "Колесо зубчатое",
            document.Name);

        Assert.Equal(
            "Сталь 40Х",
            document.Material);

        Assert.Equal(
            5.86m,
            document.MassKg);

        Assert.Empty(
            document.Components);
    }

    [Fact]
    public async Task ReadAsync_WhenJsonFileNameDoesNotMatchDiskFileName_ThrowsInvalidDataException()
    {
        var folderPath = CreateTempDirectory();

        var path = Path.Combine(
            folderPath,
            "Фактическое имя.m3d");

        try
        {
            var json = """
            {
              "formatVersion": 1,
              "fileName": "Другое имя.m3d",
              "type": "Part",
              "designation": "ТЕСТ.123456.701",
              "name": "Тестовая деталь",
              "properties": {
                "material": "Сталь",
                "mass": 1.0
              },
              "components": []
            }
            """;

            await File.WriteAllTextAsync(
                path,
                json);

            var reader =
                new JsonCadDocumentReader();

            var exception =
                await Assert.ThrowsAsync<InvalidDataException>(
                    () => reader.ReadAsync(
                        path,
                        CancellationToken.None));

            Assert.Contains(
                "does not match disk file name",
                exception.Message,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(
                folderPath,
                recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_WithUnsupportedFormatVersion_ThrowsInvalidDataException()
    {
        var folderPath = CreateTempDirectory();

        var path = Path.Combine(
            folderPath,
            "Деталь.m3d");

        try
        {
            var json = """
            {
              "formatVersion": 2,
              "fileName": "Деталь.m3d",
              "type": "Part",
              "designation": "ТЕСТ.123456.702",
              "name": "Деталь",
              "properties": {
                "material": "Сталь",
                "mass": 1.0
              },
              "components": []
            }
            """;

            await File.WriteAllTextAsync(
                path,
                json);

            var reader =
                new JsonCadDocumentReader();

            var exception =
                await Assert.ThrowsAsync<InvalidDataException>(
                    () => reader.ReadAsync(
                        path,
                        CancellationToken.None));

            Assert.Contains(
                "Unsupported CAD format version",
                exception.Message,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(
                folderPath,
                recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"MiniPdmTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(
            path);

        return path;
    }
}