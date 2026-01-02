using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Items;
using NullAndVoid.Targeting;

namespace NullAndVoid.Combat;

/// <summary>
/// Breakdown of accuracy modifiers for display.
/// </summary>
public struct AccuracyBreakdown
{
    public int BaseAccuracy;
    public int DistanceModifier;
    public int TargetSizeModifier;
    public int TargetStateModifier;
    public int AttackerMovementModifier;
    public int CoverModifier;
    public int WeaponModifier;
    public int EquipmentModifier;
    public int FinalAccuracy;

    public List<string> GetModifierStrings()
    {
        var result = new List<string> { $"Base: {BaseAccuracy}%" };

        if (DistanceModifier != 0)
            result.Add($"Distance: {FormatModifier(DistanceModifier)}%");
        if (TargetSizeModifier != 0)
            result.Add($"Target Size: {FormatModifier(TargetSizeModifier)}%");
        if (TargetStateModifier != 0)
            result.Add($"Target State: {FormatModifier(TargetStateModifier)}%");
        if (AttackerMovementModifier != 0)
            result.Add($"Movement: {FormatModifier(AttackerMovementModifier)}%");
        if (CoverModifier != 0)
            result.Add($"Cover: {FormatModifier(CoverModifier)}%");
        if (WeaponModifier != 0)
            result.Add($"Weapon: {FormatModifier(WeaponModifier)}%");
        if (EquipmentModifier != 0)
            result.Add($"Equipment: {FormatModifier(EquipmentModifier)}%");

        result.Add($"Final: {FinalAccuracy}%");
        return result;
    }

    private static string FormatModifier(int value) => value > 0 ? $"+{value}" : value.ToString();
}

/// <summary>
/// Target size categories affecting accuracy.
/// </summary>
public enum TargetSize
{
    Tiny,       // -30% to hit
    Small,      // -10% to hit
    Normal,     // No modifier
    Large,      // +10% to hit
    Huge        // +30% to hit
}

/// <summary>
/// Calculates hit chance for ranged attacks based on various factors.
/// </summary>
public static class AccuracyCalculator
{
    // Base accuracy when no weapon accuracy is specified
    public const int DEFAULT_BASE_ACCURACY = 60;

    // Minimum and maximum accuracy after all modifiers
    public const int MIN_ACCURACY = 5;
    public const int MAX_ACCURACY = 95;

    // Distance modifiers
    public const int CLOSE_RANGE_THRESHOLD = 6;
    public const int CLOSE_RANGE_BONUS_PER_TILE = 3;  // +3% per tile if < 6 range
    public const int LONG_RANGE_PENALTY_PER_TILE = 2; // -2% per tile beyond optimal

    // Target size modifiers
    private static readonly Dictionary<TargetSize, int> _sizeModifiers = new()
    {
        { TargetSize.Tiny, -30 },
        { TargetSize.Small, -10 },
        { TargetSize.Normal, 0 },
        { TargetSize.Large, 10 },
        { TargetSize.Huge, 30 }
    };

    // Movement modifiers
    public const int MOVED_LAST_TURN_PENALTY = -10;
    public const int STATIONARY_BONUS = 10;  // If didn't move for 2+ turns

    // Target state modifiers
    public const int TARGET_IMMOBILE_BONUS = 10;
    public const int TARGET_STUNNED_BONUS = 15;
    public const int TARGET_FLYING_PENALTY = -10;

    // Cover modifiers
    public const int PARTIAL_COVER_PENALTY = -20;
    public const int HEAVY_COVER_PENALTY = -40;

