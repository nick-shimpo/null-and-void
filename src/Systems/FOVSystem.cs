using Godot;
using System.Collections.Generic;
using NullAndVoid.World;

namespace NullAndVoid.Systems;

/// <summary>
/// Field of View system using recursive shadowcasting.
/// Determines which tiles are visible from a given position.
/// </summary>
public class FOVSystem
{
    private readonly HashSet<Vector2I> _visibleTiles = new();
    private readonly HashSet<Vector2I> _exploredTiles = new();
    private int _viewRadius;
    private Vector2I _origin;

    // Multipliers for the eight octants of the FOV
    private static readonly int[,] Multipliers = new int[,]
    {
        { 1, 0, 0, -1, -1, 0, 0, 1 },
        { 0, 1, -1, 0, 0, -1, 1, 0 },
        { 0, 1, 1, 0, 0, -1, -1, 0 },
        { 1, 0, 0, 1, -1, 0, 0, -1 }
    };

    public IReadOnlySet<Vector2I> VisibleTiles => _visibleTiles;
    public IReadOnlySet<Vector2I> ExploredTiles => _exploredTiles;

    /// <summary>
    /// Calculate visible tiles from a position with given radius.
    /// </summary>
    public void Calculate(Vector2I origin, int radius)
    {
        _origin = origin;
        _viewRadius = radius;
        _visibleTiles.Clear();

        // Origin is always visible
        _visibleTiles.Add(origin);
        _exploredTiles.Add(origin);

        // Calculate FOV for all 8 octants
        for (int octant = 0; octant < 8; octant++)
        {
            CastLight(1, 1.0f, 0.0f,
                Multipliers[0, octant], Multipliers[1, octant],
                Multipliers[2, octant], Multipliers[3, octant]);
        }
    }

    /// <summary>
    /// Check if a tile is currently visible.
    /// </summary>
    public bool IsVisible(Vector2I position)
    {
        return _visibleTiles.Contains(position);
    }

    /// <summary>
    /// Check if a tile has ever been explored.
    /// </summary>
    public bool IsExplored(Vector2I position)
    {
        return _exploredTiles.Contains(position);
    }

    /// <summary>
    /// Recursive shadowcasting for one octant.
    /// </summary>
    private void CastLight(int row, float startSlope, float endSlope,
        int xx, int xy, int yx, int yy)
    {
        if (startSlope < endSlope)
            return;

        float nextStartSlope = startSlope;

        for (int i = row; i <= _viewRadius; i++)
        {
            bool blocked = false;

            for (int dx = -i, dy = -i; dx <= 0; dx++)
            {
                float leftSlope = (dx - 0.5f) / (dy + 0.5f);
                float rightSlope = (dx + 0.5f) / (dy - 0.5f);

                if (startSlope < rightSlope)
                    continue;
                if (endSlope > leftSlope)
                    break;

                // Translate relative coordinates to map coordinates
                int mapX = _origin.X + dx * xx + dy * xy;
                int mapY = _origin.Y + dx * yx + dy * yy;
                Vector2I mapPos = new(mapX, mapY);

                // Check if within radius (using Euclidean distance for circular FOV)
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance <= _viewRadius)
                {
                    _visibleTiles.Add(mapPos);
                    _exploredTiles.Add(mapPos);
                }

                // Check if tile blocks vision
                bool isBlocking = !TileMapManager.Instance.IsInBounds(mapPos) ||
                                  TileMapManager.Instance.GetTileAt(mapPos) == TileMapManager.TileType.Wall;

                if (blocked)
                {
                    if (isBlocking)
                    {
                        nextStartSlope = rightSlope;
                    }
                    else
                    {
                        blocked = false;
                        startSlope = nextStartSlope;
                    }
                }
                else if (isBlocking && i < _viewRadius)
                {
                    blocked = true;
                    CastLight(i + 1, startSlope, leftSlope, xx, xy, yx, yy);
                    nextStartSlope = rightSlope;
                }
            }

            if (blocked)
                break;
        }
    }

    /// <summary>
    /// Clear explored tiles (for new level).
    /// </summary>
    public void ClearExplored()
    {
        _exploredTiles.Clear();
    }
}
