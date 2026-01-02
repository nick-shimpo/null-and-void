using System.Collections.Generic;
using Godot;
using NullAndVoid.Items;

namespace NullAndVoid.Combat;

/// <summary>
/// Contains all weapon-specific data for an item that functions as a weapon.
/// Attach this to an Item via Item.WeaponData to make it a usable weapon.
/// </summary>
public partial class WeaponData : Resource
{
    #region Identity

    /// <summary>
    /// Category of weapon determining usage pattern.
    /// </summary>
    [Export] public WeaponCategory Category { get; set; } = WeaponCategory.Melee;

    /// <summary>
    /// Primary damage type dealt by this weapon.
    /// </summary>
    [Export] public DamageType DamageType { get; set; } = DamageType.Kinetic;

    #endregion

    #region Damage

    /// <summary>
    /// Minimum damage per hit.
    /// </summary>
    [Export] public int MinDamage { get; set; } = 1;

    /// <summary>
    /// Maximum damage per hit.
    /// </summary>
    [Export] public int MaxDamage { get; set; } = 5;

    /// <summary>
    /// Number of projectiles/hits per attack (for shotgun-style weapons).
    /// </summary>
    [Export] public int ProjectileCount { get; set; } = 1;

    /// <summary>
    /// Critical hit chance (0-100). Default 5%.
    /// </summary>
    [Export] public int CriticalChance { get; set; } = 5;

    /// <summary>
    /// Critical hit damage multiplier. Default 2x.
    /// </summary>
    [Export] public float CriticalMultiplier { get; set; } = 2.0f;

    #endregion

    #region Range & Targeting

    /// <summary>
    /// Maximum range in tiles. 1 = melee only.
    /// </summary>
    [Export] public int Range { get; set; } = 1;

    /// <summary>
    /// Base accuracy (0-100). 0 for melee (auto-hit).
    /// </summary>
    [Export] public int BaseAccuracy { get; set; } = 0;

    /// <summary>
    /// Area of effect radius. 0 = single target.
    /// </summary>
    [Export] public int AreaRadius { get; set; } = 0;

    /// <summary>
    /// If true, can fire without line of sight (lobbed weapons).
    /// </summary>
    [Export] public bool IndirectFire { get; set; } = false;

    /// <summary>
    /// If true, ignores partial cover (beams, lasers).
    /// </summary>
    [Export] public bool IgnoresCover { get; set; } = false;

    #endregion

    #region Costs & Timing

    /// <summary>
    /// Energy cost per attack.
    /// </summary>
    [Export] public int EnergyCost { get; set; } = 0;

    /// <summary>
    /// Action cost in time units. 100 = standard action.
    /// </summary>
    [Export] public int ActionCost { get; set; } = 100;

    /// <summary>
    /// Cooldown in turns before weapon can be used again. 0 = no cooldown.
    /// </summary>
    [Export] public int Cooldown { get; set; } = 0;

    /// <summary>
    /// Current cooldown remaining. Runtime state, not exported.
    /// </summary>
    public int CurrentCooldown { get; set; } = 0;

    /// <summary>
    /// Whether the weapon is ready to fire.
    /// </summary>
    public bool IsReady => CurrentCooldown <= 0;

    #endregion

    #region Ammunition

    /// <summary>
    /// Type of ammo required. Null = uses energy only (no physical ammo).
    /// </summary>
    public AmmoType? RequiredAmmoType { get; set; } = null;

    /// <summary>
    /// Ammo consumed per shot.
    /// </summary>
    [Export] public int AmmoPerShot { get; set; } = 1;

    /// <summary>
    /// Whether this weapon uses physical ammunition (vs energy only).
    /// </summary>
    public bool UsesAmmo => RequiredAmmoType.HasValue;

    /// <summary>
    /// Whether this weapon only uses energy (no physical ammo).
    /// </summary>
    public bool UsesEnergyOnly => !RequiredAmmoType.HasValue;

    #endregion

    #region Penetration

    /// <summary>
    /// Whether this energy weapon can penetrate walls.
    /// </summary>
    [Export] public bool CanPenetrate { get; set; } = false;

    /// <summary>
    /// Bonus penetration power for wall destruction.
    /// Added to MaxDamage when calculating penetration depth.
    /// </summary>
    [Export] public int PenetrationPower { get; set; } = 0;

    #endregion

    #region Seeker Properties

    /// <summary>
    /// Whether this weapon fires seeker (homing) projectiles.
    /// </summary>
    [Export] public bool IsSeeker { get; set; } = false;

    /// <summary>
    /// Fuel/turns before seeker expires.
    /// </summary>
    [Export] public int SeekerFuel { get; set; } = 10;

    /// <summary>
    /// Tiles per turn the seeker moves.
    /// </summary>
    [Export] public int SeekerSpeed { get; set; } = 2;

    #endregion

    #region Effects

    /// <summary>
    /// Primary status effect applied on hit.
    /// </summary>
    [Export] public WeaponEffect PrimaryEffect { get; set; } = WeaponEffect.None;

    /// <summary>
    /// Chance to apply primary effect (0-100).
    /// </summary>
    [Export] public int EffectChance { get; set; } = 0;

    /// <summary>
    /// Duration of effect in turns.
    /// </summary>
    [Export] public int EffectDuration { get; set; } = 0;

    /// <summary>
    /// For chain effects, how many additional targets.
    /// </summary>
    [Export] public int ChainTargets { get; set; } = 0;

    /// <summary>
    /// Knockback distance in tiles.
    /// </summary>
    [Export] public int KnockbackDistance { get; set; } = 0;

    /// <summary>
    /// Heat generated per shot (affects accuracy when overheated).
    /// </summary>
    [Export] public int HeatGenerated { get; set; } = 0;

