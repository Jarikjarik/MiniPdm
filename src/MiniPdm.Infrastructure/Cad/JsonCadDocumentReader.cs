using System.Text.Json;
using MiniPdm.Application.Abstractions.Cad;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Infrastructure.Cad;

public sealed class JsonCadDocumentReader : ICadDocumentReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<CadDocument> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);

        var dto = await JsonSerializer.DeserializeAsync<CadDocumentDto>(
            stream,
            JsonOptions,
            cancellationToken);

        if (dto is null)
        {
            throw new InvalidDataException(
                $"CAD document '{path}' is empty or invalid.");
        }

        if (!Enum.TryParse<ObjectType>(
                dto.Type,
                ignoreCase: false,
                out var objectType))
        {
            throw new InvalidDataException(
                $"Unknown CAD object type '{dto.Type}'.");
        }

        var components = (dto.Components ?? [])
            .Select(component => new CadComponent(
                component.File ?? string.Empty,
                component.Count))
            .ToArray();

        return new CadDocument(
            dto.FormatVersion,
            dto.FileName ?? string.Empty,
            objectType,
            dto.Designation,
            dto.Name ?? string.Empty,
            dto.Properties?.Material,
            dto.Properties?.Mass,
            components);
    }

    private sealed class CadDocumentDto
    {
        public int FormatVersion { get; init; }

        public string? FileName { get; init; }

        public string? Type { get; init; }

        public string? Designation { get; init; }

        public string? Name { get; init; }

        public CadPropertiesDto? Properties { get; init; }

        public List<CadComponentDto>? Components { get; init; }
    }

    private sealed class CadPropertiesDto
    {
        public string? Material { get; init; }

        public decimal? Mass { get; init; }
    }

    private sealed class CadComponentDto
    {
        public string? File { get; init; }

        public int Count { get; init; }
    }
}