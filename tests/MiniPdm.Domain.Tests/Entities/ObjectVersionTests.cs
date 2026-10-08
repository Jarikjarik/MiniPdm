using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Domain.Tests.Entities;

public class ObjectVersionTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesVersion()
    {
        var createdAt = DateTimeOffset.UtcNow;

        var version = new ObjectVersion(
            1,
            VersionState.InWork,
            "Сталь 40Х",
            5.86m,
            createdAt);

        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(VersionState.InWork, version.State);
        Assert.Equal("Сталь 40Х", version.Material);
        Assert.Equal(5.86m, version.MassKg);
        Assert.Equal(createdAt, version.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidVersionNumber_ThrowsArgumentOutOfRangeException(
        int versionNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ObjectVersion(
                versionNumber,
                VersionState.InWork,
                "Сталь 40Х",
                5.86m,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_WithNegativeMass_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ObjectVersion(
                1,
                VersionState.InWork,
                "Сталь 40Х",
                -1m,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_WithNullMass_CreatesVersion()
    {
        var version = new ObjectVersion(
            1,
            VersionState.InWork,
            "Сталь 40Х",
            null,
            DateTimeOffset.UtcNow);

        Assert.Null(version.MassKg);
    }

    [Fact]
    public void ChangeState_FromInWorkToApproved_ChangesState()
    {
        var version = CreateInWorkVersion();

        version.ChangeState(VersionState.Approved);

        Assert.Equal(VersionState.Approved, version.State);
    }

    [Fact]
    public void ChangeState_FromApprovedToCancelled_ChangesState()
    {
        var version = CreateInWorkVersion();

        version.ChangeState(VersionState.Approved);
        version.ChangeState(VersionState.Cancelled);

        Assert.Equal(VersionState.Cancelled, version.State);
    }

    [Fact]
    public void ChangeState_WithForbiddenTransition_ThrowsInvalidOperationException()
    {
        var version = CreateInWorkVersion();

        version.ChangeState(VersionState.Approved);

        Assert.Throws<InvalidOperationException>(() =>
            version.ChangeState(VersionState.InWork));
    }

    private static ObjectVersion CreateInWorkVersion()
    {
        return new ObjectVersion(
            1,
            VersionState.InWork,
            "Сталь 40Х",
            5.86m,
            DateTimeOffset.UtcNow);
    }
}