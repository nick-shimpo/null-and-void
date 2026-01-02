namespace NullAndVoid.Destruction;

/// <summary>
/// Represents the destruction state of a tile.
/// Terrain transitions: Intact → Damaged → HeavilyDamaged → Destroyed
/// </summary>
public enum DestructionState
{
    Intact = 0,         // 100% - 67% HP
    Damaged = 1,        // 66% - 34% HP
    HeavilyDamaged = 2, // 33% - 1% HP
    Destroyed = 3       // 0% HP - becomes debris
}

/// <summary>
/// Extension methods for destruction state.
/// </summary>
public static class DestructionStateExtensions
{
    /// <summary>
    /// Get destruction state based on HP percentage.
    /// </summary>
    public static DestructionState FromHPPercent(float percent)
    {
        if (percent <= 0)
            return DestructionState.Destroyed;
        if (percent <= 0.33f)
            return DestructionState.HeavilyDamaged;
        if (percent <= 0.66f)
            return DestructionState.Damaged;
        return DestructionState.Intact;
    }

    /// <summary>
    /// Get color multiplier for damaged state.
    /// </summary>
    public static float GetColorMultiplier(this DestructionState state)
    {
        return state switch
        {
            DestructionState.Intact => 1.0f,
            DestructionState.Damaged => 0.8f,
            DestructionState.HeavilyDamaged => 0.6f,
            DestructionState.Destroyed => 0.4f,
            _ => 1.0f
        };
    }
}
