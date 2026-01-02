using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Entities;
using NullAndVoid.Rendering;

namespace NullAndVoid.World;

/// <summary>
/// Generates a test battlefield with diverse terrain types for combat testing.
/// Features buildings (with ceilings), ruins (no ceiling), rivers, forests, and cover positions.
/// </summary>
public class BattlefieldGenerator
{
    public int Width { get; }
    public int Height { get; }

    private readonly DestructibleTile[,] _tiles;
    private readonly bool[,] _hasCeiling;
    private readonly Random _random = new();

    // Spawn point collections
    public Vector2I PlayerSpawn { get; private set; }
    public List<(Vector2I pos, EnemyArchetype type)> EnemySpawns { get; } = new();
    public List<Vector2I> WeaponSpawns { get; } = new();
    public List<Vector2I> AmmoSpawns { get; } = new();

    // Walkable positions for random spawning
    private readonly List<Vector2I> _walkablePositions = new();

    public BattlefieldGenerator(int width = 80, int height = 60)
    {
        Width = width;
        Height = height;
        _tiles = new DestructibleTile[width, height];
        _hasCeiling = new bool[width, height];
    }

    /// <summary>
    /// Generate the battlefield with all terrain zones.
    /// </summary>
    public void Generate()
    {
        // 1. Fill with grass base (outdoor, no ceiling)
        FillWithGrass();

        // 2. Create terrain zones
        CreateRiver();              // River runs top-center to bottom-center
        CreateBuildings();          // Buildings with ceilings
        CreateRuins();              // Ruined structures (no ceiling)
        CreateForest();             // Forest area
        CreateCoverPositions();     // Scattered debris and cover
        CreateDefensiveLine();      // Barricades

        // 3. Build walkable position list
        BuildWalkableList();

        // 4. Determine spawn points
        DetermineSpawnPoints();
    }

    /// <summary>
    /// Get the generated tiles for use with GameMap.
    /// </summary>
    public DestructibleTile[,] GetTiles() => _tiles;

    /// <summary>
    /// Get ceiling data for artillery calculations.
    /// </summary>
    public bool[,] GetCeilingData() => _hasCeiling;

    /// <summary>
    /// Check if a position has a ceiling.
    /// </summary>
    public bool HasCeiling(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;
        return _hasCeiling[x, y];
    }

