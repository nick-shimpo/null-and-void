using System.Collections.Generic;
using Godot;
using NullAndVoid.AI;
using NullAndVoid.AI.Behaviors;

namespace NullAndVoid.Entities;

/// <summary>
/// Factory for creating enemy instances with preconfigured behavior sets.
/// Each archetype represents a distinct enemy type with specific AI patterns.
/// </summary>
public static class EnemyFactory
{
    /// <summary>
    /// Create an enemy of the specified archetype at the given position.
    /// </summary>
    public static Enemy CreateEnemy(EnemyArchetype archetype, Vector2I position, string? customName = null)
    {
        return archetype switch
        {
            EnemyArchetype.ScoutDrone => CreateScoutDrone(position, customName),
            EnemyArchetype.PatrolGuard => CreatePatrolGuard(position, customName),
            EnemyArchetype.HeavySentry => CreateHeavySentry(position, customName),
            EnemyArchetype.Hunter => CreateHunter(position, customName),
            EnemyArchetype.SwarmBot => CreateSwarmBot(position, customName),
            EnemyArchetype.Ambusher => CreateAmbusher(position, customName),
            EnemyArchetype.Bomber => CreateBomber(position, customName),
            _ => CreatePatrolGuard(position, customName)
        };
    }

    /// <summary>
    /// Create a random enemy from the available archetypes.
    /// </summary>
    public static Enemy CreateRandomEnemy(Vector2I position)
    {
        var archetypes = new[]
        {
            EnemyArchetype.ScoutDrone,
            EnemyArchetype.PatrolGuard,
            EnemyArchetype.Hunter,
            EnemyArchetype.SwarmBot,
        };

        var archetype = archetypes[GD.RandRange(0, archetypes.Length - 1)];
        return CreateEnemy(archetype, position);
    }

    /// <summary>
    /// Scout Drone - Fast, fragile, alerts others when it sees the player.
    /// Role: Early warning system.
    /// </summary>
    private static Enemy CreateScoutDrone(Vector2I position, string? customName)
    {
        var enemy = new Enemy
        {
            EntityName = customName ?? "Scout Drone",
            GridPosition = position,
            BaseMaxHealth = 15,
            BaseAttackDamage = 3,
            BaseSightRange = 10,
            BaseSpeed = 130,  // Fast
            BaseNoise = 30,   // Quiet
            SkipDefaultBehaviors = true
        };

        // Configure behaviors: Flee when hurt, Attack only if cornered, otherwise flee and alert
        enemy.Behaviors.Clear();
        enemy.Behaviors.AddBehaviors(
            new FleeBehavior(fleeThreshold: 0.5f, fleeDistance: 8),
            new MeleeAttackBehavior { Priority = 30 },  // Only attack if adjacent and can't flee
            new ChaseBehavior(persistence: 2) { Priority = 60 },  // Brief chase
            new PatrolBehavior(PatrolType.Random, patrolRadius: 8),
            new WanderBehavior(moveChance: 0.7f)
        );

        return enemy;
    }

    /// <summary>
    /// Patrol Guard - Standard enemy that patrols and attacks.
    /// Role: Basic threat, backbone of enemy forces.
    /// </summary>
    private static Enemy CreatePatrolGuard(Vector2I position, string? customName)
    {
        var enemy = new Enemy
        {
            EntityName = customName ?? "Patrol Guard",
            GridPosition = position,
            BaseMaxHealth = 25,
            BaseAttackDamage = 8,
            BaseSightRange = 8,
            BaseSpeed = 100,  // Normal
            BaseNoise = 50,
            SkipDefaultBehaviors = true
        };

        // Configure behaviors: Standard attack/chase pattern with patrol
        enemy.Behaviors.Clear();
        enemy.Behaviors.AddBehaviors(
            new MeleeAttackBehavior(),
            new ChaseBehavior(persistence: 6),
            new InvestigateBehavior(duration: 4),
            new PatrolBehavior(PatrolType.Random, patrolRadius: 5),
            new WanderBehavior(moveChance: 0.3f)
        );

        return enemy;
    }

