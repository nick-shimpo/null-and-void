using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Chase a visible target or move toward last known position.
/// Uses simple greedy movement, optionally with pathfinding.
/// </summary>
public class ChaseBehavior : IBehavior
{
    public string Name => "Chase";
    public int Priority { get; set; } = BehaviorPriorities.ChaseTarget;

    /// <summary>
    /// How many turns to continue chasing after losing sight of target.
    /// </summary>
    public int Persistence { get; set; } = 5;

    /// <summary>
    /// Whether to use A* pathfinding (when available).
    /// If false, uses greedy movement toward target.
    /// </summary>
    public bool UsePathfinding { get; set; } = true;

    public ChaseBehavior() { }

    public ChaseBehavior(int persistence, bool usePathfinding = true)
    {
        Persistence = persistence;
        UsePathfinding = usePathfinding;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Can chase if we can see the target
        if (context.CanSeeTarget && context.Target != null)
            return true;

        // Or if we recently saw them and have a last known position
        if (context.Memory.LastKnownTargetPos.HasValue &&
            context.Memory.TurnsSinceTargetSeen <= Persistence)
            return true;

        return false;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        var self = context.Self;
        Vector2I targetPos;

        // Determine target position
        if (context.CanSeeTarget && context.Target != null)
        {
            targetPos = context.Target.GridPosition;
        }
        else if (context.Memory.LastKnownTargetPos.HasValue)
        {
            targetPos = context.Memory.LastKnownTargetPos.Value;

            // If we've reached the last known position, clear it
            if (self.GridPosition == targetPos)
            {
                context.Memory.LastKnownTargetPos = null;
                return new BehaviorResult(false, ActionCosts.Wait, "Lost target");
            }
        }
        else
        {
            return new BehaviorResult(false, ActionCosts.Wait, "No target to chase");
        }

        // Try to move toward target
        bool moved = false;
        var memory = context.Memory;

        if (UsePathfinding)
        {
            // Check if we have a valid cached path to this destination
            if (!memory.IsPathValid(targetPos))
            {
                // Recalculate and cache the full path
                memory.CachedPath = Pathfinding.FindPath(self.GridPosition, targetPos, context.TileMap);
                memory.CachedPathDestination = targetPos;
            }

            // Use cached path if available
            if (memory.CachedPath != null && memory.CachedPath.Count > 1)
            {
                var nextStep = memory.CachedPath[1]; // [0] is current position

                if (context.IsPositionFree(nextStep))
                {
                    var direction = nextStep - self.GridPosition;
                    self.Move(direction);
                    memory.CachedPath.RemoveAt(0); // Consume the step we just took
                    moved = true;
                }
                else
                {
                    // Path blocked - invalidate and recalculate next turn
                    memory.InvalidatePath();
                }
            }
        }

        // Fallback to greedy movement if pathfinding failed or disabled
        if (!moved)
        {
            moved = TryGreedyMove(context, targetPos);
        }

        // Note: Animation delay removed - handled by TurnAnimator batching
        await Task.CompletedTask;

        if (moved)
        {
            return new BehaviorResult(true, ActionCosts.Move, $"Chasing toward {targetPos}");
        }
        else
        {
            return new BehaviorResult(false, ActionCosts.Wait, "Path blocked");
        }
    }

    /// <summary>
    /// Try to move toward target using greedy pathfinding.
    /// Tries direct diagonal first, then cardinals.
    /// </summary>
    private bool TryGreedyMove(BehaviorContext context, Vector2I targetPos)
    {
        var self = context.Self;
        var direction = context.GetDirectionTo(targetPos);

        // Try direct diagonal/cardinal movement
        var newPos = self.GridPosition + direction;
        if (context.IsPositionFree(newPos))
        {
            self.Move(direction);
            return true;
        }

        // If diagonal was blocked, try cardinals separately
        if (direction.X != 0 && direction.Y != 0)
        {
            // Try horizontal
            var horizPos = self.GridPosition + new Vector2I(direction.X, 0);
            if (context.IsPositionFree(horizPos))
            {
                self.Move(new Vector2I(direction.X, 0));
                return true;
            }

            // Try vertical
            var vertPos = self.GridPosition + new Vector2I(0, direction.Y);
            if (context.IsPositionFree(vertPos))
            {
                self.Move(new Vector2I(0, direction.Y));
                return true;
            }
        }

        return false;
    }
}
