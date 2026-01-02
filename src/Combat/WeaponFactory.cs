using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Items;

// Note: AmmoType is in NullAndVoid.Items namespace

namespace NullAndVoid.Combat;

/// <summary>
/// Factory for creating weapon items with predefined or random stats.
/// </summary>
public static class WeaponFactory
{
    private static readonly Random _random = new();

    // Rarity colors (same as ItemFactory)
    private static readonly Dictionary<ItemRarity, Color> _rarityColors = new()
    {
        { ItemRarity.Common, new Color(0.7f, 0.7f, 0.7f) },
        { ItemRarity.Uncommon, new Color(0.3f, 0.9f, 0.3f) },
        { ItemRarity.Rare, new Color(0.3f, 0.5f, 1.0f) },
        { ItemRarity.Epic, new Color(0.7f, 0.3f, 0.9f) },
        { ItemRarity.Legendary, new Color(1.0f, 0.6f, 0.1f) }
    };

    // Weapon color palettes by damage type
    private static readonly Dictionary<DamageType, Color> _damageTypeColors = new()
    {
        { DamageType.Kinetic, new Color(0.8f, 0.7f, 0.5f) },      // Brass/bullet
        { DamageType.Thermal, new Color(1.0f, 0.4f, 0.1f) },      // Orange/fire
        { DamageType.Electromagnetic, new Color(0.3f, 0.7f, 1.0f) }, // Electric blue
        { DamageType.Explosive, new Color(1.0f, 0.6f, 0.0f) },    // Explosion orange
        { DamageType.Impact, new Color(0.6f, 0.6f, 0.7f) }        // Metal gray
    };

    #region Starter Weapons

