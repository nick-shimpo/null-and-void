using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.Systems;
using NullAndVoid.World;

namespace NullAndVoid.AI;

/// <summary>
/// Result of executing a behavior.
/// </summary>
/// <param name="Success">Whether the behavior completed successfully.</param>
/// <param name="ActionCost">Energy cost of the action taken.</param>
/// <param name="ActionTaken">Description of what action was performed.</param>
public record BehaviorResult(bool Success, int ActionCost, string ActionTaken);

/// <summary>
/// Interface for all enemy behaviors.
/// Behaviors are evaluated in priority order - first one that can execute wins.
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Display name for debugging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Priority for evaluation order. Lower values = higher priority.
    /// Suggested ranges:
    /// - 0-19: Emergency/survival behaviors (flee when critical)
    /// - 20-39: Combat behaviors (attack, ranged attack)
    /// - 40-59: Pursuit behaviors (chase, investigate)
    /// - 60-79: Default behaviors (patrol, guard)
    /// - 80-99: Fallback behaviors (wander, idle)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Check if this behavior can execute given the current context.
    /// Should be fast - avoid expensive calculations here.
    /// </summary>
    bool CanExecute(BehaviorContext context);

    /// <summary>
    /// Execute the behavior and return the result.
    /// Only called if CanExecute returned true.
    /// </summary>
    Task<BehaviorResult> Execute(BehaviorContext context);
}

/// <summary>
/// Context passed to behaviors for decision making.
/// Contains all information a behavior needs about the current situation.
/// </summary>
public class BehaviorContext
{
    /// <summary>
    /// The enemy executing the behavior.
    /// </summary>
    public Enemy Self { get; }

    /// <summary>
    /// The primary target (usually the player). May be null if not detected.
    /// </summary>
    public Player? Target { get; set; }

    /// <summary>
    /// Reference to the enemy's persistent memory.
    /// </summary>
    public EnemyMemory Memory { get; }

    /// <summary>
    /// The tile map for pathfinding and collision checks.
    /// </summary>
    public TileMapManager TileMap { get; }

    /// <summary>
    /// FOV system for visibility checks.
    /// </summary>
    public FOVSystem? FOV { get; set; }

    /// <summary>
    /// Whether the target is currently visible to this enemy.
    /// </summary>
    public bool CanSeeTarget { get; set; }

    /// <summary>
    /// Distance to the target in tiles (Manhattan distance).
    /// </summary>
    public int DistanceToTarget { get; set; }

    /// <summary>
    /// The scene tree for finding other entities.
    /// </summary>
    public SceneTree SceneTree { get; }

    public BehaviorContext(Enemy self, EnemyMemory memory, TileMapManager tileMap, SceneTree sceneTree)
    {
        Self = self;
        Memory = memory;
        TileMap = tileMap;
        SceneTree = sceneTree;
    }

    /// <summary>
    /// Update context with current target information.
    /// Called at the start of each turn before behavior evaluation.
    /// </summary>
    public void UpdateTargetInfo(Player? player)
    {
        Target = player;

        if (player == null)
        {
            CanSeeTarget = false;
            DistanceToTarget = int.MaxValue;
            return;
        }

        // Calculate distance
        DistanceToTarget = Mathf.Abs(Self.GridPosition.X - player.GridPosition.X) +
                          Mathf.Abs(Self.GridPosition.Y - player.GridPosition.Y);

        // Check visibility - need line of sight AND within sight range
        // Use cached LOS result if positions haven't changed
        if (DistanceToTarget <= Self.SightRange)
        {
            if (Memory.CachedCanSeeTarget.HasValue &&
                Memory.CachedLOSSelfPos == Self.GridPosition &&
                Memory.CachedLOSTargetPos == player.GridPosition)
            {
                // Positions unchanged, use cached result
                CanSeeTarget = Memory.CachedCanSeeTarget.Value;
            }
            else
            {
                // Positions changed, recalculate and cache
                CanSeeTarget = LineOfSight.HasClearPath(Self.GridPosition, player.GridPosition, TileMap);
                Memory.CachedCanSeeTarget = CanSeeTarget;
                Memory.CachedLOSSelfPos = Self.GridPosition;
                Memory.CachedLOSTargetPos = player.GridPosition;
            }
        }
        else
        {
            CanSeeTarget = false;
            Memory.CachedCanSeeTarget = false;
        }

        // Update memory based on visibility
        if (CanSeeTarget)
        {
            Memory.LastKnownTargetPos = player.GridPosition;
            Memory.TurnsSinceTargetSeen = 0;
            Memory.AlertLevel = Mathf.Min(100, Memory.AlertLevel + 30);
        }
        else
        {
            Memory.TurnsSinceTargetSeen++;
            // Decay alert level over time
            Memory.AlertLevel = Mathf.Max(0, Memory.AlertLevel - 5);
        }
    }

    /// <summary>
    /// Get the health percentage of the enemy (0.0 to 1.0).
    /// </summary>
    public float GetHealthPercent()
    {
        if (Self.MaxHealth <= 0)
            return 1.0f;
        return (float)Self.CurrentHealth / Self.MaxHealth;
    }

    /// <summary>
    /// Check if a position is walkable and not occupied by another entity.
    /// Uses O(1) EntityGrid lookup instead of O(N) group iteration.
    /// </summary>
    public bool IsPositionFree(Vector2I position)
    {
        // Check tile walkability
        if (!TileMap.IsWalkable(position))
            return false;

        // O(1) check if position is occupied by any entity other than self
        return !EntityGrid.Instance.IsOccupiedByOther(position, Self);
    }

    /// <summary>
    /// Get direction vector from self to a target position.
    /// Returns normalized step direction (-1, 0, or 1 for each axis).
    /// </summary>
    public Vector2I GetDirectionTo(Vector2I target)
    {
        int dx = target.X - Self.GridPosition.X;
        int dy = target.Y - Self.GridPosition.Y;

        return new Vector2I(
            dx != 0 ? (dx > 0 ? 1 : -1) : 0,
            dy != 0 ? (dy > 0 ? 1 : -1) : 0
        );
    }

    /// <summary>
    /// Get direction vector away from a threat position.
    /// </summary>
    public Vector2I GetDirectionAwayFrom(Vector2I threat)
    {
        var towardsThreat = GetDirectionTo(threat);
        return new Vector2I(-towardsThreat.X, -towardsThreat.Y);
    }
}
