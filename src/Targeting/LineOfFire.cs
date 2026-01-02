using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.World;

namespace NullAndVoid.Targeting;

/// <summary>
/// Result of a line of fire check.
/// </summary>
public enum LineOfFireResult
{
    /// <summary>
    /// Clear shot - no obstacles between attacker and target.
    /// </summary>
    Clear,

    /// <summary>
    /// Partial cover - some obstruction but shot possible with penalty.
    /// </summary>
    PartialCover,

    /// <summary>
    /// Blocked - solid wall or obstacle prevents shot.
    /// </summary>
    Blocked,

    /// <summary>
    /// Target is beyond weapon range.
    /// </summary>
    OutOfRange,

    /// <summary>
    /// No valid target at position.
    /// </summary>
    NoTarget
}

/// <summary>
/// Information about an obstruction in the line of fire.
/// </summary>
public struct LineOfFireInfo
{
    public LineOfFireResult Result;
    public Vector2I? BlockingPosition;
    public int Distance;
    public int CoverPenalty;  // Accuracy penalty from cover (0, -20, -40)
    public List<Vector2I> Path;  // All tiles in the line

    public static LineOfFireInfo Clear(int distance, List<Vector2I> path) => new()
    {
        Result = LineOfFireResult.Clear,
        BlockingPosition = null,
        Distance = distance,
        CoverPenalty = 0,
        Path = path
    };

    public static LineOfFireInfo PartialCover(int distance, Vector2I coverPos, List<Vector2I> path) => new()
    {
        Result = LineOfFireResult.PartialCover,
        BlockingPosition = coverPos,
        Distance = distance,
        CoverPenalty = -20,
        Path = path
    };

    public static LineOfFireInfo Blocked(Vector2I blockPos, List<Vector2I> path) => new()
    {
        Result = LineOfFireResult.Blocked,
        BlockingPosition = blockPos,
        Distance = 0,
        CoverPenalty = -100,
        Path = path
    };

    public static LineOfFireInfo OutOfRange(int distance) => new()
    {
        Result = LineOfFireResult.OutOfRange,
        BlockingPosition = null,
        Distance = distance,
        CoverPenalty = 0,
        Path = new List<Vector2I>()
    };
}

/// <summary>
/// Calculates line of fire between two positions using Bresenham's algorithm.
/// </summary>
public static class LineOfFire
{
    /// <summary>
    /// Check if there's a clear line of fire from origin to target.
    /// </summary>
    public static LineOfFireInfo Check(Vector2I origin, Vector2I target, int maxRange = int.MaxValue)
    {
        int distance = GetDistance(origin, target);

        // Check range first
        if (distance > maxRange)
        {
            return LineOfFireInfo.OutOfRange(distance);
        }

        // Get all tiles in the line
        var path = GetLine(origin, target);

        // Check each tile (skip origin and target)
        Vector2I? partialCoverPos = null;

        for (int i = 1; i < path.Count - 1; i++)
        {
            var pos = path[i];

            // Check if tile blocks line of sight
            if (BlocksLineOfSight(pos))
            {
                return LineOfFireInfo.Blocked(pos, path);
            }

            // Check for partial cover (low obstacles, other entities)
            if (ProvidesPartialCover(pos) && partialCoverPos == null)
            {
                partialCoverPos = pos;
            }
        }

        if (partialCoverPos.HasValue)
        {
            return LineOfFireInfo.PartialCover(distance, partialCoverPos.Value, path);
        }

        return LineOfFireInfo.Clear(distance, path);
    }

    /// <summary>
    /// Get all tiles along a line from origin to target using Bresenham's algorithm.
    /// </summary>
    public static List<Vector2I> GetLine(Vector2I origin, Vector2I target)
    {
        var result = new List<Vector2I>();

        int x0 = origin.X;
        int y0 = origin.Y;
        int x1 = target.X;
        int y1 = target.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            result.Add(new Vector2I(x0, y0));

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

        return result;
    }

    /// <summary>
    /// Check if a position completely blocks line of sight.
    /// </summary>
    private static bool BlocksLineOfSight(Vector2I position)
    {
        // Use TileMapManager if available
        if (TileMapManager.Instance != null)
        {
            return !TileMapManager.Instance.IsTransparent(position);
        }

        return false;
    }

    /// <summary>
    /// Check if a position provides partial cover.
    /// </summary>
    private static bool ProvidesPartialCover(Vector2I position)
    {
        // Future: Check for low walls, destructible objects, other entities
        // For now, only full blocking or clear
        return false;
    }

    /// <summary>
    /// Get Chebyshev distance (allows diagonal movement).
    /// </summary>
    public static int GetDistance(Vector2I a, Vector2I b)
    {
        return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    /// <summary>
    /// Get Manhattan distance.
    /// </summary>
    public static int GetManhattanDistance(Vector2I a, Vector2I b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Check if target is within range.
    /// </summary>
    public static bool IsInRange(Vector2I origin, Vector2I target, int range)
    {
        return GetDistance(origin, target) <= range;
    }

    /// <summary>
    /// Get all positions within a radius (for AoE).
    /// </summary>
    public static List<Vector2I> GetPositionsInRadius(Vector2I center, int radius)
    {
        var result = new List<Vector2I>();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                // Use Chebyshev distance for square radius
                // Use Manhattan for diamond shape: Math.Abs(dx) + Math.Abs(dy) <= radius
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) <= radius)
                {
                    result.Add(center + new Vector2I(dx, dy));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get positions in radius that have line of sight to center.
    /// </summary>
    public static List<Vector2I> GetVisiblePositionsInRadius(Vector2I center, int radius)
    {
        var result = new List<Vector2I>();

        foreach (var pos in GetPositionsInRadius(center, radius))
        {
            if (pos == center || Check(center, pos, radius).Result == LineOfFireResult.Clear)
            {
                result.Add(pos);
            }
        }

        return result;
    }
}
