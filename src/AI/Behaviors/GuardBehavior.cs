using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Guard a position and attack any target that enters the guarded area.
/// </summary>
public class GuardBehavior : IBehavior
{
    public string Name => "Guard";
    public int Priority { get; set; } = BehaviorPriorities.Guard;

    /// <summary>
    /// Radius around guard position to watch.
    /// </summary>
    public int GuardRadius { get; set; } = 4;

    /// <summary>
    /// Maximum distance to chase an intruder.
    /// </summary>
    public int MaxChaseDistance { get; set; } = 6;

    /// <summary>
    /// Whether to return to guard position when no threats.
    /// </summary>
    public bool ReturnToPost { get; set; } = true;

    public GuardBehavior() { }

    public GuardBehavior(int guardRadius, int maxChaseDistance = 6)
    {
        GuardRadius = guardRadius;
        MaxChaseDistance = maxChaseDistance;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Need a guard position set
        if (!context.Memory.GuardPosition.HasValue)
            return false;

        // This behavior handles both guarding and returning to post
        return true;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        var self = context.Self;
        var guardPos = context.Memory.GuardPosition!.Value;

        // Calculate distance from guard position
        int distFromPost = LineOfSight.ManhattanDistance(self.GridPosition, guardPos);

        // If target is visible and within chase range, we don't guard - let chase behavior handle it
        if (context.CanSeeTarget && context.Target != null)
        {
            // Check if target is within guard radius (intruder detected)
            var targetDistFromPost = LineOfSight.ManhattanDistance(context.Target.GridPosition, guardPos);

            if (targetDistFromPost <= GuardRadius || distFromPost <= MaxChaseDistance)
            {
                // Let other behaviors (chase/attack) handle the intruder
                await Task.CompletedTask;
                return new BehaviorResult(false, ActionCosts.Wait, "Intruder detected");
            }
        }

        // No threat - return to guard position if needed
        if (ReturnToPost && distFromPost > 0)
        {
            var nextStep = Pathfinding.GetNextStep(self.GridPosition, guardPos, context.TileMap);
            if (nextStep.HasValue && context.IsPositionFree(nextStep.Value))
            {
                var direction = nextStep.Value - self.GridPosition;
                self.Move(direction);
                await Task.Delay(50);
                return new BehaviorResult(true, ActionCosts.Move, "Returning to guard post");
            }
        }

        // At guard position or can't return - stand guard
        await Task.CompletedTask;
        return new BehaviorResult(true, ActionCosts.Wait, "Guarding");
    }
}