    /// <summary>
    /// Heavy Sentry - Slow, tanky, prefers to hold position.
    /// Role: Area denial, chokepoint defender.
    /// </summary>
    private static Enemy CreateHeavySentry(Vector2I position, string? customName)
    {
        var enemy = new Enemy
        {
            EntityName = customName ?? "Heavy Sentry",
            GridPosition = position,
            BaseMaxHealth = 50,
            BaseAttackDamage = 12,
            BaseSightRange = 6,
            BaseSpeed = 70,  // Slow
            BaseNoise = 80,  // Loud
            SkipDefaultBehaviors = true
        };

        // Set up guard position
        enemy.Memory.GuardPosition = position;

        // Configure behaviors: Guard position, only attack adjacent targets
        enemy.Behaviors.Clear();
        enemy.Behaviors.AddBehaviors(
            new MeleeAttackBehavior(),
            new GuardBehavior(guardRadius: 3, maxChaseDistance: 2),  // Doesn't chase far
            new WanderBehavior(moveChance: 0.1f)  // Rarely moves
        );

        return enemy;
    }

    /// <summary>
    /// Hunter - Actively hunts the player, uses investigation.
    /// Role: Aggressive pursuer.
    /// </summary>
    private static Enemy CreateHunter(Vector2I position, string? customName)
    {
        var enemy = new Enemy
        {
            EntityName = customName ?? "Hunter",
            GridPosition = position,
            BaseMaxHealth = 20,
            BaseAttackDamage = 10,
            BaseSightRange = 10,
            BaseSpeed = 110,  // Quick
            BaseNoise = 40,
            SkipDefaultBehaviors = true
        };

        // Configure behaviors: Aggressive chase, investigates thoroughly
        enemy.Behaviors.Clear();
        enemy.Behaviors.AddBehaviors(
            new FleeBehavior(fleeThreshold: 0.15f, fleeDistance: 5),  // Only flee at very low health
            new MeleeAttackBehavior(),
            new ChaseBehavior(persistence: 10) { UsePathfinding = true },  // Long persistence
            new InvestigateBehavior(duration: 8, respondToAlerts: true),
            new PatrolBehavior(PatrolType.Random, patrolRadius: 10),  // Wide patrol
            new WanderBehavior(moveChance: 0.6f)
        );

        return enemy;
    }

    /// <summary>
    /// Swarm Bot - Weak individually, dangerous in groups.
    /// Role: Overwhelming numbers, distraction.
    /// </summary>
    private static Enemy CreateSwarmBot(Vector2I position, string? customName)
    {
        var enemy = new Enemy
        {
            EntityName = customName ?? "Swarm Bot",
            GridPosition = position,
            BaseMaxHealth = 8,
            BaseAttackDamage = 4,
            BaseSightRange = 6,
            BaseSpeed = 120,  // Fast
            BaseNoise = 20,   // Quiet
            SkipDefaultBehaviors = true
        };

        // Configure behaviors: Aggressive, no flee, always chase
        enemy.Behaviors.Clear();
        enemy.Behaviors.AddBehaviors(
            new MeleeAttackBehavior(),
            new ChaseBehavior(persistence: 8),
            new WanderBehavior(moveChance: 0.8f)  // Active movement
        );

        return enemy;
    }

