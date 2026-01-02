using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Attack an adjacent target with a melee attack.
/// Highest priority combat behavior - always attack if adjacent.
/// </summary>
public class MeleeAttackBehavior : IBehavior
{
    public string Name => "MeleeAttack";
    public int Priority { get; set; } = BehaviorPriorities.MeleeAttack;

    /// <summary>
    /// Damage dealt by the attack. If 0, uses the enemy's AttackDamage.
    /// </summary>
    public int Damage { get; set; } = 0;

    public MeleeAttackBehavior() { }

    public MeleeAttackBehavior(int priority)
    {
        Priority = priority;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Need a target
        if (context.Target == null)
            return false;

        // Target must be adjacent (distance 1)
        return context.DistanceToTarget <= 1;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        var target = context.Target!;
        var self = context.Self;

        // Calculate damage
        int damage = Damage > 0 ? Damage : self.AttackDamage;

        // Deal damage
        target.TakeDamage(damage);

        // Emit attack event
        EventBus.Instance.EmitAttackPerformed(self, target, damage);

        GD.Print($"{self.EntityName} attacks {target.EntityName} for {damage} damage!");

        // Note: Animation delay removed - handled by TurnAnimator batching
        await Task.CompletedTask;

        return new BehaviorResult(true, ActionCosts.Attack, $"Attacked {target.EntityName}");
    }
}
