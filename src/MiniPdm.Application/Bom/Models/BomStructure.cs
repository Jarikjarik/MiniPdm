namespace MiniPdm.Application.Bom.Models;

public sealed record BomStructure(
    BomObject Root,
    IReadOnlyList<BomTreeNode> Nodes);