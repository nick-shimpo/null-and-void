using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;

namespace NullAndVoid.Items;

/// <summary>
/// Factory for creating predefined ammunition types.
/// </summary>
public static class AmmoFactory
{
    private static readonly Random _random = new();

    #region Basic Ammunition

    /// <summary>
    /// Standard kinetic rounds for projectile weapons.
    /// </summary>
    public static Ammunition CreateKineticRounds(int quantity = 30)
    {
        return new Ammunition
        {
            AmmoId = "ammo_kinetic",
            Name = "Kinetic Rounds",
            Description = "Standard ballistic ammunition. Reliable and common.",
            Type = AmmoType.Basic,
            Quantity = quantity,
            MaxStack = 100,
            DamageMultiplier = 1.0f,
            DisplayColor = new Color(0.7f, 0.6f, 0.4f),
            Rarity = ItemRarity.Common
        };
    }

    /// <summary>
    /// Armor-piercing rounds with penetration bonus.
    /// </summary>
    public static Ammunition CreateArmorPiercingRounds(int quantity = 20)
    {
        return new Ammunition
        {
            AmmoId = "ammo_ap",
            Name = "Armor-Piercing Rounds",
            Description = "Hardened penetrator rounds that punch through armor.",
            Type = AmmoType.Basic,
            Quantity = quantity,
            MaxStack = 50,
            DamageMultiplier = 0.9f,  // Slightly less raw damage
            PenetrationBonus = 5,
            DisplayColor = new Color(0.3f, 0.3f, 0.4f),
            Rarity = ItemRarity.Uncommon
        };
    }

    /// <summary>
    /// High-velocity rounds with accuracy bonus.
    /// </summary>
    public static Ammunition CreateHighVelocityRounds(int quantity = 25)
    {
        return new Ammunition
        {
            AmmoId = "ammo_hv",
            Name = "High-Velocity Rounds",
            Description = "Precisely machined rounds for improved accuracy.",
            Type = AmmoType.Basic,
            Quantity = quantity,
            MaxStack = 75,
            DamageMultiplier = 1.0f,
            AccuracyModifier = 10,
            DisplayColor = new Color(0.5f, 0.7f, 0.8f),
            Rarity = ItemRarity.Uncommon
        };
    }

    /// <summary>
    /// Explosive-tipped rounds.
    /// </summary>
    public static Ammunition CreateExplosiveRounds(int quantity = 15)
    {
        return new Ammunition
        {
            AmmoId = "ammo_explosive",
            Name = "Explosive Rounds",
            Description = "Rounds with micro-explosive tips. Devastating on impact.",
            Type = AmmoType.Basic,
            Quantity = quantity,
            MaxStack = 30,
            DamageMultiplier = 1.3f,
            OverrideDamageType = DamageType.Explosive,
            AccuracyModifier = -5,  // Less accurate
            DisplayColor = new Color(1.0f, 0.5f, 0.2f),
            Rarity = ItemRarity.Rare
        };
    }

    /// <summary>
    /// Incendiary rounds that set targets on fire.
    /// </summary>
    public static Ammunition CreateIncendiaryRounds(int quantity = 20)
    {
        return new Ammunition
        {
            AmmoId = "ammo_incendiary",
            Name = "Incendiary Rounds",
            Description = "Thermite-tipped rounds that ignite targets.",
            Type = AmmoType.Basic,
            Quantity = quantity,
            MaxStack = 40,
            DamageMultiplier = 0.85f,
            OverrideDamageType = DamageType.Thermal,
            AppliedEffect = WeaponEffect.Burning,
            EffectChance = 60,
            EffectDuration = 3,
            DisplayColor = new Color(1.0f, 0.4f, 0.1f),
            Rarity = ItemRarity.Uncommon
        };
    }

