using Godot;
using NullAndVoid.Core;
using NullAndVoid.Entities;

namespace NullAndVoid.Systems;

/// <summary>
/// Handles combat resolution between entities.
/// </summary>
public static class CombatSystem
{
    /// <summary>
    /// Perform a melee attack from attacker to target.
    /// </summary>
    public static int PerformMeleeAttack(Node attacker, Node target, int baseDamage)
    {
        int actualDamage = 0;

        if (target is Enemy enemy)
        {
            actualDamage = enemy.HealthComponent?.TakeDamage(baseDamage, attacker) ?? 0;
            EventBus.Instance.EmitAttackPerformed(attacker, target, actualDamage);

            string attackerName = attacker is Entity e ? e.EntityName : "Unknown";
            GD.Print($"{attackerName} attacks {enemy.EntityName} for {actualDamage} damage!");
        }
        else if (target is Player player)
        {
            player.TakeDamage(baseDamage);
            actualDamage = baseDamage;
            EventBus.Instance.EmitAttackPerformed(attacker, target, actualDamage);

            string attackerName = attacker is Entity e ? e.EntityName : "Unknown";
            GD.Print($"{attackerName} attacks {player.EntityName} for {actualDamage} damage!");
        }

        return actualDamage;
    }

    /// <summary>
    /// Check if an entity is at a specific position.
    /// </summary>
    public static Entity? GetEntityAtPosition(SceneTree tree, Vector2I position)
    {
        // Check for enemies
        var enemies = tree.GetNodesInGroup("Enemies");
        foreach (var node in enemies)
        {
            if (node is Enemy enemy && enemy.GridPosition == position)
                return enemy;
        }

        // Check for player
        var players = tree.GetNodesInGroup("Player");
        foreach (var node in players)
        {
            if (node is Player player && player.GridPosition == position)
                return player;
        }

        return null;
    }

    /// <summary>
    /// Check if there's an enemy at a position.
    /// </summary>
    public static Enemy? GetEnemyAtPosition(SceneTree tree, Vector2I position)
    {
        var enemies = tree.GetNodesInGroup("Enemies");
        foreach (var node in enemies)
        {
            if (node is Enemy enemy && enemy.GridPosition == position && enemy.IsActive)
                return enemy;
        }
        return null;
    }

    /// <summary>
    /// Calculate Manhattan distance between two positions.
    /// </summary>
    public static int ManhattanDistance(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Calculate Chebyshev distance (allows diagonal = 1).
    /// </summary>
    public static int ChebyshevDistance(Vector2I a, Vector2I b)
    {
        return Mathf.Max(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Y - b.Y));
    }
}
