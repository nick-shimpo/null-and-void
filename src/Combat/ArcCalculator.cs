using System.Collections.Generic;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Targeting;
using NullAndVoid.World;

namespace NullAndVoid.Combat;

/// <summary>
/// Result of orbital strike clearance check.
/// </summary>
public enum OrbitalClearance
{
    /// <summary>
    /// Clear shot - no ceiling obstruction.
    /// </summary>
    Clear,

    /// <summary>
    /// Weak ceiling that can be destroyed by the strike.
    /// </summary>
    DestructibleCeiling,

    /// <summary>
    /// Reinforced ceiling blocks the strike.
    /// </summary>
    Blocked
}

/// <summary>
/// Result of an arc trajectory check.
/// </summary>
public class ArcResult
{
    /// <summary>
    /// Whether the arc shot is possible.
    /// </summary>
    public bool CanArc { get; set; }

    /// <summary>
    /// Reason if blocked.
    /// </summary>
    public string BlockReason { get; set; } = "";

    /// <summary>
    /// Path of the arc (for visualization).
    /// </summary>
    public List<Vector2I> ArcPath { get; } = new();

    /// <summary>
    /// Highest point of the arc in tiles above ground.
    /// </summary>
    public int ArcHeight { get; set; }

    /// <summary>
    /// Tiles in the impact zone.
    /// </summary>
    public List<Vector2I> ImpactZone { get; } = new();
}

/// <summary>
/// Calculates arc trajectories for artillery and lobbed weapons.
/// Artillery is blocked by ceilings (cannot fire indoors).
/// </summary>
public static class ArcCalculator
{
    /// <summary>
    /// Minimum ceiling strength that blocks all artillery.
    /// </summary>
    public const int ReinforcedCeilingThreshold = 30;

    /// <summary>
    /// Check if artillery can hit target by arcing over obstacles.
    /// Artillery is blocked by any ceiling tile - cannot fire indoors.
    /// </summary>
    public static ArcResult CanArcTo(
        Vector2I origin,
        Vector2I target,
        int minArcHeight,
        TileMapManager tileMap,
        GameMap? gameMap = null)
    {
        var result = new ArcResult
        {
            ArcHeight = minArcHeight
        };

        var line = GetLine(origin, target);
        result.ArcPath.AddRange(line);

        foreach (var tile in line)
        {
            if (tile == origin)
                continue;

            // Check if tile has a ceiling
            if (gameMap != null && HasCeiling(tile, gameMap))
            {
                result.CanArc = false;
                result.BlockReason = "Blocked by ceiling - cannot fire indoors";
                return result;
            }
        }

        // Check target position specifically
        if (gameMap != null && HasCeiling(target, gameMap))
        {
            result.CanArc = false;
            result.BlockReason = "Target is indoors - artillery cannot reach";
            return result;
        }

        result.CanArc = true;
        return result;
    }

    /// <summary>
    /// Check if orbital strike can hit target.
    /// Can destroy weak ceilings (strength < 30), blocked by reinforced.
    /// </summary>
    public static OrbitalClearance CheckOrbitalClearance(
        Vector2I target,
        TileMapManager tileMap,
        GameMap gameMap)
    {
        if (!HasCeiling(target, gameMap))
            return OrbitalClearance.Clear;

        int ceilingStrength = GetCeilingStrength(target, gameMap);
        if (ceilingStrength < ReinforcedCeilingThreshold)
            return OrbitalClearance.DestructibleCeiling;

        return OrbitalClearance.Blocked;
    }

