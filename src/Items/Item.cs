using Godot;
using NullAndVoid.Combat;

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

    #region Identification System

    /// <summary>
    /// The type of item (Module, Consumable, etc.).
    /// </summary>
    [Export] public ItemType Type { get; set; } = ItemType.Module;

    /// <summary>
    /// The specific consumable type (Analyzer, Repair Kit, Energy Cell, etc.).
    /// Only relevant when Type is Consumable.
    /// </summary>
    [Export] public ConsumableType ConsumableCategory { get; set; } = ConsumableType.None;

    /// <summary>
    /// The value/amount for consumable effects (e.g., energy restored, repair amount).
    /// </summary>
    [Export] public int ConsumableValue { get; set; } = 0;

    /// <summary>
    /// The specific module type (Weapon, Shield, Generator, etc.).
    /// Determines which mount slots can accept this module.
    /// Always visible even when unidentified.
    /// </summary>
    [Export] public ModuleType ModuleCategory { get; set; } = ModuleType.None;

    /// <summary>
    /// Whether this item has been identified.
    /// Unidentified items only show ModuleCategory and BootCost.
    /// </summary>
    [Export] public bool IsIdentified { get; set; } = false;

    /// <summary>
    /// Energy cost to mount/boot this module.
    /// Always visible even when unidentified.
    /// </summary>
    [Export] public int BootCost { get; set; } = 5;

    /// <summary>
    /// Maximum armor rating of this module.
    /// Module absorbs damage before core integrity.
    /// Varies by rarity: Common=8, Uncommon=11, Rare=14, Epic=17, Legendary=20.
    /// </summary>
    [Export] public int MaxModuleArmor { get; set; } = 8;

    /// <summary>
    /// Current armor remaining on this module.
    /// When 0, module is disabled.
    /// </summary>
    public int CurrentModuleArmor { get; set; } = 8;

    /// <summary>
    /// Whether this module's armor has been initialized.
    /// Prevents re-initialization on re-equip (which would repair the module).
    /// </summary>
    public bool ArmorInitialized { get; private set; } = false;

    /// <summary>
    /// Whether this module is disabled (armor depleted).
    /// </summary>
    public bool IsDisabled => CurrentModuleArmor <= 0;

    /// <summary>
    /// Resistance percentages against various damage types.
    /// Base 50% for all types.
    /// </summary>
    [Export] public int FireResistance { get; set; } = 50;
    [Export] public int EMPResistance { get; set; } = 50;
    [Export] public int ExplosiveResistance { get; set; } = 50;

    /// <summary>
    /// Exposure rating determining hit likelihood in combat.
    /// Higher = more likely to be targeted. Armor modules have high exposure
    /// to "tank" damage for other modules.
    /// Default 10 = baseline exposure.
    /// </summary>
    [Export] public int Exposure { get; set; } = 10;

    /// <summary>
    /// Initialize module armor to max value.
    /// Only initializes once - subsequent calls are ignored to preserve damage state.
    /// </summary>
    public void InitializeArmor()
    {
        if (!ArmorInitialized)
        {
            CurrentModuleArmor = MaxModuleArmor;
            ArmorInitialized = true;
        }
    }

    /// <summary>
    /// Fully repair this module's armor to maximum.
    /// Use this for repair kits - bypasses the initialization check.
    /// </summary>
    public void FullyRepairModule()
    {
        CurrentModuleArmor = MaxModuleArmor;
        ArmorInitialized = true;
    }

    /// <summary>
    /// Identify this item, revealing all stats.
    /// </summary>
    public void Identify()
    {
        IsIdentified = true;
    }

    /// <summary>
    /// Get the display name based on identification status.
    /// </summary>
    public string GetDisplayName()
    {
        if (IsIdentified || Type == ItemType.Consumable)
            return Name;

        // Unidentified: show module category if known
        if (ModuleCategory != ModuleType.None)
            return $"Unidentified {ModuleCategory.GetDisplayName()}";

        // Fallback to slot-based name
        return SlotType switch
        {
            EquipmentSlotType.Core => "Unidentified Core Module",
            EquipmentSlotType.Utility => "Unidentified Utility Module",
            EquipmentSlotType.Base => "Unidentified Base Module",
            _ => "Unidentified Module"
        };
    }

    /// <summary>
    /// Get the display description based on identification status.
    /// </summary>
    public string GetDisplayDescription()
    {
        if (IsIdentified || Type == ItemType.Consumable)
            return Description;

        string typeInfo = ModuleCategory != ModuleType.None
            ? ModuleCategory.GetDisplayName().ToLower()
            : SlotType.ToString().ToLower() + " module";

        return $"An unidentified {typeInfo}. Mount it or use an Analyzer to identify.";
    }

    /// <summary>
    /// Get stats string based on identification status.
    /// Unidentified items only show Module Type and Boot Cost.
    /// </summary>
    public string GetDisplayStatsString()
    {
        if (IsIdentified || Type == ItemType.Consumable)
            return GetStatsString();

        // Unidentified: show module type and boot cost
        if (ModuleCategory != ModuleType.None)
            return $"[{ModuleCategory}] BOOT {BootCost}";

        return $"BOOT {BootCost}";
    }

    #endregion

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

    // Energy properties
    /// <summary>
    /// Energy consumed per turn when this module is active.
    /// </summary>
    [Export] public int EnergyConsumption { get; set; } = 0;

    /// <summary>
    /// Energy produced per turn (for power sources).
    /// </summary>
    [Export] public int EnergyOutput { get; set; } = 0;

    /// <summary>
    /// Bonus to energy reserve capacity (batteries).
    /// </summary>
    [Export] public int BonusEnergyCapacity { get; set; } = 0;

    /// <summary>
    /// Speed modifier. Positive = faster, negative = slower.
    /// </summary>
    [Export] public int BonusSpeed { get; set; } = 0;

    /// <summary>
    /// Noise modifier. Positive = louder, negative = quieter.
    /// </summary>
    [Export] public int BonusNoise { get; set; } = 0;

    /// <summary>
    /// Bonus mount points provided by cargo expansion modules.
    /// Allows additional equipment slots when equipped.
    /// </summary>
    [Export] public int BonusMountPoints { get; set; } = 0;

    /// <summary>
    /// Whether this module can be toggled on/off.
    /// </summary>
    [Export] public bool IsToggleable { get; set; } = false;

    /// <summary>
    /// Current active state for toggleable modules.
    /// Non-exported since it's runtime state.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Net energy impact when active (output - consumption).
    /// </summary>
    public int NetEnergyImpact => IsActive ? EnergyOutput - EnergyConsumption : 0;

    #region Shield Generator Properties

    /// <summary>
    /// Shield points generated per turn when module is active.
    /// Only applies to Shield type modules with this value > 0.
    /// </summary>
    [Export] public int ShieldOutput { get; set; } = 0;

    /// <summary>
    /// Contribution to maximum shield pool capacity.
    /// Total capacity = sum of all equipped shield generator capacities.
    /// </summary>
    [Export] public int ShieldCapacity { get; set; } = 0;

    /// <summary>
    /// Energy cost per turn to maintain shield generation.
    /// Consumed from energy reserves each turn when shields are active.
    /// </summary>
    [Export] public int ShieldEnergyCost { get; set; } = 0;

    /// <summary>
    /// Whether this is an active shield generator (vs passive armor module).
    /// </summary>
    public bool IsShieldGenerator => ModuleCategory == ModuleType.Shield && ShieldOutput > 0;

    #endregion

    #region Active Ability Properties

    /// <summary>
    /// Whether this module has an activatable ability.
    /// </summary>
    [Export] public bool HasActiveAbility { get; set; } = false;

    /// <summary>
    /// Name of the active ability for display.
    /// </summary>
    [Export] public string AbilityName { get; set; } = "";

    /// <summary>
    /// Description of what the ability does.
    /// </summary>
    [Export] public string AbilityDescription { get; set; } = "";

    /// <summary>
    /// Energy cost to activate the ability.
    /// </summary>
    [Export] public int AbilityEnergyCost { get; set; } = 0;

    /// <summary>
    /// Cooldown in turns between ability uses.
    /// </summary>
    [Export] public int AbilityCooldown { get; set; } = 0;

    /// <summary>
    /// Current cooldown remaining. Runtime state.
    /// </summary>
    public int CurrentAbilityCooldown { get; set; } = 0;

    /// <summary>
    /// Whether the ability is ready to use.
    /// </summary>
    public bool IsAbilityReady => HasActiveAbility && CurrentAbilityCooldown <= 0;

    /// <summary>
    /// Radius of the ability effect (for AoE abilities).
    /// </summary>
    [Export] public int AbilityRadius { get; set; } = 0;

    /// <summary>
    /// Primary effect applied by the ability.
    /// </summary>
    [Export] public WeaponEffect AbilityEffect { get; set; } = WeaponEffect.None;

    /// <summary>
    /// Duration of the ability effect in turns.
    /// </summary>
    [Export] public int AbilityEffectDuration { get; set; } = 0;

    /// <summary>
    /// Knockback distance for knockback abilities.
    /// </summary>
    [Export] public int AbilityKnockback { get; set; } = 0;

    /// <summary>
    /// Damage dealt by the ability (if any).
    /// </summary>
    [Export] public int AbilityDamage { get; set; } = 0;

    /// <summary>
    /// Start the ability cooldown after use.
    /// </summary>
    public void StartAbilityCooldown()
    {
        CurrentAbilityCooldown = AbilityCooldown;
    }

    /// <summary>
    /// Tick the ability cooldown by one turn.
    /// </summary>
    public void TickAbilityCooldown()
    {
        if (CurrentAbilityCooldown > 0)
            CurrentAbilityCooldown--;
    }

    #endregion

    #region Weapon Properties

    /// <summary>
    /// Weapon-specific data. If null, this item is not a weapon.
    /// </summary>
    [Export] public WeaponData? WeaponData { get; set; } = null;

    /// <summary>
    /// Whether this item is a weapon module.
    /// </summary>
    public bool IsWeapon => WeaponData != null;

    /// <summary>
    /// Whether this weapon is ready to fire (not on cooldown).
    /// </summary>
    public bool IsWeaponReady => WeaponData?.IsReady ?? false;

    /// <summary>
    /// Whether this is a melee weapon.
    /// </summary>
    public bool IsMeleeWeapon => WeaponData?.IsMelee ?? false;

    /// <summary>
    /// Whether this is a ranged weapon.
    /// </summary>
    public bool IsRangedWeapon => WeaponData?.IsRanged ?? false;

    #endregion

    /// <summary>
    /// Create a copy of this item.
    /// </summary>
    public Item Clone()
    {
        var clone = new Item
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
            BonusSightRange = BonusSightRange,
            EnergyConsumption = EnergyConsumption,
            EnergyOutput = EnergyOutput,
            BonusEnergyCapacity = BonusEnergyCapacity,
            BonusSpeed = BonusSpeed,
            BonusNoise = BonusNoise,
            BonusMountPoints = BonusMountPoints,
            IsToggleable = IsToggleable,
            IsActive = true,  // New items start active
            WeaponData = WeaponData?.Clone(),  // Clone weapon data if present
            // Identification system
            Type = Type,
            ConsumableCategory = ConsumableCategory,
            ConsumableValue = ConsumableValue,
            ModuleCategory = ModuleCategory,
            IsIdentified = IsIdentified,
            BootCost = BootCost,
            MaxModuleArmor = MaxModuleArmor,
            FireResistance = FireResistance,
            EMPResistance = EMPResistance,
            ExplosiveResistance = ExplosiveResistance,
            Exposure = Exposure,
            // Shield generator properties
            ShieldOutput = ShieldOutput,
            ShieldCapacity = ShieldCapacity,
            ShieldEnergyCost = ShieldEnergyCost,
            // Active ability properties
            HasActiveAbility = HasActiveAbility,
            AbilityName = AbilityName,
            AbilityDescription = AbilityDescription,
            AbilityEnergyCost = AbilityEnergyCost,
            AbilityCooldown = AbilityCooldown,
            AbilityRadius = AbilityRadius,
            AbilityEffect = AbilityEffect,
            AbilityEffectDuration = AbilityEffectDuration,
            AbilityKnockback = AbilityKnockback,
            AbilityDamage = AbilityDamage
        };
        clone.InitializeArmor();
        return clone;
    }

    /// <summary>
    /// Apply damage to this module's armor.
    /// Returns remaining damage that passes through.
    /// </summary>
    public int TakeModuleDamage(int damage)
    {
        if (IsDisabled)
            return damage;

        int absorbed = Mathf.Min(damage, CurrentModuleArmor);
        CurrentModuleArmor -= absorbed;

        if (IsDisabled)
        {
            GD.Print($"Module {Name} has been disabled!");
        }

        return damage - absorbed;
    }

    /// <summary>
    /// Repair this module's armor.
    /// </summary>
    public void RepairModule(int amount)
    {
        CurrentModuleArmor = Mathf.Min(MaxModuleArmor, CurrentModuleArmor + amount);
    }

    /// <summary>
    /// Get formatted stats string.
    /// </summary>
    public string GetStatsString()
    {
        var stats = new System.Collections.Generic.List<string>();

        // Weapon stats first if this is a weapon
        if (WeaponData != null)
        {
            stats.Add($"DMG {WeaponData.DamageString}");
            if (WeaponData.Range > 1)
                stats.Add($"RNG {WeaponData.Range}");
            if (WeaponData.BaseAccuracy > 0)
                stats.Add($"ACC {WeaponData.BaseAccuracy}%");
            if (WeaponData.AreaRadius > 0)
                stats.Add($"RAD {WeaponData.AreaRadius}");
            if (WeaponData.EnergyCost > 0)
                stats.Add($"NRG {WeaponData.EnergyCost}");
        }

        // Energy stats (most important for power management)
        if (EnergyOutput > 0)
            stats.Add($"PWR +{EnergyOutput}");
        if (EnergyConsumption > 0)
            stats.Add($"USE -{EnergyConsumption}");
        if (BonusEnergyCapacity != 0)
            stats.Add($"CAP {FormatBonus(BonusEnergyCapacity)}");

        // Shield generator stats
        if (ShieldOutput > 0)
            stats.Add($"SHD {ShieldOutput}/t");
        if (ShieldCapacity > 0)
            stats.Add($"CAP {ShieldCapacity}");
        if (ShieldEnergyCost > 0)
            stats.Add($"USE -{ShieldEnergyCost}/t");

        // Non-weapon combat stats (passive bonuses)
        if (WeaponData == null && BonusDamage != 0)
            stats.Add($"DMG {FormatBonus(BonusDamage)}");
        if (BonusArmor != 0)
            stats.Add($"ARM {FormatBonus(BonusArmor)}");
        // Show exposure for armor modules (high = tanks damage)
        if (ModuleCategory == ModuleType.Armor)
            stats.Add($"EXP {Exposure}");
        if (BonusHealth != 0)
            stats.Add($"INT {FormatBonus(BonusHealth)}");
        if (BonusSightRange != 0)
            stats.Add($"VIS {FormatBonus(BonusSightRange)}");

        // Mobility/stealth stats
        if (BonusSpeed != 0)
            stats.Add($"SPD {FormatBonus(BonusSpeed)}");
        if (BonusNoise != 0)
            stats.Add($"NSE {FormatBonus(BonusNoise)}");

        // Cargo expansion
        if (BonusMountPoints != 0)
            stats.Add($"SLOTS {FormatBonus(BonusMountPoints)}");

        return stats.Count > 0 ? string.Join(" ", stats) : "No stats";
    }

    /// <summary>
    /// Get short stats string for compact display.
    /// </summary>
    public string GetShortStatsString()
    {
        var stats = new System.Collections.Generic.List<string>();

        // Weapon stats take priority
        if (WeaponData != null)
        {
            stats.Add($"DMG {WeaponData.DamageString}");
            if (WeaponData.Range > 1)
                stats.Add($"RNG {WeaponData.Range}");
            return string.Join(" ", stats);
        }

        // Show energy impact first
        int netEnergy = EnergyOutput - EnergyConsumption;
        if (netEnergy != 0)
            stats.Add($"PWR{FormatBonus(netEnergy)}");

        // Then primary stat based on slot type
        if (BonusDamage != 0)
            stats.Add($"DMG{FormatBonus(BonusDamage)}");
        else if (BonusArmor != 0)
            stats.Add($"ARM{FormatBonus(BonusArmor)}");
        else if (BonusSpeed != 0)
            stats.Add($"SPD{FormatBonus(BonusSpeed)}");

        return stats.Count > 0 ? string.Join(" ", stats) : "";
    }

    private static string FormatBonus(int value) => value > 0 ? $"+{value}" : value.ToString();
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

