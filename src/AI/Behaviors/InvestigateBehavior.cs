using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Investigate a position (from noise, ally alert, or last known target position).
/// </summary>
public class InvestigateBehavior : IBehavior
{
    public string Name => "Investigate";
    public int Priority { get; set; } = BehaviorPriorities.InvestigatePosition;

    /// <summary>
    /// Turns to spend investigating before giving up.
    /// </summary>
    public int InvestigateDuration { get; set; } = 5;

    /// <summary>
    /// Whether to investigate ally alerts.
    /// </summary>
    public bool RespondToAlerts { get; set; } = true;

    /// <summary>
    /// Whether to investigate last known target position.
    /// </summary>
    public bool InvestigateLastKnown { get; set; } = true;

    public InvestigateBehavior() { }

    public InvestigateBehavior(int duration, bool respondToAlerts = true)
    {
        InvestigateDuration = duration;
        RespondToAlerts = respondToAlerts;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Don't investigate if we can see the target
        if (context.CanSeeTarget)
            return false;

        // Check for investigation triggers
        if (RespondToAlerts && context.Memory.InvestigatePosition.HasValue)
            return true;

        if (InvestigateLastKnown && context.Memory.LastKnownTargetPos.HasValue)
        {
            // Only investigate if we've lost sight recently
            if (context.Memory.TurnsSinceTargetSeen > 0 &&
                context.Memory.TurnsSinceTargetSeen <= InvestigateDuration)
                return true;
        }

        return false;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        var self = context.Self;
        var memory = context.Memory;

        // Determine investigation target
        Vector2I investigatePos;

        if (memory.InvestigatePosition.HasValue)
        {
            investigatePos = memory.InvestigatePosition.Value;
        }
        else if (memory.LastKnownTargetPos.HasValue)
        {
            investigatePos = memory.LastKnownTargetPos.Value;
        }
        else
        {
            await Task.CompletedTask;
            return new BehaviorResult(false, ActionCosts.Wait, "No position to investigate");
        }

        // Check if we've reached the investigation point
        if (self.GridPosition == investigatePos)
        {
            // Look around (essentially wait)
            memory.InvestigatePosition = null;
            memory.InvestigateTurnsRemaining = 0;

            // Clear last known if we were investigating it
            if (memory.LastKnownTargetPos == investigatePos)
            {
                memory.LastKnownTargetPos = null;
            }

            await Task.CompletedTask;
            return new BehaviorResult(true, ActionCosts.Wait, "Investigating - nothing found");
        }

        // Move toward investigation point
        var nextStep = Pathfinding.GetNextStep(self.GridPosition, investigatePos, context.TileMap);
        if (nextStep.HasValue && context.IsPositionFree(nextStep.Value))
        {
            var direction = nextStep.Value - self.GridPosition;
            self.Move(direction);
            // Note: Animation delay removed - handled by TurnAnimator batching
            return new BehaviorResult(true, ActionCosts.Move, $"Investigating {investigatePos}");
        }

        // Can't reach - give up
        memory.InvestigatePosition = null;
        memory.InvestigateTurnsRemaining = 0;

        await Task.CompletedTask;
        return new BehaviorResult(false, ActionCosts.Wait, "Can't reach investigation point");
    }
}
