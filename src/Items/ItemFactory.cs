using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;

namespace NullAndVoid.Items;

/// <summary>
/// Factory for creating items with random or predefined stats.
/// </summary>
public static class ItemFactory
{
    private static readonly Random _random = new();

    // Item name prefixes by rarity
    private static readonly string[] _commonPrefixes = { "Basic", "Standard", "Simple", "Old" };
    private static readonly string[] _uncommonPrefixes = { "Enhanced", "Improved", "Modified", "Tuned" };
    private static readonly string[] _rarePrefixes = { "Advanced", "Superior", "Refined", "Precision" };
    private static readonly string[] _epicPrefixes = { "Elite", "Prototype", "Experimental", "Quantum" };
    private static readonly string[] _legendaryPrefixes = { "Ancient", "Mythic", "Omega", "Prime" };

    // Base module names by slot type - organized by category
    private static readonly Dictionary<EquipmentSlotType, string[]> _moduleNames = new()
    {
        { EquipmentSlotType.Core, new[] { "Processor", "Combat Matrix", "Targeting System", "Neural Link" } },
        { EquipmentSlotType.Utility, new[] { "Scanner", "Shield Generator", "Sensor Array", "Cloak Device" } },
        { EquipmentSlotType.Base, new[] { "Armor Plating", "Chassis", "Frame Reinforcement", "Hull Module" } }
    };

    // Power source names
    private static readonly string[] _powerSourceNames = { "Reactor", "Power Core", "Fusion Cell", "Generator", "Energy Matrix" };

    // Battery/capacitor names
    private static readonly string[] _batteryNames = { "Battery Pack", "Capacitor", "Energy Cell", "Reserve Tank", "Power Buffer" };

    // Propulsion names (with their base speed bonuses and costs)
    private static readonly (string name, int speedBonus, int energyCost)[] _propulsionTypes = {
        ("Treads", -20, 2),         // Slow but stable
        ("Wheels", 0, 3),           // Standard
        ("Legs", 10, 4),            // Agile
        ("Hover Jets", 20, 6),      // Fast
        ("Flight Module", 30, 8)    // Fastest
    };

    // Stealth module names
    private static readonly string[] _stealthNames = { "Noise Dampener", "Silent Runner", "Stealth Field", "Muffler", "Acoustic Cloak" };

    // Rarity colors
    private static readonly Dictionary<ItemRarity, Color> _rarityColors = new()
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

        // Roll for special module types (30% chance for each slot type)
        int specialRoll = _random.Next(100);
        if (specialRoll < 30)
        {
            return slotType switch
            {
                EquipmentSlotType.Core => CreatePowerSource(rarity.Value),
                EquipmentSlotType.Utility => CreatePropulsion(rarity.Value),
                EquipmentSlotType.Base => CreateBattery(rarity.Value),
                _ => CreateStandardModule(slotType, rarity.Value)
            };
        }

        // 15% chance for stealth module (utility only)
        if (slotType == EquipmentSlotType.Utility && _random.Next(100) < 15)
        {
            return CreateStealthModule(rarity.Value);
        }

