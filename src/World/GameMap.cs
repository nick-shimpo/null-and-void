using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Rendering;
using NullAndVoid.Systems;

namespace NullAndVoid.World;

/// <summary>
/// Game map that integrates destructible terrain, fire simulation, and the existing game systems.
/// Replaces the simple TileType enum with full DestructibleTile support.
/// </summary>
public class GameMap
{
    public int Width { get; }
    public int Height { get; }

    private readonly DestructibleTile[,] _tiles;
    private bool[,]? _hasCeiling;

    /// <summary>
    /// Whether explicit ceiling data has been set.
    /// </summary>
    public bool HasExplicitCeilingData => _hasCeiling != null;
    public FOVSystem FOV { get; } = new();
    public FireSimulation FireSim { get; }
    public ExplosionSystem ExplosionSys { get; }

    // Events for game integration
    public event Action<Vector2I, DestructibleTile>? TileDestroyed;
    public event Action<Vector2I>? TileIgnited;
    public event Action<Vector2I, ExplosionData>? ExplosionTriggered;

    public GameMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new DestructibleTile[width, height];
        FireSim = new FireSimulation(_tiles, width, height);
        ExplosionSys = new ExplosionSystem(_tiles, width, height, FireSim);

        // Initialize with empty tiles
        Clear();
    }

    /// <summary>
    /// Clear map to default state.
    /// </summary>
    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _tiles[x, y] = DestructibleTile.Create(
                    ASCIIChars.Void,
                    ASCIIColors.BgDark,
                    MaterialProperties.Stone,
                    blocksMovement: true,
                    blocksSight: true
                );
            }
        }
    }

    /// <summary>
    /// Get the underlying tiles array for use by DestructionManager.
    /// </summary>
    public DestructibleTile[,] GetTilesArray() => _tiles;

    /// <summary>
    /// Get tile at position.
    /// </summary>
    public ref DestructibleTile GetTile(int x, int y)
    {
        if (IsInBounds(x, y))
            return ref _tiles[x, y];

        // Return a dummy for out of bounds
        throw new ArgumentOutOfRangeException($"Position ({x}, {y}) is out of bounds");
    }

    /// <summary>
    /// Get tile safely (returns copy for out of bounds).
    /// </summary>
    public DestructibleTile GetTileSafe(int x, int y)
    {
        if (IsInBounds(x, y))
            return _tiles[x, y];

        return DestructibleTile.Create(
            ASCIIChars.Void,
            ASCIIColors.BgDark,
            MaterialProperties.Stone,
            blocksMovement: true,
            blocksSight: true
        );
    }

    /// <summary>
    /// Set tile at position.
    /// </summary>
    public void SetTile(int x, int y, DestructibleTile tile)
    {
        if (IsInBounds(x, y))
            _tiles[x, y] = tile;
    }

    /// <summary>
    /// Check if position is within bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool IsInBounds(Vector2I pos) => IsInBounds(pos.X, pos.Y);

    /// <summary>
    /// Check if position is walkable.
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;
        var tile = _tiles[x, y];
        return !tile.BlocksMovement && tile.State != DestructionState.Destroyed;
    }

    public bool IsWalkable(Vector2I pos) => IsWalkable(pos.X, pos.Y);

    /// <summary>
    /// Check if position blocks sight.
    /// </summary>
    public bool BlocksSight(int x, int y)
    {
        if (!IsInBounds(x, y))
            return true;
        var tile = _tiles[x, y];
        return tile.BlocksSight && tile.State != DestructionState.Destroyed;
    }

    public bool BlocksSight(Vector2I pos) => BlocksSight(pos.X, pos.Y);

    /// <summary>
    /// Apply damage to a tile.
    /// </summary>
    public bool DamageTile(int x, int y, int damage, Destruction.DamageType damageType)
    {
        if (!IsInBounds(x, y))
            return false;

        ref var tile = ref _tiles[x, y];
        bool destroyed = tile.TakeDamage(damage, damageType);

        if (destroyed)
        {
            TileDestroyed?.Invoke(new Vector2I(x, y), tile);
        }

        if (tile.Fire.IsActive)
        {
            FireSim.RegisterFire(x, y);
            TileIgnited?.Invoke(new Vector2I(x, y));
        }

        return destroyed;
    }

    public bool DamageTile(Vector2I pos, int damage, Destruction.DamageType damageType)
        => DamageTile(pos.X, pos.Y, damage, damageType);

    /// <summary>
    /// Trigger an explosion at position.
    /// </summary>
    public List<ExplosionResult> TriggerExplosion(int x, int y, ExplosionData explosion)
    {
        ExplosionTriggered?.Invoke(new Vector2I(x, y), explosion);
        return ExplosionSys.Explode(x, y, explosion);
    }

    public List<ExplosionResult> TriggerExplosion(Vector2I pos, ExplosionData explosion)
        => TriggerExplosion(pos.X, pos.Y, explosion);

    /// <summary>
    /// Ignite a tile.
    /// </summary>
    public bool IgniteTile(int x, int y, FireIntensity intensity = FireIntensity.Spark)
    {
        if (!IsInBounds(x, y))
            return false;

        bool ignited = FireSim.Ignite(x, y, intensity);
        if (ignited)
        {
            TileIgnited?.Invoke(new Vector2I(x, y));
        }
        return ignited;
    }

    public bool IgniteTile(Vector2I pos, FireIntensity intensity = FireIntensity.Spark)
        => IgniteTile(pos.X, pos.Y, intensity);

    /// <summary>
    /// Extinguish fire at position.
    /// </summary>
    public void ExtinguishTile(int x, int y)
    {
        FireSim.Extinguish(x, y);
    }

    /// <summary>
    /// Update all tile animations.
    /// </summary>
    public void Update(float delta)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _tiles[x, y].Update(delta);
            }
        }

        ExplosionSys.UpdateVisuals(delta);
    }

    /// <summary>
    /// Process one turn of fire simulation.
    /// </summary>
    public void ProcessFireTurn()
    {
        FireSim.ProcessTurn();

        // Process chain reactions
        while (ExplosionSys.ProcessChainReactions())
        {
            // Keep processing until all chain reactions complete
        }
    }

    /// <summary>
    /// Update FOV from a position.
    /// </summary>
    public void UpdateFOV(Vector2I origin, int radius)
    {
        // Update FOV system's blocking check to use our tile data
        FOV.SetBlockingCheck((pos) => BlocksSight(pos));
        FOV.Calculate(origin, radius);
    }

    /// <summary>
    /// Find a random walkable position.
    /// </summary>
    public Vector2I? FindRandomWalkablePosition()
    {
        var walkable = new List<Vector2I>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (IsWalkable(x, y))
                {
                    walkable.Add(new Vector2I(x, y));
                }
            }
        }

        if (walkable.Count == 0)
            return null;

        var random = new Random();
        return walkable[random.Next(walkable.Count)];
    }

    /// <summary>
    /// Set explicit ceiling data for artillery calculations.
    /// </summary>
    public void SetCeilingData(bool[,] ceilingData)
    {
        _hasCeiling = ceilingData;
    }

    /// <summary>
    /// Check if position has a ceiling (blocks artillery fire).
    /// Uses explicit data if set, otherwise infers from wall count.
    /// </summary>
    public bool HasCeiling(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;

        // Use explicit ceiling data if available
        if (_hasCeiling != null)
            return _hasCeiling[x, y];

        // Fallback: infer from surrounding walls
        return InferCeilingFromWalls(x, y);
    }

    public bool HasCeiling(Vector2I pos) => HasCeiling(pos.X, pos.Y);

    /// <summary>
    /// Infer if position is "indoors" based on surrounding walls.
    /// Position is considered to have a ceiling if surrounded by 3+ walls.
    /// </summary>
    private bool InferCeilingFromWalls(int x, int y)
    {
        int adjacentWalls = 0;

        var neighbors = new (int dx, int dy)[]
        {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        foreach (var (dx, dy) in neighbors)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (!IsInBounds(nx, ny))
                continue;

            var tile = _tiles[nx, ny];
            if (tile.BlocksMovement && tile.State != DestructionState.Destroyed)
                adjacentWalls++;
        }

        return adjacentWalls >= 3;
    }

    /// <summary>
    /// Get fire damage at position (for entities standing in fire).
    /// </summary>
    public int GetFireDamageAt(int x, int y)
    {
        if (!IsInBounds(x, y))
            return 0;
        return _tiles[x, y].Fire.Data.DamagePerTurn;
    }

    public int GetFireDamageAt(Vector2I pos) => GetFireDamageAt(pos.X, pos.Y);

    /// <summary>
    /// Check if position has active fire.
    /// </summary>
    public bool HasFireAt(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;
        return _tiles[x, y].Fire.IsActive;
    }

    public bool HasFireAt(Vector2I pos) => HasFireAt(pos.X, pos.Y);

    /// <summary>
    /// Get active fire count.
    /// </summary>
    public int ActiveFireCount => FireSim.ActiveFireCount;

    /// <summary>
    /// Load from simple TileType array (backwards compatibility).
    /// </summary>
    public void LoadFromTileTypes(TileMapManager.TileType[,] mapData)
    {
        int w = Math.Min(mapData.GetLength(0), Width);
        int h = Math.Min(mapData.GetLength(1), Height);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                _tiles[x, y] = mapData[x, y] switch
                {
                    TileMapManager.TileType.Floor => CreateFloorTile(),
                    TileMapManager.TileType.Wall => CreateWallTile(),
                    _ => CreateVoidTile()
                };
            }
        }
    }

    // Factory methods for common tile types

    public static DestructibleTile CreateFloorTile()
    {
        var tile = DestructibleTile.Create(
            ASCIIChars.Floor,
            ASCIIColors.Floor,
            MaterialProperties.Stone,
            blocksMovement: false,
            blocksSight: false
        );
        return tile;
    }

    public static DestructibleTile CreateWallTile(bool wooden = false)
    {
        return wooden
            ? DestructibleTile.WoodWall()
            : DestructibleTile.StoneWall();
    }

    public static DestructibleTile CreateVoidTile()
    {
        return DestructibleTile.Create(
            ASCIIChars.Void,
            ASCIIColors.BgDark,
            MaterialProperties.Stone,
            blocksMovement: true,
            blocksSight: true
        );
    }

    public static DestructibleTile CreateDoorTile(bool open = false)
    {
        char doorChar = open ? ASCIIChars.DoorOpen : ASCIIChars.DoorClosed;
        return DestructibleTile.Create(
            doorChar,
            ASCIIColors.Door,
            MaterialProperties.Wood,
            blocksMovement: !open,
            blocksSight: !open,
            damagedChar: doorChar,
            destroyedChar: '/',
            debrisChar: ASCIIChars.Floor
        );
    }

    public static DestructibleTile CreateStairsTile(bool down = true)
    {
        char stairChar = down ? ASCIIChars.StairsDown : ASCIIChars.StairsUp;
        var tile = DestructibleTile.Create(
            stairChar,
            ASCIIColors.Stairs,
            MaterialProperties.Stone,
            blocksMovement: false,
            blocksSight: false
        );
        tile.Foreground = DancingColor.Pulse(ASCIIColors.Stairs);
        return tile;
    }

    public static DestructibleTile CreateWaterTile(bool deep = false)
    {
        char waterChar = deep ? ASCIIChars.DeepWater : ASCIIChars.Water;
        Color waterColor = deep ? ASCIIColors.DeepWater : ASCIIColors.WaterFg;

        var tile = DestructibleTile.Create(
            waterChar,
            waterColor,
            MaterialProperties.Water,
            blocksMovement: deep,
            blocksSight: false
        );
        tile.Foreground = DancingColor.Water(waterColor);
        return tile;
    }

    public static DestructibleTile CreateGrassTile()
    {
        return DestructibleTile.GrassTile();
    }

    public static DestructibleTile CreateTreeTile(bool evergreen = false)
    {
        return DestructibleTile.Tree(evergreen);
    }

    public static DestructibleTile CreateRuinWall()
    {
        var tile = DestructibleTile.StoneWall();
        tile.State = DestructionState.Damaged;
        tile.CurrentHP = tile.Material.MaxHitPoints / 2;
        tile.Character = ASCIIChars.WallDamaged;
        return tile;
    }

    public static DestructibleTile CreateCoverDebris()
    {
        return DestructibleTile.Create(
            ASCIIChars.Boulder,
            ASCIIColors.Rock,
            MaterialProperties.Stone,
            blocksMovement: true,
            blocksSight: true
        );
    }

    public static DestructibleTile CreateBarricade()
    {
        return DestructibleTile.Create(
            '=',
            ASCIIColors.TechWall,
            MaterialProperties.Metal,
            blocksMovement: true,
            blocksSight: true,
            damagedChar: '-',
            destroyedChar: '_'
        );
    }
}
