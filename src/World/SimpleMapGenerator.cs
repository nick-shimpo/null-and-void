using System;
using Godot;

namespace NullAndVoid.World;

/// <summary>
/// Simple map generator for testing. Creates basic room layouts.
/// </summary>
public static class SimpleMapGenerator
{
    /// <summary>
    /// Generates a simple rectangular room with walls around the edges.
    /// </summary>
    public static TileMapManager.TileType[,] GenerateSimpleRoom(int width, int height)
    {
        var map = new TileMapManager.TileType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Walls on edges
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = TileMapManager.TileType.Wall;
                }
                else
                {
                    map[x, y] = TileMapManager.TileType.Floor;
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Generates a room with some random pillars/obstacles.
    /// </summary>
    public static TileMapManager.TileType[,] GenerateRoomWithPillars(int width, int height, int pillarCount)
    {
        var map = GenerateSimpleRoom(width, height);
        var random = new Random();

        for (int i = 0; i < pillarCount; i++)
        {
            // Place pillars in the interior (not on edges or center)
            int x = random.Next(2, width - 2);
            int y = random.Next(2, height - 2);

            // Don't place in the center (player spawn area)
            int centerX = width / 2;
            int centerY = height / 2;
            if (Math.Abs(x - centerX) <= 2 && Math.Abs(y - centerY) <= 2)
                continue;

            map[x, y] = TileMapManager.TileType.Wall;
        }

        return map;
    }

    /// <summary>
    /// Generates a simple dungeon with multiple connected rooms.
    /// </summary>
    public static TileMapManager.TileType[,] GenerateSimpleDungeon(int width, int height)
    {
        var map = new TileMapManager.TileType[width, height];
        var random = new Random();

        // Fill with walls
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x, y] = TileMapManager.TileType.Wall;
            }
        }

        // Create 3-5 rooms
        int roomCount = random.Next(3, 6);
        var rooms = new Godot.Collections.Array<Rect2I>();

        for (int i = 0; i < roomCount; i++)
        {
            int roomWidth = random.Next(5, 12);
            int roomHeight = random.Next(5, 10);
            int roomX = random.Next(1, width - roomWidth - 1);
            int roomY = random.Next(1, height - roomHeight - 1);

            var room = new Rect2I(roomX, roomY, roomWidth, roomHeight);
            rooms.Add(room);

            // Carve out the room
            for (int x = roomX; x < roomX + roomWidth; x++)
            {
                for (int y = roomY; y < roomY + roomHeight; y++)
                {
                    map[x, y] = TileMapManager.TileType.Floor;
                }
            }
        }

        // Connect rooms with corridors
        for (int i = 1; i < rooms.Count; i++)
        {
            var room1 = rooms[i - 1];
            var room2 = rooms[i];

            var center1 = room1.Position + room1.Size / 2;
            var center2 = room2.Position + room2.Size / 2;

            // Horizontal then vertical corridor
            if (random.Next(2) == 0)
            {
                CarveHorizontalCorridor(map, center1.X, center2.X, center1.Y);
                CarveVerticalCorridor(map, center1.Y, center2.Y, center2.X);
            }
            else
            {
                CarveVerticalCorridor(map, center1.Y, center2.Y, center1.X);
                CarveHorizontalCorridor(map, center1.X, center2.X, center2.Y);
            }
        }

        return map;
    }

    private static void CarveHorizontalCorridor(TileMapManager.TileType[,] map, int x1, int x2, int y)
    {
        int minX = Math.Min(x1, x2);
        int maxX = Math.Max(x1, x2);

        for (int x = minX; x <= maxX; x++)
        {
            if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1))
            {
                map[x, y] = TileMapManager.TileType.Floor;
            }
        }
    }

    private static void CarveVerticalCorridor(TileMapManager.TileType[,] map, int y1, int y2, int x)
    {
        int minY = Math.Min(y1, y2);
        int maxY = Math.Max(y1, y2);

        for (int y = minY; y <= maxY; y++)
        {
            if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1))
            {
                map[x, y] = TileMapManager.TileType.Floor;
            }
        }
    }

    /// <summary>
    /// Gets the center position of the first room (for player spawn).
    /// </summary>
    public static Vector2I GetSpawnPosition(TileMapManager.TileType[,] map)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        // Find the first floor tile near the center
        int centerX = width / 2;
        int centerY = height / 2;

        // Spiral outward from center to find a floor tile
        for (int radius = 0; radius < Math.Max(width, height); radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        if (map[x, y] == TileMapManager.TileType.Floor)
                        {
                            return new Vector2I(x, y);
                        }
                    }
                }
            }
        }

        // Fallback to center
        return new Vector2I(centerX, centerY);
    }
}
