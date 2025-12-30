using Godot;
using System;
using System.Collections.Generic;

namespace NullAndVoid.Items;

/// <summary>
/// Factory for creating items with random or predefined stats.
/// </summary>
public static class ItemFactory
{
    private static readonly Random _random = new();

    // Item name prefixes by rarity
    private static readonly string[] CommonPrefixes = { "Basic", "Standard", "Simple", "Old" };
    private static readonly string[] UncommonPrefixes = { "Enhanced", "Improved", "Modified", "Tuned" };
    private static readonly string[] RarePrefixes = { "Advanced", "Superior", "Refined", "Precision" };
    private static readonly string[] EpicPrefixes = { "Elite", "Prototype", "Experimental", "Quantum" };
    private static readonly string[] LegendaryPrefixes = { "Ancient", "Mythic", "Omega", "Prime" };

    // Base module names by slot type
    private static readonly Dictionary<EquipmentSlotType, string[]> ModuleNames = new()
    {
        { EquipmentSlotType.Core, new[] { "Processor", "Power Core", "Combat Matrix", "Targeting System", "Neural Link" } },
        { EquipmentSlotType.Utility, new[] { "Scanner", "Shield Generator", "Repair Module", "Sensor Array", "Cloak Device" } },
        { EquipmentSlotType.Base, new[] { "Armor Plating", "Chassis", "Frame Reinforcement", "Hull Module", "Structural Core" } }
    };

    // Rarity colors
    private static readonly Dictionary<ItemRarity, Color> RarityColors = new()
    {
        { ItemRarity.Common, new Color(0.7f, 0.7f, 0.7f) },
        { ItemRarity.Uncommon, new Color(0.3f, 0.9f, 0.3f) },
        { ItemRarity.Rare, new Color(0.3f, 0.5f, 1.0f) },
        { ItemRarity.Epic, new Color(0.7f, 0.3f, 0.9f) },
        { ItemRarity.Legendary, new Color(1.0f, 0.6f, 0.1f) }
    };

    /// <summary>
    /// Create a random item of the specified slot type and rarity.
    /// </summary>
    public static Item CreateRandomItem(EquipmentSlotType slotType = EquipmentSlotType.Any, ItemRarity? rarity = null)
    {
        // Determine slot type if Any
        if (slotType == EquipmentSlotType.Any)
        {
            var types = new[] { EquipmentSlotType.Core, EquipmentSlotType.Utility, EquipmentSlotType.Base };
            slotType = types[_random.Next(types.Length)];
        }

        // Determine rarity if not specified
        rarity ??= RollRarity();

        // Get name components
        string prefix = GetPrefix(rarity.Value);
        string baseName = ModuleNames[slotType][_random.Next(ModuleNames[slotType].Length)];

        // Create item
        var item = new Item
        {
            Name = $"{prefix} {baseName}",
            SlotType = slotType,
            Rarity = rarity.Value,
            DisplayColor = RarityColors[rarity.Value]
        };

        // Generate stats based on rarity and slot type
        GenerateStats(item, slotType, rarity.Value);

        // Generate descriptions
        item.ShortDesc = item.GetStatsString();
        item.Description = GenerateDescription(item);

        return item;
    }

    /// <summary>
    /// Roll for rarity with weighted probability.
    /// </summary>
    private static ItemRarity RollRarity()
    {
        int roll = _random.Next(100);

        if (roll < 50) return ItemRarity.Common;      // 50%
        if (roll < 75) return ItemRarity.Uncommon;    // 25%
        if (roll < 90) return ItemRarity.Rare;        // 15%
        if (roll < 98) return ItemRarity.Epic;        // 8%
        return ItemRarity.Legendary;                   // 2%
    }

    private static string GetPrefix(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => CommonPrefixes[_random.Next(CommonPrefixes.Length)],
            ItemRarity.Uncommon => UncommonPrefixes[_random.Next(UncommonPrefixes.Length)],
            ItemRarity.Rare => RarePrefixes[_random.Next(RarePrefixes.Length)],
            ItemRarity.Epic => EpicPrefixes[_random.Next(EpicPrefixes.Length)],
            ItemRarity.Legendary => LegendaryPrefixes[_random.Next(LegendaryPrefixes.Length)],
            _ => "Unknown"
        };
    }

    private static void GenerateStats(Item item, EquipmentSlotType slotType, ItemRarity rarity)
    {
        int rarityMultiplier = (int)rarity + 1;

        // Slot type determines primary stat focus
        switch (slotType)
        {
            case EquipmentSlotType.Core:
                // Core modules focus on damage and sight
                item.BonusDamage = _random.Next(1, 3) * rarityMultiplier;
                if (_random.Next(2) == 0)
                    item.BonusSightRange = _random.Next(1, 2) * rarityMultiplier;
                break;

            case EquipmentSlotType.Utility:
                // Utility modules are varied
                int utilType = _random.Next(3);
                if (utilType == 0)
                    item.BonusSightRange = _random.Next(1, 3) * rarityMultiplier;
                else if (utilType == 1)
                    item.BonusArmor = _random.Next(1, 2) * rarityMultiplier;
                else
                    item.BonusHealth = _random.Next(3, 8) * rarityMultiplier;
                break;

            case EquipmentSlotType.Base:
                // Base modules focus on defense
                item.BonusArmor = _random.Next(1, 3) * rarityMultiplier;
                item.BonusHealth = _random.Next(2, 6) * rarityMultiplier;
                break;
        }
    }

    private static string GenerateDescription(Item item)
    {
        string slotDesc = item.SlotType switch
        {
            EquipmentSlotType.Core => "A core processing module",
            EquipmentSlotType.Utility => "A utility support module",
            EquipmentSlotType.Base => "A base structural module",
            _ => "A mysterious module"
        };

        string rarityDesc = item.Rarity switch
        {
            ItemRarity.Common => "of standard manufacture",
            ItemRarity.Uncommon => "with enhanced capabilities",
            ItemRarity.Rare => "of superior craftsmanship",
            ItemRarity.Epic => "with experimental technology",
            ItemRarity.Legendary => "of ancient and powerful origin",
            _ => ""
        };

        return $"{slotDesc} {rarityDesc}.\n\n{item.GetStatsString()}";
    }

    /// <summary>
    /// Create a specific predefined item.
    /// </summary>
    public static Item CreateStarterWeapon()
    {
        return new Item
        {
            Name = "Salvaged Blaster",
            ShortDesc = "DMG +2",
            Description = "A basic weapon module salvaged from the ruins. It still works... mostly.",
            SlotType = EquipmentSlotType.Core,
            Rarity = ItemRarity.Common,
            DisplayColor = RarityColors[ItemRarity.Common],
            BonusDamage = 2
        };
    }

    public static Item CreateStarterArmor()
    {
        return new Item
        {
            Name = "Scrap Plating",
            ShortDesc = "ARM +1 HP +5",
            Description = "Makeshift armor plating welded together from scrap metal.",
            SlotType = EquipmentSlotType.Base,
            Rarity = ItemRarity.Common,
            DisplayColor = RarityColors[ItemRarity.Common],
            BonusArmor = 1,
            BonusHealth = 5
        };
    }
}