    /// <summary>
    /// EMP rounds that disrupt electronic systems.
    /// </summary>
    public static Ammunition CreateEMPRounds(int quantity = 15)
    {
        return new Ammunition
        {
            AmmoId = "ammo_emp",
            Name = "EMP Rounds",
            Description = "Electromagnetic pulse rounds that corrupt enemy systems.",
            Type = AmmoType.Basic,
            Quantity = quantity,
            MaxStack = 30,
            DamageMultiplier = 0.7f,
            OverrideDamageType = DamageType.Electromagnetic,
            AppliedEffect = WeaponEffect.Corrupted,
            EffectChance = 50,
            EffectDuration = 2,
            DisplayColor = new Color(0.3f, 0.6f, 1.0f),
            Rarity = ItemRarity.Rare
        };
    }

    #endregion

    #region Energy Ammunition

    /// <summary>
    /// Standard power cells for energy weapons.
    /// </summary>
    public static Ammunition CreatePowerCells(int quantity = 20)
    {
        return new Ammunition
        {
            AmmoId = "ammo_power_cell",
            Name = "Power Cells",
            Description = "Standard energy cells for beam weapons.",
            Type = AmmoType.Energy,
            Quantity = quantity,
            MaxStack = 50,
            DamageMultiplier = 1.0f,
            DisplayColor = new Color(0.4f, 0.8f, 0.4f),
            Rarity = ItemRarity.Common
        };
    }

    /// <summary>
    /// Overcharged power cells with bonus damage.
    /// </summary>
    public static Ammunition CreateOverchargedCells(int quantity = 10)
    {
        return new Ammunition
        {
            AmmoId = "ammo_overcharged",
            Name = "Overcharged Cells",
            Description = "Unstable energy cells that deal extra damage.",
            Type = AmmoType.Energy,
            Quantity = quantity,
            MaxStack = 25,
            DamageMultiplier = 1.5f,
            AccuracyModifier = -5,
            DisplayColor = new Color(0.8f, 1.0f, 0.3f),
            Rarity = ItemRarity.Rare
        };
    }

    /// <summary>
    /// Focused power cells for precision shots.
    /// </summary>
    public static Ammunition CreateFocusedCells(int quantity = 15)
    {
        return new Ammunition
        {
            AmmoId = "ammo_focused",
            Name = "Focused Cells",
            Description = "Precision-tuned cells for accurate fire.",
            Type = AmmoType.Energy,
            Quantity = quantity,
            MaxStack = 30,
            DamageMultiplier = 0.9f,
            AccuracyModifier = 15,
            DisplayColor = new Color(0.2f, 0.6f, 1.0f),
            Rarity = ItemRarity.Uncommon
        };
    }

    #endregion

    #region Seeker Ammunition

    /// <summary>
    /// Standard seeker missile fuel.
    /// </summary>
    public static Ammunition CreateSeekerFuel(int quantity = 5)
    {
        return new Ammunition
        {
            AmmoId = "ammo_seeker",
            Name = "Seeker Fuel",
            Description = "Propellant for guided seeker missiles.",
            Type = AmmoType.Seeker,
            Quantity = quantity,
            MaxStack = 15,
            DamageMultiplier = 1.0f,
            DisplayColor = new Color(0.9f, 0.3f, 0.3f),
            Rarity = ItemRarity.Rare
        };
    }

    /// <summary>
    /// Enhanced seeker fuel with longer tracking range.
    /// </summary>
    public static Ammunition CreateEnhancedSeekerFuel(int quantity = 3)
    {
        return new Ammunition
        {
            AmmoId = "ammo_seeker_enhanced",
            Name = "Enhanced Seeker Fuel",
            Description = "High-performance propellant for extended tracking.",
            Type = AmmoType.Seeker,
            Quantity = quantity,
            MaxStack = 10,
            DamageMultiplier = 1.2f,
            DisplayColor = new Color(1.0f, 0.4f, 0.8f),
            Rarity = ItemRarity.Epic
        };
    }

    #endregion

    #region Orbital Ammunition

    /// <summary>
    /// Standard orbital strike rocket propellant.
    /// </summary>
    public static Ammunition CreateOrbitalRockets(int quantity = 2)
    {
        return new Ammunition
        {
            AmmoId = "ammo_orbital",
            Name = "Orbital Rocket",
            Description = "Rare rocket propellant for low-orbit strike weapons.",
            Type = AmmoType.Orbital,
            Quantity = quantity,
            MaxStack = 5,
            DamageMultiplier = 1.0f,
            DisplayColor = new Color(1.0f, 0.8f, 0.2f),
            Rarity = ItemRarity.Epic
        };
    }

