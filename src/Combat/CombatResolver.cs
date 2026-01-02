using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Components;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.Targeting;

namespace NullAndVoid.Combat;

/// <summary>
/// Result of a single attack against a target.
/// </summary>
public class AttackResult
{
    public Node Attacker { get; set; } = null!;
    public Node? Target { get; set; }
    public Item Weapon { get; set; } = null!;
    public Vector2I TargetPosition { get; set; }
    public bool Hit { get; set; }
    public int Accuracy { get; set; }
    public DamageResult Damage { get; set; }
    public int ActionCost { get; set; }
    public int EnergyCost { get; set; }
    public string Message { get; set; } = "";

    /// <summary>
    /// Whether the target was killed by this attack.
    /// </summary>
    public bool TargetKilled { get; set; }

    /// <summary>
    /// Module destroyed on target (if any).
    /// </summary>
    public string? DestroyedModule { get; set; }
}

/// <summary>
/// Result of a full attack action (may hit multiple targets for AoE).
/// </summary>
public class CombatResult
{
    public Node Attacker { get; set; } = null!;
    public Item Weapon { get; set; } = null!;
    public Vector2I PrimaryTarget { get; set; }
    public List<AttackResult> Results { get; set; } = new();
    public int TotalDamage { get; set; }
    public int TargetsHit { get; set; }
    public int TargetsKilled { get; set; }
    public int ActionCost { get; set; }
    public int EnergyCost { get; set; }
    public bool OutOfAmmo { get; set; }
    public bool Success => !OutOfAmmo && (TargetsHit > 0 || (Weapon.WeaponData?.AreaRadius > 0));

    public string GetSummary()
    {
        if (OutOfAmmo)
            return "Out of ammunition!";

        if (Results.Count == 0)
            return "No targets in range";

        if (Results.Count == 1)
        {
            var r = Results[0];
            if (!r.Hit)
                return $"Missed {r.Target?.Name ?? "target"}";
            return $"Hit {r.Target?.Name ?? "target"} for {r.Damage.FinalDamage} damage" +
                   (r.TargetKilled ? " (destroyed)" : "");
        }

        return $"Hit {TargetsHit} targets for {TotalDamage} total damage" +
               (TargetsKilled > 0 ? $" ({TargetsKilled} destroyed)" : "");
    }
}

/// <summary>
/// Resolves combat attacks including hit/miss, damage, and effects.
/// </summary>
public static class CombatResolver
{
    private static readonly Random _random = new();

    /// <summary>
    /// Resolve a ranged attack from targeting info.
    /// </summary>
    public static CombatResult ResolveRangedAttack(
        Node attacker,
        AttackInfo attackInfo,
        SceneTree sceneTree)
    {
        var weapon = attackInfo.Weapon;
        var weaponData = weapon.WeaponData!;

        var result = new CombatResult
        {
            Attacker = attacker,
            Weapon = weapon,
            PrimaryTarget = attackInfo.TargetPosition,
            ActionCost = weaponData.ActionCost,
            EnergyCost = weaponData.EnergyCost
        };

        // Check and consume ammo if weapon requires it
        if (weaponData.UsesAmmo)
        {
            var inventory = GetAttackerInventory(attacker);
            if (inventory == null || !inventory.HasAmmo(weaponData.RequiredAmmoType!.Value, weaponData.AmmoPerShot))
            {
                result.OutOfAmmo = true;
                return result;
            }

            // Consume ammo
            inventory.ConsumeAmmo(weaponData.RequiredAmmoType!.Value, weaponData.AmmoPerShot);
        }

        // Handle AoE weapons
        if (weaponData.AreaRadius > 0)
        {
            result.Results = ResolveAoEAttack(attacker, weapon, attackInfo, sceneTree);
        }
        else
        {
            // Single target attack
            if (attackInfo.Target != null)
            {
                var attackResult = ResolveSingleAttack(
                    attacker,
                    attackInfo.Target.Entity,
                    weapon,
                    attackInfo.LineOfFire,
                    attackInfo.Accuracy);
                result.Results.Add(attackResult);
            }
        }

        // Calculate totals
        foreach (var r in result.Results)
        {
            if (r.Hit)
            {
                result.TargetsHit++;
                result.TotalDamage += r.Damage.FinalDamage;
                if (r.TargetKilled)
                    result.TargetsKilled++;
            }
        }

        // Start weapon cooldown
        weaponData.StartCooldown();

        return result;
    }

