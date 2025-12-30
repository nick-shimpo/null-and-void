using Godot;
using System.Collections.Generic;
using NullAndVoid.Items;

namespace NullAndVoid.Components;

/// <summary>
/// Equipment component managing equipped items in slots.
/// Slots: 2 Core, 2 Utility, 2 Base
/// </summary>
public partial class Equipment : Node
{
    // Equipment slots
    private Item?[] _coreSlots = new Item?[2];
    private Item?[] _utilitySlots = new Item?[2];
    private Item?[] _baseSlots = new Item?[2];

    [Signal] public delegate void ItemEquippedEventHandler(Item item, EquipmentSlotType slotType, int slotIndex);
    [Signal] public delegate void ItemUnequippedEventHandler(Item item, EquipmentSlotType slotType, int slotIndex);
    [Signal] public delegate void EquipmentChangedEventHandler();

    // Computed stat bonuses from all equipped items
    public int TotalBonusDamage { get; private set; }
    public int TotalBonusArmor { get; private set; }
    public int TotalBonusHealth { get; private set; }
    public int TotalBonusSightRange { get; private set; }

    /// <summary>
    /// Get all equipped items.
    /// </summary>
    public IEnumerable<Item> GetAllEquippedItems()
    {
        foreach (var item in _coreSlots)
            if (item != null) yield return item;
        foreach (var item in _utilitySlots)
            if (item != null) yield return item;
        foreach (var item in _baseSlots)
            if (item != null) yield return item;
    }

    /// <summary>
    /// Get item in a specific slot.
    /// </summary>
    public Item? GetItemInSlot(EquipmentSlotType slotType, int index)
    {
        var slots = GetSlotArray(slotType);
        if (slots == null || index < 0 || index >= slots.Length)
            return null;
        return slots[index];
    }

    /// <summary>
    /// Equip an item to a slot. Returns the previously equipped item if any.
    /// </summary>
    public Item? Equip(Item item, EquipmentSlotType slotType, int index)
    {
        // Validate slot type compatibility
        if (item.SlotType != EquipmentSlotType.Any && item.SlotType != slotType)
        {
            GD.Print($"Cannot equip {item.Name} to {slotType} slot - requires {item.SlotType}");
            return null;
        }

        var slots = GetSlotArray(slotType);
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        // Get previous item
        var previousItem = slots[index];

        // Equip new item
        slots[index] = item;

        // Emit signals
        if (previousItem != null)
            EmitSignal(SignalName.ItemUnequipped, previousItem, (int)slotType, index);

        EmitSignal(SignalName.ItemEquipped, item, (int)slotType, index);
        EmitSignal(SignalName.EquipmentChanged);

        RecalculateStats();

        return previousItem;
    }

    /// <summary>
    /// Unequip an item from a slot.
    /// </summary>
    public Item? Unequip(EquipmentSlotType slotType, int index)
    {
        var slots = GetSlotArray(slotType);
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        var item = slots[index];
        if (item == null)
            return null;

        slots[index] = null;

        EmitSignal(SignalName.ItemUnequipped, item, (int)slotType, index);
        EmitSignal(SignalName.EquipmentChanged);

        RecalculateStats();

        return item;
    }

    /// <summary>
    /// Find the first empty slot for an item type.
    /// </summary>
    public (EquipmentSlotType slotType, int index)? FindEmptySlot(EquipmentSlotType preferredType)
    {
        // If item can go in any slot, check all in order
        if (preferredType == EquipmentSlotType.Any)
        {
            var coreSlot = FindEmptyInArray(_coreSlots);
            if (coreSlot >= 0) return (EquipmentSlotType.Core, coreSlot);

            var utilitySlot = FindEmptyInArray(_utilitySlots);
            if (utilitySlot >= 0) return (EquipmentSlotType.Utility, utilitySlot);

            var baseSlot = FindEmptyInArray(_baseSlots);
            if (baseSlot >= 0) return (EquipmentSlotType.Base, baseSlot);

            return null;
        }

        // Check preferred slot type
        var slots = GetSlotArray(preferredType);
        if (slots != null)
        {
            var emptyIndex = FindEmptyInArray(slots);
            if (emptyIndex >= 0)
                return (preferredType, emptyIndex);
        }

        return null;
    }

    private int FindEmptyInArray(Item?[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return i;
        }
        return -1;
    }

    private Item?[]? GetSlotArray(EquipmentSlotType slotType)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => _coreSlots,
            EquipmentSlotType.Utility => _utilitySlots,
            EquipmentSlotType.Base => _baseSlots,
            _ => null
        };
    }

    private void RecalculateStats()
    {
        TotalBonusDamage = 0;
        TotalBonusArmor = 0;
        TotalBonusHealth = 0;
        TotalBonusSightRange = 0;

        foreach (var item in GetAllEquippedItems())
        {
            TotalBonusDamage += item.BonusDamage;
            TotalBonusArmor += item.BonusArmor;
            TotalBonusHealth += item.BonusHealth;
            TotalBonusSightRange += item.BonusSightRange;
        }
    }

    /// <summary>
    /// Get a summary of all slots for display.
    /// </summary>
    public List<EquipmentSlotInfo> GetAllSlots()
    {
        var slots = new List<EquipmentSlotInfo>();

        for (int i = 0; i < 2; i++)
            slots.Add(new EquipmentSlotInfo(EquipmentSlotType.Core, i, _coreSlots[i]));
        for (int i = 0; i < 2; i++)
            slots.Add(new EquipmentSlotInfo(EquipmentSlotType.Utility, i, _utilitySlots[i]));
        for (int i = 0; i < 2; i++)
            slots.Add(new EquipmentSlotInfo(EquipmentSlotType.Base, i, _baseSlots[i]));

        return slots;
    }
}

/// <summary>
/// Info about a single equipment slot.
/// </summary>
public record EquipmentSlotInfo(EquipmentSlotType SlotType, int Index, Item? Item)
{
    public string SlotName => $"{SlotType} {Index + 1}";
    public bool IsEmpty => Item == null;
}
