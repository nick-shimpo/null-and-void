using System.Collections.Generic;
using Godot;

namespace NullAndVoid.AI;

/// <summary>
/// Persistent memory for enemy AI between turns.
/// Tracks what the enemy knows/remembers about the world.
/// </summary>
public class EnemyMemory
{
    /// <summary>
    /// Last known position of the primary target (usually player).
    /// Null if target has never been seen.
    /// </summary>
    public Vector2I? LastKnownTargetPos { get; set; }

    /// <summary>
    /// Number of turns since the target was last seen.
    /// Resets to 0 when target is visible.
    /// </summary>
    public int TurnsSinceTargetSeen { get; set; } = int.MaxValue;

    /// <summary>
    /// Current alert level (0-100).
    /// 0 = calm/unaware
    /// 50 = suspicious/investigating
    /// 100 = fully alert/combat mode
    /// </summary>
    public int AlertLevel { get; set; } = 0;

    /// <summary>
    /// Current patrol waypoint index.
    /// </summary>
    public int CurrentWaypointIndex { get; set; } = 0;

    /// <summary>
    /// List of patrol waypoints. Can be set externally or generated.
    /// </summary>
    public List<Vector2I> PatrolWaypoints { get; set; } = new();

    /// <summary>
    /// Current patrol target position (for random patrol).
    /// </summary>
    public Vector2I? PatrolTarget { get; set; }

    /// <summary>
    /// Position this enemy is guarding. Used by Guard behavior.
    /// </summary>
    public Vector2I? GuardPosition { get; set; }

    /// <summary>
    /// Home/spawn position. Enemy may return here when idle.
    /// </summary>
    public Vector2I HomePosition { get; set; }

    /// <summary>
    /// Position where enemy is hiding/ambushing from.
    /// </summary>
    public Vector2I? AmbushPosition { get; set; }

    /// <summary>
    /// Turns remaining in current ambush. Decrements each turn.
    /// </summary>
    public int AmbushTurnsRemaining { get; set; } = 0;

    /// <summary>
    /// Whether this enemy has been alerted by another enemy.
    /// </summary>
    public bool AlertedByAlly { get; set; } = false;

    /// <summary>
    /// Position to investigate (from noise or ally alert).
    /// </summary>
    public Vector2I? InvestigatePosition { get; set; }

    /// <summary>
    /// Turns remaining to investigate a position.
    /// </summary>
    public int InvestigateTurnsRemaining { get; set; } = 0;

    /// <summary>
    /// Turns since this enemy last called for reinforcements.
    /// Used to prevent spam.
    /// </summary>
    public int TurnsSinceCalledReinforcements { get; set; } = int.MaxValue;

    /// <summary>
    /// Cached path to target. May be null or stale.
    /// </summary>
    public List<Vector2I>? CachedPath { get; set; }

    /// <summary>
    /// The destination the cached path leads to.
    /// Used to invalidate cache when destination changes.
    /// </summary>
    public Vector2I? CachedPathDestination { get; set; }

    /// <summary>
    /// Cached LOS result. Null if not cached.
    /// </summary>
    public bool? CachedCanSeeTarget { get; set; }

    /// <summary>
    /// Self position when LOS was last calculated.
    /// </summary>
    public Vector2I CachedLOSSelfPos { get; set; }

    /// <summary>
    /// Target position when LOS was last calculated.
    /// </summary>
    public Vector2I CachedLOSTargetPos { get; set; }

    /// <summary>
    /// Initialize memory with spawn position.
    /// </summary>
    public void Initialize(Vector2I spawnPosition)
    {
        HomePosition = spawnPosition;
        GuardPosition = spawnPosition;
    }

    /// <summary>
    /// Called at the start of each turn to update timers.
    /// </summary>
    public void OnTurnStart()
    {
        // Increment time-based counters (capped to prevent overflow)
        if (TurnsSinceTargetSeen < int.MaxValue - 1)
            TurnsSinceTargetSeen++;

        if (TurnsSinceCalledReinforcements < int.MaxValue - 1)
            TurnsSinceCalledReinforcements++;

        // Decrement investigate timer
        if (InvestigateTurnsRemaining > 0)
        {
            InvestigateTurnsRemaining--;
            if (InvestigateTurnsRemaining <= 0)
            {
                InvestigatePosition = null;
            }
        }

        // Decrement ambush timer
        if (AmbushTurnsRemaining > 0)
        {
            AmbushTurnsRemaining--;
        }

        // Clear ally alert after processing
        AlertedByAlly = false;
    }

    /// <summary>
    /// Alert this enemy about a position (from ally or noise).
    /// </summary>
    public void AlertToPosition(Vector2I position, int investigateTurns = 10)
    {
        AlertedByAlly = true;
        InvestigatePosition = position;
        InvestigateTurnsRemaining = investigateTurns;
        AlertLevel = Mathf.Min(100, AlertLevel + 50);
    }

    /// <summary>
    /// Set the enemy into ambush mode at current position.
    /// </summary>
    public void SetupAmbush(Vector2I position, int duration = 20)
    {
        AmbushPosition = position;
        AmbushTurnsRemaining = duration;
    }

    /// <summary>
    /// Check if the cached path is still valid for the given destination.
    /// </summary>
    public bool IsPathValid(Vector2I destination)
    {
        return CachedPath != null &&
               CachedPath.Count > 0 &&
               CachedPathDestination == destination;
    }

    /// <summary>
    /// Invalidate the cached path.
    /// </summary>
    public void InvalidatePath()
    {
        CachedPath = null;
        CachedPathDestination = null;
    }

    /// <summary>
    /// Invalidate the cached LOS result.
    /// </summary>
    public void InvalidateLOS()
    {
        CachedCanSeeTarget = null;
    }

    /// <summary>
    /// Get the next waypoint for patrol.
    /// Cycles through waypoints in order.
    /// </summary>
    public Vector2I? GetNextWaypoint()
    {
        if (PatrolWaypoints.Count == 0)
            return null;

        var waypoint = PatrolWaypoints[CurrentWaypointIndex];
        return waypoint;
    }

    /// <summary>
    /// Advance to the next waypoint.
    /// </summary>
    public void AdvanceWaypoint()
    {
        if (PatrolWaypoints.Count == 0)
            return;

        CurrentWaypointIndex = (CurrentWaypointIndex + 1) % PatrolWaypoints.Count;
    }

    /// <summary>
    /// Reset memory to initial state.
    /// </summary>
    public void Reset()
    {
        LastKnownTargetPos = null;
        TurnsSinceTargetSeen = int.MaxValue;
        AlertLevel = 0;
        CurrentWaypointIndex = 0;
        PatrolTarget = null;
        AmbushPosition = null;
        AmbushTurnsRemaining = 0;
        AlertedByAlly = false;
        InvestigatePosition = null;
        InvestigateTurnsRemaining = 0;
        TurnsSinceCalledReinforcements = int.MaxValue;
        CachedPath = null;
        CachedPathDestination = null;
        CachedCanSeeTarget = null;
    }
}
