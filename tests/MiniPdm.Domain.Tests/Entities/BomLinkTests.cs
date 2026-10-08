using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Domain.Tests.Entities;

public class BomLinkTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesBomLink()
    {
        var parentVersion = CreateParentVersion();
        var childObject = CreateChildObject();

        var link = new BomLink(
            parentVersion,
            childObject,
            2);

        Assert.Equal(parentVersion, link.ParentVersion);
        Assert.Equal(childObject, link.ChildObject);
        Assert.Equal(2, link.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_WithInvalidQuantity_ThrowsArgumentOutOfRangeException(
        int quantity)
    {
        var parentVersion = CreateParentVersion();
        var childObject = CreateChildObject();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BomLink(
                parentVersion,
                childObject,
                quantity));
    }

    [Fact]
    public void Constructor_WithNullParentVersion_ThrowsArgumentNullException()
    {
        var childObject = CreateChildObject();

        Assert.Throws<ArgumentNullException>(() =>
            new BomLink(
                null!,
                childObject,
                1));
    }

    [Fact]
    public void Constructor_WithNullChildObject_ThrowsArgumentNullException()
    {
        var parentVersion = CreateParentVersion();

        Assert.Throws<ArgumentNullException>(() =>
            new BomLink(
                parentVersion,
                null!,
                1));
    }

    private static ObjectVersion CreateParentVersion()
    {
        return new ObjectVersion(
            1,
            VersionState.InWork,
            null,
            null,
            DateTimeOffset.UtcNow);
    }

    private static PdmObject CreateChildObject()
    {
        return new PdmObject(
            ObjectType.Part,
            "РДЦЛ.304112.302",
            "Колесо зубчатое");
    }
}