/// <summary>
/// Types of items.
/// </summary>
public enum ItemType
{
    /// <summary>
    /// Equipment module that can be mounted.
    /// </summary>
    Module,

    /// <summary>
    /// Consumable item that is used and destroyed.
    /// </summary>
    Consumable,

    /// <summary>
    /// Quest or key item.
    /// </summary>
    KeyItem
}

/// <summary>
/// Types of consumable items and their effects.
/// </summary>
public enum ConsumableType
{
    /// <summary>Not a consumable or unspecified.</summary>
    None,
    /// <summary>Identifies an unidentified module.</summary>
    Analyzer,
    /// <summary>Repairs module armor (integrity).</summary>
    ModuleRepair,
    /// <summary>Repairs damaged mount points.</summary>
    MountRepair,
    /// <summary>Restores energy to reserves.</summary>
    EnergyCell,
    /// <summary>Restores health/integrity.</summary>
    HealthPack,
    /// <summary>Temporary speed buff.</summary>
    Stimulant
}

/// <summary>
/// Specific module type categories per Module Requirements document.
/// Determines which mount slots can accept this module.
/// </summary>
public enum ModuleType
{
    /// <summary>
    /// Not a module or unspecified.
    /// </summary>
    None,

    // === CORE MOUNT TYPES ===
    /// <summary>
    /// Logic/processing modules. Core mount only.
    /// </summary>
    Logic,

