using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Domain.Tests.Entities;

public class PdmObjectTests
{
    [Fact]
    public void Constructor_WithValidPart_CreatesObject()
    {
        var pdmObject = new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");

        Assert.Equal(ObjectType.Part, pdmObject.Type);
        Assert.Equal("РДЦЛ.304112.302", pdmObject.Designation);
        Assert.Equal("Колесо зубчатое", pdmObject.Name);
    }

    [Fact]
    public void Constructor_WithValidAssembly_CreatesObject()
    {
        var pdmObject = new PdmObject(
            ObjectType.Assembly,
            "РДЦЛ.304112.300",
            "Вал промежуточный в сборе");

        Assert.Equal(ObjectType.Assembly, pdmObject.Type);
        Assert.Equal("РДЦЛ.304112.300", pdmObject.Designation);
    }

    [Fact]
    public void Constructor_WithStandardPartWithoutDesignation_CreatesObject()
    {
        var pdmObject = new PdmObject(
            ObjectType.StandardPart,
            null,
            "Подшипник 208 ГОСТ 8338-75");

        Assert.Equal(ObjectType.StandardPart, pdmObject.Type);
        Assert.Null(pdmObject.Designation);
    }

    [Fact]
    public void Constructor_WithPartWithoutDesignation_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PdmObject(
                ObjectType.Part,
                null,
                "Колесо зубчатое"));
    }

    [Fact]
    public void Constructor_WithInvalidDesignation_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PdmObject(
                ObjectType.Part,
                "РДЦЛ.30411.302",
                "Колесо зубчатое"));
    }

    [Fact]
    public void Constructor_WithStandardPartAndDesignation_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PdmObject(
                ObjectType.StandardPart,
                "РДЦЛ.304112.302",
                "Подшипник"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new PdmObject(
                ObjectType.StandardPart,
                null,
                name!));
    }

    [Fact]
    public void AddVersion_WithNewVersion_AddsVersion()
    {
        var pdmObject = new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");

        var version = new ObjectVersion(
            1,
            VersionState.InWork,
            "Сталь 40Х",
            5.86m,
            DateTimeOffset.UtcNow);

        pdmObject.AddVersion(version);

        Assert.Single(pdmObject.Versions);
        Assert.Contains(version, pdmObject.Versions);
    }

    [Fact]
    public void AddVersion_WithDuplicateVersionNumber_ThrowsInvalidOperationException()
    {
        var pdmObject = new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");

        pdmObject.AddVersion(new ObjectVersion(
            1,
            VersionState.InWork,
            "Сталь 40Х",
            5.86m,
            DateTimeOffset.UtcNow));

        var duplicateVersion = new ObjectVersion(
            1,
            VersionState.InWork,
            "Сталь 40Х",
            5.92m,
            DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            pdmObject.AddVersion(duplicateVersion));
    }

    [Fact]
    public void AddVersion_WithNullVersion_ThrowsArgumentNullException()
    {
        var pdmObject = new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");

        Assert.Throws<ArgumentNullException>(() =>
            pdmObject.AddVersion(null!));
    }

    [Fact]
    public void CurrentVersion_ReturnsVersionWithHighestNumber()
    {
        var pdmObject = new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");

        pdmObject.AddVersion(new ObjectVersion(
            1,
            VersionState.Approved,
            "Сталь 40Х",
            5.86m,
            DateTimeOffset.UtcNow));

        var latestVersion = new ObjectVersion(
            2,
            VersionState.InWork,
            "Сталь 40Х",
            5.92m,
            DateTimeOffset.UtcNow);

        pdmObject.AddVersion(latestVersion);

        Assert.Equal(latestVersion, pdmObject.CurrentVersion);
    }

    [Fact]
    public void CurrentVersion_SkipsCancelledVersion()
    {
        var pdmObject = new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");

        var approvedVersion = new ObjectVersion(
            1,
            VersionState.Approved,
            "Сталь 40Х",
            5.86m,
            DateTimeOffset.UtcNow);

        var cancelledVersion = new ObjectVersion(
            2,
            VersionState.InWork,
            "Сталь 40Х",
            5.92m,
            DateTimeOffset.UtcNow);

        cancelledVersion.ChangeState(VersionState.Cancelled);

        pdmObject.AddVersion(approvedVersion);
        pdmObject.AddVersion(cancelledVersion);

        Assert.Equal(approvedVersion, pdmObject.CurrentVersion);
    }

    [Fact]
    public void CurrentVersion_WhenAllVersionsAreCancelled_ReturnsNull()
    {
        var pdmObject = new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");

        var version = new ObjectVersion(
            1,
            VersionState.InWork,
            "Сталь 40Х",
            5.86m,
            DateTimeOffset.UtcNow);

        version.ChangeState(VersionState.Cancelled);
        pdmObject.AddVersion(version);

        Assert.Null(pdmObject.CurrentVersion);
    }
}