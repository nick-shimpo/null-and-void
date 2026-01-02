using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using NullAndVoid.Entities;

namespace NullAndVoid.Effects;

/// <summary>
/// Batches enemy action animations to avoid blocking computation.
/// Actions are queued during enemy turn processing, then played with staggered timing.
/// </summary>
public class TurnAnimator
{
    private static TurnAnimator? _instance;
    public static TurnAnimator Instance => _instance ??= new TurnAnimator();

    private readonly Queue<EnemyAction> _pendingActions = new();

    /// <summary>
    /// Delay between individual action animations in milliseconds.
    /// </summary>
    public int StaggerDelayMs { get; set; } = 15;

    /// <summary>
    /// Whether to skip animations entirely for maximum performance.
    /// </summary>
    public bool SkipAnimations { get; set; } = false;

    /// <summary>
    /// Queue an action for batched animation playback.
    /// </summary>
    public void QueueAction(Enemy actor, EnemyActionType type, Vector2I? targetPosition = null, Entity? targetEntity = null, int damage = 0)
    {
        _pendingActions.Enqueue(new EnemyAction(actor, type, targetPosition, targetEntity, damage));
    }

    /// <summary>
    /// Queue a movement action.
    /// </summary>
    public void QueueMove(Enemy actor, Vector2I from, Vector2I to)
    {
        QueueAction(actor, EnemyActionType.Move, to);
    }

    /// <summary>
    /// Queue a melee attack action.
    /// </summary>
    public void QueueMeleeAttack(Enemy actor, Entity target, int damage)
    {
        QueueAction(actor, EnemyActionType.MeleeAttack, target.GridPosition, target, damage);
    }

    /// <summary>
    /// Queue a ranged attack action.
    /// </summary>
    public void QueueRangedAttack(Enemy actor, Entity target, int damage)
    {
        QueueAction(actor, EnemyActionType.RangedAttack, target.GridPosition, target, damage);
    }

    /// <summary>
    /// Play all queued animations with staggered timing.
    /// Called after all enemy computations are complete.
    /// </summary>
    public async Task PlayAnimations()
    {
        if (SkipAnimations)
        {
            _pendingActions.Clear();
            return;
        }

        while (_pendingActions.Count > 0)
        {
            var action = _pendingActions.Dequeue();

            // Trigger visual effect based on action type
            PlayActionEffect(action);

            // Stagger animations if more remain
            if (_pendingActions.Count > 0 && StaggerDelayMs > 0)
            {
                await Task.Delay(StaggerDelayMs);
            }
        }
    }

    /// <summary>
    /// Play visual effect for a single action.
    /// </summary>
    private void PlayActionEffect(EnemyAction action)
    {
        switch (action.Type)
        {
            case EnemyActionType.Move:
                // Movement is already visually handled by entity position updates
                // Could add footstep particles here if desired
                break;

            case EnemyActionType.MeleeAttack:
                // Could trigger attack animation/flash on actor
                // Could trigger damage flash on target
                if (action.TargetEntity != null)
                {
                    // Visual feedback is handled by combat system signals
                }
                break;

            case EnemyActionType.RangedAttack:
                // Could spawn projectile particle
                break;

            case EnemyActionType.Wait:
                // No visual effect needed
                break;
        }
    }

    /// <summary>
    /// Get count of pending animations (for debugging).
    /// </summary>
    public int PendingCount => _pendingActions.Count;

    /// <summary>
    /// Clear all pending animations without playing them.
    /// </summary>
    public void Clear()
    {
        _pendingActions.Clear();
    }
}

/// <summary>
/// Represents a queued enemy action for animation.
/// </summary>
public record EnemyAction(
    Enemy Actor,
    EnemyActionType Type,
    Vector2I? TargetPosition,
    Entity? TargetEntity,
    int Damage
);

/// <summary>
/// Types of enemy actions that can be animated.
/// </summary>
public enum EnemyActionType
{
    Move,
    MeleeAttack,
    RangedAttack,
    Wait
}
