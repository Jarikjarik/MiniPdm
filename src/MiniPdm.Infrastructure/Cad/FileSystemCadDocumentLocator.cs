using MiniPdm.Application.Abstractions.Cad;

namespace MiniPdm.Infrastructure.Cad;

public sealed class FileSystemCadDocumentLocator : ICadDocumentLocator
{
    public Task<IReadOnlyCollection<string>> GetDocumentPathsAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"CAD folder '{folderPath}' was not found.");
        }

        var paths = Directory
            .EnumerateFiles(
                folderPath,
                "*.*",
                SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var extension = Path.GetExtension(path);

                return extension.Equals(
                           ".a3d",
                           StringComparison.OrdinalIgnoreCase)
                       || extension.Equals(
                           ".m3d",
                           StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<string>>(paths);
    }
}