using Godot;

namespace NullAndVoid.Destruction;

/// <summary>
/// Defines the physical properties of a material for destruction and fire simulation.
/// Based on research from CDDA, RimWorld, and Dwarf Fortress material systems.
/// </summary>
public struct MaterialProperties
{
    public string Name;
    public int Hardness;           // 0-100, resistance to physical damage
    public int MaxHitPoints;       // Total HP before destruction
    public float Flammability;     // 0.0-1.0, chance to catch fire
    public int BurnDuration;       // Turns to burn completely (0 = doesn't burn)
    public float FireResistance;   // 0.0-1.0, damage reduction from fire
    public bool ConductsFire;      // Can spread fire through it without burning
    public string DebrisType;      // What it becomes when destroyed
    public string BurnedType;      // What it becomes after burning out

    // Predefined materials

    public static readonly MaterialProperties Wood = new()
    {
        Name = "Wood",
        Hardness = 30,
        MaxHitPoints = 20,
        Flammability = 0.8f,
        BurnDuration = 15,
        FireResistance = 0.0f,
        ConductsFire = true,
        DebrisType = "Rubble",
        BurnedType = "Ash"
    };

    public static readonly MaterialProperties Stone = new()
    {
        Name = "Stone",
        Hardness = 70,
        MaxHitPoints = 50,
        Flammability = 0.0f,
        BurnDuration = 0,
        FireResistance = 1.0f,
        ConductsFire = false,
        DebrisType = "Rubble",
        BurnedType = "Stone" // Doesn't burn
    };

    public static readonly MaterialProperties Metal = new()
    {
        Name = "Metal",
        Hardness = 80,
        MaxHitPoints = 60,
        Flammability = 0.0f,
        BurnDuration = 0,
        FireResistance = 0.8f,
        ConductsFire = true, // Conducts heat
        DebrisType = "Scrap",
        BurnedType = "Metal"
    };

    public static readonly MaterialProperties Concrete = new()
    {
        Name = "Concrete",
        Hardness = 60,
        MaxHitPoints = 40,
        Flammability = 0.0f,
        BurnDuration = 0,
        FireResistance = 0.9f,
        ConductsFire = false,
        DebrisType = "Rubble",
        BurnedType = "Concrete"
    };

    public static readonly MaterialProperties Glass = new()
    {
        Name = "Glass",
        Hardness = 20,
        MaxHitPoints = 10,
        Flammability = 0.0f,
        BurnDuration = 0,
        FireResistance = 0.5f,
        ConductsFire = false,
        DebrisType = "Shards",
        BurnedType = "Glass"
    };

    public static readonly MaterialProperties Vegetation = new()
    {
        Name = "Vegetation",
        Hardness = 10,
        MaxHitPoints = 8,
        Flammability = 0.9f,
        BurnDuration = 8,
        FireResistance = 0.0f,
        ConductsFire = true,
        DebrisType = "None",
        BurnedType = "Ash"
    };

    public static readonly MaterialProperties Grass = new()
    {
        Name = "Grass",
        Hardness = 5,
        MaxHitPoints = 3,
        Flammability = 0.6f,
        BurnDuration = 3,
        FireResistance = 0.0f,
        ConductsFire = true,
        DebrisType = "None",
        BurnedType = "Scorched"
    };

    public static readonly MaterialProperties Tech = new()
    {
        Name = "Tech",
        Hardness = 50,
        MaxHitPoints = 30,
        Flammability = 0.3f,
        BurnDuration = 5,
        FireResistance = 0.4f,
        ConductsFire = false,
        DebrisType = "Scrap",
        BurnedType = "Scrap"
    };

    public static readonly MaterialProperties Flesh = new()
    {
        Name = "Flesh",
        Hardness = 20,
        MaxHitPoints = 0, // Uses entity HP
        Flammability = 0.7f,
        BurnDuration = 10,
        FireResistance = 0.0f,
        ConductsFire = false,
        DebrisType = "Corpse",
        BurnedType = "Ash"
    };

    public static readonly MaterialProperties Water = new()
    {
        Name = "Water",
        Hardness = 0,
        MaxHitPoints = 0,
        Flammability = 0.0f,
        BurnDuration = 0,
        FireResistance = 1.0f,
        ConductsFire = false,
        DebrisType = "None",
        BurnedType = "None"
    };

    /// <summary>
    /// Get material by name.
    /// </summary>
    public static MaterialProperties GetMaterial(string name)
    {
        return name.ToLower() switch
        {
            "wood" => Wood,
            "stone" => Stone,
            "metal" => Metal,
            "concrete" => Concrete,
            "glass" => Glass,
            "vegetation" => Vegetation,
            "grass" => Grass,
            "tech" => Tech,
            "flesh" => Flesh,
            "water" => Water,
            _ => Stone // Default to stone
        };
    }

    /// <summary>
    /// Calculate damage after material resistance.
    /// </summary>
    public readonly int CalculateDamage(int baseDamage, DamageType damageType)
    {
        float multiplier = damageType switch
        {
            DamageType.Physical => 1.0f - (Hardness / 200f), // Hardness reduces physical
            DamageType.Explosive => 1.2f - (Hardness / 250f), // Explosives bypass some hardness
            DamageType.Fire => 1.0f - FireResistance,
            DamageType.Energy => 1.0f - (Hardness / 300f), // Energy cuts through most things
            DamageType.Corrosive => Hardness > 50 ? 0.5f : 1.5f, // Better vs soft materials
            _ => 1.0f
        };

        return Mathf.Max(1, (int)(baseDamage * multiplier));
    }
}
