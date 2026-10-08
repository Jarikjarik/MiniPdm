using MiniPdm.Domain.Enums;
using MiniPdm.Domain.Validation;

namespace MiniPdm.Domain.Entities;

public sealed class PdmObject
{
    private readonly List<ObjectVersion> _versions = [];
    public long Id { get; private set; }

    public ObjectType Type { get; }

    public string? Designation { get; }

    public string Name { get; }

    public IReadOnlyCollection<ObjectVersion> Versions => _versions.AsReadOnly();

    public ObjectVersion? CurrentVersion =>
    _versions
        .Where(v => v.State != VersionState.Cancelled)
        .OrderByDescending(v => v.VersionNumber)
        .FirstOrDefault();

    public PdmObject(
        ObjectType type,
        string? designation,
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Object name cannot be empty.",
                nameof(name));
        }

        if (type == ObjectType.StandardPart)
        {
            if (designation is not null)
            {
                throw new ArgumentException(
                    "Standard part must not have a designation.",
                    nameof(designation));
            }
        }
        else
        {
            if (!DesignationValidator.IsValid(designation))
            {
                throw new ArgumentException(
                    "Assembly or part must have a valid designation.",
                    nameof(designation));
            }
        }

        Type = type;
        Designation = designation;
        Name = name;
    }
    public void AddVersion(ObjectVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        if (_versions.Any(v => v.VersionNumber == version.VersionNumber))
        {
            throw new InvalidOperationException(
                $"Version {version.VersionNumber} already exists.");
        }

        _versions.Add(version);
    }
}