    /// <summary>
    /// Tactical nuclear warhead for orbital strikes.
    /// </summary>
    public static Ammunition CreateTacticalNuke(int quantity = 1)
    {
        return new Ammunition
        {
            AmmoId = "ammo_nuke",
            Name = "Tactical Warhead",
            Description = "Devastating nuclear warhead. Use with extreme caution.",
            Type = AmmoType.Orbital,
            Quantity = quantity,
            MaxStack = 1,
            DamageMultiplier = 3.0f,
            AppliedEffect = WeaponEffect.Burning,
            EffectChance = 100,
            EffectDuration = 5,
            DisplayColor = new Color(0.8f, 1.0f, 0.1f),
            Rarity = ItemRarity.Legendary
        };
    }

    #endregion

    #region Random Generation

    /// <summary>
    /// Create random ammunition based on rarity.
    /// </summary>
    public static Ammunition CreateRandomAmmo(ItemRarity? rarity = null)
    {
        rarity ??= RollRarity();

        return rarity.Value switch
        {
            ItemRarity.Common => _random.Next(3) switch
            {
                0 => CreateKineticRounds(_random.Next(20, 50)),
                1 => CreatePowerCells(_random.Next(15, 30)),
                _ => CreateKineticRounds(_random.Next(25, 60))
            },
            ItemRarity.Uncommon => _random.Next(4) switch
            {
                0 => CreateArmorPiercingRounds(_random.Next(15, 30)),
                1 => CreateHighVelocityRounds(_random.Next(20, 40)),
                2 => CreateIncendiaryRounds(_random.Next(15, 30)),
                _ => CreateFocusedCells(_random.Next(10, 20))
            },
            ItemRarity.Rare => _random.Next(4) switch
            {
                0 => CreateExplosiveRounds(_random.Next(10, 20)),
                1 => CreateEMPRounds(_random.Next(10, 20)),
                2 => CreateOverchargedCells(_random.Next(8, 15)),
                _ => CreateSeekerFuel(_random.Next(3, 8))
            },
            ItemRarity.Epic => _random.Next(3) switch
            {
                0 => CreateEnhancedSeekerFuel(_random.Next(2, 5)),
                1 => CreateOrbitalRockets(_random.Next(1, 3)),
                _ => CreateExplosiveRounds(_random.Next(15, 25))
            },
            ItemRarity.Legendary => CreateTacticalNuke(1),
            _ => CreateKineticRounds(30)
        };
    }

    /// <summary>
    /// Create ammo appropriate for a specific ammo type.
    /// </summary>
    public static Ammunition CreateAmmoOfType(AmmoType type, int quantity = -1)
    {
        return type switch
        {
            AmmoType.Basic => CreateKineticRounds(quantity > 0 ? quantity : 30),
            AmmoType.Energy => CreatePowerCells(quantity > 0 ? quantity : 20),
            AmmoType.Seeker => CreateSeekerFuel(quantity > 0 ? quantity : 5),
            AmmoType.Orbital => CreateOrbitalRockets(quantity > 0 ? quantity : 2),
            _ => CreateKineticRounds(quantity > 0 ? quantity : 30)
        };
    }

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

    #endregion

    #region Utility

    /// <summary>
    /// Get all available ammo type names for documentation/testing.
    /// </summary>
    public static List<string> GetAllAmmoTypes()
    {
        return new List<string>
        {
            // Basic
            "Kinetic Rounds",
            "Armor-Piercing Rounds",
            "High-Velocity Rounds",
            "Explosive Rounds",
            "Incendiary Rounds",
            "EMP Rounds",
            // Energy
            "Power Cells",
            "Overcharged Cells",
            "Focused Cells",
            // Seeker
            "Seeker Fuel",
            "Enhanced Seeker Fuel",
            // Orbital
            "Orbital Rocket",
            "Tactical Warhead"
        };
    }

    #endregion
}