    /// <summary>
    /// Create the starting melee weapon.
    /// </summary>
    public static Item CreateStarterMeleeWeapon()
    {
        return new Item
        {
            Name = "Plasma Cutter",
            ShortDesc = "DMG 6-10 RNG 1",
            Description = "A repurposed industrial cutting tool. Still cuts through things effectively.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = ItemRarity.Common,
            DisplayColor = _damageTypeColors[DamageType.Thermal],
            ModuleCategory = ModuleType.Weapon,
            EnergyConsumption = 2,
            BootCost = 3,
            IsIdentified = true,  // Starter items are pre-identified
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Melee,
                DamageType = DamageType.Thermal,
                MinDamage = 6,
                MaxDamage = 10,
                Range = 1,
                BaseAccuracy = 0,  // Melee auto-hits
                EnergyCost = 3,
                ActionCost = 100,
                PrimaryEffect = WeaponEffect.Burning,
                EffectChance = 15,
                EffectDuration = 2,
                NoiseGenerated = 15,
                ProjectileStyle = ProjectileStyle.None,
                ImpactChar = "*",
                ImpactColor = new Color(1.0f, 0.5f, 0.0f)
            }
        };
    }

    /// <summary>
    /// Create the starting ranged weapon.
    /// </summary>
    public static Item CreateStarterRangedWeapon()
    {
        return new Item
        {
            Name = "Nail Gun",
            ShortDesc = "DMG 4-6 RNG 8",
            Description = "A pneumatic nail driver repurposed for combat. Fast but weak. Uses basic ammo.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = ItemRarity.Common,
            DisplayColor = _damageTypeColors[DamageType.Kinetic],
            ModuleCategory = ModuleType.Weapon,
            EnergyConsumption = 1,
            BootCost = 2,
            IsIdentified = true,  // Starter items are pre-identified
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Projectile,
                DamageType = DamageType.Kinetic,
                MinDamage = 4,
                MaxDamage = 6,
                Range = 8,
                BaseAccuracy = 75,
                EnergyCost = 2,
                ActionCost = 50,  // Fast weapon
                RequiredAmmoType = AmmoType.Basic,
                AmmoPerShot = 1,
                NoiseGenerated = 20,
                ProjectileChar = "-",
                ProjectileColor = new Color(0.7f, 0.7f, 0.7f),
                ProjectileStyle = ProjectileStyle.Bullet,
                ProjectileSpeed = 40.0f,
                ImpactChar = "*",
                ImpactColor = new Color(1.0f, 0.8f, 0.0f)
            }
        };
    }

    #endregion

    #region Melee Weapons

    public static Item CreatePlasmaCutter(ItemRarity rarity = ItemRarity.Common)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Plasma Cutter",
            Description = "An industrial cutting tool that superheats materials for precise cuts.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 2 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Melee,
                DamageType = DamageType.Thermal,
                MinDamage = 6 + rarityBonus * 2,
                MaxDamage = 10 + rarityBonus * 3,
                Range = 1,
                EnergyCost = 3 + rarityBonus,
                ActionCost = 100,
                PrimaryEffect = WeaponEffect.Burning,
                EffectChance = 15 + rarityBonus * 5,
                EffectDuration = 2 + rarityBonus / 2,
                NoiseGenerated = 15,
                ProjectileStyle = ProjectileStyle.None,
                ImpactColor = new Color(1.0f, 0.5f, 0.0f)
            }
        };
    }

    public static Item CreateArcWelder(ItemRarity rarity = ItemRarity.Common)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Arc Welder",
            Description = "An electric welding tool. The arc can jump to nearby targets.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 3 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Melee,
                DamageType = DamageType.Electromagnetic,
                MinDamage = 5 + rarityBonus * 2,
                MaxDamage = 8 + rarityBonus * 2,
                Range = 1,
                EnergyCost = 5 + rarityBonus,
                ActionCost = 100,
                PrimaryEffect = WeaponEffect.Chain,
                ChainTargets = 1 + rarityBonus / 2,
                EffectChance = 50 + rarityBonus * 10,
                NoiseGenerated = 25,
                ProjectileStyle = ProjectileStyle.None,
                ImpactChar = "~",
                ImpactColor = new Color(0.5f, 0.8f, 1.0f)
            }
        };
    }

    public static Item CreatePneumaticRam(ItemRarity rarity = ItemRarity.Common)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Pneumatic Ram",
            Description = "A hydraulic piston that delivers devastating knockback.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 2 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Melee,
                DamageType = DamageType.Impact,
                MinDamage = 8 + rarityBonus * 2,
                MaxDamage = 14 + rarityBonus * 3,
                Range = 1,
                EnergyCost = 4 + rarityBonus,
                ActionCost = 120,  // Slightly slow
                PrimaryEffect = WeaponEffect.Knockback,
                KnockbackDistance = 2 + rarityBonus / 2,
                EffectChance = 100,  // Always knockback
                NoiseGenerated = 30,
                ProjectileStyle = ProjectileStyle.None,
                ImpactChar = "!",
                ImpactColor = new Color(0.8f, 0.8f, 0.9f)
            }
        };
    }

    public static Item CreateVibroSaw(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Vibro-Saw",
            Description = "High-frequency oscillating blade that can sever enemy modules.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 4 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Melee,
                DamageType = DamageType.Kinetic,
                MinDamage = 12 + rarityBonus * 3,
                MaxDamage = 18 + rarityBonus * 4,
                Range = 1,
                EnergyCost = 8 + rarityBonus,
                ActionCost = 100,
                CriticalChance = 10 + rarityBonus * 3,
                CriticalMultiplier = 2.5f,
                PrimaryEffect = WeaponEffect.Sever,
                EffectChance = 25 + rarityBonus * 5,
                NoiseGenerated = 35,
                ProjectileStyle = ProjectileStyle.None,
                ImpactChar = "/",
                ImpactColor = new Color(1.0f, 0.3f, 0.3f)
            }
        };
    }

    public static Item CreateThermalLance(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Thermal Lance",
            Description = "Superheated cutting tool. Extreme damage and guaranteed burn.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 4 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Melee,
                DamageType = DamageType.Thermal,
                MinDamage = 15 + rarityBonus * 3,
                MaxDamage = 20 + rarityBonus * 4,
                Range = 1,
                EnergyCost = 10 + rarityBonus * 2,
                ActionCost = 120,
                PrimaryEffect = WeaponEffect.Burning,
                EffectChance = 100,  // Always burns
                EffectDuration = 3 + rarityBonus / 2,
                NoiseGenerated = 25,
                HeatGenerated = 8 + rarityBonus,
                ProjectileStyle = ProjectileStyle.None,
                ImpactChar = "^",
                ImpactColor = new Color(1.0f, 0.3f, 0.0f)
            }
        };
    }

    public static Item CreateEMPBlade(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " EMP Blade",
            Description = "Electrified blade that disrupts enemy systems.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 3 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Melee,
                DamageType = DamageType.Electromagnetic,
                MinDamage = 4 + rarityBonus * 2,
                MaxDamage = 8 + rarityBonus * 2,
                Range = 1,
                EnergyCost = 8 + rarityBonus,
                ActionCost = 100,
                PrimaryEffect = WeaponEffect.Corrupted,
                EffectChance = 40 + rarityBonus * 10,
                EffectDuration = 3 + rarityBonus / 2,
                NoiseGenerated = 20,
                ProjectileStyle = ProjectileStyle.None,
                ImpactChar = "~",
                ImpactColor = new Color(0.4f, 0.7f, 1.0f)
            }
        };
    }

    #endregion

    #region Projectile Weapons

    public static Item CreateNailGun(ItemRarity rarity = ItemRarity.Common)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Nail Gun",
            Description = "Pneumatic nail driver. Fast rate of fire, moderate accuracy. Uses basic ammo.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 1,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Projectile,
                DamageType = DamageType.Kinetic,
                MinDamage = 4 + rarityBonus,
                MaxDamage = 6 + rarityBonus * 2,
                Range = 8 + rarityBonus,
                BaseAccuracy = 75 + rarityBonus * 3,
                EnergyCost = 2,
                ActionCost = 50,  // Fast
                RequiredAmmoType = AmmoType.Basic,
                AmmoPerShot = 1,
                NoiseGenerated = 20,
                ProjectileChar = "-",
                ProjectileColor = new Color(0.7f, 0.7f, 0.7f),
                ProjectileStyle = ProjectileStyle.Bullet,
                ProjectileSpeed = 40.0f,
                ImpactColor = new Color(1.0f, 0.8f, 0.0f)
            }
        };
    }

    public static Item CreateRivetCannon(ItemRarity rarity = ItemRarity.Uncommon)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Rivet Cannon",
            Description = "Heavy-duty rivet launcher. Packs a punch at medium range. Uses basic ammo.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 2,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Projectile,
                DamageType = DamageType.Kinetic,
                MinDamage = 8 + rarityBonus * 2,
                MaxDamage = 12 + rarityBonus * 3,
                Range = 12 + rarityBonus,
                BaseAccuracy = 60 + rarityBonus * 4,
                EnergyCost = 5 + rarityBonus,
                ActionCost = 100,
                RequiredAmmoType = AmmoType.Basic,
                AmmoPerShot = 2,  // Heavy rounds
                CriticalChance = 8 + rarityBonus * 2,
                NoiseGenerated = 35,
                ProjectileChar = "=",
                ProjectileColor = new Color(0.6f, 0.5f, 0.4f),
                ProjectileStyle = ProjectileStyle.Bullet,
                ProjectileSpeed = 35.0f,
                ImpactColor = new Color(1.0f, 0.6f, 0.0f)
            }
        };
    }

    public static Item CreateRailDriver(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Rail Driver",
            Description = "Electromagnetic accelerator. Extreme range and penetration. Uses basic ammo.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 4 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Projectile,
                DamageType = DamageType.Kinetic,
                MinDamage = 15 + rarityBonus * 3,
                MaxDamage = 25 + rarityBonus * 5,
                Range = 18 + rarityBonus * 2,
                BaseAccuracy = 55 + rarityBonus * 5,
                EnergyCost = 12 + rarityBonus * 2,
                ActionCost = 150,  // Slow but powerful
                RequiredAmmoType = AmmoType.Basic,
                AmmoPerShot = 3,  // High-powered shot
                Cooldown = 1,  // 1 turn cooldown
                CriticalChance = 15 + rarityBonus * 3,
                CriticalMultiplier = 2.5f,
                NoiseGenerated = 50,
                ProjectileChar = "=",
                ProjectileColor = new Color(0.4f, 0.6f, 1.0f),
                ProjectileStyle = ProjectileStyle.Beam,
                ProjectileSpeed = 100.0f,  // Nearly instant
                ImpactChar = "*",
                ImpactColor = new Color(0.6f, 0.8f, 1.0f)
            }
        };
    }

    public static Item CreateFlechetteLauncher(ItemRarity rarity = ItemRarity.Uncommon)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Flechette Launcher",
            Description = "Fires a spread of razor-sharp flechettes. Devastating at close range. Uses basic ammo.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 2,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Projectile,
                DamageType = DamageType.Kinetic,
                MinDamage = 2 + rarityBonus,
                MaxDamage = 4 + rarityBonus,
                ProjectileCount = 5 + rarityBonus,  // Multiple hits!
                Range = 6 + rarityBonus / 2,
                BaseAccuracy = 70 + rarityBonus * 2,
                EnergyCost = 8 + rarityBonus,
                ActionCost = 100,
                RequiredAmmoType = AmmoType.Basic,
                AmmoPerShot = 5 + rarityBonus,  // Uses many flechettes per shot
                NoiseGenerated = 40,
                ProjectileChar = ".",
                ProjectileColor = new Color(0.8f, 0.8f, 0.8f),
                ProjectileStyle = ProjectileStyle.Bullet,
                ProjectileSpeed = 45.0f,
                ImpactColor = new Color(1.0f, 0.4f, 0.4f)
            }
        };
    }

    #endregion

    #region Energy Weapons

    public static Item CreateLaserEmitter(ItemRarity rarity = ItemRarity.Uncommon)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Laser Emitter",
            Description = "Focused light beam. High accuracy, ignores partial cover.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 3 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Energy,
                DamageType = DamageType.Thermal,
                MinDamage = 6 + rarityBonus * 2,
                MaxDamage = 10 + rarityBonus * 3,
                Range = 15 + rarityBonus,
                BaseAccuracy = 80 + rarityBonus * 3,
                EnergyCost = 8 + rarityBonus * 2,
                ActionCost = 100,
                IgnoresCover = true,
                PrimaryEffect = WeaponEffect.Burning,
                EffectChance = 20 + rarityBonus * 5,
                EffectDuration = 2,
                NoiseGenerated = 15,  // Quiet
                HeatGenerated = 5 + rarityBonus,
                ProjectileChar = "-",
                ProjectileColor = new Color(1.0f, 0.2f, 0.2f),
                ProjectileStyle = ProjectileStyle.Beam,
                ProjectileSpeed = 200.0f,  // Instant
                ImpactColor = new Color(1.0f, 0.5f, 0.0f)
            }
        };
    }

    public static Item CreatePulseRifle(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Pulse Rifle",
            Description = "Fires electromagnetic pulses. Can corrupt enemy systems.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 4 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Energy,
                DamageType = DamageType.Electromagnetic,
                MinDamage = 8 + rarityBonus * 2,
                MaxDamage = 14 + rarityBonus * 3,
                Range = 12 + rarityBonus,
                BaseAccuracy = 65 + rarityBonus * 4,
                EnergyCost = 10 + rarityBonus * 2,
                ActionCost = 100,
                PrimaryEffect = WeaponEffect.Corrupted,
                EffectChance = 30 + rarityBonus * 5,
                EffectDuration = 3 + rarityBonus / 2,
                NoiseGenerated = 25,
                HeatGenerated = 3 + rarityBonus,
                ProjectileChar = "o",
                ProjectileColor = new Color(0.3f, 0.7f, 1.0f),
                ProjectileStyle = ProjectileStyle.Bullet,
                ProjectileSpeed = 30.0f,
                ImpactChar = "*",
                ImpactColor = new Color(0.5f, 0.8f, 1.0f)
            }
        };
    }

    public static Item CreateArcProjector(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Arc Projector",
            Description = "Projects electrical arcs that chain between targets.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 5 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Energy,
                DamageType = DamageType.Electromagnetic,
                MinDamage = 5 + rarityBonus * 2,
                MaxDamage = 8 + rarityBonus * 2,
                Range = 6 + rarityBonus,
                BaseAccuracy = 70 + rarityBonus * 3,
                EnergyCost = 12 + rarityBonus * 2,
                ActionCost = 100,
                PrimaryEffect = WeaponEffect.Chain,
                ChainTargets = 3 + rarityBonus,
                EffectChance = 80,
                NoiseGenerated = 35,
                HeatGenerated = 8 + rarityBonus,
                ProjectileChar = "~",
                ProjectileColor = new Color(0.5f, 0.8f, 1.0f),
                ProjectileStyle = ProjectileStyle.Arc,
                ProjectileSpeed = 25.0f,
                ImpactChar = "*",
                ImpactColor = new Color(0.3f, 0.6f, 1.0f)
            }
        };
    }

    public static Item CreatePlasmaCaster(ItemRarity rarity = ItemRarity.Epic)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Plasma Caster",
            Description = "Launches superheated plasma. Splash damage on impact.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 6 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.Energy,
                DamageType = DamageType.Thermal,
                MinDamage = 12 + rarityBonus * 3,
                MaxDamage = 20 + rarityBonus * 4,
                Range = 8 + rarityBonus,
                BaseAccuracy = 55 + rarityBonus * 5,
                AreaRadius = 1,  // Small splash
                EnergyCost = 15 + rarityBonus * 3,
                ActionCost = 120,
                PrimaryEffect = WeaponEffect.Burning,
                EffectChance = 40 + rarityBonus * 5,
                EffectDuration = 3,
                NoiseGenerated = 45,
                HeatGenerated = 12 + rarityBonus * 2,
                ProjectileChar = "O",
                ProjectileColor = new Color(0.2f, 1.0f, 0.5f),
                ProjectileStyle = ProjectileStyle.Bullet,
                ProjectileSpeed = 20.0f,
                ImpactChar = "*",
                ImpactColor = new Color(0.3f, 1.0f, 0.6f)
            }
        };
    }

    #endregion

    #region Area Effect Weapons

    public static Item CreateFragGrenade(ItemRarity rarity = ItemRarity.Uncommon)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Frag Grenade",
            Description = "Throwable explosive. Damages all targets in blast radius.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.AreaEffect,
                DamageType = DamageType.Explosive,
                MinDamage = 8 + rarityBonus * 2,
                MaxDamage = 15 + rarityBonus * 3,
                Range = 8 + rarityBonus,
                AreaRadius = 2 + rarityBonus / 2,
                BaseAccuracy = 65 + rarityBonus * 3,
                IndirectFire = true,  // Can throw over obstacles
                EnergyCost = 0,  // No energy, but limited use
                ActionCost = 100,
                Cooldown = 2,  // 2 turn cooldown
                NoiseGenerated = 60,
                ProjectileChar = "o",
                ProjectileColor = new Color(0.5f, 0.5f, 0.5f),
                ProjectileStyle = ProjectileStyle.Lobbed,
                ProjectileSpeed = 15.0f,
                ImpactChar = "*",
                ImpactColor = new Color(1.0f, 0.6f, 0.0f)
            }
        };
    }

    public static Item CreateEMPBomb(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " EMP Bomb",
            Description = "Electromagnetic pulse device. Disables electronics in area.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.AreaEffect,
                DamageType = DamageType.Electromagnetic,
                MinDamage = 3 + rarityBonus,
                MaxDamage = 6 + rarityBonus * 2,
                Range = 6 + rarityBonus,
                AreaRadius = 3 + rarityBonus / 2,
                BaseAccuracy = 70 + rarityBonus * 3,
                IndirectFire = true,
                EnergyCost = 5 + rarityBonus,
                ActionCost = 100,
                Cooldown = 3,
                PrimaryEffect = WeaponEffect.Stunned,
                EffectChance = 50 + rarityBonus * 10,
                EffectDuration = 2,
                NoiseGenerated = 40,
                ProjectileChar = "o",
                ProjectileColor = new Color(0.3f, 0.6f, 1.0f),
                ProjectileStyle = ProjectileStyle.Lobbed,
                ProjectileSpeed = 12.0f,
                ImpactChar = "*",
                ImpactColor = new Color(0.5f, 0.8f, 1.0f)
            }
        };
    }

    public static Item CreateIncendiaryGrenade(ItemRarity rarity = ItemRarity.Uncommon)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Incendiary Grenade",
            Description = "Creates a burning zone on impact. Sets targets ablaze.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.AreaEffect,
                DamageType = DamageType.Thermal,
                MinDamage = 6 + rarityBonus * 2,
                MaxDamage = 10 + rarityBonus * 2,
                Range = 8 + rarityBonus,
                AreaRadius = 2 + rarityBonus / 2,
                BaseAccuracy = 65 + rarityBonus * 3,
                IndirectFire = true,
                EnergyCost = 0,
                ActionCost = 100,
                Cooldown = 2,
                PrimaryEffect = WeaponEffect.Burning,
                EffectChance = 80 + rarityBonus * 5,
                EffectDuration = 3 + rarityBonus / 2,
                NoiseGenerated = 50,
                ProjectileChar = "o",
                ProjectileColor = new Color(1.0f, 0.4f, 0.0f),
                ProjectileStyle = ProjectileStyle.Lobbed,
                ProjectileSpeed = 15.0f,
                ImpactChar = "*",
                ImpactColor = new Color(1.0f, 0.5f, 0.0f)
            }
        };
    }

    public static Item CreateClusterLauncher(ItemRarity rarity = ItemRarity.Epic)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Cluster Launcher",
            Description = "Fires a cluster of micro-explosives. Devastates an area.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 5 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.AreaEffect,
                DamageType = DamageType.Explosive,
                MinDamage = 5 + rarityBonus * 2,
                MaxDamage = 8 + rarityBonus * 2,
                ProjectileCount = 4 + rarityBonus,  // Multiple clusters
                Range = 12 + rarityBonus,
                AreaRadius = 1,  // Each cluster has small radius
                BaseAccuracy = 60 + rarityBonus * 4,
                EnergyCost = 18 + rarityBonus * 2,
                ActionCost = 150,
                Cooldown = 3,
                NoiseGenerated = 70,
                ProjectileChar = "o",
                ProjectileColor = new Color(0.8f, 0.4f, 0.0f),
                ProjectileStyle = ProjectileStyle.Lobbed,
                ProjectileSpeed = 10.0f,
                ImpactChar = "*",
                ImpactColor = new Color(1.0f, 0.6f, 0.0f)
            }
        };
    }

    public static Item CreateShockwaveGenerator(ItemRarity rarity = ItemRarity.Rare)
    {
        int rarityBonus = (int)rarity;
        return new Item
        {
            Name = GetRarityPrefix(rarity) + " Shockwave Generator",
            Description = "Generates a kinetic blast wave around the user. Knocks back all nearby.",
            SlotType = EquipmentSlotType.Utility,
            Rarity = rarity,
            DisplayColor = _rarityColors[rarity],
            EnergyConsumption = 4 + rarityBonus,
            WeaponData = new WeaponData
            {
                Category = WeaponCategory.AreaEffect,
                DamageType = DamageType.Impact,
                MinDamage = 6 + rarityBonus * 2,
                MaxDamage = 10 + rarityBonus * 2,
                Range = 0,  // Centered on self
                AreaRadius = 2 + rarityBonus / 2,
                BaseAccuracy = 100,  // Always hits in radius
                EnergyCost = 20 + rarityBonus * 3,
                ActionCost = 100,
                Cooldown = 4,
                PrimaryEffect = WeaponEffect.Knockback,
                KnockbackDistance = 2 + rarityBonus / 2,
                EffectChance = 100,
                NoiseGenerated = 60,
                ProjectileStyle = ProjectileStyle.Explosion,
                ImpactChar = "*",
                ImpactColor = new Color(0.8f, 0.8f, 1.0f)
            }
        };
    }

    #endregion

    #region Random Weapon Generation

    /// <summary>
    /// Create a random weapon of the specified rarity.
    /// </summary>
    public static Item CreateRandomWeapon(ItemRarity? rarity = null)
    {
        rarity ??= RollRarity();

        // Weight weapon types by rarity
        int roll = _random.Next(100);

        return rarity.Value switch
        {
            ItemRarity.Common => roll switch
            {
                < 35 => CreateNailGun(rarity.Value),
                < 60 => CreatePlasmaCutter(rarity.Value),
                < 80 => CreatePneumaticRam(rarity.Value),
                _ => CreateArcWelder(rarity.Value)
            },
            ItemRarity.Uncommon => roll switch
            {
                < 15 => CreateRivetCannon(rarity.Value),
                < 30 => CreateFlechetteLauncher(rarity.Value),
                < 45 => CreateLaserEmitter(rarity.Value),
                < 55 => CreateArcWelder(rarity.Value),
                < 70 => CreateFragGrenade(rarity.Value),
                < 85 => CreateIncendiaryGrenade(rarity.Value),
                _ => CreatePneumaticRam(rarity.Value)
            },
            ItemRarity.Rare => roll switch
            {
                < 15 => CreateRailDriver(rarity.Value),
                < 30 => CreatePulseRifle(rarity.Value),
                < 45 => CreateArcProjector(rarity.Value),
                < 55 => CreateVibroSaw(rarity.Value),
                < 65 => CreateThermalLance(rarity.Value),
                < 75 => CreateEMPBlade(rarity.Value),
                < 85 => CreateEMPBomb(rarity.Value),
                _ => CreateShockwaveGenerator(rarity.Value)
            },
            ItemRarity.Epic or ItemRarity.Legendary => roll switch
            {
                < 20 => CreatePlasmaCaster(rarity.Value),
                < 35 => CreateClusterLauncher(rarity.Value),
                < 50 => CreateRailDriver(rarity.Value),
                < 65 => CreateVibroSaw(rarity.Value),
                < 80 => CreateArcProjector(rarity.Value),
                < 90 => CreateShockwaveGenerator(rarity.Value),
                _ => CreateThermalLance(rarity.Value)
            },
            _ => CreateNailGun(rarity.Value)
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

    private static string GetRarityPrefix(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "",
            ItemRarity.Uncommon => "Enhanced",
            ItemRarity.Rare => "Advanced",
            ItemRarity.Epic => "Prototype",
            ItemRarity.Legendary => "Omega",
            _ => ""
        };
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get all available weapon types for documentation/testing.
    /// </summary>
    public static List<string> GetAllWeaponTypes()
    {
        return new List<string>
        {
            // Melee
            "Plasma Cutter",
            "Arc Welder",
            "Pneumatic Ram",
            "Vibro-Saw",
            "Thermal Lance",
            "EMP Blade",
            // Projectile
            "Nail Gun",
            "Rivet Cannon",
            "Rail Driver",
            "Flechette Launcher",
            // Energy
            "Laser Emitter",
            "Pulse Rifle",
            "Arc Projector",
            "Plasma Caster",
            // AoE
            "Frag Grenade",
            "EMP Bomb",
            "Incendiary Grenade",
            "Cluster Launcher",
            "Shockwave Generator"
        };
    }

    #endregion
}
