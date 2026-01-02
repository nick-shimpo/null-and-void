using System.Collections.Generic;
using Godot;
using NullAndVoid.World;

namespace NullAndVoid.AI;

/// <summary>
/// Line of sight utilities using Bresenham's line algorithm.
/// </summary>
public static class LineOfSight
{
    /// <summary>
    /// Check if there's a clear line of sight between two positions.
    /// Uses Bresenham's line algorithm to check all tiles along the path.
    /// </summary>
    /// <param name="from">Starting position.</param>
    /// <param name="to">Target position.</param>
    /// <param name="tileMap">Tile map for walkability checks.</param>
    /// <returns>True if line of sight is clear.</returns>
    public static bool HasClearPath(Vector2I from, Vector2I to, TileMapManager tileMap)
    {
        foreach (var point in GetLine(from, to))
        {
            // Skip start and end points
            if (point == from || point == to)
                continue;

            // Check if this tile blocks sight
            if (!tileMap.IsWalkable(point))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if there's a clear line of sight using a custom blocking check.
    /// </summary>
    /// <param name="from">Starting position.</param>
    /// <param name="to">Target position.</param>
    /// <param name="blocksLOS">Function that returns true if a tile blocks LOS.</param>
    /// <returns>True if line of sight is clear.</returns>
    public static bool HasClearPath(Vector2I from, Vector2I to, System.Func<Vector2I, bool> blocksLOS)
    {
        foreach (var point in GetLine(from, to))
        {
            // Skip start and end points
            if (point == from || point == to)
                continue;

            // Check if this tile blocks sight
            if (blocksLOS(point))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Get all points along a line using Bresenham's algorithm.
    /// </summary>
    /// <param name="from">Starting position.</param>
    /// <param name="to">Ending position.</param>
    /// <returns>Enumerable of all points along the line.</returns>
    public static IEnumerable<Vector2I> GetLine(Vector2I from, Vector2I to)
    {
        int x0 = from.X;
        int y0 = from.Y;
        int x1 = to.X;
        int y1 = to.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            yield return new Vector2I(x0, y0);

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
    }

    /// <summary>
    /// Get all points within a certain range that have line of sight.
    /// Useful for determining visible tiles.
    /// </summary>
    /// <param name="origin">Center position.</param>
    /// <param name="range">Maximum range.</param>
    /// <param name="blocksLOS">Function that returns true if a tile blocks LOS.</param>
    /// <returns>List of visible positions.</returns>
    public static List<Vector2I> GetVisibleTiles(Vector2I origin, int range, System.Func<Vector2I, bool> blocksLOS)
    {
        var visible = new List<Vector2I>();
        var checked_ = new HashSet<Vector2I>();

        // Check all tiles within range
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                // Skip if outside circular range
                if (dx * dx + dy * dy > range * range)
                    continue;

                var target = new Vector2I(origin.X + dx, origin.Y + dy);

                // Skip if already checked
                if (!checked_.Add(target))
                    continue;

                // Check line of sight
                if (HasClearPath(origin, target, blocksLOS))
                {
                    visible.Add(target);
                }
            }
        }

        return visible;
    }

    /// <summary>
    /// Calculate the Euclidean distance between two points.
    /// </summary>
    public static float EuclideanDistance(Vector2I from, Vector2I to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculate the Manhattan distance between two points.
    /// </summary>
    public static int ManhattanDistance(Vector2I from, Vector2I to)
    {
        return Mathf.Abs(to.X - from.X) + Mathf.Abs(to.Y - from.Y);
    }

    /// <summary>
    /// Calculate the Chebyshev distance (diagonal movement allowed).
    /// </summary>
    public static int ChebyshevDistance(Vector2I from, Vector2I to)
    {
        return Mathf.Max(Mathf.Abs(to.X - from.X), Mathf.Abs(to.Y - from.Y));
    }
}
