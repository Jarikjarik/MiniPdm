using MiniPdm.Infrastructure.Cad;

namespace MiniPdm.IntegrationTests.Cad;

public class FileSystemCadDocumentLocatorTests
{
    [Fact]
    public async Task GetDocumentPathsAsync_ReturnsOnlyCadFiles()
    {
        var folderPath = Path.Combine(
            Path.GetTempPath(),
            $"MiniPdmTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(folderPath);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(folderPath, "Сборка.a3d"),
                "{}");

            await File.WriteAllTextAsync(
                Path.Combine(folderPath, "Деталь.m3d"),
                "{}");

            await File.WriteAllTextAsync(
                Path.Combine(folderPath, "README.txt"),
                "Not a CAD document");

            var locator = new FileSystemCadDocumentLocator();

            var paths = await locator.GetDocumentPathsAsync(
                folderPath,
                CancellationToken.None);

            Assert.Equal(2, paths.Count);

            Assert.Contains(
                paths,
                path => Path.GetFileName(path) == "Сборка.a3d");

            Assert.Contains(
                paths,
                path => Path.GetFileName(path) == "Деталь.m3d");

            Assert.DoesNotContain(
                paths,
                path => Path.GetFileName(path) == "README.txt");
        }
        finally
        {
            Directory.Delete(folderPath, recursive: true);
        }
    }
}