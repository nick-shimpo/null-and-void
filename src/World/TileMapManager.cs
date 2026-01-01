using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Core;
using NullAndVoid.Systems;

namespace NullAndVoid.World;

/// <summary>
/// Manages the game's tile map, including collision detection and tile data.
/// Renders tiles using direct draw calls for simplicity and efficiency.
/// </summary>
public partial class TileMapManager : Node2D
{
    private static TileMapManager? _instance;
    public static TileMapManager Instance => _instance ?? throw new InvalidOperationException("TileMapManager not initialized");

    [Export] public int TileSize { get; set; } = 32;
    [Export] public Vector2I MapSize { get; set; } = new Vector2I(50, 50);
    [Export] public bool EnableFOV { get; set; } = true;

    // Internal map data for collision checks
    private TileType[,]? _mapData;

    // FOV System
    public FOVSystem FOV { get; private set; } = new();

    public enum TileType
    {
        Empty,
        Floor,
        Wall
    }

    public override void _EnterTree()
    {
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public override void _Ready()
    {
        _mapData = new TileType[MapSize.X, MapSize.Y];
    }

    /// <summary>
    /// Update FOV from a position.
    /// </summary>
    public void UpdateFOV(Vector2I origin, int radius)
    {
        FOV.Calculate(origin, radius);
    }

    /// <summary>
    /// Checks if a position is walkable (not a wall or out of bounds).
    /// </summary>
    public bool IsWalkable(Vector2I position)
    {
        if (!IsInBounds(position))
            return false;

        if (_mapData == null)
            return false;

        return _mapData[position.X, position.Y] == TileType.Floor;
    }

    /// <summary>
    /// Checks if a position is within map bounds.
    /// </summary>
    public bool IsInBounds(Vector2I position)
    {
        return position.X >= 0 && position.X < MapSize.X &&
               position.Y >= 0 && position.Y < MapSize.Y;
    }

    /// <summary>
    /// Checks if a position allows line of sight (transparent).
    /// Walls block sight, floor allows it.
    /// </summary>
    public bool IsTransparent(Vector2I position)
    {
        if (!IsInBounds(position) || _mapData == null)
            return false;

        return _mapData[position.X, position.Y] != TileType.Wall;
    }

    /// <summary>
    /// Gets the tile type at a position.
    /// </summary>
    public TileType GetTileAt(Vector2I position)
    {
        if (!IsInBounds(position) || _mapData == null)
            return TileType.Empty;

        return _mapData[position.X, position.Y];
    }

    /// <summary>
    /// Sets a tile at the specified position.
    /// </summary>
    public void SetTile(Vector2I position, TileType type)
    {
        if (!IsInBounds(position) || _mapData == null)
            return;

        _mapData[position.X, position.Y] = type;
    }

    /// <summary>
    /// Clears the entire map.
    /// </summary>
    public void ClearMap()
    {
        if (_mapData == null)
            return;

        for (int x = 0; x < MapSize.X; x++)
        {
            for (int y = 0; y < MapSize.Y; y++)
            {
                _mapData[x, y] = TileType.Empty;
            }
        }
    }

    /// <summary>
    /// Loads map data from a 2D array.
    /// </summary>
    public void LoadMap(TileType[,] mapData)
    {
        MapSize = new Vector2I(mapData.GetLength(0), mapData.GetLength(1));
        _mapData = new TileType[MapSize.X, MapSize.Y];

        for (int x = 0; x < MapSize.X; x++)
        {
            for (int y = 0; y < MapSize.Y; y++)
            {
                _mapData[x, y] = mapData[x, y];
            }
        }
    }

    /// <summary>
    /// Converts grid position to world position.
    /// </summary>
    public Vector2 GridToWorld(Vector2I gridPos)
    {
        return new Vector2(
            gridPos.X * TileSize + TileSize / 2,
            gridPos.Y * TileSize + TileSize / 2
        );
    }

    /// <summary>
    /// Converts world position to grid position.
    /// </summary>
    public Vector2I WorldToGrid(Vector2 worldPos)
    {
        return new Vector2I(
            (int)(worldPos.X / TileSize),
            (int)(worldPos.Y / TileSize)
        );
    }

    /// <summary>
    /// Finds a random walkable position on the map.
    /// </summary>
    public Vector2I? FindRandomWalkablePosition()
    {
        if (_mapData == null)
            return null;

        var walkablePositions = new List<Vector2I>();

        for (int x = 0; x < MapSize.X; x++)
        {
            for (int y = 0; y < MapSize.Y; y++)
            {
                if (_mapData[x, y] == TileType.Floor)
                {
                    walkablePositions.Add(new Vector2I(x, y));
                }
            }
        }

        if (walkablePositions.Count == 0)
            return null;

        var random = new Random();
        return walkablePositions[random.Next(walkablePositions.Count)];
    }
}
