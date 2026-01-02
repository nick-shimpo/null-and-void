using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Patrol between waypoints or randomly within an area.
/// Default behavior for guards and sentries.
/// </summary>
public class PatrolBehavior : IBehavior
{
    public string Name => "Patrol";
    public int Priority { get; set; } = BehaviorPriorities.Patrol;

    /// <summary>
    /// Type of patrol pattern.
    /// </summary>
    public PatrolType Type { get; set; } = PatrolType.Random;

    /// <summary>
    /// Radius for random patrol (from home position).
    /// </summary>
    public int PatrolRadius { get; set; } = 5;

    /// <summary>
    /// Turns to wait at each waypoint (for waypoint patrol).
    /// </summary>
    public int WaypointWaitTurns { get; set; } = 0;

    private int _waitCounter = 0;

    public PatrolBehavior() { }

    public PatrolBehavior(PatrolType type, int patrolRadius = 5)
    {
        Type = type;
        PatrolRadius = patrolRadius;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Only patrol if no target visible and not alerted
        if (context.CanSeeTarget)
            return false;

        if (context.Memory.AlertLevel > 50)
            return false;

        return true;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        return Type switch
        {
            PatrolType.Waypoint => await ExecuteWaypointPatrol(context),
            PatrolType.Random => await ExecuteRandomPatrol(context),
            PatrolType.ReturnHome => await ExecuteReturnHome(context),
            _ => await ExecuteRandomPatrol(context)
        };
    }

    /// <summary>
    /// Patrol between predefined waypoints.
    /// </summary>
    private async Task<BehaviorResult> ExecuteWaypointPatrol(BehaviorContext context)
    {
        var self = context.Self;
        var memory = context.Memory;

        // Check if we have waypoints
        if (memory.PatrolWaypoints.Count == 0)
        {
            // Fall back to random patrol
            return await ExecuteRandomPatrol(context);
        }

        var currentWaypoint = memory.GetNextWaypoint();
        if (currentWaypoint == null)
        {
            return new BehaviorResult(true, ActionCosts.Wait, "No waypoint");
        }

        // Check if at current waypoint
        if (self.GridPosition == currentWaypoint.Value)
        {
            // Wait at waypoint
            if (_waitCounter < WaypointWaitTurns)
            {
                _waitCounter++;
                await Task.CompletedTask;
                return new BehaviorResult(true, ActionCosts.Wait, "Waiting at waypoint");
            }

            // Move to next waypoint
            _waitCounter = 0;
            memory.AdvanceWaypoint();
            currentWaypoint = memory.GetNextWaypoint();

            if (currentWaypoint == null)
            {
                return new BehaviorResult(true, ActionCosts.Wait, "No next waypoint");
            }
        }

        // Move toward current waypoint
        var nextStep = Pathfinding.GetNextStep(self.GridPosition, currentWaypoint.Value, context.TileMap);
        if (nextStep.HasValue && context.IsPositionFree(nextStep.Value))
        {
            var direction = nextStep.Value - self.GridPosition;
            self.Move(direction);
            // Note: Animation delay removed - handled by TurnAnimator batching
            return new BehaviorResult(true, ActionCosts.Move, "Patrolling to waypoint");
        }

        // Path blocked, try next waypoint
        memory.AdvanceWaypoint();
        await Task.CompletedTask;
        return new BehaviorResult(false, ActionCosts.Wait, "Waypoint path blocked");
    }

    /// <summary>
    /// Random patrol within radius of home position.
    /// </summary>
    private async Task<BehaviorResult> ExecuteRandomPatrol(BehaviorContext context)
    {
        var self = context.Self;
        var memory = context.Memory;

        // Check if we need a new patrol target
        if (!memory.PatrolTarget.HasValue ||
            self.GridPosition == memory.PatrolTarget.Value)
        {
            // Pick a new random point within patrol radius
            memory.PatrolTarget = GetRandomPointNear(memory.HomePosition, PatrolRadius, context);
        }

        if (!memory.PatrolTarget.HasValue)
        {
            await Task.CompletedTask;
            return new BehaviorResult(true, ActionCosts.Wait, "No valid patrol target");
        }

        // Move toward patrol target
        var nextStep = Pathfinding.GetNextStep(self.GridPosition, memory.PatrolTarget.Value, context.TileMap);
        if (nextStep.HasValue && context.IsPositionFree(nextStep.Value))
        {
            var direction = nextStep.Value - self.GridPosition;
            self.Move(direction);
            // Note: Animation delay removed - handled by TurnAnimator batching
            return new BehaviorResult(true, ActionCosts.Move, "Random patrol");
        }

        // Path blocked, pick new target
        memory.PatrolTarget = null;
        await Task.CompletedTask;
        return new BehaviorResult(false, ActionCosts.Wait, "Patrol path blocked");
    }

    /// <summary>
    /// Return to home position.
    /// </summary>
    private async Task<BehaviorResult> ExecuteReturnHome(BehaviorContext context)
    {
        var self = context.Self;
        var memory = context.Memory;

        // Already at home
        if (self.GridPosition == memory.HomePosition)
        {
            await Task.CompletedTask;
            return new BehaviorResult(true, ActionCosts.Wait, "At home position");
        }

        // Move toward home
        var nextStep = Pathfinding.GetNextStep(self.GridPosition, memory.HomePosition, context.TileMap);
        if (nextStep.HasValue && context.IsPositionFree(nextStep.Value))
        {
            var direction = nextStep.Value - self.GridPosition;
            self.Move(direction);
            // Note: Animation delay removed - handled by TurnAnimator batching
            return new BehaviorResult(true, ActionCosts.Move, "Returning home");
        }

        await Task.CompletedTask;
        return new BehaviorResult(false, ActionCosts.Wait, "Can't reach home");
    }

    /// <summary>
    /// Get a random walkable point near a position.
    /// </summary>
    private Vector2I? GetRandomPointNear(Vector2I center, int radius, BehaviorContext context)
    {
        const int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            int dx = GD.RandRange(-radius, radius);
            int dy = GD.RandRange(-radius, radius);
            var target = new Vector2I(center.X + dx, center.Y + dy);

            if (context.TileMap.IsWalkable(target))
            {
                return target;
            }
        }

        return null;
    }
}

/// <summary>
/// Types of patrol patterns.
/// </summary>
public enum PatrolType
{
    /// <summary>
    /// Move between predefined waypoints in order.
    /// </summary>
    Waypoint,

    /// <summary>
    /// Move randomly within patrol radius.
    /// </summary>
    Random,

    /// <summary>
    /// Return to home/spawn position.
    /// </summary>
    ReturnHome
}
