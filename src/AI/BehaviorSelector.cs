using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI;

/// <summary>
/// Evaluates behaviors in priority order and executes the first one that can run.
/// This implements a simplified behavior tree pattern where behaviors are
/// evaluated top-to-bottom by priority.
/// </summary>
public class BehaviorSelector
{
    private readonly List<IBehavior> _behaviors = new();
    private bool _sorted = false;

    /// <summary>
    /// Name of this selector for debugging.
    /// </summary>
    public string Name { get; set; } = "BehaviorSelector";

    /// <summary>
    /// Enable debug logging.
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// The last behavior that was executed.
    /// </summary>
    public IBehavior? LastExecutedBehavior { get; private set; }

    /// <summary>
    /// Add a behavior to the selector.
    /// </summary>
    public void AddBehavior(IBehavior behavior)
    {
        _behaviors.Add(behavior);
        _sorted = false;
    }

    /// <summary>
    /// Add multiple behaviors to the selector.
    /// </summary>
    public void AddBehaviors(params IBehavior[] behaviors)
    {
        foreach (var behavior in behaviors)
        {
            _behaviors.Add(behavior);
        }
        _sorted = false;
    }

    /// <summary>
    /// Remove a behavior from the selector.
    /// </summary>
    public bool RemoveBehavior(IBehavior behavior)
    {
        return _behaviors.Remove(behavior);
    }

    /// <summary>
    /// Remove a behavior by name.
    /// </summary>
    public bool RemoveBehavior(string name)
    {
        var behavior = _behaviors.Find(b => b.Name == name);
        if (behavior != null)
        {
            return _behaviors.Remove(behavior);
        }
        return false;
    }

    /// <summary>
    /// Clear all behaviors.
    /// </summary>
    public void Clear()
    {
        _behaviors.Clear();
        _sorted = true;
    }

    /// <summary>
    /// Get a behavior by name.
    /// </summary>
    public IBehavior? GetBehavior(string name)
    {
        return _behaviors.Find(b => b.Name == name);
    }

    /// <summary>
    /// Check if a behavior exists by name.
    /// </summary>
    public bool HasBehavior(string name)
    {
        return _behaviors.Any(b => b.Name == name);
    }

    /// <summary>
    /// Get all behaviors, sorted by priority.
    /// </summary>
    public IReadOnlyList<IBehavior> GetBehaviors()
    {
        EnsureSorted();
        return _behaviors.AsReadOnly();
    }

    /// <summary>
    /// Evaluate behaviors and execute the first one that can run.
    /// Returns the result of the executed behavior, or a default "no action" result.
    /// </summary>
    public async Task<BehaviorResult> Evaluate(BehaviorContext context)
    {
        EnsureSorted();

        // Early termination: distant, unaware enemies just idle
        // Saves expensive behavior evaluation for enemies that won't act anyway
        // BUT don't terminate if enemy has a known target position (e.g., from being hit)
        if (context.DistanceToTarget > 15 &&
            context.Memory.AlertLevel < 5 &&
            !context.Memory.LastKnownTargetPos.HasValue)
        {
            LastExecutedBehavior = null;
            return new BehaviorResult(false, ActionCosts.Wait, "Idle (distant)");
        }

        if (DebugMode)
        {
            GD.Print($"[{Name}] Evaluating {_behaviors.Count} behaviors for {context.Self.EntityName}");
        }

        foreach (var behavior in _behaviors)
        {
            if (behavior.CanExecute(context))
            {
                if (DebugMode)
                {
                    GD.Print($"[{Name}] Executing behavior: {behavior.Name} (priority {behavior.Priority})");
                }

                LastExecutedBehavior = behavior;
                var result = await behavior.Execute(context);

                if (DebugMode)
                {
                    GD.Print($"[{Name}] Behavior {behavior.Name} completed: {result.ActionTaken} (success: {result.Success})");
                }

                return result;
            }
            else if (DebugMode)
            {
                GD.Print($"[{Name}] Behavior {behavior.Name} cannot execute");
            }
        }

        // No behavior could execute - return idle result
        LastExecutedBehavior = null;

        if (DebugMode)
        {
            GD.Print($"[{Name}] No behavior executed - idle");
        }

        return new BehaviorResult(false, ActionCosts.Wait, "Idle");
    }

    /// <summary>
    /// Get the first behavior that can execute without actually executing it.
    /// Useful for previewing AI decisions.
    /// </summary>
    public IBehavior? GetExecutableBehavior(BehaviorContext context)
    {
        EnsureSorted();

        foreach (var behavior in _behaviors)
        {
            if (behavior.CanExecute(context))
            {
                return behavior;
            }
        }

        return null;
    }

    /// <summary>
    /// Get a debug string showing all behaviors and their priorities.
    /// </summary>
    public string GetDebugInfo()
    {
        EnsureSorted();

        var lines = new List<string>
        {
            $"=== {Name} ===",
            $"Behaviors: {_behaviors.Count}",
            $"Last executed: {LastExecutedBehavior?.Name ?? "None"}"
        };

        foreach (var behavior in _behaviors)
        {
            lines.Add($"  [{behavior.Priority:D2}] {behavior.Name}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Ensure behaviors are sorted by priority.
    /// </summary>
    private void EnsureSorted()
    {
        if (!_sorted)
        {
            _behaviors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _sorted = true;
        }
    }
}

/// <summary>
/// Standard behavior priorities.
/// Lower values = higher priority (evaluated first).
/// </summary>
public static class BehaviorPriorities
{
    // Emergency behaviors (0-19)
    public const int EmergencyFlee = 5;
    public const int SelfDestruct = 10;

    // Combat behaviors (20-39)
    public const int MeleeAttack = 20;
    public const int RangedAttack = 25;
    public const int ChargeAttack = 28;

    // Pursuit behaviors (40-59)
    public const int ChaseTarget = 40;
    public const int InvestigateNoise = 45;
    public const int InvestigatePosition = 48;

    // Tactical behaviors (60-79)
    public const int MaintainDistance = 60;
    public const int Flee = 65;
    public const int CallReinforcements = 70;

    // Default behaviors (80-99)
    public const int Patrol = 80;
    public const int Guard = 85;
    public const int Wander = 90;
    public const int Ambush = 92;
    public const int Idle = 99;
}