    /// <summary>
    /// Calculate accuracy for an attack.
    /// </summary>
    public static AccuracyBreakdown Calculate(
        WeaponData weapon,
        Vector2I attackerPos,
        Vector2I targetPos,
        LineOfFireInfo lineOfFire,
        TargetSize targetSize = TargetSize.Normal,
        bool targetImmobile = false,
        bool targetStunned = false,
        bool targetFlying = false,
        bool attackerMovedLastTurn = false,
        int attackerStationaryTurns = 0,
        int equipmentAccuracyBonus = 0)
    {
        var breakdown = new AccuracyBreakdown();

        // Base accuracy from weapon
        breakdown.BaseAccuracy = weapon.BaseAccuracy > 0 ? weapon.BaseAccuracy : DEFAULT_BASE_ACCURACY;

        // Distance modifier
        int distance = lineOfFire.Distance;
        if (distance < CLOSE_RANGE_THRESHOLD)
        {
            // Bonus for close range
            breakdown.DistanceModifier = (CLOSE_RANGE_THRESHOLD - distance) * CLOSE_RANGE_BONUS_PER_TILE;
        }
        else if (distance > weapon.Range)
        {
            // Penalty for beyond optimal range
            breakdown.DistanceModifier = -(distance - weapon.Range) * LONG_RANGE_PENALTY_PER_TILE;
        }

        // Target size modifier
        breakdown.TargetSizeModifier = _sizeModifiers[targetSize];

        // Target state modifiers
        if (targetImmobile)
            breakdown.TargetStateModifier += TARGET_IMMOBILE_BONUS;
        if (targetStunned)
            breakdown.TargetStateModifier += TARGET_STUNNED_BONUS;
        if (targetFlying)
            breakdown.TargetStateModifier += TARGET_FLYING_PENALTY;

        // Attacker movement modifier
        if (attackerMovedLastTurn)
            breakdown.AttackerMovementModifier = MOVED_LAST_TURN_PENALTY;
        else if (attackerStationaryTurns >= 2)
            breakdown.AttackerMovementModifier = STATIONARY_BONUS;

        // Cover modifier from line of fire
        breakdown.CoverModifier = lineOfFire.CoverPenalty;

        // Weapon-specific modifiers (some weapons ignore cover)
        if (weapon.IgnoresCover && breakdown.CoverModifier < 0)
        {
            // Energy weapons ignore partial cover
            if (lineOfFire.Result == LineOfFireResult.PartialCover)
            {
                breakdown.WeaponModifier = -breakdown.CoverModifier;  // Negate cover penalty
            }
        }

        // Equipment bonus (from targeting computers, etc.)
        breakdown.EquipmentModifier = equipmentAccuracyBonus;

        // Calculate final accuracy
        int total = breakdown.BaseAccuracy
            + breakdown.DistanceModifier
            + breakdown.TargetSizeModifier
            + breakdown.TargetStateModifier
            + breakdown.AttackerMovementModifier
            + breakdown.CoverModifier
            + breakdown.WeaponModifier
            + breakdown.EquipmentModifier;

        // Clamp to valid range
        breakdown.FinalAccuracy = Math.Clamp(total, MIN_ACCURACY, MAX_ACCURACY);

        return breakdown;
    }

    /// <summary>
    /// Simplified accuracy calculation for quick checks.
    /// </summary>
    public static int CalculateSimple(WeaponData weapon, int distance, LineOfFireResult lofResult)
    {
        return CalculateSimple(weapon.BaseAccuracy, weapon.IgnoresCover, distance, lofResult);
    }

    /// <summary>
    /// Simplified accuracy calculation using IWeaponStats (testable without Godot).
    /// </summary>
    public static int CalculateSimple(IWeaponStats weapon, int distance, LineOfFireResult lofResult)
    {
        return CalculateSimple(weapon.BaseAccuracy, weapon.IgnoresCover, distance, lofResult);
    }

    /// <summary>
    /// Simplified accuracy calculation with primitive parameters (fully testable).
    /// </summary>
    public static int CalculateSimple(int baseAccuracy, bool ignoresCover, int distance, LineOfFireResult lofResult)
    {
        int accuracy = baseAccuracy > 0 ? baseAccuracy : DEFAULT_BASE_ACCURACY;

        // Distance modifier
        if (distance < CLOSE_RANGE_THRESHOLD)
        {
            accuracy += (CLOSE_RANGE_THRESHOLD - distance) * CLOSE_RANGE_BONUS_PER_TILE;
        }

        // Cover penalty
        if (lofResult == LineOfFireResult.PartialCover && !ignoresCover)
        {
            accuracy += PARTIAL_COVER_PENALTY;
        }

        return Math.Clamp(accuracy, MIN_ACCURACY, MAX_ACCURACY);
    }

    /// <summary>
    /// Get target size modifier.
    /// </summary>
    public static int GetSizeModifier(TargetSize size)
    {
        return _sizeModifiers[size];
    }

    /// <summary>
    /// Roll to hit with the given accuracy.
    /// </summary>
    public static bool RollToHit(int accuracy, Random? random = null)
    {
        random ??= new Random();
        return random.Next(100) < accuracy;
    }

    /// <summary>
    /// Get accuracy color for UI display.
    /// </summary>
    public static Color GetAccuracyColor(int accuracy)
    {
        if (accuracy >= 80)
            return new Color(0.3f, 1.0f, 0.3f);  // Green - high chance
        if (accuracy >= 60)
            return new Color(0.8f, 0.8f, 0.3f);  // Yellow - decent chance
        if (accuracy >= 40)
            return new Color(1.0f, 0.6f, 0.2f);  // Orange - risky
        return new Color(1.0f, 0.3f, 0.3f);      // Red - low chance
    }
}
