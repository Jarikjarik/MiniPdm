using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Application.Import;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Application.Tests.Import;

public class CadFolderReaderTests
{
    [Fact]
    public async Task ReadAsync_WhenOneDocumentFails_ContinuesReadingOtherDocuments()
    {
        var locator = new FakeCadDocumentLocator(
        [
            "good.m3d",
            "broken.m3d"
        ]);

        var reader = new FakeCadDocumentReader();

        var service = new CadFolderReader(
            locator,
            reader);

        var result = await service.ReadAsync(
            "ignored-folder",
            CancellationToken.None);

        Assert.Single(result.Documents);
        Assert.Single(result.Errors);

        Assert.Equal(
            "good.m3d",
            result.Documents.Single().FileName);

        Assert.Equal(
            "broken.m3d",
            result.Errors.Single().Path);
    }

    private sealed class FakeCadDocumentLocator
        : ICadDocumentLocator
    {
        private readonly IReadOnlyCollection<string> _paths;

        public FakeCadDocumentLocator(
            IReadOnlyCollection<string> paths)
        {
            _paths = paths;
        }

        public Task<IReadOnlyCollection<string>> GetDocumentPathsAsync(
            string folderPath,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_paths);
        }
    }

    private sealed class FakeCadDocumentReader
        : ICadDocumentReader
    {
        public Task<CadDocument> ReadAsync(
            string path,
            CancellationToken cancellationToken)
        {
            if (path == "broken.m3d")
            {
                throw new InvalidDataException(
                    "Invalid CAD document.");
            }

            return Task.FromResult(
                new CadDocument(
                    1,
                    path,
                    ObjectType.Part,
                    "РДЦЛ.304112.302",
                    "Колесо зубчатое",
                    "Сталь 40Х",
                    5.86m,
                    []));
        }
    }
}