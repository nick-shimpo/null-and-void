namespace NullAndVoid.Combat;

/// <summary>
/// Interface for weapon statistics used in damage calculations.
/// Allows for testable implementations that don't depend on Godot.
/// </summary>
public interface IWeaponStats
{
    int MinDamage { get; }
    int MaxDamage { get; }
    int CriticalChance { get; }
    float CriticalMultiplier { get; }
    DamageType DamageType { get; }
    WeaponEffect PrimaryEffect { get; }
    int EffectChance { get; }
    int EffectDuration { get; }
}

/// <summary>
/// Pure data struct for weapon statistics, usable in unit tests.
/// </summary>
public struct WeaponStats : IWeaponStats
{
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public int CriticalChance { get; set; }
    public float CriticalMultiplier { get; set; }
    public DamageType DamageType { get; set; }
    public WeaponEffect PrimaryEffect { get; set; }
    public int EffectChance { get; set; }
    public int EffectDuration { get; set; }

    public static WeaponStats Default => new()
    {
        MinDamage = 5,
        MaxDamage = 10,
        CriticalChance = 5,
        CriticalMultiplier = 1.5f,
        DamageType = DamageType.Kinetic,
        PrimaryEffect = WeaponEffect.None,
        EffectChance = 0,
        EffectDuration = 0
    };
}
