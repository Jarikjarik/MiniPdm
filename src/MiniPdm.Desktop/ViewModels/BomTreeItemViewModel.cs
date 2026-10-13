using System.Collections.ObjectModel;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Desktop.ViewModels;

public sealed class BomTreeItemViewModel
{
    public BomTreeItemViewModel(
        long objectId,
        ObjectType objectType,
        string? designation,
        string name,
        int quantity,
        long totalQuantity,
        bool isCycle)
    {
        ObjectId = objectId;
        ObjectType = objectType;
        Designation = designation;
        Name = name;
        Quantity = quantity;
        TotalQuantity = totalQuantity;
        IsCycle = isCycle;
    }

    public long ObjectId { get; }

    public ObjectType ObjectType { get; }

    public string? Designation { get; }

    public string Name { get; }

    public int Quantity { get; }

    public long TotalQuantity { get; }

    public bool IsCycle { get; }

    public ObservableCollection<BomTreeItemViewModel> Children { get; } = [];

    public string DisplayText
    {
        get
        {
            var designation =
                string.IsNullOrWhiteSpace(Designation)
                    ? string.Empty
                    : $"{Designation} — ";

            var quantity =
                Quantity > 1
                    ? $" × {Quantity}"
                    : string.Empty;

            var cycle =
                IsCycle
                    ? " [ЦИКЛ]"
                    : string.Empty;

            return
                $"{designation}{Name}{quantity}{cycle}";
        }
    }
}