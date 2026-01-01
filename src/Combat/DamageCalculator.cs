using System;
using System.Collections.Generic;
using Godot;

namespace NullAndVoid.Combat;

/// <summary>
/// Result of a damage calculation.
/// </summary>
public struct DamageResult
{
    public int RawDamage;
    public int DamageTypeModifier;
    public int ArmorReduction;
    public int FinalDamage;
    public bool IsCritical;
    public int CriticalBonus;
    public DamageType DamageType;
    public WeaponEffect AppliedEffect;
    public int EffectDuration;

    public static DamageResult Miss() => new()
    {
        RawDamage = 0,
        FinalDamage = 0,
        IsCritical = false,
        DamageType = DamageType.Kinetic,
        AppliedEffect = WeaponEffect.None
    };

    public string GetDescription()
    {
        if (FinalDamage == 0 && RawDamage == 0)
            return "MISS";

        var parts = new List<string>();

        if (IsCritical)
            parts.Add($"CRITICAL! {RawDamage} + {CriticalBonus}");
        else
            parts.Add($"{RawDamage}");

        if (DamageTypeModifier != 0)
            parts.Add($"type {(DamageTypeModifier > 0 ? "+" : "")}{DamageTypeModifier}");

        if (ArmorReduction > 0)
            parts.Add($"armor -{ArmorReduction}");

        parts.Add($"= {FinalDamage} damage");

        if (AppliedEffect != WeaponEffect.None)
            parts.Add($"({AppliedEffect} {EffectDuration}t)");

        return string.Join(" ", parts);
    }
}

/// <summary>
/// Calculates damage for attacks including armor, damage type, and effects.
/// </summary>
public static class DamageCalculator
{
    // Minimum damage that always gets through
    public const int MIN_DAMAGE = 1;

    // Damage type effectiveness multipliers
    // Format: (attackType, defenseType) -> multiplier
    // For now, simple armor/shield interactions
    public const float KINETIC_VS_ARMOR_BONUS = 1.5f;
    public const float KINETIC_VS_SHIELD_PENALTY = 0.75f;
    public const float THERMAL_VS_SHIELD_BONUS = 1.25f;
    public const float EM_PHYSICAL_PENALTY = 0.5f;
    public const float EXPLOSIVE_SALVAGE_PENALTY = 0.75f;  // Reduces loot

    private static readonly Random _random = new();