    /// <summary>
    /// Resolve a melee attack (bump-attack).
    /// </summary>
    public static AttackResult ResolveMeleeAttack(
        Node attacker,
        Node target,
        Item weapon)
    {
        var weaponData = weapon.WeaponData!;

        // Melee always hits (no accuracy roll)
        var damage = DamageCalculator.Calculate(
            weaponData,
            GetTargetArmor(target),
            GetTargetHasShield(target));

        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target,
            Weapon = weapon,
            TargetPosition = GetTargetPosition(target),
            Hit = true,
            Accuracy = 100,
            Damage = damage,
            ActionCost = weaponData.ActionCost,
            EnergyCost = weaponData.EnergyCost
        };

        // Apply damage
        ApplyDamage(target, damage, result);

        // Generate message
        result.Message = GenerateAttackMessage(result);

        // Emit events
        EmitAttackEvents(result);

        return result;
    }

    /// <summary>
    /// Resolve a single ranged attack against a target.
    /// </summary>
    private static AttackResult ResolveSingleAttack(
        Node attacker,
        Node target,
        Item weapon,
        LineOfFireInfo lineOfFire,
        int accuracy)
    {
        var weaponData = weapon.WeaponData!;

        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target,
            Weapon = weapon,
            TargetPosition = GetTargetPosition(target),
            Accuracy = accuracy,
            ActionCost = weaponData.ActionCost,
            EnergyCost = weaponData.EnergyCost
        };

        // Roll to hit
        result.Hit = AccuracyCalculator.RollToHit(accuracy, _random);

        if (result.Hit)
        {
            // Calculate damage
            result.Damage = DamageCalculator.Calculate(
                weaponData,
                GetTargetArmor(target),
                GetTargetHasShield(target),
                _random);

            // Apply damage
            ApplyDamage(target, result.Damage, result);
        }
        else
        {
            result.Damage = DamageResult.Miss();
        }

        // Generate message
        result.Message = GenerateAttackMessage(result);

        // Emit events
        EmitAttackEvents(result);

        return result;
    }

    /// <summary>
    /// Resolve an AoE attack.
    /// </summary>
    private static List<AttackResult> ResolveAoEAttack(
        Node attacker,
        Item weapon,
        AttackInfo attackInfo,
        SceneTree sceneTree)
    {
        var results = new List<AttackResult>();
        var weaponData = weapon.WeaponData!;
        var center = attackInfo.TargetPosition;

        // Get all enemies in affected tiles
        var enemies = sceneTree.GetNodesInGroup("Enemies");

        foreach (var node in enemies)
        {
            if (node is not Entity entity)
                continue;

            var entityPos = entity.GridPosition;
            int distanceFromCenter = LineOfFire.GetDistance(center, entityPos);

            if (distanceFromCenter <= weaponData.AreaRadius)
            {
                // Calculate damage falloff
                int baseDamage = weaponData.RollDamage(_random);
                int aoeDamage = DamageCalculator.CalculateAoEDamage(
                    baseDamage,
                    distanceFromCenter,
                    weaponData.AreaRadius);

                // AoE always hits targets in radius (no accuracy roll)
                var damageResult = new DamageResult
                {
                    RawDamage = aoeDamage,
                    FinalDamage = Math.Max(1, aoeDamage - GetTargetArmor(node)),
                    ArmorReduction = GetTargetArmor(node),
                    DamageType = weaponData.DamageType
                };

                var attackResult = new AttackResult
                {
                    Attacker = attacker,
                    Target = node,
                    Weapon = weapon,
                    TargetPosition = entityPos,
                    Hit = true,
                    Accuracy = 100,
                    Damage = damageResult,
                    ActionCost = 0,  // Only primary attack costs action
                    EnergyCost = 0
                };

                // Apply damage
                ApplyDamage(node, damageResult, attackResult);
                attackResult.Message = GenerateAttackMessage(attackResult);
                EmitAttackEvents(attackResult);

                results.Add(attackResult);
            }
        }

        return results;
    }

    /// <summary>
    /// Apply damage to a target.
    /// </summary>
    private static void ApplyDamage(Node target, DamageResult damage, AttackResult result)
    {
        // Get attacker position for AI alerting
        Vector2I? attackerPos = null;
        if (result.Attacker is Entity attackerEntity)
        {
            attackerPos = attackerEntity.GridPosition;
        }

        if (target is Enemy enemy)
        {
            enemy.TakeDamage(damage.FinalDamage, attackerPos);
            result.TargetKilled = enemy.CurrentHealth <= 0;
        }
        else if (target is Player player)
        {
            player.TakeDamage(damage.FinalDamage);
            result.TargetKilled = player.CurrentHealth <= 0;
        }
    }

    /// <summary>
    /// Get target's armor value.
    /// </summary>
    private static int GetTargetArmor(Node target)
    {
        if (target is Enemy enemy)
            return enemy.AttributesComponent?.Armor ?? 0;
        if (target is Player player)
            return player.Armor;
        return 0;
    }

    /// <summary>
    /// Check if target has a shield module.
    /// </summary>
    private static bool GetTargetHasShield(Node target)
    {
        // Future: Check equipment for shield modules
        return false;
    }

    /// <summary>
    /// Get target's grid position.
    /// </summary>
    private static Vector2I GetTargetPosition(Node target)
    {
        if (target is Entity entity)
            return entity.GridPosition;
        return Vector2I.Zero;
    }

    /// <summary>
    /// Generate attack message for combat log.
    /// </summary>
    private static string GenerateAttackMessage(AttackResult result)
    {
        string attackerName = result.Attacker is Entity ae ? ae.EntityName : "Unknown";
        string targetName = result.Target is Entity te ? te.EntityName : "target";
        string weaponName = result.Weapon.Name;

        if (!result.Hit)
        {
            return $"{attackerName} fires {weaponName} at {targetName}... MISS!";
        }

        string damageStr = result.Damage.IsCritical
            ? $"CRITICAL! {result.Damage.FinalDamage}"
            : $"{result.Damage.FinalDamage}";

        string suffix = result.TargetKilled ? " (destroyed)" : "";

        return $"{attackerName} hits {targetName} with {weaponName} for {damageStr} damage{suffix}";
    }

    /// <summary>
    /// Emit combat events.
    /// </summary>
    private static void EmitAttackEvents(AttackResult result)
    {
        if (result.Hit && result.Target != null)
        {
            EventBus.Instance.EmitAttackPerformed(result.Attacker, result.Target, result.Damage.FinalDamage);

            if (result.Target is Entity entity)
            {
                int remainingHealth = 0;
                if (result.Target is Enemy enemy)
                    remainingHealth = enemy.CurrentHealth;
                else if (result.Target is Player player)
                    remainingHealth = player.CurrentHealth;

                EventBus.Instance.EmitEntityDamaged(result.Target, result.Damage.FinalDamage, remainingHealth);
            }
        }
    }

    /// <summary>
    /// Get inventory component from attacker node.
    /// </summary>
    private static Inventory? GetAttackerInventory(Node attacker)
    {
        if (attacker is Player player)
            return player.InventoryComponent;

        // For enemies or other entities, try to get inventory child
        return attacker.GetNodeOrNull<Inventory>("Inventory");
    }

    /// <summary>
    /// Check if the attacker has enough ammo for a weapon.
    /// </summary>
    public static bool HasAmmoForWeapon(Node attacker, WeaponData weaponData)
    {
        if (!weaponData.UsesAmmo)
            return true;

        var inventory = GetAttackerInventory(attacker);
        if (inventory == null)
            return false;

        return inventory.HasAmmo(weaponData.RequiredAmmoType!.Value, weaponData.AmmoPerShot);
    }

    /// <summary>
    /// Get the current ammo count for a weapon's ammo type.
    /// </summary>
    public static int GetAmmoCountForWeapon(Node attacker, WeaponData weaponData)
    {
        if (!weaponData.UsesAmmo)
            return -1;  // -1 indicates unlimited (energy only)

        var inventory = GetAttackerInventory(attacker);
        if (inventory == null)
            return 0;

        return inventory.GetAmmoCount(weaponData.RequiredAmmoType!.Value);
    }
}