    /// <summary>
    /// Ambusher - Waits hidden, launches surprise attacks.
    /// Role: Surprise damage, punishes careless exploration.
    /// </summary>
    private static Enemy CreateAmbusher(Vector2I position, string? customName)
    {
        var enemy = new Enemy
        {
            EntityName = customName ?? "Ambusher",
            GridPosition = position,
            BaseMaxHealth = 15,
            BaseAttackDamage = 15,  // High damage surprise attack
            BaseSightRange = 5,
            BaseSpeed = 100,
            BaseNoise = 10,  // Very quiet
            SkipDefaultBehaviors = true
        };

        // Set up ambush
        enemy.Memory.SetupAmbush(position, duration: 30);

        // Configure behaviors: Ambush, then aggressive attack, flee if hurt
        enemy.Behaviors.Clear();
        enemy.Behaviors.AddBehaviors(
            new FleeBehavior(fleeThreshold: 0.3f, fleeDistance: 6),
            new MeleeAttackBehavior(),
            new ChaseBehavior(persistence: 3),  // Brief chase after ambush
            new AmbushBehavior(triggerRange: 2, requireLineOfSight: true),
            new WanderBehavior(moveChance: 0.2f)  // Mostly stationary
        );

        return enemy;
    }

    /// <summary>
    /// Bomber - Explodes when adjacent to target, dealing massive damage.
    /// Role: Suicide threat, area denial.
    /// </summary>
    private static Enemy CreateBomber(Vector2I position, string? customName)
    {
        var enemy = new Enemy
        {
            EntityName = customName ?? "Bomber",
            GridPosition = position,
            BaseMaxHealth = 10,
            BaseAttackDamage = 25,  // Massive explosion damage
            BaseSightRange = 7,
            BaseSpeed = 80,  // Slow
            BaseNoise = 60,
            SkipDefaultBehaviors = true
        };

        // Configure behaviors: No flee, relentlessly chase and "attack" (explode)
        enemy.Behaviors.Clear();
        enemy.Behaviors.AddBehaviors(
            new MeleeAttackBehavior(),  // "Attack" is explosion
            new ChaseBehavior(persistence: 15) { UsePathfinding = true },  // Relentless pursuit
            new WanderBehavior(moveChance: 0.4f)
        );

        return enemy;
    }

    /// <summary>
    /// Get display info for an archetype (for UI/debugging).
    /// </summary>
    public static EnemyArchetypeInfo GetArchetypeInfo(EnemyArchetype archetype)
    {
        return archetype switch
        {
            EnemyArchetype.ScoutDrone => new EnemyArchetypeInfo(
                "Scout Drone", "Fast reconnaissance unit that alerts others",
                Health: 15, Damage: 3, Speed: 130, "d"),
            EnemyArchetype.PatrolGuard => new EnemyArchetypeInfo(
                "Patrol Guard", "Standard patrol unit",
                Health: 25, Damage: 8, Speed: 100, "g"),
            EnemyArchetype.HeavySentry => new EnemyArchetypeInfo(
                "Heavy Sentry", "Slow but tough stationary defender",
                Health: 50, Damage: 12, Speed: 70, "S"),
            EnemyArchetype.Hunter => new EnemyArchetypeInfo(
                "Hunter", "Aggressive pursuer with good tracking",
                Health: 20, Damage: 10, Speed: 110, "h"),
            EnemyArchetype.SwarmBot => new EnemyArchetypeInfo(
                "Swarm Bot", "Weak but fast, dangerous in numbers",
                Health: 8, Damage: 4, Speed: 120, "s"),
            EnemyArchetype.Ambusher => new EnemyArchetypeInfo(
                "Ambusher", "Hidden threat that strikes from hiding",
                Health: 15, Damage: 15, Speed: 100, "a"),
            EnemyArchetype.Bomber => new EnemyArchetypeInfo(
                "Bomber", "Suicidal unit that explodes on contact",
                Health: 10, Damage: 25, Speed: 80, "B"),
            _ => new EnemyArchetypeInfo("Unknown", "Unknown enemy type", 20, 5, 100, "?")
        };
    }
}

/// <summary>
/// Available enemy archetypes.
/// </summary>
public enum EnemyArchetype
{
    ScoutDrone,
    PatrolGuard,
    HeavySentry,
    Hunter,
    SwarmBot,
    Ambusher,
    Bomber
}

/// <summary>
/// Display information about an enemy archetype.
/// </summary>
public record EnemyArchetypeInfo(
    string Name,
    string Description,
    int Health,
    int Damage,
    int Speed,
    string AsciiChar
);
