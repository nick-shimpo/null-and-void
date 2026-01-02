using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Flee from a threat when health is low.
/// Emergency survival behavior with high priority.
/// </summary>
public class FleeBehavior : IBehavior
{
    public string Name => "Flee";
    public int Priority { get; set; } = BehaviorPriorities.EmergencyFlee;

    /// <summary>
    /// Health percentage threshold to trigger flee (0.0 to 1.0).
    /// </summary>
    public float FleeThreshold { get; set; } = 0.25f;

    /// <summary>
    /// Minimum distance to flee before considering safe.
    /// </summary>
    public int FleeDistance { get; set; } = 6;

    /// <summary>
    /// If true, only flee when target is visible.
    /// </summary>
    public bool OnlyFleeWhenSeen { get; set; } = false;

    public FleeBehavior() { }

    public FleeBehavior(float fleeThreshold, int fleeDistance = 6)
    {
        FleeThreshold = fleeThreshold;
        FleeDistance = fleeDistance;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Check health threshold
        if (context.GetHealthPercent() > FleeThreshold)
            return false;

        // If only flee when seen, check visibility
        if (OnlyFleeWhenSeen && !context.CanSeeTarget)
            return false;

        // Need a target to flee from
        if (context.Target == null)
            return false;

        // Don't flee if already at safe distance
        if (context.DistanceToTarget >= FleeDistance)
            return false;

        return true;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        var self = context.Self;
        var target = context.Target!;

        // Get flee direction
        var fleeDir = Pathfinding.GetFleeDirection(
            self.GridPosition,
            target.GridPosition,
            context.TileMap
        );

        if (fleeDir == Vector2I.Zero)
        {
            // No valid flee direction - cornered!
            await Task.CompletedTask;
            return new BehaviorResult(false, ActionCosts.Wait, "Cornered - can't flee");
        }

        var newPos = self.GridPosition + fleeDir;
        if (context.IsPositionFree(newPos))
        {
            self.Move(fleeDir);
            // Note: Animation delay removed - handled by TurnAnimator batching
            return new BehaviorResult(true, ActionCosts.Move, "Fleeing");
        }

        // Primary flee direction blocked, try perpendicular
        var perpendiculars = GetPerpendiculars(fleeDir);
        foreach (var perpDir in perpendiculars)
        {
            var perpPos = self.GridPosition + perpDir;
            if (context.IsPositionFree(perpPos))
            {
                // Make sure this perpendicular direction doesn't move closer to threat
                var currentDist = LineOfSight.EuclideanDistance(self.GridPosition, target.GridPosition);
                var newDist = LineOfSight.EuclideanDistance(perpPos, target.GridPosition);

                if (newDist >= currentDist)
                {
                    self.Move(perpDir);
                    // Note: Animation delay removed - handled by TurnAnimator batching
                    return new BehaviorResult(true, ActionCosts.Move, "Fleeing (perpendicular)");
                }
            }
        }

        await Task.CompletedTask;
        return new BehaviorResult(false, ActionCosts.Wait, "Can't find flee route");
    }

    /// <summary>
    /// Get perpendicular directions to a given direction.
    /// </summary>
    private static Vector2I[] GetPerpendiculars(Vector2I dir)
    {
        // For cardinal directions, return the other two cardinals
        if (dir.X == 0 || dir.Y == 0)
        {
            if (dir.X == 0)
                return new[] { new Vector2I(1, 0), new Vector2I(-1, 0) };
            else
                return new[] { new Vector2I(0, 1), new Vector2I(0, -1) };
        }

        // For diagonals, return the two adjacent cardinals
        return new[] { new Vector2I(dir.X, 0), new Vector2I(0, dir.Y) };
    }
}