    /// <summary>
    /// Calculate damage for an attack using WeaponData.
    /// </summary>
    public static DamageResult Calculate(
        WeaponData weapon,
        int targetArmor,
        bool targetHasShield = false,
        Random? random = null)
    {
        random ??= _random;

        var result = new DamageResult
        {
            DamageType = weapon.DamageType,
            AppliedEffect = WeaponEffect.None
        };

        // Roll base damage
        result.RawDamage = weapon.RollDamage(random);

        // Check for critical hit
        result.IsCritical = weapon.RollCritical(random);
        if (result.IsCritical)
        {
            int critDamage = weapon.ApplyCritical(result.RawDamage);
            result.CriticalBonus = critDamage - result.RawDamage;
            result.RawDamage = critDamage;
        }

        // Apply damage type modifiers
        result.DamageTypeModifier = CalculateDamageTypeModifier(
            weapon.DamageType,
            result.RawDamage,
            targetArmor,
            targetHasShield);

        int modifiedDamage = result.RawDamage + result.DamageTypeModifier;

        // Apply armor reduction
        result.ArmorReduction = Math.Min(targetArmor, modifiedDamage - MIN_DAMAGE);
        result.FinalDamage = Math.Max(MIN_DAMAGE, modifiedDamage - targetArmor);

        // Roll for status effect
        if (weapon.PrimaryEffect != WeaponEffect.None && weapon.EffectChance > 0)
        {
            if (random.Next(100) < weapon.EffectChance)
            {
                result.AppliedEffect = weapon.PrimaryEffect;
                result.EffectDuration = weapon.EffectDuration;
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate damage for an attack using IWeaponStats (testable without Godot).
    /// </summary>
    public static DamageResult Calculate(
        IWeaponStats weapon,
        int targetArmor,
        bool targetHasShield = false,
        Random? random = null)
    {
        random ??= _random;

        var result = new DamageResult
        {
            DamageType = weapon.DamageType,
            AppliedEffect = WeaponEffect.None
        };

        // Roll base damage
        result.RawDamage = RollDamage(weapon.MinDamage, weapon.MaxDamage, random);

        // Check for critical hit
        bool isCritical = RollCritical(weapon.CriticalChance, random);
        if (isCritical)
        {
            result.IsCritical = true;
            int critDamage = ApplyCritical(result.RawDamage, weapon.CriticalMultiplier);
            result.CriticalBonus = critDamage - result.RawDamage;
            result.RawDamage = critDamage;
        }

        // Apply damage type modifiers
        result.DamageTypeModifier = CalculateDamageTypeModifier(
            weapon.DamageType,
            result.RawDamage,
            targetArmor,
            targetHasShield);

        int modifiedDamage = result.RawDamage + result.DamageTypeModifier;

        // Apply armor reduction
        result.ArmorReduction = Math.Min(targetArmor, modifiedDamage - MIN_DAMAGE);
        result.FinalDamage = Math.Max(MIN_DAMAGE, modifiedDamage - targetArmor);

        // Roll for status effect
        if (weapon.PrimaryEffect != WeaponEffect.None && weapon.EffectChance > 0)
        {
            if (random.Next(100) < weapon.EffectChance)
            {
                result.AppliedEffect = weapon.PrimaryEffect;
                result.EffectDuration = weapon.EffectDuration;
            }
        }

        return result;
    }

    /// <summary>
    /// Roll damage in a range (extracted for testability).
    /// </summary>
    public static int RollDamage(int minDamage, int maxDamage, Random? random = null)
    {
        random ??= _random;
        return random.Next(minDamage, maxDamage + 1);
    }

    /// <summary>
    /// Check if an attack is a critical hit (extracted for testability).
    /// </summary>
    public static bool RollCritical(int criticalChance, Random? random = null)
    {
        if (criticalChance <= 0)
            return false;
        random ??= _random;
        return random.NextDouble() * 100 < criticalChance;
    }

    /// <summary>
    /// Apply critical hit multiplier (extracted for testability).
    /// </summary>
    public static int ApplyCritical(int baseDamage, float criticalMultiplier)
    {
        return (int)(baseDamage * criticalMultiplier);
    }

    /// <summary>
    /// Calculate damage type modifier based on attack vs defense type.
    /// </summary>
    private static int CalculateDamageTypeModifier(
        DamageType damageType,
        int baseDamage,
        int targetArmor,
        bool targetHasShield)
    {
        float modifier = 1.0f;

        switch (damageType)
        {
            case DamageType.Kinetic:
                // Kinetic is good vs armor, bad vs shields
                if (targetArmor > 0)
                    modifier = KINETIC_VS_ARMOR_BONUS;
                if (targetHasShield)
                    modifier *= KINETIC_VS_SHIELD_PENALTY;
                break;

            case DamageType.Thermal:
                // Thermal is good vs shields
                if (targetHasShield)
                    modifier = THERMAL_VS_SHIELD_BONUS;
                break;

            case DamageType.Electromagnetic:
                // EM does less physical damage but has special effects
                modifier = EM_PHYSICAL_PENALTY;
                break;

            case DamageType.Explosive:
                // Explosive is consistent but reduces salvage
                modifier = 1.0f;
                break;

            case DamageType.Impact:
                // Impact is pure physical
                modifier = 1.0f;
                break;
        }

        // Return the difference from base damage
        int modifiedDamage = (int)(baseDamage * modifier);
        return modifiedDamage - baseDamage;
    }

    /// <summary>
    /// Calculate AoE damage falloff based on distance from center.
    /// </summary>
    public static int CalculateAoEDamage(int baseDamage, int distanceFromCenter, int maxRadius)
    {
        if (distanceFromCenter == 0)
            return baseDamage;

        // Linear falloff from center
        float falloff = 1.0f - ((float)distanceFromCenter / (maxRadius + 1));
        return Math.Max(1, (int)(baseDamage * falloff));
    }

    /// <summary>
    /// Calculate knockback distance based on damage and target weight.
    /// </summary>
    public static int CalculateKnockback(int knockbackBase, int damageDealt, bool targetIsHeavy = false)
    {
        int distance = knockbackBase;

        // More damage = more knockback
        if (damageDealt > 20)
            distance += 1;
        if (damageDealt > 40)
            distance += 1;

        // Heavy targets resist knockback
        if (targetIsHeavy)
            distance = Math.Max(1, distance / 2);

        return distance;
    }

    /// <summary>
    /// Get damage color for UI display.
    /// </summary>
    public static Color GetDamageColor(int damage, bool isCritical)
    {
        if (isCritical)
            return new Color(1.0f, 0.8f, 0.0f);  // Gold for crits

        if (damage >= 20)
            return new Color(1.0f, 0.3f, 0.3f);  // Red for high damage
        if (damage >= 10)
            return new Color(1.0f, 0.6f, 0.3f);  // Orange for medium
        if (damage >= 5)
            return new Color(1.0f, 1.0f, 0.3f);  // Yellow for low
        return new Color(0.8f, 0.8f, 0.8f);      // Gray for minimal
    }

    /// <summary>
    /// Get damage type color for visual effects.
    /// </summary>
    public static Color GetDamageTypeColor(DamageType type)
    {
        return type switch
        {
            DamageType.Kinetic => new Color(0.8f, 0.7f, 0.5f),       // Brass/bullet
            DamageType.Thermal => new Color(1.0f, 0.4f, 0.1f),       // Orange/fire
            DamageType.Electromagnetic => new Color(0.3f, 0.7f, 1.0f), // Electric blue
            DamageType.Explosive => new Color(1.0f, 0.6f, 0.0f),     // Explosion orange
            DamageType.Impact => new Color(0.6f, 0.6f, 0.7f),        // Metal gray
            _ => new Color(1.0f, 1.0f, 1.0f)
        };
    }
}
