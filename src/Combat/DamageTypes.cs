namespace NullAndVoid.Combat;

/// <summary>
/// Types of damage that weapons can deal.
/// Each type has different interactions with armor, shields, and status effects.
/// </summary>
public enum DamageType
{
    /// <summary>
    /// Physical projectile damage (bullets, nails, rails).
    /// +50% vs Armor, -25% vs Shields. Can cause bleeding.
    /// </summary>
    Kinetic,

    /// <summary>
    /// Heat-based damage (lasers, plasma, fire).
    /// +25% vs Shields, applies Burning. Ignores partial cover.
    /// </summary>
    Thermal,

    /// <summary>
    /// Electrical/EMP damage (arc weapons, pulse rifles).
    /// -50% physical damage, causes Corruption effect. Can disable modules.
    /// </summary>
    Electromagnetic,

    /// <summary>
    /// Blast damage (grenades, rockets, mines).
    /// Guaranteed AoE, reduced salvage from destroyed enemies.
    /// </summary>
    Explosive,

    /// <summary>
    /// Physical force damage (rams, knockback weapons).
    /// Causes knockback, targets weakest module. No critical hits.
    /// </summary>
    Impact
}

/// <summary>
/// Categories of weapons determining their usage patterns.
/// </summary>
public enum WeaponCategory
{
    /// <summary>
    /// Close-range weapons activated by bump-attack.
    /// High damage, low energy cost, no accuracy roll.
    /// </summary>
    Melee,

    /// <summary>
    /// Mid-range weapons firing physical projectiles.
    /// Affected by accuracy, may have limited ammo in future.
    /// </summary>
    Projectile,

    /// <summary>
    /// Energy-based ranged weapons.
    /// High energy cost, special effects, no ammunition.
    /// </summary>
    Energy,

    /// <summary>
    /// Area-of-effect weapons.
    /// Damage multiple targets, require positioning.
    /// </summary>
    AreaEffect
}

/// <summary>
/// Visual style for projectile animations.
/// </summary>
public enum ProjectileStyle
{
    /// <summary>
    /// No visible projectile (melee, instant hit).
    /// </summary>
    None,

    /// <summary>
    /// Fast-moving single character projectile.
    /// </summary>
    Bullet,

    /// <summary>
    /// Instant line from attacker to target.
    /// </summary>
    Beam,

    /// <summary>
    /// Curved/bouncing projectile path.
    /// </summary>
    Arc,

    /// <summary>
    /// Arcing projectile with gravity (grenades).
    /// </summary>
    Lobbed,

    /// <summary>
    /// Expanding ring from impact point.
    /// </summary>
    Explosion
}

/// <summary>
/// Status effects that weapons can apply.
/// </summary>
public enum WeaponEffect
{
    /// <summary>
    /// No special effect.
    /// </summary>
    None,

    /// <summary>
    /// Target takes damage over time. Can spread.
    /// </summary>
    Burning,

    /// <summary>
    /// Target has reduced accuracy and may skip turns.
    /// </summary>
    Corrupted,

    /// <summary>
    /// Target cannot act for 1-2 turns.
    /// </summary>
    Stunned,

    /// <summary>
    /// Target is pushed back. Wall collision = extra damage.
    /// </summary>
    Knockback,

    /// <summary>
    /// Attack chains to additional nearby targets.
    /// </summary>
    Chain,

    /// <summary>
    /// Can destroy/disable enemy modules.
    /// </summary>
    Sever,

    /// <summary>
    /// Reduces target's armor temporarily.
    /// </summary>
    ArmorBreak
}