    /// <summary>
    /// Energy storage modules. Core mount only.
    /// </summary>
    Battery,

    /// <summary>
    /// Power generation modules. Core mount only.
    /// </summary>
    Generator,

    // === UTILITY MOUNT TYPES ===
    /// <summary>
    /// Weapon modules. Utility mount only.
    /// </summary>
    Weapon,

    /// <summary>
    /// Detection/scanning modules. Utility mount only.
    /// </summary>
    Sensor,

    /// <summary>
    /// Defensive shield modules. Utility mount only.
    /// </summary>
    Shield,

    // === BASE MOUNT TYPES ===
    /// <summary>
    /// Tracked propulsion. Base mount only.
    /// </summary>
    Treads,

    /// <summary>
    /// Legged propulsion. Base mount only.
    /// </summary>
    Legs,

    /// <summary>
    /// Aerial propulsion. Base mount only.
    /// </summary>
    Flight,

    /// <summary>
    /// Storage/cargo modules. Base mount only.
    /// </summary>
    Cargo,

    // === SPECIALIZED TYPES ===
    /// <summary>
    /// Armor plating modules. Can be mounted in Core or Base slots.
    /// Provides high damage reduction but reduces speed.
    /// </summary>
    Armor,

    /// <summary>
    /// Active utility modules with cooldown abilities.
    /// Utility mount only. Has special activated effects.
    /// </summary>
    UtilityActive
}

