using Godot;
using System.Collections.Generic;
using NullAndVoid.Items;

namespace NullAndVoid.Components;

/// <summary>
/// Inventory component for storing items.
/// </summary>
public partial class Inventory : Node
{
    [Export] public int MaxSlots { get; set; } = 20;

    private readonly List<Item> _items = new();

    [Signal] public delegate void ItemAddedEventHandler(Item item);
    [Signal] public delegate void ItemRemovedEventHandler(Item item);
    [Signal] public delegate void InventoryChangedEventHandler();

    public IReadOnlyList<Item> Items => _items;
    public int ItemCount => _items.Count;
    public int FreeSlots => MaxSlots - _items.Count;
    public bool IsFull => _items.Count >= MaxSlots;

    /// <summary>
    /// Add an item to the inventory.
    /// </summary>
    public bool AddItem(Item item)
    {
        if (IsFull)
        {
            GD.Print("Inventory is full!");
            return false;
        }

        _items.Add(item);
        EmitSignal(SignalName.ItemAdded, item);
        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    /// <summary>
    /// Remove an item from the inventory.
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (_items.Remove(item))
        {
            EmitSignal(SignalName.ItemRemoved, item);
            EmitSignal(SignalName.InventoryChanged);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove an item by index.
    /// </summary>
    public Item? RemoveItemAt(int index)
    {
        if (index < 0 || index >= _items.Count)
            return null;

        var item = _items[index];
        _items.RemoveAt(index);
        EmitSignal(SignalName.ItemRemoved, item);
        EmitSignal(SignalName.InventoryChanged);
        return item;
    }

    /// <summary>
    /// Get an item by index.
    /// </summary>
    public Item? GetItem(int index)
    {
        if (index < 0 || index >= _items.Count)
            return null;
        return _items[index];
    }

    /// <summary>
    /// Check if inventory contains an item.
    /// </summary>
    public bool HasItem(Item item)
    {
        return _items.Contains(item);
    }

    /// <summary>
    /// Find items by slot type.
    /// </summary>
    public List<Item> GetItemsBySlotType(EquipmentSlotType slotType)
    {
        var result = new List<Item>();
        foreach (var item in _items)
        {
            if (item.SlotType == slotType || item.SlotType == EquipmentSlotType.Any)
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// Clear all items from inventory.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        EmitSignal(SignalName.InventoryChanged);
    }
}
