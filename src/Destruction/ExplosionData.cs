using Godot;

namespace NullAndVoid.Destruction;

/// <summary>
/// Data defining an explosion's properties.
/// Based on research from roguelike AoE systems.
/// </summary>
public struct ExplosionData
{
    public int BaseDamage;        // Damage at epicenter
    public int Radius;            // Tiles from center
    public float FalloffCurve;    // 1.0 = linear, 2.0 = quadratic
    public int KnockbackForce;    // Tiles pushed (0 = none)
    public bool CausesFire;       // Ignites flammable tiles
    public float TerrainDamage;   // Multiplier for terrain damage
    public Color FlashColor;      // Visual flash color
    public string Name;

    // Predefined explosion types

    public static readonly ExplosionData Grenade = new()
    {
        Name = "Grenade",
        BaseDamage = 20,
        Radius = 3,
        FalloffCurve = 1.0f,
        KnockbackForce = 1,
        CausesFire = false,
        TerrainDamage = 0.8f,
        FlashColor = Color.Color8(255, 200, 100)
    };

    public static readonly ExplosionData Incendiary = new()
    {
        Name = "Incendiary",
        BaseDamage = 10,
        Radius = 4,
        FalloffCurve = 0.8f,
        KnockbackForce = 0,
        CausesFire = true,
        TerrainDamage = 0.5f,
        FlashColor = Color.Color8(255, 150, 50)
    };

    public static readonly ExplosionData Plasma = new()
    {
        Name = "Plasma",
        BaseDamage = 30,
        Radius = 2,
        FalloffCurve = 1.5f,
        KnockbackForce = 0,
        CausesFire = true,
        TerrainDamage = 1.2f,
        FlashColor = Color.Color8(100, 200, 255)
    };

    public static readonly ExplosionData Missile = new()
    {
        Name = "Missile",
        BaseDamage = 40,
        Radius = 5,
        FalloffCurve = 1.2f,
        KnockbackForce = 2,
        CausesFire = true,
        TerrainDamage = 1.5f,
        FlashColor = Color.Color8(255, 255, 200)
    };

    public static readonly ExplosionData EMP = new()
    {
        Name = "EMP",
        BaseDamage = 0,
        Radius = 6,
        FalloffCurve = 0.5f,
        KnockbackForce = 0,
        CausesFire = false,
        TerrainDamage = 0.2f, // Only affects tech
        FlashColor = Color.Color8(150, 200, 255)
    };

    public static readonly ExplosionData FuelExplosion = new()
    {
        Name = "Fuel Explosion",
        BaseDamage = 25,
        Radius = 4,
        FalloffCurve = 1.0f,
        KnockbackForce = 2,
        CausesFire = true,
        TerrainDamage = 1.0f,
        FlashColor = Color.Color8(255, 180, 50)
    };

    public static readonly ExplosionData SmallBlast = new()
    {
        Name = "Small Blast",
        BaseDamage = 12,
        Radius = 2,
        FalloffCurve = 1.0f,
        KnockbackForce = 1,
        CausesFire = false,
        TerrainDamage = 0.6f,
        FlashColor = Color.Color8(255, 220, 150)
    };

    /// <summary>
    /// Calculate damage at a given distance from epicenter.
    /// </summary>
    public readonly int CalculateDamageAtDistance(float distance)
    {
        if (distance >= Radius)
            return 0;

        float normalizedDist = distance / Radius;
        float falloff = 1.0f - Mathf.Pow(normalizedDist, FalloffCurve);

        return Mathf.Max(1, (int)(BaseDamage * falloff));
    }

    /// <summary>
    /// Calculate knockback tiles at a given distance.
    /// </summary>
    public readonly int CalculateKnockbackAtDistance(float distance)
    {
        if (KnockbackForce == 0 || distance >= Radius)
            return 0;

        float normalizedDist = distance / Radius;
        float force = KnockbackForce * (1.0f - normalizedDist);

        return Mathf.Max(0, (int)force);
    }
}