/// <summary>
/// Helper extensions for ModuleType.
/// </summary>
public static class ModuleTypeExtensions
{
    /// <summary>
    /// Get the required mount slot type for this module type.
    /// </summary>
    public static EquipmentSlotType GetRequiredSlotType(this ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Logic => EquipmentSlotType.Core,
            ModuleType.Battery => EquipmentSlotType.Core,
            ModuleType.Generator => EquipmentSlotType.Core,
            ModuleType.Weapon => EquipmentSlotType.Utility,
            ModuleType.Sensor => EquipmentSlotType.Utility,
            ModuleType.Shield => EquipmentSlotType.Utility,
            ModuleType.UtilityActive => EquipmentSlotType.Utility,
            ModuleType.Treads => EquipmentSlotType.Base,
            ModuleType.Legs => EquipmentSlotType.Base,
            ModuleType.Flight => EquipmentSlotType.Base,
            ModuleType.Cargo => EquipmentSlotType.Base,
            ModuleType.Armor => EquipmentSlotType.Any,  // Armor can go in Core or Base
            _ => EquipmentSlotType.Any
        };
    }

    /// <summary>
    /// Check if this module type can be mounted in the given slot type.
    /// </summary>
    public static bool CanMountIn(this ModuleType moduleType, EquipmentSlotType slotType)
    {
        if (moduleType == ModuleType.None)
            return true; // No restriction

        // Armor can be mounted in Core or Base slots only
        if (moduleType == ModuleType.Armor)
            return slotType == EquipmentSlotType.Core || slotType == EquipmentSlotType.Base;

        return moduleType.GetRequiredSlotType() == slotType;
    }

    /// <summary>
    /// Get a display name for this module type.
    /// </summary>
    public static string GetDisplayName(this ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Logic => "Logic Module",
            ModuleType.Battery => "Battery",
            ModuleType.Generator => "Generator",
            ModuleType.Weapon => "Weapon",
            ModuleType.Sensor => "Sensor",
            ModuleType.Shield => "Shield",
            ModuleType.UtilityActive => "Utility Device",
            ModuleType.Treads => "Treads",
            ModuleType.Legs => "Legs",
            ModuleType.Flight => "Flight System",
            ModuleType.Cargo => "Cargo Module",
            ModuleType.Armor => "Armor Plating",
            _ => "Unknown Module"
        };
    }
}
