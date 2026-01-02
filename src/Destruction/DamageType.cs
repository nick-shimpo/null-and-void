namespace NullAndVoid.Destruction;

/// <summary>
/// Types of damage that can affect terrain and entities.
/// Different damage types interact differently with materials.
/// </summary>
public enum DamageType
{
    /// <summary>Physical damage from bullets, melee, impacts.</summary>
    Physical,

    /// <summary>Explosive damage with area effect and knockback.</summary>
    Explosive,

    /// <summary>Fire/heat damage, can ignite flammable materials.</summary>
    Fire,

    /// <summary>Energy damage from lasers, plasma weapons.</summary>
    Energy,

    /// <summary>Corrosive damage from acid, dissolves materials.</summary>
    Corrosive
}
