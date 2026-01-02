using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Wait in ambush until a target comes within range, then attack.
/// Used by hidden/stealth enemies.
/// </summary>
public class AmbushBehavior : IBehavior
{
    public string Name => "Ambush";
    public int Priority { get; set; } = BehaviorPriorities.Ambush;

    /// <summary>
    /// Range at which the ambush is triggered.
    /// </summary>
    public int TriggerRange { get; set; } = 3;

    /// <summary>
    /// If true, the enemy must have line of sight to trigger ambush.
    /// </summary>
    public bool RequireLineOfSight { get; set; } = true;

    /// <summary>
    /// If true, sets up ambush position automatically when not alerted.
    /// </summary>
    public bool AutoSetupAmbush { get; set; } = true;

    /// <summary>
    /// Duration to wait in ambush (turns) before giving up.
    /// </summary>
    public int AmbushDuration { get; set; } = 20;

    public AmbushBehavior() { }

    public AmbushBehavior(int triggerRange, bool requireLineOfSight = true)
    {
        TriggerRange = triggerRange;
        RequireLineOfSight = requireLineOfSight;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Don't ambush if already alerted
        if (context.Memory.AlertLevel > 30)
            return false;

        // Auto-setup ambush at current position if not already set
        if (AutoSetupAmbush && !context.Memory.AmbushPosition.HasValue)
        {
            context.Memory.SetupAmbush(context.Self.GridPosition, AmbushDuration);
        }

        // Need an ambush position
        if (!context.Memory.AmbushPosition.HasValue)
            return false;

        // Only ambush from the designated position
        if (context.Self.GridPosition != context.Memory.AmbushPosition.Value)
            return false;

        // Check if ambush has expired
        if (context.Memory.AmbushTurnsRemaining <= 0)
        {
            context.Memory.AmbushPosition = null;
            return false;
        }

        return true;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        var self = context.Self;
        var target = context.Target;

        // Check if target is in trigger range
        bool shouldTrigger = false;

        if (target != null && context.DistanceToTarget <= TriggerRange)
        {
            if (RequireLineOfSight)
            {
                shouldTrigger = context.CanSeeTarget;
            }
            else
            {
                shouldTrigger = true;
            }
        }

        if (shouldTrigger)
        {
            // Ambush triggered! Become fully alert
            context.Memory.AlertLevel = 100;
            context.Memory.AmbushPosition = null;
            context.Memory.AmbushTurnsRemaining = 0;

            GD.Print($"{self.EntityName} springs an ambush!");

            // Return false so other behaviors (like attack) can execute this turn
            await Task.CompletedTask;
            return new BehaviorResult(false, ActionCosts.Free, "Ambush triggered!");
        }

        // Continue waiting in ambush
        await Task.CompletedTask;
        return new BehaviorResult(true, ActionCosts.Wait, "Waiting in ambush");
    }
}
