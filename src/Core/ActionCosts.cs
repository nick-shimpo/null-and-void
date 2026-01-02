namespace NullAndVoid.Core;

/// <summary>
/// Standard action costs for the energy-based turn system.
/// Base cost is 100 energy units (matches EnergyScheduler.STANDARD_ACTION_COST).
/// </summary>
public static class ActionCosts
{
    // === STANDARD ACTIONS ===
    /// <summary>Standard movement (one tile).</summary>
    public const int Move = 100;

    /// <summary>Standard melee or ranged attack.</summary>
    public const int Attack = 100;

    /// <summary>Wait/skip turn.</summary>
    public const int Wait = 100;

    /// <summary>Pick up an item.</summary>
    public const int PickupItem = 50;

    /// <summary>Drop an item.</summary>
    public const int DropItem = 50;

    /// <summary>Use a consumable item.</summary>
    public const int UseItem = 75;

    /// <summary>Equip an item (always one full turn).</summary>
    public const int EquipItem = 100;

    /// <summary>Unequip a module (quick action).</summary>
    public const int UnequipModule = 50;

    /// <summary>Toggle a module on/off.</summary>
    public const int ToggleModule = 25;

    /// <summary>Free action (no energy cost).</summary>
    public const int Free = 0;

    /// <summary>Open/close a door.</summary>
    public const int UseDoor = 50;

    /// <summary>Use stairs.</summary>
    public const int UseStairs = 100;

    // === PROPULSION MOVEMENT COSTS ===
    // These replace the base Move cost depending on equipped propulsion
    public static class Propulsion
    {
        /// <summary>Flight - fastest but high energy cost.</summary>
        public const int Flight = 50;

        /// <summary>Hover - fast and agile.</summary>
        public const int Hover = 60;

        /// <summary>Legs - balanced.</summary>
        public const int Legs = 80;

        /// <summary>Wheels - standard speed.</summary>
        public const int Wheels = 100;

        /// <summary>Treads - slow but stable.</summary>
        public const int Treads = 120;

        /// <summary>Core movement (no propulsion) - emergency fallback.</summary>
        public const int Core = 150;
    }

    // === ACTION MODIFIERS ===
    /// <summary>Quick action (half turn).</summary>
    public const int QuickAction = 50;

    /// <summary>Slow action (1.5x turn).</summary>
    public const int SlowAction = 150;

    /// <summary>Full action (2x turn).</summary>
    public const int FullAction = 200;

    // === TERRAIN MODIFIERS ===
    public static class Terrain
    {
        public const float Normal = 1.0f;
        public const float Difficult = 1.5f;
        public const float Water = 2.0f;
        public const float Rubble = 1.25f;
    }

    /// <summary>
    /// Calculate movement cost based on propulsion type.
    /// </summary>
    public static int GetMovementCost(PropulsionType propulsion)
    {
        return propulsion switch
        {
            PropulsionType.Flight => Propulsion.Flight,
            PropulsionType.Hover => Propulsion.Hover,
            PropulsionType.Legs => Propulsion.Legs,
            PropulsionType.Wheels => Propulsion.Wheels,
            PropulsionType.Treads => Propulsion.Treads,
            PropulsionType.None => Propulsion.Core,
            _ => Move
        };
    }

    /// <summary>
    /// Calculate movement cost with terrain modifier.
    /// </summary>
    public static int GetMovementCost(PropulsionType propulsion, TerrainType terrain)
    {
        int baseCost = GetMovementCost(propulsion);
        float terrainMod = GetTerrainModifier(terrain, propulsion);
        return (int)(baseCost * terrainMod);
    }

    /// <summary>
    /// Get terrain modifier for a propulsion type.
    /// Some propulsion types ignore certain terrain penalties.
    /// </summary>
    public static float GetTerrainModifier(TerrainType terrain, PropulsionType propulsion)
    {
        // Flying and hovering ignore most terrain
        if (propulsion == PropulsionType.Flight || propulsion == PropulsionType.Hover)
        {
            return terrain == TerrainType.Water ? Terrain.Normal : Terrain.Normal;
        }

        return terrain switch
        {
            TerrainType.Normal => Terrain.Normal,
            TerrainType.Difficult => Terrain.Difficult,
            TerrainType.Water => Terrain.Water,
            TerrainType.Rubble => Terrain.Rubble,
            _ => Terrain.Normal
        };
    }
}

/// <summary>
/// Types of propulsion systems that affect movement speed and cost.
/// </summary>
public enum PropulsionType
{
    /// <summary>No propulsion - uses core movement (slow).</summary>
    None,

    /// <summary>Bipedal or multi-legged locomotion.</summary>
    Legs,

    /// <summary>Wheeled movement.</summary>
    Wheels,

    /// <summary>Tank-style tracked movement.</summary>
    Treads,

    /// <summary>Hovering (anti-gravity or air cushion).</summary>
    Hover,

    /// <summary>Full flight capability.</summary>
    Flight
}

/// <summary>
/// Types of terrain that affect movement cost.
/// </summary>
public enum TerrainType
{
    Normal,
    Difficult,
    Water,
    Rubble
}