        return CreateStandardModule(slotType, rarity.Value);
    }

    private static Item CreateStandardModule(EquipmentSlotType slotType, ItemRarity rarity)
    {
        // Get name components
        string prefix = GetPrefix(rarity);
        string baseName = _moduleNames[slotType][_random.Next(_moduleNames[slotType].Length)];

        // Determine ModuleCategory based on slot type and name
        ModuleType moduleCategory = DetermineModuleCategory(slotType, baseName);

        // Create item
        var item = new Item
        {
            Name = $"{prefix} {baseName}",
            SlotType = slotType,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = moduleCategory,
            IsIdentified = false  // New modules start unidentified
        };

        // Generate stats based on rarity and slot type
        GenerateStats(item, slotType, rarity);

        // Generate descriptions
        item.ShortDesc = item.GetShortStatsString();
        item.Description = GenerateDescription(item);

        return item;
    }

    /// <summary>
    /// Determine the ModuleCategory based on slot type and module name.
    /// </summary>
    private static ModuleType DetermineModuleCategory(EquipmentSlotType slotType, string baseName)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => baseName switch
            {
                "Processor" or "Combat Matrix" or "Neural Link" => ModuleType.Logic,
                "Targeting System" => ModuleType.Logic,
                _ => ModuleType.Logic
            },
            EquipmentSlotType.Utility => baseName switch
            {
                "Scanner" or "Sensor Array" => ModuleType.Sensor,
                "Shield Generator" => ModuleType.Shield,
                "Cloak Device" => ModuleType.Sensor,
                _ => ModuleType.Sensor
            },
            EquipmentSlotType.Base => baseName switch
            {
                "Armor Plating" or "Hull Module" => ModuleType.Cargo,
                "Chassis" or "Frame Reinforcement" => ModuleType.Cargo,
                _ => ModuleType.Cargo
            },
            _ => ModuleType.None
        };
    }

    /// <summary>
    /// Roll for rarity with weighted probability.
    /// </summary>
    private static ItemRarity RollRarity()
    {
        int roll = _random.Next(100);

        if (roll < 50)
            return ItemRarity.Common;      // 50%
        if (roll < 75)
            return ItemRarity.Uncommon;    // 25%
        if (roll < 90)
            return ItemRarity.Rare;        // 15%
        if (roll < 98)
            return ItemRarity.Epic;        // 8%
        return ItemRarity.Legendary;                   // 2%
    }

    private static string GetPrefix(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => _commonPrefixes[_random.Next(_commonPrefixes.Length)],
            ItemRarity.Uncommon => _uncommonPrefixes[_random.Next(_uncommonPrefixes.Length)],
            ItemRarity.Rare => _rarePrefixes[_random.Next(_rarePrefixes.Length)],
            ItemRarity.Epic => _epicPrefixes[_random.Next(_epicPrefixes.Length)],
            ItemRarity.Legendary => _legendaryPrefixes[_random.Next(_legendaryPrefixes.Length)],
            _ => "Unknown"
        };
    }

    private static void GenerateStats(Item item, EquipmentSlotType slotType, ItemRarity rarity)
    {
        int rarityMultiplier = (int)rarity + 1;
        int baseEnergyCost = Math.Max(1, 3 - (int)rarity / 2); // Higher rarity = slightly more efficient

        // Slot type determines primary stat focus
        switch (slotType)
        {
            case EquipmentSlotType.Core:
                // Core modules focus on damage and sight
                item.BonusDamage = _random.Next(1, 3) * rarityMultiplier;
                if (_random.Next(2) == 0)
                    item.BonusSightRange = _random.Next(1, 2) * rarityMultiplier;
                // Combat modules consume energy
                item.EnergyConsumption = _random.Next(2, 4);
                break;

            case EquipmentSlotType.Utility:
                // Utility modules are varied
                int utilType = _random.Next(3);
                if (utilType == 0)
                {
                    item.BonusSightRange = _random.Next(1, 3) * rarityMultiplier;
                    item.EnergyConsumption = _random.Next(1, 3);
                }
                else if (utilType == 1)
                {
                    item.BonusArmor = _random.Next(1, 2) * rarityMultiplier;
                    item.EnergyConsumption = _random.Next(2, 4);
                    item.IsToggleable = true; // Shields can be toggled
                }
                else
                {
                    // Structural reinforcement provides armor, not integrity
                    item.BonusArmor = _random.Next(2, 4) * rarityMultiplier;
                }
                break;

            case EquipmentSlotType.Base:
                // Base modules focus on defense - passive (no energy cost)
                // Provides higher armor since no other stats
                item.BonusArmor = _random.Next(2, 5) * rarityMultiplier;
                break;
        }
    }

    /// <summary>
    /// Create a power source module (generates energy).
    /// </summary>
    public static Item CreatePowerSource(ItemRarity rarity)
    {
        int rarityMultiplier = (int)rarity + 1;
        string prefix = GetPrefix(rarity);
        string baseName = _powerSourceNames[_random.Next(_powerSourceNames.Length)];

        // Power output scales with rarity: 5-8 common, up to 20-30 legendary
        int baseOutput = 5 + _random.Next(0, 4);
        int output = baseOutput + (3 * (int)rarity);

        var item = new Item
        {
            Name = $"{prefix} {baseName}",
            SlotType = EquipmentSlotType.Core,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Generator,
            IsIdentified = false,
            BootCost = 3 + (int)rarity,
            EnergyOutput = output,
            EnergyConsumption = 0,
            ShortDesc = $"PWR +{output}",
            Description = $"A power generation module that produces {output} energy per turn."
        };

        return item;
    }

    /// <summary>
    /// Create a battery/capacitor module (stores energy).
    /// </summary>
    public static Item CreateBattery(ItemRarity rarity)
    {
        int rarityMultiplier = (int)rarity + 1;
        string prefix = GetPrefix(rarity);
        string baseName = _batteryNames[_random.Next(_batteryNames.Length)];

        // Capacity scales with rarity: 20-30 common, up to 80-120 legendary
        int baseCapacity = 20 + _random.Next(0, 11);
        int capacity = baseCapacity + (20 * (int)rarity);

        var item = new Item
        {
            Name = $"{prefix} {baseName}",
            SlotType = EquipmentSlotType.Core,  // Battery goes in Core per requirements
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Battery,
            IsIdentified = false,
            BootCost = 2 + (int)rarity,
            BonusEnergyCapacity = capacity,
            ShortDesc = $"CAP +{capacity}",
            Description = $"An energy storage module with {capacity} reserve capacity."
        };

        return item;
    }

    /// <summary>
    /// Create a propulsion module (affects speed).
    /// </summary>
    public static Item CreatePropulsion(ItemRarity rarity)
    {
        int rarityMultiplier = (int)rarity + 1;
        string prefix = GetPrefix(rarity);

        // Pick a propulsion type
        int propIndex = _random.Next(_propulsionTypes.Length);
        var (baseName, baseSpeed, baseEnergyCost) = _propulsionTypes[propIndex];

        // Determine module category based on propulsion type
        ModuleType propCategory = propIndex switch
        {
            0 => ModuleType.Treads,   // Treads
            1 => ModuleType.Legs,     // Wheels (treated as legs for simplicity)
            2 => ModuleType.Legs,     // Legs
            3 => ModuleType.Flight,   // Hover Jets
            4 => ModuleType.Flight,   // Flight Module
            _ => ModuleType.Legs
        };

        // Rarity improves speed bonus and reduces energy cost slightly
        int speedBonus = baseSpeed + (_random.Next(0, 5) * rarityMultiplier);
        int energyCost = Math.Max(1, baseEnergyCost - (int)rarity / 2);

        var item = new Item
        {
            Name = $"{prefix} {baseName}",
            SlotType = EquipmentSlotType.Base,  // Propulsion goes in Base per requirements
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = propCategory,
            IsIdentified = false,
            BootCost = 4 + (int)rarity,
            BonusSpeed = speedBonus,
            EnergyConsumption = energyCost,
            IsToggleable = true, // Can disable propulsion to save energy
            ShortDesc = $"SPD {(speedBonus >= 0 ? "+" : "")}{speedBonus} USE -{energyCost}",
            Description = $"A propulsion system providing {(speedBonus >= 0 ? "+" : "")}{speedBonus} speed at {energyCost} energy/turn."
        };

        return item;
    }

    /// <summary>
    /// Create a stealth module (reduces noise).
    /// </summary>
    public static Item CreateStealthModule(ItemRarity rarity)
    {
        int rarityMultiplier = (int)rarity + 1;
        string prefix = GetPrefix(rarity);
        string baseName = _stealthNames[_random.Next(_stealthNames.Length)];

        // Noise reduction scales with rarity: -10 to -15 common, up to -40 legendary
        int noiseReduction = -10 - _random.Next(0, 6) - (8 * (int)rarity);
        int energyCost = 3 + _random.Next(0, 3) + (int)rarity;

        var item = new Item
        {
            Name = $"{prefix} {baseName}",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Sensor,  // Stealth is a sensor type
            IsIdentified = false,
            BootCost = 3 + (int)rarity,
            BonusNoise = noiseReduction,
            EnergyConsumption = energyCost,
            IsToggleable = true,
            ShortDesc = $"NSE {noiseReduction} USE -{energyCost}",
            Description = $"A stealth system reducing noise by {-noiseReduction} at {energyCost} energy/turn."
        };

        return item;
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

        string energyDesc = "";
        if (item.EnergyConsumption > 0)
            energyDesc = $" Consumes {item.EnergyConsumption} energy/turn.";
        else if (item.EnergyOutput > 0)
            energyDesc = $" Produces {item.EnergyOutput} energy/turn.";

        return $"{slotDesc} {rarityDesc}.{energyDesc}";
    }

    /// <summary>
    /// Create a specific predefined item - starter melee weapon.
    /// </summary>
    public static Item CreateStarterWeapon()
    {
        return WeaponFactory.CreateStarterMeleeWeapon();
    }

    /// <summary>
    /// Create a starter ranged weapon.
    /// </summary>
    public static Item CreateStarterRangedWeapon()
    {
        return WeaponFactory.CreateStarterRangedWeapon();
    }

    /// <summary>
    /// Create a specific predefined item - starter armor.
    /// </summary>
    public static Item CreateStarterArmor()
    {
        return new Item
        {
            Name = "Scrap Plating",
            ShortDesc = "ARM +3",
            Description = "Makeshift armor plating welded together from scrap metal.",
            SlotType = EquipmentSlotType.Base,
            Rarity = ItemRarity.Common,
            DisplayColor = _rarityColors[ItemRarity.Common],
            ModuleCategory = ModuleType.Cargo,  // Armor/hull is cargo type
            BonusArmor = 3,  // Provides flat damage reduction
            BootCost = 2,
            IsIdentified = true  // Starter items are pre-identified
        };
    }

    /// <summary>
    /// Create a starter power core.
    /// </summary>
    public static Item CreateStarterPowerCore()
    {
        return new Item
        {
            Name = "Basic Reactor",
            ShortDesc = "PWR +5",
            Description = "A simple power reactor providing baseline energy output.",
            SlotType = EquipmentSlotType.Core,
            Rarity = ItemRarity.Common,
            DisplayColor = _rarityColors[ItemRarity.Common],
            ModuleCategory = ModuleType.Generator,
            EnergyOutput = 5,
            BootCost = 3,
            IsIdentified = true  // Starter items are pre-identified
        };
    }

    /// <summary>
    /// Create a starter propulsion system.
    /// </summary>
    public static Item CreateStarterPropulsion()
    {
        return new Item
        {
            Name = "Basic Wheels",
            ShortDesc = "SPD +0 USE -2",
            Description = "Standard wheeled propulsion. Nothing fancy, but reliable.",
            SlotType = EquipmentSlotType.Base,  // Propulsion goes in Base per requirements
            Rarity = ItemRarity.Common,
            DisplayColor = _rarityColors[ItemRarity.Common],
            ModuleCategory = ModuleType.Legs,  // Wheels are treated as legs
            BonusSpeed = 0,
            EnergyConsumption = 2,
            BootCost = 4,
            IsToggleable = true,
            IsActive = true,
            IsIdentified = true  // Starter items are pre-identified
        };
    }

    #region Consumable Items

    /// <summary>
    /// Create a Portable Analyzer consumable.
    /// Used to identify unidentified modules without mounting risk.
    /// </summary>
    public static Item CreatePortableAnalyzer()
    {
        return new Item
        {
            Name = "Portable Analyzer",
            ShortDesc = "Identifies modules",
            Description = "A handheld diagnostic tool that can identify unknown modules, revealing their true properties without the risk of mounting.",
            Type = ItemType.Consumable,
            SlotType = EquipmentSlotType.Any,
            Rarity = ItemRarity.Uncommon,
            DisplayColor = new Color(0.3f, 0.8f, 1.0f),  // Cyan
            IsIdentified = true  // Consumables are always identified
        };
    }

    /// <summary>
    /// Create a Portable Repair Kit consumable.
    /// Used to repair damaged mount points.
    /// </summary>
    public static Item CreatePortableRepairKit()
    {
        return new Item
        {
            Name = "Portable Repair Kit",
            ShortDesc = "Repairs mount points",
            Description = "A compact toolkit containing everything needed to repair a damaged equipment mount point, restoring it to working condition.",
            Type = ItemType.Consumable,
            SlotType = EquipmentSlotType.Any,
            Rarity = ItemRarity.Uncommon,
            DisplayColor = new Color(1.0f, 0.8f, 0.2f),  // Yellow/Gold
            IsIdentified = true  // Consumables are always identified
        };
    }

    /// <summary>
    /// Create a Module Repair Kit consumable.
    /// Used to repair damaged modules (restore module armor).
    /// </summary>
    public static Item CreateModuleRepairKit()
    {
        return new Item
        {
            Name = "Module Repair Kit",
            ShortDesc = "Repairs module armor",
            Description = "Specialized repair materials that can restore a damaged module's protective casing, bringing it back to operational status.",
            Type = ItemType.Consumable,
            SlotType = EquipmentSlotType.Any,
            Rarity = ItemRarity.Common,
            DisplayColor = new Color(0.6f, 0.6f, 0.6f),  // Gray
            IsIdentified = true  // Consumables are always identified
        };
    }

    /// <summary>
    /// Create a random consumable item.
    /// </summary>
    public static Item CreateRandomConsumable()
    {
        int roll = _random.Next(100);

        if (roll < 40)
            return CreateModuleRepairKit();
        else if (roll < 70)
            return CreatePortableRepairKit();
        else
            return CreatePortableAnalyzer();
    }

    #endregion
}
