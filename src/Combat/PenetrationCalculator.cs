using System.Collections.Generic;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Targeting;
using NullAndVoid.World;

namespace NullAndVoid.Combat;

/// <summary>
/// Result of a penetration calculation for energy weapons.
/// </summary>
public class PenetrationResult
{
    /// <summary>
    /// Tiles destroyed by the beam.
    /// </summary>
    public List<Vector2I> DestroyedTiles { get; } = new();

    /// <summary>
    /// Position where the beam stopped (if blocked).
    /// </summary>
    public Vector2I? StoppedAt { get; set; }

    /// <summary>
    /// Remaining damage after penetration.
    /// </summary>
    public int DamageAtTarget { get; set; }

    /// <summary>
    /// Path of the beam.
    /// </summary>
    public List<Vector2I> BeamPath { get; } = new();

    /// <summary>
    /// Whether the beam reached the target.
    /// </summary>
    public bool ReachedTarget => StoppedAt == null;

    /// <summary>
    /// Total tiles penetrated.
    /// </summary>
    public int TilesPenetrated => DestroyedTiles.Count;
}

/// <summary>
/// Calculates beam penetration for energy weapons that can destroy walls.
/// Energy weapons with sufficient power can punch through walls, losing damage as they go.
/// </summary>
public static class PenetrationCalculator
{
    /// <summary>
    /// Calculate penetration path for an energy beam.
    /// </summary>
    /// <param name="weapon">Weapon firing the beam.</param>
    /// <param name="origin">Starting position.</param>
    /// <param name="target">Target position.</param>
    /// <param name="gameMap">The game map for tile access.</param>
    /// <returns>Penetration result with destroyed tiles and remaining damage.</returns>
    public static PenetrationResult CalculatePenetration(
        WeaponData weapon,
        Vector2I origin,
        Vector2I target,
        GameMap gameMap)
    {
        var result = new PenetrationResult();
        var line = GetLine(origin, target);
        result.BeamPath.AddRange(line);

        // Beam power budget = max damage + penetration bonus
        int remainingPower = weapon.MaxDamage + weapon.PenetrationPower;
        result.DamageAtTarget = weapon.MaxDamage;

        foreach (var tile in line)
        {
            // Skip the origin tile
            if (tile == origin)
                continue;

            if (!IsInBounds(tile, gameMap.Width, gameMap.Height))
            {
                result.StoppedAt = tile;
                result.DamageAtTarget = 0;
                break;
            }

            var tileData = gameMap.GetTileSafe(tile.X, tile.Y);

            // Check if tile blocks passage
            if (tileData.BlocksMovement && tileData.State != DestructionState.Destroyed)
            {
                // Calculate wall strength based on material and remaining HP
                int wallStrength = CalculateWallStrength(tileData);

                if (remainingPower >= wallStrength)
                {
                    // Destroy wall, continue through
                    result.DestroyedTiles.Add(tile);
                    remainingPower -= wallStrength;

                    // Damage reduction after penetration
                    int damageLost = wallStrength / 2;
                    result.DamageAtTarget = Mathf.Max(0, result.DamageAtTarget - damageLost);
                }
                else
                {
                    // Beam stopped
                    result.StoppedAt = tile;
                    result.DamageAtTarget = 0;
                    break;
                }
            }

            // Stop if we reached the target
            if (tile == target)
                break;
        }

        return result;
    }

    /// <summary>
    /// Apply penetration to the game world, destroying tiles.
    /// </summary>
    public static void ApplyPenetration(PenetrationResult result, GameMap gameMap)
    {
        var tiles = gameMap.GetTilesArray();

        foreach (var pos in result.DestroyedTiles)
        {
            if (!IsInBounds(pos, gameMap.Width, gameMap.Height))
                continue;

            ref var tile = ref tiles[pos.X, pos.Y];

            // Destroy the tile by reducing HP to 0
            tile.CurrentHP = 0;
            tile.State = DestructionState.Destroyed;
            tile.BlocksMovement = false;
            tile.BlocksSight = false;

            // Change visual to debris
            tile.Character = '.';
            tile.BaseColor = new Color(0.4f, 0.3f, 0.3f);
        }
    }

    /// <summary>
    /// Calculate effective wall strength for penetration.
    /// </summary>
    private static int CalculateWallStrength(DestructibleTile tile)
    {
        // Base strength from remaining HP
        int baseStrength = tile.CurrentHP;

        // Hardness adds resistance
        int hardnessBonus = tile.Material.Hardness / 5;

        return baseStrength + hardnessBonus;
    }

    /// <summary>
    /// Check if a weapon can penetrate.
    /// </summary>
    public static bool CanWeaponPenetrate(WeaponData weapon)
    {
        // Only energy weapons can penetrate
        if (weapon.Category != WeaponCategory.Energy)
            return false;

        // Must have penetration enabled
        return weapon.CanPenetrate;
    }

    /// <summary>
    /// Get estimated number of walls that could be penetrated.
    /// </summary>
    public static int EstimatePenetrationDepth(WeaponData weapon, MaterialProperties material)
    {
        if (!CanWeaponPenetrate(weapon))
            return 0;

        int power = weapon.MaxDamage + weapon.PenetrationPower;
        int wallStrength = material.MaxHitPoints + (material.Hardness / 5);

        if (wallStrength <= 0)
            return 100;  // Effectively infinite

        return power / wallStrength;
    }

    /// <summary>
    /// Bresenham's line algorithm for beam path.
    /// </summary>
    private static List<Vector2I> GetLine(Vector2I start, Vector2I end)
    {
        var path = new List<Vector2I>();

        int x0 = start.X, y0 = start.Y;
        int x1 = end.X, y1 = end.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            path.Add(new Vector2I(x0, y0));

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return path;
    }

    private static bool IsInBounds(Vector2I pos, int width, int height)
    {
        return pos.X >= 0 && pos.X < width && pos.Y >= 0 && pos.Y < height;
    }
}
