using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NullAndVoid.Components;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.Targeting;

namespace NullAndVoid.Combat;

/// <summary>
/// Controls automatic weapon firing at the start of each player turn.
/// Weapons can be toggled to auto-fire mode, targeting enemies in range automatically.
/// </summary>
public class AutoFireController
{
    private static AutoFireController? _instance;
    public static AutoFireController Instance => _instance ??= new AutoFireController();

    private readonly HashSet<string> _autoFireWeaponIds = new();

    /// <summary>
    /// Toggle auto-fire mode for a weapon.
    /// </summary>
    public void ToggleAutoFire(Item weapon)
    {
        if (!weapon.IsWeapon)
            return;

        if (_autoFireWeaponIds.Contains(weapon.Id))
        {
            _autoFireWeaponIds.Remove(weapon.Id);
            GD.Print($"[AutoFire] Disabled for {weapon.Name}");
        }
        else
        {
            _autoFireWeaponIds.Add(weapon.Id);
            GD.Print($"[AutoFire] Enabled for {weapon.Name}");
        }
    }

    /// <summary>
    /// Enable auto-fire for a weapon.
    /// </summary>
    public void EnableAutoFire(Item weapon)
    {
        if (!weapon.IsWeapon)
            return;

        _autoFireWeaponIds.Add(weapon.Id);
    }

    /// <summary>
    /// Disable auto-fire for a weapon.
    /// </summary>
    public void DisableAutoFire(Item weapon)
    {
        _autoFireWeaponIds.Remove(weapon.Id);
    }

    /// <summary>
    /// Check if a weapon has auto-fire enabled.
    /// </summary>
    public bool IsAutoFireEnabled(Item weapon)
    {
        return _autoFireWeaponIds.Contains(weapon.Id);
    }

    /// <summary>
    /// Clear all auto-fire settings (e.g., on level change).
    /// </summary>
    public void ClearAll()
    {
        _autoFireWeaponIds.Clear();
    }

    /// <summary>
    /// Process all auto-fire weapons at the start of player turn.
    /// Returns list of attack results for display/animation.
    /// </summary>
    public List<CombatResult> ProcessAutoFire(Player player, SceneTree tree)
    {
        var results = new List<CombatResult>();

        if (_autoFireWeaponIds.Count == 0)
            return results;

        // Get all equipped weapons with auto-fire enabled
        var equipment = player.EquipmentComponent;
        if (equipment == null)
            return results;

        var autoFireWeapons = equipment.GetAllEquippedItems()
            .Where(item => item.IsWeapon && _autoFireWeaponIds.Contains(item.Id))
            .ToList();

        if (autoFireWeapons.Count == 0)
            return results;

        // Get all enemies
        var enemies = tree.GetNodesInGroup("Enemies")
            .Cast<Enemy>()
            .Where(e => e.CurrentHealth > 0)
            .ToList();

        if (enemies.Count == 0)
            return results;

        foreach (var weapon in autoFireWeapons)
        {
            var weaponData = weapon.WeaponData!;

            // Skip if weapon is on cooldown
            if (!weaponData.IsReady)
                continue;

            // Skip if out of ammo
            if (!CombatResolver.HasAmmoForWeapon(player, weaponData))
                continue;

            // Skip if not enough energy
            if (player.AttributesComponent == null ||
                player.AttributesComponent.CurrentEnergyReserve < weaponData.EnergyCost)
                continue;

            // Find best target in range
            var target = SelectTarget(player, weapon, enemies);
            if (target == null)
                continue;

            // Check line of fire
            var lof = LineOfFire.Check(player.GridPosition, target.GridPosition, weaponData.Range);
            if (lof.Result == LineOfFireResult.Blocked && !weaponData.IndirectFire)
                continue;

            if (lof.Result == LineOfFireResult.OutOfRange)
                continue;

            // Create attack info
            var targetInfo = new TargetInfo
            {
                Entity = target,
                Position = target.GridPosition,
                Name = target.EntityName,
                CurrentHealth = target.CurrentHealth,
                MaxHealth = target.MaxHealth
            };

            var attackInfo = new AttackInfo
            {
                Weapon = weapon,
                Target = targetInfo,
                TargetPosition = target.GridPosition,
                LineOfFire = lof,
                Accuracy = AccuracyCalculator.CalculateSimple(weaponData, lof.Distance, lof.Result)
            };

            // Apply auto-fire accuracy penalty (-10%)
            attackInfo.Accuracy = Math.Max(5, attackInfo.Accuracy - 10);

            // Consume energy
            player.AttributesComponent?.TryConsumeEnergy(weaponData.EnergyCost);

            // Resolve the attack
            var result = CombatResolver.ResolveRangedAttack(player, attackInfo, tree);
            results.Add(result);

            // Remove killed enemies from target list
            if (result.TargetsKilled > 0)
            {
                enemies.RemoveAll(e => e.CurrentHealth <= 0);
            }

            GD.Print($"[AutoFire] {weapon.Name} fired at {target.EntityName}: {result.GetSummary()}");
        }

        return results;
    }

    /// <summary>
    /// Select the best target for a weapon.
    /// Priority: Lowest health > Closest > Highest threat level
    /// </summary>
    private Enemy? SelectTarget(Player player, Item weapon, List<Enemy> enemies)
    {
        var weaponData = weapon.WeaponData!;
        int range = weaponData.Range;

        return enemies
            .Where(e => e.CurrentHealth > 0)
            .Where(e => GetDistance(player.GridPosition, e.GridPosition) <= range)
            .Where(e =>
            {
                // Check line of fire
                var lof = LineOfFire.Check(player.GridPosition, e.GridPosition, range);
                return lof.Result != LineOfFireResult.Blocked || weaponData.IndirectFire;
            })
            .OrderBy(e => e.CurrentHealth)  // Lowest health first (finish off wounded)
            .ThenBy(e => GetDistance(player.GridPosition, e.GridPosition))  // Then closest
            .ThenByDescending(e => e.BaseAttackDamage)  // Then highest threat
            .FirstOrDefault();
    }

    /// <summary>
    /// Get Manhattan distance between two positions.
    /// </summary>
    private static int GetDistance(Vector2I a, Vector2I b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Get list of weapon IDs with auto-fire enabled.
    /// For save/load purposes.
    /// </summary>
    public IReadOnlySet<string> GetAutoFireWeaponIds()
    {
        return _autoFireWeaponIds;
    }

    /// <summary>
    /// Restore auto-fire settings from saved data.
    /// </summary>
    public void RestoreAutoFireSettings(IEnumerable<string> weaponIds)
    {
        _autoFireWeaponIds.Clear();
        foreach (var id in weaponIds)
        {
            _autoFireWeaponIds.Add(id);
        }
    }

    /// <summary>
    /// Reset singleton instance (for testing/restarting).
    /// </summary>
    public static void Reset()
    {
        _instance = null;
    }
}
