using System;
using System.Collections.Generic;
using Godot;

namespace NullAndVoid.Destruction;

/// <summary>
/// Manages fire spread simulation using cellular automata.
/// Based on research from Brogue, CDDA, and wildfire simulation papers.
/// </summary>
public class FireSimulation
{
    private readonly DestructibleTile[,] _tiles;
    private readonly int _width;
    private readonly int _height;
    private readonly Random _random = new();

    // Spread directions (8-way)
    private static readonly Vector2I[] _neighbors = {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1, 0),              new(1, 0),
        new(-1, 1),  new(0, 1),  new(1, 1)
    };

    // Wind effect (optional)
    public Vector2 WindDirection { get; set; } = Vector2.Zero;
    public float WindStrength { get; set; } = 0f;

    /// <summary>
    /// Chance for adjacent water to extinguish fire per turn.
    /// </summary>
    public float WaterExtinguishChance { get; set; } = 0.4f;

    // Tracking
    private readonly HashSet<Vector2I> _activeFires = new();
    private readonly List<Vector2I> _pendingIgnitions = new();

    public FireSimulation(DestructibleTile[,] tiles, int width, int height)
    {
        _tiles = tiles;
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Get count of active fire tiles.
    /// </summary>
    public int ActiveFireCount => _activeFires.Count;

    /// <summary>
    /// Register a tile as having active fire.
    /// </summary>
    public void RegisterFire(int x, int y)
    {
        if (IsInBounds(x, y))
            _activeFires.Add(new Vector2I(x, y));
    }

    /// <summary>
    /// Process one turn of fire simulation.
    /// </summary>
    public void ProcessTurn()
    {
        _pendingIgnitions.Clear();

        // Process each active fire
        var firesToRemove = new List<Vector2I>();

        foreach (var pos in _activeFires)
        {
            ref var tile = ref _tiles[pos.X, pos.Y];

            // Check if adjacent to water - water extinguishes fire
            if (IsAdjacentToWater(pos.X, pos.Y))
            {
                // Each adjacent water tile has a chance to extinguish
                int waterCount = CountAdjacentWater(pos.X, pos.Y);
                float extinguishChance = 1.0f - Mathf.Pow(1.0f - WaterExtinguishChance, waterCount);

                if (_random.NextDouble() < extinguishChance)
                {
                    tile.Extinguish();
                    firesToRemove.Add(pos);
                    continue;
                }
            }

            // Advance fire state
            bool stillActive = tile.AdvanceFireTurn();

            if (!stillActive)
            {
                firesToRemove.Add(pos);
                continue;
            }

            // Try to spread fire
            TrySpreadFire(pos.X, pos.Y, tile.Fire);
        }

        // Remove expired fires
        foreach (var pos in firesToRemove)
        {
            _activeFires.Remove(pos);
        }

        // Apply pending ignitions
        foreach (var pos in _pendingIgnitions)
        {
            ref var tile = ref _tiles[pos.X, pos.Y];
            if (tile.TryIgnite())
            {
                _activeFires.Add(pos);
            }
        }
    }

    /// <summary>
    /// Try to spread fire from a tile to its neighbors.
    /// </summary>
    private void TrySpreadFire(int x, int y, FireState fire)
    {
        if (!fire.IsBurning)
            return;

        float baseSpreadChance = fire.Data.SpreadChance;

        foreach (var offset in _neighbors)
        {
            int nx = x + offset.X;
            int ny = y + offset.Y;

            if (!IsInBounds(nx, ny))
                continue;

            ref var neighbor = ref _tiles[nx, ny];

            // Skip non-flammable or already burning
            if (neighbor.Material.Flammability <= 0)
                continue;
            if (neighbor.Fire.IsActive)
                continue;

            // Calculate spread chance
            float spreadChance = baseSpreadChance * neighbor.Material.Flammability;

            // Wind effect
            if (WindStrength > 0)
            {
                Vector2 spreadDir = new Vector2(offset.X, offset.Y).Normalized();
                float windBonus = spreadDir.Dot(WindDirection.Normalized()) * WindStrength * 0.3f;
                spreadChance *= (1.0f + windBonus);
            }

            // Diagonal spread is less likely
            if (offset.X != 0 && offset.Y != 0)
            {
                spreadChance *= 0.7f;
            }

            // Roll for spread
            if (_random.NextDouble() < spreadChance)
            {
                _pendingIgnitions.Add(new Vector2I(nx, ny));
            }
        }
    }

    /// <summary>
    /// Ignite a specific tile.
    /// </summary>
    public bool Ignite(int x, int y, FireIntensity intensity = FireIntensity.Spark)
    {
        if (!IsInBounds(x, y))
            return false;

        ref var tile = ref _tiles[x, y];

        if (tile.Material.Flammability <= 0)
            return false;
        if (tile.Fire.IsActive)
        {
            // Intensify existing fire
            tile.Fire.Intensify();
            return true;
        }

        tile.SetFire(intensity);
        _activeFires.Add(new Vector2I(x, y));
        return true;
    }

    /// <summary>
    /// Extinguish fire at a specific tile.
    /// </summary>
    public void Extinguish(int x, int y)
    {
        if (!IsInBounds(x, y))
            return;

        ref var tile = ref _tiles[x, y];
        tile.Extinguish();
        _activeFires.Remove(new Vector2I(x, y));
    }

    /// <summary>
    /// Extinguish all fires in a radius (e.g., from water).
    /// </summary>
    public void ExtinguishRadius(int centerX, int centerY, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                {
                    Extinguish(x, y);
                }
            }
        }
    }

    /// <summary>
    /// Get all positions with active fire.
    /// </summary>
    public IEnumerable<Vector2I> GetActiveFirePositions()
    {
        return _activeFires;
    }

    /// <summary>
    /// Check if fire is blocking a path (for AI).
    /// </summary>
    public bool IsFireBlocking(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;
        return _tiles[x, y].Fire.Intensity >= FireIntensity.Flame;
    }

    /// <summary>
    /// Get fire damage at position.
    /// </summary>
    public int GetFireDamage(int x, int y)
    {
        if (!IsInBounds(x, y))
            return 0;
        return _tiles[x, y].Fire.Data.DamagePerTurn;
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    /// <summary>
    /// Check if a tile is adjacent to water.
    /// </summary>
    private bool IsAdjacentToWater(int x, int y)
    {
        foreach (var offset in _neighbors)
        {
            int nx = x + offset.X;
            int ny = y + offset.Y;

            if (!IsInBounds(nx, ny))
                continue;

            if (_tiles[nx, ny].Material.Name == "Water")
                return true;
        }
        return false;
    }

    /// <summary>
    /// Count how many adjacent tiles are water.
    /// </summary>
    private int CountAdjacentWater(int x, int y)
    {
        int count = 0;
        foreach (var offset in _neighbors)
        {
            int nx = x + offset.X;
            int ny = y + offset.Y;

            if (!IsInBounds(nx, ny))
                continue;

            if (_tiles[nx, ny].Material.Name == "Water")
                count++;
        }
        return count;
    }
}
