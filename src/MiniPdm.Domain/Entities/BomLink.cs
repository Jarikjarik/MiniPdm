namespace MiniPdm.Domain.Entities;

public sealed class BomLink
{
    public ObjectVersion ParentVersion { get; }

    public PdmObject ChildObject { get; }

    public int Quantity { get; }

    public BomLink(
        ObjectVersion parentVersion,
        PdmObject childObject,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(parentVersion);
        ArgumentNullException.ThrowIfNull(childObject);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        ParentVersion = parentVersion;
        ChildObject = childObject;
        Quantity = quantity;
    }
}