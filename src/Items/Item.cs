using Godot;

namespace NullAndVoid.Items;

/// <summary>
/// Base class for all items/modules in the game.
/// </summary>
public partial class Item : Resource
{
    /// <summary>
    /// Unique identifier for this item instance.
    /// </summary>
    [Export] public string Id { get; set; } = System.Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the item.
    /// </summary>
    [Export] public string Name { get; set; } = "Unknown Module";

    /// <summary>
    /// Short description for equipment bar.
    /// </summary>
    [Export] public string ShortDesc { get; set; } = "";

    /// <summary>
    /// Full description for inventory screen.
    /// </summary>
    [Export] public string Description { get; set; } = "An unidentified module.";

    /// <summary>
    /// The type of equipment slot this item can be equipped to.
    /// </summary>
    [Export] public EquipmentSlotType SlotType { get; set; } = EquipmentSlotType.Any;

    /// <summary>
    /// Rarity level of the item.
    /// </summary>
    [Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    /// <summary>
    /// Visual representation (for future sprite support).
    /// </summary>
    [Export] public Color DisplayColor { get; set; } = new Color(0.7f, 0.7f, 0.7f);

    // Stats modifiers when equipped
    [Export] public int BonusHealth { get; set; } = 0;
    [Export] public int BonusDamage { get; set; } = 0;
    [Export] public int BonusArmor { get; set; } = 0;
    [Export] public int BonusSightRange { get; set; } = 0;

    /// <summary>
    /// Create a copy of this item.
    /// </summary>
    public Item Clone()
    {
        return new Item
        {
            Id = System.Guid.NewGuid().ToString(),
            Name = Name,
            ShortDesc = ShortDesc,
            Description = Description,
            SlotType = SlotType,
            Rarity = Rarity,
            DisplayColor = DisplayColor,
            BonusHealth = BonusHealth,
            BonusDamage = BonusDamage,
            BonusArmor = BonusArmor,
            BonusSightRange = BonusSightRange
        };
    }

    /// <summary>
    /// Get formatted stats string.
    /// </summary>
    public string GetStatsString()
    {
        var stats = new System.Collections.Generic.List<string>();

        if (BonusDamage != 0)
            stats.Add($"DMG {(BonusDamage > 0 ? "+" : "")}{BonusDamage}");
        if (BonusArmor != 0)
            stats.Add($"ARM {(BonusArmor > 0 ? "+" : "")}{BonusArmor}");
        if (BonusHealth != 0)
            stats.Add($"HP {(BonusHealth > 0 ? "+" : "")}{BonusHealth}");
        if (BonusSightRange != 0)
            stats.Add($"VIS {(BonusSightRange > 0 ? "+" : "")}{BonusSightRange}");

        return stats.Count > 0 ? string.Join(" ", stats) : "No stats";
    }
}

/// <summary>
/// Types of equipment slots.
/// </summary>
public enum EquipmentSlotType
{
    Any,        // Can be equipped to any slot
    Core,       // Core modules (primary systems)
    Utility,    // Utility modules (support systems)
    Base        // Base modules (structural/defensive)
}

/// <summary>
/// Item rarity levels.
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
