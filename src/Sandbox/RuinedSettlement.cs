using System;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Ruined Settlement sandbox: Demonstrates structure rendering, decay states,
/// ruins being reclaimed by nature, and mixed terrain interactions.
/// </summary>
public class RuinedSettlement : SandboxEnvironment
{
    public override string Name => "Ruined Settlement";
    public override string Description => "An abandoned settlement being reclaimed by nature. Demonstrates structure walls in various states of decay, rubble piles, overgrown ruins, and road remnants through the settlement.";
    public override int Width => 50;
    public override int Height => 30;

    private readonly Random _random = new();

    protected override void Generate()
    {
        // Fill with overgrown grass
        FillWithOvergrowth();

        // Add the main road
        AddRoad();

        // Add ruined buildings
        AddBuildings();

        // Add rubble and debris
        AddRubble();

        // Add nature reclaiming the edges
        AddOvergrowth();
    }

    private void FillWithOvergrowth()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char grassChar = _random.Next(3) switch
                {
                    0 => ASCIIChars.TallGrass,
                    1 => ASCIIChars.Scrub,
                    _ => ASCIIChars.Grass
                };

                SetTile(x, y, TerrainTile.Dancing(
                    grassChar,
                    DancingColor.GrassWave(ASCIIColors.GrassDry),
                    ASCIIColors.BgDark
                ));
            }
        }
    }

    private void AddRoad()
    {
        // Main road running through the center
        int roadY = Height / 2;
        int roadWidth = 3;

        for (int x = 0; x < Width; x++)
        {
            // Vary road slightly
            int offset = (int)(Math.Sin(x * 0.1) * 0.5);

            for (int dy = -roadWidth / 2; dy <= roadWidth / 2; dy++)
            {
                int y = roadY + dy + offset;
                if (y >= 0 && y < Height)
                {
                    // Road surface - damaged in places
                    char roadChar = _random.Next(10) == 0 ? ASCIIChars.Gravel : ASCIIChars.Road;
                    SetTile(x, y, TerrainTile.Static(
                        roadChar,
                        ASCIIColors.Road,
                        ASCIIColors.BgDark
                    ));
                }
            }
        }
    }

    private void AddBuildings()
    {
        // Building 1: Top left - mostly intact
        AddBuilding(5, 4, 10, 8, decay: 0.2f);

        // Building 2: Top right - heavily damaged
        AddBuilding(30, 3, 12, 7, decay: 0.6f);

        // Building 3: Bottom left - small, very ruined
        AddBuilding(8, Height - 12, 8, 6, decay: 0.8f);

        // Building 4: Bottom right - medium, partially intact
        AddBuilding(35, Height - 10, 10, 7, decay: 0.4f);
    }

    private void AddBuilding(int x, int y, int width, int height, float decay)
    {
        // Draw walls
        for (int wx = x; wx < x + width; wx++)
        {
            // Top wall
            if (_random.NextDouble() > decay)
                SetWallTile(wx, y, decay);

            // Bottom wall
            if (_random.NextDouble() > decay)
                SetWallTile(wx, y + height - 1, decay);
        }

        for (int wy = y; wy < y + height; wy++)
        {
            // Left wall
            if (_random.NextDouble() > decay)
                SetWallTile(x, wy, decay);

            // Right wall
            if (_random.NextDouble() > decay)
                SetWallTile(x + width - 1, wy, decay);
        }

        // Floor interior
        for (int fy = y + 1; fy < y + height - 1; fy++)
        {
            for (int fx = x + 1; fx < x + width - 1; fx++)
            {
                if (_random.NextDouble() > decay * 0.5)
                {
                    SetTile(fx, fy, TerrainTile.Static(
                        ASCIIChars.Floor,
                        ASCIIColors.RuinStone,
                        ASCIIColors.BgDark
                    ));
                }
            }
        }

        // Add a door opening (if not too decayed)
        if (decay < 0.7f)
        {
            int doorX = x + width / 2;
            int doorY = y + height - 1;
            SetTile(doorX, doorY, TerrainTile.Static(
                ASCIIChars.DoorOpen,
                ASCIIColors.Door,
                ASCIIColors.BgDark
            ));
        }

        // Add broken columns inside larger buildings
        if (width > 8 && height > 6 && decay > 0.3f)
        {
            SetTile(x + 2, y + 2, TerrainTile.Static(
                ASCIIChars.BrokenColumn,
                ASCIIColors.RuinStone,
                ASCIIColors.BgDark,
                blocksMovement: true
            ));
            SetTile(x + width - 3, y + 2, TerrainTile.Static(
                ASCIIChars.Pillar,
                ASCIIColors.RuinStone,
                ASCIIColors.BgDark,
                blocksMovement: true
            ));
        }
    }

    private void SetWallTile(int x, int y, float decay)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        char wallChar;
        Color wallColor;

        float damage = (float)_random.NextDouble() * decay;

        if (damage < 0.2f)
        {
            wallChar = ASCIIChars.Wall;
            wallColor = ASCIIColors.Wall;
        }
        else if (damage < 0.5f)
        {
            wallChar = ASCIIChars.WallDamaged;
            wallColor = ASCIIColors.RuinStone;
        }
        else
        {
            wallChar = ASCIIChars.WallCrumbling;
            wallColor = ASCIIColors.Rubble;
        }

        SetTile(x, y, TerrainTile.Static(
            wallChar,
            wallColor,
            ASCIIColors.BgDark,
            blocksMovement: true,
            blocksSight: wallChar == ASCIIChars.Wall
        ));
    }

    private void AddRubble()
    {
        // Scatter rubble around buildings
        for (int i = 0; i < 40; i++)
        {
            int x = _random.Next(Width);
            int y = _random.Next(Height);

            var tile = GetTile(x, y);
            if (tile.Character == ASCIIChars.Grass ||
                tile.Character == ASCIIChars.TallGrass ||
                tile.Character == ASCIIChars.Scrub)
            {
                char rubbleChar = _random.Next(3) == 0 ? ASCIIChars.RubbleHeavy : ASCIIChars.Rubble;
                SetTile(x, y, TerrainTile.Static(
                    rubbleChar,
                    ASCIIColors.Rubble,
                    ASCIIColors.BgDark
                ));
            }
        }
    }

    private void AddOvergrowth()
    {
        // Add vegetation growing on/near ruins
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = GetTile(x, y);

                // Check if near a wall and chance for moss
                if (tile.Character == ASCIIChars.Wall && _random.Next(5) == 0)
                {
                    SetTile(x, y, TerrainTile.Dancing(
                        ASCIIChars.Wall,
                        DancingColor.Subtle(ASCIIColors.RuinOvergrown),
                        ASCIIColors.BgDark,
                        blocksMovement: true,
                        blocksSight: true
                    ));
                }
            }
        }
    }
}