    /// <summary>
    /// Check if a position is walkable.
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;
        return !_tiles[x, y].BlocksMovement;
    }

    #region Terrain Generation

    private void FillWithGrass()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _tiles[x, y] = DestructibleTile.GrassTile();
                _hasCeiling[x, y] = false;
            }
        }
    }

    private void CreateRiver()
    {
        // River runs from top to bottom in the middle-left area
        int riverX = Width / 3;
        int riverWidth = 4;

        for (int y = 0; y < Height; y++)
        {
            // Add some meandering
            int offset = (int)(Math.Sin(y * 0.2) * 2);

            for (int dx = 0; dx < riverWidth; dx++)
            {
                int x = riverX + offset + dx;
                if (x >= 0 && x < Width)
                {
                    // Deep water in center, shallow at edges
                    bool isDeep = dx == 1 || dx == 2;
                    _tiles[x, y] = GameMap.CreateWaterTile(isDeep);
                }
            }
        }

        // Create a bridge in the middle
        int bridgeY = Height / 2;
        for (int dx = 0; dx < riverWidth + 2; dx++)
        {
            int x = riverX - 1 + dx;
            if (x >= 0 && x < Width)
            {
                _tiles[x, bridgeY] = CreateBridgeTile();
                _tiles[x, bridgeY + 1] = CreateBridgeTile();
            }
        }
    }

    private void CreateBuildings()
    {
        // Building A - Top right (with ceiling)
        CreateBuilding(Width - 18, 2, 15, 12, "A", hasCeiling: true);

        // Building B - Middle right (with ceiling)
        CreateBuilding(Width - 16, Height / 2, 12, 10, "B", hasCeiling: true);
    }

    private void CreateBuilding(int startX, int startY, int width, int height, string id, bool hasCeiling)
    {
        // Walls
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                if (x < 0 || y < 0)
                    continue;

                bool isWall = x == startX || x == startX + width - 1 ||
                              y == startY || y == startY + height - 1;

                if (isWall)
                {
                    _tiles[x, y] = DestructibleTile.StoneWall();
                }
                else
                {
                    _tiles[x, y] = GameMap.CreateFloorTile();
                }

                // Set ceiling for interior
                _hasCeiling[x, y] = hasCeiling;
            }
        }

        // Add doors
        int doorY = startY + height / 2;
        if (startX >= 0 && doorY < Height)
        {
            _tiles[startX, doorY] = GameMap.CreateDoorTile(true);
        }

        // Add some interior cover
        int pillarX = startX + width / 3;
        int pillarY = startY + height / 3;
        if (pillarX < Width && pillarY < Height && pillarX >= 0 && pillarY >= 0)
        {
            _tiles[pillarX, pillarY] = CreatePillarTile();
            _tiles[pillarX + width / 3, pillarY] = CreatePillarTile();
            _tiles[pillarX, pillarY + height / 3] = CreatePillarTile();
            _tiles[pillarX + width / 3, pillarY + height / 3] = CreatePillarTile();
        }
    }

    private void CreateRuins()
    {
        // Ruins zone - Left side, no ceiling (exposed to sky)
        CreateRuinedStructure(3, Height / 2 - 5, 12, 10);

        // Second ruins - Bottom right
        CreateRuinedStructure(Width - 20, Height - 15, 10, 8);
    }

    private void CreateRuinedStructure(int startX, int startY, int width, int height)
    {
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                if (x < 0 || y < 0)
                    continue;

                bool isWall = x == startX || x == startX + width - 1 ||
                              y == startY || y == startY + height - 1;

                if (isWall)
                {
                    // Damaged walls with gaps
                    if (_random.Next(100) < 70)
                    {
                        _tiles[x, y] = CreateRuinWall();
                    }
                    else
                    {
                        _tiles[x, y] = CreateRubbleTile();
                    }
                }
                else
                {
                    // Interior floor with some debris
                    if (_random.Next(100) < 20)
                    {
                        _tiles[x, y] = CreateRubbleTile();
                    }
                    else
                    {
                        _tiles[x, y] = GameMap.CreateFloorTile();
                    }
                }

                // No ceiling - exposed to artillery
                _hasCeiling[x, y] = false;
            }
        }
    }

    private void CreateForest()
    {
        // Forest in bottom-left quadrant
        int forestStartX = 2;
        int forestStartY = Height - 18;
        int forestWidth = 20;
        int forestHeight = 15;

        for (int y = forestStartY; y < forestStartY + forestHeight && y < Height; y++)
        {
            for (int x = forestStartX; x < forestStartX + forestWidth && x < Width; x++)
            {
                if (x < 0 || y < 0)
                    continue;

                // Mix of trees and grass
                int roll = _random.Next(100);
                if (roll < 40)
                {
                    _tiles[x, y] = DestructibleTile.Tree(_random.Next(2) == 0);
                }
                else if (roll < 60)
                {
                    // Tall grass / scrub
                    _tiles[x, y] = CreateScrubTile();
                }
                // else keep grass
            }
        }
    }

    private void CreateCoverPositions()
    {
        // Scatter debris and cover in open areas
        // Central plaza area
        int centerX = Width / 2;
        int centerY = Height / 2;

        // Add some cover debris
        for (int i = 0; i < 15; i++)
        {
            int x = centerX - 10 + _random.Next(20);
            int y = centerY - 8 + _random.Next(16);

            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                if (!_tiles[x, y].BlocksMovement)
                {
                    _tiles[x, y] = CreateCoverDebris();
                }
            }
        }

        // Open field area (top-left) with scattered cover
        for (int i = 0; i < 10; i++)
        {
            int x = 5 + _random.Next(Width / 3 - 10);
            int y = 3 + _random.Next(Height / 3 - 5);

            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                if (!_tiles[x, y].BlocksMovement)
                {
                    _tiles[x, y] = CreateCoverDebris();
                }
            }
        }
    }

    private void CreateDefensiveLine()
    {
        // Barricade line in the bottom center
        int lineY = Height - 8;
        int lineStartX = Width / 3;
        int lineEndX = Width * 2 / 3;

        for (int x = lineStartX; x < lineEndX; x += 3)
        {
            if (x >= 0 && x < Width && lineY < Height)
            {
                _tiles[x, lineY] = CreateBarricade();
                if (x + 1 < Width)
                    _tiles[x + 1, lineY] = CreateBarricade();
            }
        }
    }

    #endregion

    #region Spawn Points

    private void BuildWalkableList()
    {
        _walkablePositions.Clear();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!_tiles[x, y].BlocksMovement)
                {
                    _walkablePositions.Add(new Vector2I(x, y));
                }
            }
        }
    }

    private void DetermineSpawnPoints()
    {
        // Player spawns in central plaza
        PlayerSpawn = new Vector2I(Width / 2, Height / 2);
        // Make sure spawn is walkable
        if (_tiles[PlayerSpawn.X, PlayerSpawn.Y].BlocksMovement)
        {
            _tiles[PlayerSpawn.X, PlayerSpawn.Y] = GameMap.CreateFloorTile();
        }

        // Enemy spawns by archetype and location
        SpawnEnemiesInZone(EnemyArchetype.ScoutDrone, 4, 5, 3, Width / 3 - 5, Height / 3 - 3);
        SpawnEnemiesInZone(EnemyArchetype.HeavySentry, 2, Width - 18, 5, 15, 10);
        SpawnEnemiesInZone(EnemyArchetype.PatrolGuard, 4, Width - 18, Height / 2, 15, 12);
        SpawnEnemiesInZone(EnemyArchetype.Hunter, 3, 3, Height / 2 - 5, 12, 10);
        SpawnEnemiesInZone(EnemyArchetype.Ambusher, 3, 2, Height - 18, 20, 15);
        SpawnEnemiesInZone(EnemyArchetype.SwarmBot, 6, Width / 2 - 10, Height / 2 - 5, 20, 10);
        SpawnEnemiesInZone(EnemyArchetype.Bomber, 3, Width / 3 - 5, 5, 10, Height - 10);

        // Weapon spawns - in ruins and defensive areas
        AddSpawnsInZone(WeaponSpawns, 4, Width - 20, Height - 15, 10, 8);
        AddSpawnsInZone(WeaponSpawns, 4, 3, Height / 2 - 5, 12, 10);

        // Ammo spawns - along defensive line and in buildings
        AddSpawnsInZone(AmmoSpawns, 6, Width / 3, Height - 10, Width / 3, 5);
        AddSpawnsInZone(AmmoSpawns, 3, Width - 16, Height / 2 + 2, 10, 6);
        AddSpawnsInZone(AmmoSpawns, 3, Width - 16, 4, 12, 8);
    }

    private void SpawnEnemiesInZone(EnemyArchetype archetype, int count, int x, int y, int w, int h)
    {
        for (int i = 0; i < count; i++)
        {
            var pos = FindWalkableInZone(x, y, w, h);
            if (pos.HasValue)
            {
                // Ensure minimum distance from player
                int dist = Math.Abs(pos.Value.X - PlayerSpawn.X) + Math.Abs(pos.Value.Y - PlayerSpawn.Y);
                if (dist >= 8)
                {
                    EnemySpawns.Add((pos.Value, archetype));
                }
            }
        }
    }

    private void AddSpawnsInZone(List<Vector2I> list, int count, int x, int y, int w, int h)
    {
        for (int i = 0; i < count; i++)
        {
            var pos = FindWalkableInZone(x, y, w, h);
            if (pos.HasValue)
            {
                list.Add(pos.Value);
            }
        }
    }

    private Vector2I? FindWalkableInZone(int startX, int startY, int width, int height)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = startX + _random.Next(width);
            int y = startY + _random.Next(height);

            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                if (!_tiles[x, y].BlocksMovement)
                {
                    return new Vector2I(x, y);
                }
            }
        }
        return null;
    }

    #endregion

    #region Tile Factories

    private static DestructibleTile CreateBridgeTile()
    {
        return DestructibleTile.Create(
            ASCIIChars.Bridge,
            ASCIIColors.Bridge,
            MaterialProperties.Wood,
            blocksMovement: false,
            blocksSight: false
        );
    }

    private static DestructibleTile CreatePillarTile()
    {
        return DestructibleTile.Create(
            ASCIIChars.Pillar,
            ASCIIColors.RuinStone,
            MaterialProperties.Stone,
            blocksMovement: true,
            blocksSight: true
        );
    }

    private static DestructibleTile CreateRuinWall()
    {
        var tile = DestructibleTile.StoneWall();
        tile.State = DestructionState.Damaged;
        tile.CurrentHP = tile.Material.MaxHitPoints / 2;
        tile.Character = ASCIIChars.WallDamaged;
        return tile;
    }

    private static DestructibleTile CreateRubbleTile()
    {
        return DestructibleTile.Create(
            ASCIIChars.Rubble,
            ASCIIColors.Rubble,
            MaterialProperties.Stone,
            blocksMovement: false,
            blocksSight: false
        );
    }

    private static DestructibleTile CreateScrubTile()
    {
        return DestructibleTile.Create(
            ASCIIChars.Scrub,
            ASCIIColors.Scrub,
            MaterialProperties.Vegetation,
            blocksMovement: false,
            blocksSight: false
        );
    }

    private static DestructibleTile CreateCoverDebris()
    {
        return DestructibleTile.Create(
            ASCIIChars.Boulder,
            ASCIIColors.Rock,
            MaterialProperties.Stone,
            blocksMovement: true,
            blocksSight: true
        );
    }

    private static DestructibleTile CreateBarricade()
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

    #endregion
}
