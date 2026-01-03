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

    // Shield generator names
    private static readonly string[] _shieldGeneratorNames = { "Shield Generator", "Force Field Emitter", "Energy Barrier", "Deflector Array", "Plasma Shield" };

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

        // Special utility modules: 15% stealth, 20% shield generator
        if (slotType == EquipmentSlotType.Utility)
        {
            int utilityRoll = _random.Next(100);
            if (utilityRoll < 15)
            {
                return CreateStealthModule(rarity.Value);
            }
            else if (utilityRoll < 35)
            {
                return CreateShieldGenerator(rarity.Value);
            }
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

    /// <summary>
    /// Calculate module armor based on item rarity.
    /// Uses values from ItemDefinitions configuration.
    /// </summary>
    private static int CalculateModuleArmor(ItemRarity rarity)
    {
        return ItemDefinitions.GetModuleArmor(rarity);
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

        // Set exposure based on module category/slot
        // Higher exposure = more likely to be hit in combat
        item.Exposure = slotType switch
        {
            EquipmentSlotType.Core => 8 + _random.Next(5),     // 8-12: internal, protected
            EquipmentSlotType.Utility => 10 + _random.Next(6), // 10-15: mixed exposure
            EquipmentSlotType.Base => 15 + _random.Next(6),    // 15-20: external, exposed
            _ => 10
        };

        // Set module armor based on rarity (higher rarity = more durable)
        item.MaxModuleArmor = CalculateModuleArmor(rarity);
        item.CurrentModuleArmor = item.MaxModuleArmor;
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
            Exposure = 12,  // Medium exposure - internal core component
            MaxModuleArmor = CalculateModuleArmor(rarity),
            ShortDesc = $"PWR +{output}",
            Description = $"A power generation module that produces {output} energy per turn."
        };
        item.CurrentModuleArmor = item.MaxModuleArmor;

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
            Exposure = 10,  // Lower exposure - protected storage
            MaxModuleArmor = CalculateModuleArmor(rarity),
            ShortDesc = $"CAP +{capacity}",
            Description = $"An energy storage module with {capacity} reserve capacity."
        };
        item.CurrentModuleArmor = item.MaxModuleArmor;

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
            Exposure = 20,  // Medium-high exposure - external locomotion
            MaxModuleArmor = CalculateModuleArmor(rarity),
            ShortDesc = $"SPD {(speedBonus >= 0 ? "+" : "")}{speedBonus} USE -{energyCost}",
            Description = $"A propulsion system providing {(speedBonus >= 0 ? "+" : "")}{speedBonus} speed at {energyCost} energy/turn."
        };
        item.CurrentModuleArmor = item.MaxModuleArmor;

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
            Exposure = 5,  // Very low exposure - stealthy by design!
            MaxModuleArmor = CalculateModuleArmor(rarity),
            ShortDesc = $"NSE {noiseReduction} USE -{energyCost}",
            Description = $"A stealth system reducing noise by {-noiseReduction} at {energyCost} energy/turn."
        };
        item.CurrentModuleArmor = item.MaxModuleArmor;

        return item;
    }

    /// <summary>
    /// Create an energy shield generator module.
    /// Generates shield points per turn by consuming energy reserves.
    /// </summary>
    public static Item CreateShieldGenerator(ItemRarity rarity)
    {
        string prefix = GetPrefix(rarity);
        string baseName = _shieldGeneratorNames[_random.Next(_shieldGeneratorNames.Length)];

        // Shield stats scale with rarity
        // Output: 5/8/11/14/17 per turn
        // Capacity: 20/35/50/65/80 max
        // Energy cost: 3/4/5/6/7 per turn
        int shieldOutput = 5 + (3 * (int)rarity);
        int shieldCapacity = 20 + (15 * (int)rarity);
        int shieldEnergyCost = 3 + (int)rarity;

        var item = new Item
        {
            Name = $"{prefix} {baseName}",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Shield,
            IsIdentified = false,
            BootCost = 5 + (int)rarity,

            // Shield generation stats
            ShieldOutput = shieldOutput,
            ShieldCapacity = shieldCapacity,
            ShieldEnergyCost = shieldEnergyCost,

            // Toggleable - can turn off to save energy
            IsToggleable = true,
            IsActive = true,

            Exposure = 18,  // Medium-high exposure - external projector
            MaxModuleArmor = CalculateModuleArmor(rarity),
            ShortDesc = $"SHD {shieldOutput}/t CAP {shieldCapacity}",
            Description = $"An energy shield generator that creates {shieldOutput} shield points per turn " +
                         $"(max {shieldCapacity}) at a cost of {shieldEnergyCost} energy/turn."
        };
        item.CurrentModuleArmor = item.MaxModuleArmor;

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
            Exposure = 25,  // Medium-high exposure - external plating
            MaxModuleArmor = 8,  // Common rarity armor
            CurrentModuleArmor = 8,
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
            Exposure = 10,  // Medium exposure - internal core
            MaxModuleArmor = 8,  // Common rarity armor
            CurrentModuleArmor = 8,
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
            Exposure = 18,  // Medium-high exposure - external locomotion
            MaxModuleArmor = 8,  // Common rarity armor
            CurrentModuleArmor = 8,
            IsToggleable = true,
            IsActive = true,
            IsIdentified = true  // Starter items are pre-identified
        };
    }

    #region Tank Build - Armor Modules

    /// <summary>
    /// Create Reinforced Plating - basic tank armor module.
    /// High armor but reduces speed.
    /// </summary>
    public static Item CreateReinforcedPlating(ItemRarity rarity = ItemRarity.Uncommon)
    {
        int rarityMult = (int)rarity + 1;
        int armor = 6 + (2 * rarityMult);
        int speedPenalty = -8 - (2 * (int)rarity);

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Reinforced Plating",
            ShortDesc = $"ARM +{armor} SPD {speedPenalty}",
            Description = $"Heavy armor plating that provides excellent protection at the cost of mobility. " +
                         $"Grants +{armor} armor but reduces speed by {-speedPenalty}.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Base,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Armor,
            IsIdentified = true,
            BootCost = 4 + (int)rarity,
            BonusArmor = armor,
            BonusSpeed = speedPenalty,
            Exposure = 40,  // HIGH exposure - designed to tank damage
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Ablative Carapace - explosive-resistant armor.
    /// High armor with explosive damage resistance.
    /// </summary>
    public static Item CreateAblativeCarapace(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityMult = (int)rarity + 1;
        int armor = 8 + (2 * rarityMult);
        int explosiveResist = 60 + (5 * (int)rarity);  // 60-80% based on rarity

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Ablative Carapace",
            ShortDesc = $"ARM +{armor} EXP RES {explosiveResist}%",
            Description = $"Layered ablative armor designed to dissipate explosive force. " +
                         $"Provides +{armor} armor and {explosiveResist}% explosive resistance.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Base,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Armor,
            IsIdentified = true,
            BootCost = 5 + (int)rarity,
            BonusArmor = armor,
            ExplosiveResistance = explosiveResist,
            Exposure = 45,  // HIGH exposure - designed to tank damage
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Titan Frame - heavy tank frame with HP bonus.
    /// Maximum armor and integrity at severe speed cost.
    /// </summary>
    public static Item CreateTitanFrame(ItemRarity rarity = ItemRarity.Epic)
    {
        int rarityMult = (int)rarity + 1;
        int armor = 12 + (3 * rarityMult);
        int hpBonus = 3 + (2 * (int)rarity);
        int speedPenalty = -15 - (5 * (int)rarity);

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Titan Frame",
            ShortDesc = $"ARM +{armor} INT +{hpBonus} SPD {speedPenalty}",
            Description = $"A massive reinforced frame built for maximum survivability. " +
                         $"Grants +{armor} armor and +{hpBonus} integrity but severely reduces speed by {-speedPenalty}.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Base,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Armor,
            IsIdentified = true,
            BootCost = 6 + (int)rarity,
            BonusArmor = armor,
            BonusHealth = hpBonus,
            BonusSpeed = speedPenalty,
            Exposure = 50,  // VERY HIGH exposure - massive tank frame
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Reactive Armor - armor that damages melee attackers.
    /// Moderate armor with melee reflection (tracked via BonusDamage on armor type).
    /// </summary>
    public static Item CreateReactiveArmor(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityMult = (int)rarity + 1;
        int armor = 4 + (2 * rarityMult);
        int reflectDamage = 2 + rarityMult;  // Damage dealt to melee attackers

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Reactive Armor",
            ShortDesc = $"ARM +{armor} REFLECT {reflectDamage}",
            Description = $"Armor plating with reactive explosive tiles that damage melee attackers. " +
                         $"Provides +{armor} armor and deals {reflectDamage} damage to enemies that hit you in melee.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Core,  // Core slot for variety
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Armor,
            IsIdentified = true,
            BootCost = 5 + (int)rarity,
            BonusArmor = armor,
            BonusDamage = reflectDamage,  // Repurposed as reflect damage for armor type
            Exposure = 35,  // HIGH exposure - designed to tank damage
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    #endregion

    #region Tank Build - Shield Variants

    /// <summary>
    /// Create Heavy Barrier - high capacity shield with projectile resistance.
    /// </summary>
    public static Item CreateHeavyBarrier(ItemRarity rarity = ItemRarity.Rare)
    {
        int shieldOutput = 3 + (2 * (int)rarity);
        int shieldCapacity = 35 + (15 * (int)rarity);
        int shieldEnergyCost = 4 + (int)rarity;

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Heavy Barrier",
            ShortDesc = $"SHD {shieldOutput}/t CAP {shieldCapacity} +25% vs PROJ",
            Description = $"A reinforced energy barrier optimized against projectile weapons. " +
                         $"Generates {shieldOutput} shield/turn (max {shieldCapacity}) with 25% bonus vs projectiles.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Shield,
            IsIdentified = true,
            BootCost = 6 + (int)rarity,
            ShieldOutput = shieldOutput,
            ShieldCapacity = shieldCapacity,
            ShieldEnergyCost = shieldEnergyCost,
            IsToggleable = true,
            IsActive = true,
            Exposure = 18,  // Medium-high exposure - external projector
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Fortress Shield - massive capacity, blocks knockback.
    /// </summary>
    public static Item CreateFortressShield(ItemRarity rarity = ItemRarity.Epic)
    {
        int shieldOutput = 2 + (int)rarity;  // Lower regen
        int shieldCapacity = 50 + (20 * (int)rarity);  // Higher capacity
        int shieldEnergyCost = 6 + (int)rarity;

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Fortress Shield",
            ShortDesc = $"SHD {shieldOutput}/t CAP {shieldCapacity} NO KNOCKBACK",
            Description = $"An immense energy barrier that anchors the user in place. " +
                         $"Generates {shieldOutput} shield/turn (max {shieldCapacity}) and prevents knockback while active.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Shield,
            IsIdentified = true,
            BootCost = 8 + (int)rarity,
            ShieldOutput = shieldOutput,
            ShieldCapacity = shieldCapacity,
            ShieldEnergyCost = shieldEnergyCost,
            IsToggleable = true,
            IsActive = true,
            BonusSpeed = -5,  // Slight speed penalty for stability
            Exposure = 20,  // Higher exposure - massive external shield
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Reactive Barrier - damages melee attackers when shield is hit.
    /// </summary>
    public static Item CreateReactiveBarrier(ItemRarity rarity = ItemRarity.Rare)
    {
        int shieldOutput = 5 + (2 * (int)rarity);
        int shieldCapacity = 25 + (12 * (int)rarity);
        int shieldEnergyCost = 4 + (int)rarity;
        int reflectDamage = 3 + (int)rarity;

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Reactive Barrier",
            ShortDesc = $"SHD {shieldOutput}/t CAP {shieldCapacity} SHOCK {reflectDamage}",
            Description = $"An electrified barrier that shocks melee attackers. " +
                         $"Generates {shieldOutput} shield/turn (max {shieldCapacity}) and deals {reflectDamage} damage to melee attackers.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Shield,
            IsIdentified = true,
            BootCost = 5 + (int)rarity,
            ShieldOutput = shieldOutput,
            ShieldCapacity = shieldCapacity,
            ShieldEnergyCost = shieldEnergyCost,
            BonusDamage = reflectDamage,  // Repurposed as shock damage for shields
            IsToggleable = true,
            IsActive = true,
            Exposure = 16,  // Medium exposure
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    #endregion

    #region Tank Build - Combat Enhancement Modules

    /// <summary>
    /// Create Targeting Matrix - accuracy and sight bonus.
    /// </summary>
    public static Item CreateTargetingMatrix(ItemRarity rarity = ItemRarity.Uncommon)
    {
        int accuracyBonus = 10 + (5 * (int)rarity);  // 10-30% based on rarity
        int sightBonus = 1 + ((int)rarity / 2);

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Targeting Matrix",
            ShortDesc = $"ACC +{accuracyBonus}% VIS +{sightBonus}",
            Description = $"Advanced targeting computer that improves weapon accuracy by {accuracyBonus}% " +
                         $"and extends visual range by {sightBonus}.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Sensor,
            IsIdentified = true,
            BootCost = 3 + (int)rarity,
            BonusSightRange = sightBonus,
            EnergyConsumption = 2 + (int)rarity / 2,
            Exposure = 5,  // LOW exposure - compact targeting sensor
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Damage Amplifier - damage and crit bonus.
    /// </summary>
    public static Item CreateDamageAmplifier(ItemRarity rarity = ItemRarity.Rare)
    {
        int damageBonus = 2 + (2 * (int)rarity);
        int critBonus = 3 + (2 * (int)rarity);  // 3-11% based on rarity

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Damage Amplifier",
            ShortDesc = $"DMG +{damageBonus} CRIT +{critBonus}%",
            Description = $"Combat amplification module that increases all damage by {damageBonus} " +
                         $"and critical hit chance by {critBonus}%.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Core,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Logic,
            IsIdentified = true,
            BootCost = 4 + (int)rarity,
            BonusDamage = damageBonus,
            EnergyConsumption = 3 + (int)rarity / 2,
            Exposure = 8,  // LOW exposure - internal processor
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Threat Analyzer - shows enemy info, bonus damage vs elites.
    /// </summary>
    public static Item CreateThreatAnalyzer(ItemRarity rarity = ItemRarity.Rare)
    {
        int sightBonus = 2 + (int)rarity / 2;
        int eliteDamageBonus = 2 + (int)rarity;

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Threat Analyzer",
            ShortDesc = $"VIS +{sightBonus} ELITE DMG +{eliteDamageBonus}",
            Description = $"Tactical analysis module that reveals enemy HP and tier, " +
                         $"extends sight by {sightBonus}, and deals +{eliteDamageBonus} damage to elite enemies.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Sensor,
            IsIdentified = true,
            BootCost = 4 + (int)rarity,
            BonusSightRange = sightBonus,
            BonusDamage = eliteDamageBonus,  // Applied vs elites only (needs combat system support)
            EnergyConsumption = 2 + (int)rarity / 2,
            Exposure = 6,  // LOW exposure - low profile sensor
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    /// <summary>
    /// Create Combat Processor - cooldown reduction and accuracy.
    /// </summary>
    public static Item CreateCombatProcessor(ItemRarity rarity = ItemRarity.Rare)
    {
        int accuracyBonus = 8 + (3 * (int)rarity);

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Combat Processor",
            ShortDesc = $"COOLDOWN -1 ACC +{accuracyBonus}%",
            Description = $"High-speed combat computer that reduces weapon cooldowns by 1 turn " +
                         $"and improves accuracy by {accuracyBonus}%.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Core,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Logic,
            IsIdentified = true,
            BootCost = 5 + (int)rarity,
            EnergyConsumption = 4 + (int)rarity / 2,
            Exposure = 8,  // LOW exposure - internal processor
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    #endregion

    #region Tank Build - Emergency Pulse Generator

    /// <summary>
    /// Create the Emergency Pulse Generator - signature tank emergency ability.
    /// Knocks back and stuns all nearby enemies. High energy cost, long cooldown.
    /// </summary>
    public static Item CreateEmergencyPulseGenerator(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityMult = (int)rarity + 1;
        int radius = 3 + (int)rarity / 2;
        int knockback = 4 + (int)rarity / 2;
        int stunDuration = 2 + (int)rarity / 2;
        int energyCost = 30 + (5 * (int)rarity);
        int cooldown = 10 - (int)rarity;  // 10/9/8/7/6 turns based on rarity
        int damage = 5 + (2 * (int)rarity);

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Emergency Pulse Generator",
            ShortDesc = $"PULSE RAD {radius} KB {knockback} STUN {stunDuration}t",
            Description = $"Emergency defense module that releases a devastating electromagnetic pulse. " +
                         $"Radius {radius}, knocks back enemies {knockback} tiles, stuns for {stunDuration} turns. " +
                         $"Costs {energyCost} energy with {cooldown} turn cooldown. Use when overwhelmed!",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.UtilityActive,
            IsIdentified = true,
            BootCost = 6 + (int)rarity,
            EnergyConsumption = 2,  // Passive drain while equipped

            // Active ability configuration
            HasActiveAbility = true,
            AbilityName = "Emergency Pulse",
            AbilityDescription = $"Release an electromagnetic shockwave that knocks back and stuns all enemies within {radius} tiles.",
            AbilityEnergyCost = energyCost,
            AbilityCooldown = cooldown,
            AbilityRadius = radius,
            AbilityEffect = WeaponEffect.Stunned,
            AbilityEffectDuration = stunDuration,
            AbilityKnockback = knockback,
            AbilityDamage = damage,

            // Module stats
            EMPResistance = 70 + (5 * (int)rarity),  // High EMP resistance
            Exposure = 12,  // Medium exposure - active utility module
            MaxModuleArmor = CalculateModuleArmor(rarity),
            CurrentModuleArmor = CalculateModuleArmor(rarity)
        };
    }

    #endregion

    #region Chassis Plating - Primary Defensive Module

    /// <summary>
    /// Create Chassis Plating - the workhorse defensive armor module.
    /// Very high exposure (tanks damage), very high module armor.
    /// This is the primary defensive option for protecting critical systems.
    /// </summary>
    public static Item CreateChassisPlating(ItemRarity rarity = ItemRarity.Common)
    {
        int exposure = 35 + (5 * (int)rarity);      // 35-55 based on rarity
        int moduleArmor = 15 + (5 * (int)rarity);   // 15-35 (much higher than normal)
        int bonusArmor = 3 + (2 * (int)rarity);     // Passive armor bonus 3-11

        return new Item
        {
            Name = $"{GetPrefix(rarity)} Chassis Plating",
            ShortDesc = $"ARM +{bonusArmor} EXP {exposure}",
            Description = $"Heavy duty chassis plating designed to absorb enemy fire. " +
                         $"High exposure rating ({exposure}) draws attacks away from critical systems. " +
                         $"Module armor: {moduleArmor}.",
            Type = ItemType.Module,
            SlotType = EquipmentSlotType.Base,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            ModuleCategory = ModuleType.Armor,
            IsIdentified = true,
            BootCost = 3 + (int)rarity,
            BonusArmor = bonusArmor,
            Exposure = exposure,
            MaxModuleArmor = moduleArmor,
            CurrentModuleArmor = moduleArmor
        };
    }

    #endregion

    #region Tank Build - Energy Cell Consumable

    /// <summary>
    /// Create an Energy Cell consumable.
    /// Restores energy immediately when used.
    /// </summary>
    public static Item CreateEnergyCell()
    {
        return new Item
        {
            Name = "Energy Cell",
            ShortDesc = "Restores 30 energy",
            Description = "A compact power cell that can be consumed to instantly restore 30 energy to your reserves.",
            Type = ItemType.Consumable,
            ConsumableCategory = ConsumableType.EnergyCell,
            ConsumableValue = 30,
            SlotType = EquipmentSlotType.Any,
            Rarity = ItemRarity.Common,
            DisplayColor = new Color(0.2f, 0.8f, 1.0f),  // Electric blue
            IsIdentified = true
        };
    }

    #endregion

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
            ConsumableCategory = ConsumableType.Analyzer,
            ConsumableValue = 1,  // Identifies 1 item
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
            ConsumableCategory = ConsumableType.MountRepair,
            ConsumableValue = 100,  // Full repair
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
            ConsumableCategory = ConsumableType.ModuleRepair,
            ConsumableValue = 25,  // Repairs 25 armor
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

        if (roll < 30)
            return CreateModuleRepairKit();
        else if (roll < 50)
            return CreatePortableRepairKit();
        else if (roll < 70)
            return CreatePortableAnalyzer();
        else
            return CreateEnergyCell();
    }

    #endregion
}
