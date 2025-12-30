using Godot;
using System;
using System.Collections.Generic;
using NullAndVoid.Core;

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

    // Tile colors
    [Export] public Color FloorColor { get; set; } = new Color(0.24f, 0.24f, 0.29f);
    [Export] public Color FloorAccentColor { get; set; } = new Color(0.30f, 0.30f, 0.35f);
    [Export] public Color WallColor { get; set; } = new Color(0.10f, 0.10f, 0.14f);
    [Export] public Color WallAccentColor { get; set; } = new Color(0.17f, 0.17f, 0.23f);

    // Internal map data for collision checks
    private TileType[,]? _mapData;

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

    public override void _Draw()
    {
        if (_mapData == null) return;

        for (int x = 0; x < MapSize.X; x++)
        {
            for (int y = 0; y < MapSize.Y; y++)
            {
                DrawTile(x, y, _mapData[x, y]);
            }
        }
    }

    private void DrawTile(int x, int y, TileType type)
    {
        var rect = new Rect2(x * TileSize, y * TileSize, TileSize, TileSize);

        switch (type)
        {
            case TileType.Floor:
                // Draw floor with subtle pattern
                DrawRect(rect, FloorColor);
                // Add small accent dots for texture
                var dotSize = 2;
                DrawRect(new Rect2(x * TileSize + 4, y * TileSize + 4, dotSize, dotSize), FloorAccentColor);
                DrawRect(new Rect2(x * TileSize + TileSize - 6, y * TileSize + TileSize - 6, dotSize, dotSize), FloorAccentColor);
                break;

            case TileType.Wall:
                // Draw wall with brick-like pattern
                DrawRect(rect, WallColor);
                // Add brick pattern
                var innerRect = new Rect2(x * TileSize + 2, y * TileSize + 2, TileSize - 4, TileSize - 4);
                DrawRect(innerRect, WallAccentColor);
                // Add cross pattern for brick effect
                DrawRect(new Rect2(x * TileSize + 2, y * TileSize + TileSize / 2 - 1, TileSize - 4, 2), WallColor);
                DrawRect(new Rect2(x * TileSize + TileSize / 2 - 1, y * TileSize + 2, 2, TileSize - 4), WallColor);
                break;

            case TileType.Empty:
                // Don't draw anything for empty tiles
                break;
        }
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

        QueueRedraw();
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

        // Trigger redraw to render all tiles
        QueueRedraw();
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