    /// <summary>
    /// Noise generated (alerts enemies).
    /// </summary>
    [Export] public int NoiseGenerated { get; set; } = 10;

    #endregion

    #region Visuals

    /// <summary>
    /// Character used for projectile animation.
    /// </summary>
    [Export] public string ProjectileChar { get; set; } = "*";

    /// <summary>
    /// Color of the projectile.
    /// </summary>
    [Export] public Color ProjectileColor { get; set; } = new Color(1.0f, 0.8f, 0.2f);

    /// <summary>
    /// Style of projectile animation.
    /// </summary>
    [Export] public ProjectileStyle ProjectileStyle { get; set; } = ProjectileStyle.None;

    /// <summary>
    /// Speed of projectile in tiles per second. Higher = faster animation.
    /// </summary>
    [Export] public float ProjectileSpeed { get; set; } = 30.0f;

    /// <summary>
    /// Character used for impact effect.
    /// </summary>
    [Export] public string ImpactChar { get; set; } = "*";

    /// <summary>
    /// Color of the impact effect.
    /// </summary>
    [Export] public Color ImpactColor { get; set; } = new Color(1.0f, 0.5f, 0.0f);

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this is a melee weapon (range 1, bump-attack).
    /// </summary>
    public bool IsMelee => Category == WeaponCategory.Melee || Range <= 1;

    /// <summary>
    /// Whether this is a ranged weapon.
    /// </summary>
    public bool IsRanged => !IsMelee;

    /// <summary>
    /// Whether this weapon has area of effect.
    /// </summary>
    public bool IsAoE => AreaRadius > 0;

    /// <summary>
    /// Average damage per hit.
    /// </summary>
    public float AverageDamage => (MinDamage + MaxDamage) / 2.0f * ProjectileCount;

    /// <summary>
    /// Get a damage range string for display.
    /// </summary>
    public string DamageString => ProjectileCount > 1
        ? $"{MinDamage}-{MaxDamage}x{ProjectileCount}"
        : $"{MinDamage}-{MaxDamage}";

    #endregion

    #region Methods

    /// <summary>
    /// Roll damage for this weapon.
    /// </summary>
    public int RollDamage(System.Random? random = null)
    {
        random ??= new System.Random();
        int total = 0;
        for (int i = 0; i < ProjectileCount; i++)
        {
            total += random.Next(MinDamage, MaxDamage + 1);
        }
        return total;
    }

    /// <summary>
    /// Check if a critical hit occurs.
    /// </summary>
    public bool RollCritical(System.Random? random = null)
    {
        random ??= new System.Random();
        return random.Next(100) < CriticalChance;
    }

    /// <summary>
    /// Apply critical multiplier to damage.
    /// </summary>
    public int ApplyCritical(int baseDamage)
    {
        return (int)(baseDamage * CriticalMultiplier);
    }

    /// <summary>
    /// Advance cooldown by one turn.
    /// </summary>
    public void TickCooldown()
    {
        if (CurrentCooldown > 0)
            CurrentCooldown--;
    }

    /// <summary>
    /// Start the cooldown after firing.
    /// </summary>
    public void StartCooldown()
    {
        CurrentCooldown = Cooldown;
    }

    /// <summary>
    /// Create a deep copy of this weapon data.
    /// </summary>
    public WeaponData Clone()
    {
        return new WeaponData
        {
            Category = Category,
            DamageType = DamageType,
            MinDamage = MinDamage,
            MaxDamage = MaxDamage,
            ProjectileCount = ProjectileCount,
            CriticalChance = CriticalChance,
            CriticalMultiplier = CriticalMultiplier,
            Range = Range,
            BaseAccuracy = BaseAccuracy,
            AreaRadius = AreaRadius,
            IndirectFire = IndirectFire,
            IgnoresCover = IgnoresCover,
            EnergyCost = EnergyCost,
            ActionCost = ActionCost,
            Cooldown = Cooldown,
            PrimaryEffect = PrimaryEffect,
            EffectChance = EffectChance,
            EffectDuration = EffectDuration,
            ChainTargets = ChainTargets,
            KnockbackDistance = KnockbackDistance,
            HeatGenerated = HeatGenerated,
            NoiseGenerated = NoiseGenerated,
            RequiredAmmoType = RequiredAmmoType,
            AmmoPerShot = AmmoPerShot,
            CanPenetrate = CanPenetrate,
            PenetrationPower = PenetrationPower,
            IsSeeker = IsSeeker,
            SeekerFuel = SeekerFuel,
            SeekerSpeed = SeekerSpeed,
            ProjectileChar = ProjectileChar,
            ProjectileColor = ProjectileColor,
            ProjectileStyle = ProjectileStyle,
            ProjectileSpeed = ProjectileSpeed,
            ImpactChar = ImpactChar,
            ImpactColor = ImpactColor,
            CurrentCooldown = 0  // Reset cooldown on clone
        };
    }

    /// <summary>
    /// Get a short stats string for display.
    /// </summary>
    public string GetStatsString()
    {
        var parts = new List<string>
        {
            $"DMG {DamageString}"
        };

        if (Range > 1)
            parts.Add($"RNG {Range}");

        if (BaseAccuracy > 0)
            parts.Add($"ACC {BaseAccuracy}%");

        if (AreaRadius > 0)
            parts.Add($"RAD {AreaRadius}");

        if (EnergyCost > 0)
            parts.Add($"NRG {EnergyCost}");

        if (RequiredAmmoType.HasValue)
            parts.Add($"AMMO {RequiredAmmoType.Value}");

        return string.Join(" ", parts);
    }

    #endregion
}
