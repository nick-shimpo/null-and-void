using System.Collections.Generic;
using System.Linq;
using Godot;
using NullAndVoid.Items;

namespace NullAndVoid.Components;

/// <summary>
/// Inventory component for storing items and ammunition.
/// </summary>
public partial class Inventory : Node
{
    [Export] public int MaxSlots { get; set; } = 20;

    private readonly List<Item> _items = new();
    private readonly Dictionary<AmmoType, List<Ammunition>> _ammoStorage = new();

    [Signal] public delegate void ItemAddedEventHandler(Item item);
    [Signal] public delegate void ItemRemovedEventHandler(Item item);
    [Signal] public delegate void InventoryChangedEventHandler();
    [Signal] public delegate void AmmoChangedEventHandler(AmmoType type, int newCount);

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
        _ammoStorage.Clear();
        EmitSignal(SignalName.InventoryChanged);
    }

    #region Ammunition Management

    /// <summary>
    /// Get total ammo count for a specific type.
    /// </summary>
    public int GetAmmoCount(AmmoType type)
    {
        if (!_ammoStorage.TryGetValue(type, out var stacks))
            return 0;

        return stacks.Sum(a => a.Quantity);
    }

    /// <summary>
    /// Check if we have enough ammo of a type.
    /// </summary>
    public bool HasAmmo(AmmoType type, int amount = 1)
    {
        return GetAmmoCount(type) >= amount;
    }

    /// <summary>
    /// Consume ammo from inventory.
    /// Returns true if successful, false if not enough ammo.
    /// </summary>
    public bool ConsumeAmmo(AmmoType type, int amount)
    {
        if (!HasAmmo(type, amount))
            return false;

        if (!_ammoStorage.TryGetValue(type, out var stacks))
            return false;

        int remaining = amount;
        var emptyStacks = new List<Ammunition>();

        foreach (var stack in stacks)
        {
            if (remaining <= 0)
                break;

            int consumed = stack.Consume(remaining);
            remaining -= consumed;

            if (stack.IsEmpty)
                emptyStacks.Add(stack);
        }

        // Remove empty stacks
        foreach (var empty in emptyStacks)
        {
            stacks.Remove(empty);
        }

        EmitSignal(SignalName.AmmoChanged, (int)type, GetAmmoCount(type));
        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    /// <summary>
    /// Add ammunition to inventory.
    /// Stacks with existing ammo of the same type.
    /// </summary>
    public void AddAmmo(Ammunition ammo)
    {
        if (!_ammoStorage.TryGetValue(ammo.Type, out var stacks))
        {
            stacks = new List<Ammunition>();
            _ammoStorage[ammo.Type] = stacks;
        }

        int remaining = ammo.Quantity;

        // Try to stack with existing ammo of same ID
        foreach (var stack in stacks)
        {
            if (remaining <= 0)
                break;

            if (stack.CanStackWith(ammo))
            {
                remaining = stack.Add(remaining);
            }
        }

        // Create new stacks for overflow
        while (remaining > 0)
        {
            var newStack = ammo.Clone(Mathf.Min(remaining, ammo.MaxStack));
            stacks.Add(newStack);
            remaining -= newStack.Quantity;
        }

        EmitSignal(SignalName.AmmoChanged, (int)ammo.Type, GetAmmoCount(ammo.Type));
        EmitSignal(SignalName.InventoryChanged);
    }

    /// <summary>
    /// Get all ammunition stacks of a specific type.
    /// </summary>
    public IReadOnlyList<Ammunition> GetAmmoStacks(AmmoType type)
    {
        if (_ammoStorage.TryGetValue(type, out var stacks))
            return stacks;
        return new List<Ammunition>();
    }

    /// <summary>
    /// Get all ammunition in inventory.
    /// </summary>
    public IEnumerable<Ammunition> GetAllAmmo()
    {
        return _ammoStorage.Values.SelectMany(s => s);
    }

    /// <summary>
    /// Get a summary of all ammo counts by type.
    /// </summary>
    public Dictionary<AmmoType, int> GetAmmoCounts()
    {
        var counts = new Dictionary<AmmoType, int>();
        foreach (var kvp in _ammoStorage)
        {
            counts[kvp.Key] = kvp.Value.Sum(a => a.Quantity);
        }
        return counts;
    }

    #endregion
}