    /// <summary>
    /// Get tiles affected by lobbed projectile impact.
    /// </summary>
    public static List<Vector2I> GetImpactZone(Vector2I target, int radius)
    {
        var tiles = new List<Vector2I>();

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                // Use Manhattan distance for simplicity
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radius)
                {
                    tiles.Add(new Vector2I(target.X + dx, target.Y + dy));
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// Calculate damage falloff for AoE based on distance from center.
    /// </summary>
    public static int CalculateAoEFalloff(int baseDamage, int distanceFromCenter, int radius)
    {
        if (distanceFromCenter <= 0)
            return baseDamage;

        if (distanceFromCenter >= radius)
            return baseDamage / 4;  // Minimum damage at edge

        // Linear falloff
        float falloffPercent = (float)(radius - distanceFromCenter) / radius;
        return Mathf.Max(1, (int)(baseDamage * falloffPercent));
    }

    /// <summary>
    /// Check line of fire with arc option.
    /// If direct fire is blocked, checks if arcing over is possible.
    /// </summary>
    public static LineOfFireInfo CheckWithArc(
        Vector2I origin,
        Vector2I target,
        int maxRange,
        bool allowIndirect,
        TileMapManager tileMap,
        GameMap? gameMap = null)
    {
        // Try direct fire first
        var direct = LineOfFire.Check(origin, target, maxRange);
        if (direct.Result == LineOfFireResult.Clear)
            return direct;

        // If indirect fire is allowed and direct is blocked, try arc
        if (allowIndirect && direct.Result == LineOfFireResult.Blocked)
        {
            var arcResult = CanArcTo(origin, target, 2, tileMap, gameMap);
            if (arcResult.CanArc)
            {
                // Arc shot possible - return as clear with arc path
                return new LineOfFireInfo
                {
                    Result = LineOfFireResult.Clear,
                    BlockingPosition = null,
                    Distance = GetManhattanDistance(origin, target),
                    CoverPenalty = 0,
                    Path = arcResult.ArcPath
                };
            }
        }

        return direct;
    }

    /// <summary>
    /// Get arc trajectory points for visualization.
    /// Returns points along a parabolic arc.
    /// </summary>
    public static List<Vector2> GetArcTrajectoryPoints(Vector2 start, Vector2 end, int numPoints, float arcHeight)
    {
        var points = new List<Vector2>();

        for (int i = 0; i <= numPoints; i++)
        {
            float t = (float)i / numPoints;

            // Linear interpolation for X and Y
            float x = Mathf.Lerp(start.X, end.X, t);
            float y = Mathf.Lerp(start.Y, end.Y, t);

            // Parabolic arc for height (subtract because Y increases downward)
            float heightOffset = -4 * arcHeight * t * (1 - t);
            y += heightOffset;

            points.Add(new Vector2(x, y));
        }

        return points;
    }

    /// <summary>
    /// Check if a position has a ceiling above it.
    /// Uses explicit ceiling data from GameMap if available.
    /// </summary>
    private static bool HasCeiling(Vector2I pos, GameMap gameMap)
    {
        // Use explicit ceiling data if available (from BattlefieldGenerator)
        if (gameMap.HasExplicitCeilingData)
            return gameMap.HasCeiling(pos.X, pos.Y);

        // Fallback: infer from surrounding walls
        // Assume walls on all sides indicate indoor area
        int adjacentWalls = 0;

        var neighbors = new[]
        {
            new Vector2I(pos.X - 1, pos.Y),
            new Vector2I(pos.X + 1, pos.Y),
            new Vector2I(pos.X, pos.Y - 1),
            new Vector2I(pos.X, pos.Y + 1)
        };

        foreach (var neighbor in neighbors)
        {
            if (!IsInBounds(neighbor, gameMap.Width, gameMap.Height))
                continue;

            var tile = gameMap.GetTileSafe(neighbor.X, neighbor.Y);
            if (tile.BlocksMovement && tile.State != DestructionState.Destroyed)
                adjacentWalls++;
        }

        // Consider position "indoor" if surrounded by 3+ walls
        return adjacentWalls >= 3;
    }

    /// <summary>
    /// Get the strength of ceiling at a position.
    /// </summary>
    private static int GetCeilingStrength(Vector2I pos, GameMap gameMap)
    {
        // For now, use the average hardness of surrounding walls
        int totalStrength = 0;
        int wallCount = 0;

        var neighbors = new[]
        {
            new Vector2I(pos.X - 1, pos.Y),
            new Vector2I(pos.X + 1, pos.Y),
            new Vector2I(pos.X, pos.Y - 1),
            new Vector2I(pos.X, pos.Y + 1)
        };

        foreach (var neighbor in neighbors)
        {
            if (!IsInBounds(neighbor, gameMap.Width, gameMap.Height))
                continue;

            var tile = gameMap.GetTileSafe(neighbor.X, neighbor.Y);
            if (tile.BlocksMovement && tile.State != DestructionState.Destroyed)
            {
                totalStrength += tile.Material.Hardness;
                wallCount++;
            }
        }

        return wallCount > 0 ? totalStrength / wallCount : 0;
    }

    /// <summary>
    /// Bresenham's line algorithm.
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

    private static int GetManhattanDistance(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }

    private static bool IsInBounds(Vector2I pos, int width, int height)
    {
        return pos.X >= 0 && pos.X < width && pos.Y >= 0 && pos.Y < height;
    }
}
