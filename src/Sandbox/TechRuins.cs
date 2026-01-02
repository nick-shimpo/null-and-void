using System;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Tech Ruins sandbox: Demonstrates sci-fi structure rendering, tech elements
/// with animated effects (flickering terminals, pulsing lights), and
/// post-apocalyptic tech decay.
/// </summary>
public class TechRuins : SandboxEnvironment
{
    public override string Name => "Tech Ruins";
    public override string Description => "A destroyed technology facility with active terminals and generators. Demonstrates sci-fi structure elements, animated tech effects (flickering screens, pulsing lights), and the contrast between functional and destroyed technology.";
    public override int Width => 50;
    public override int Height => 30;

    private readonly Random _random = new();

    protected override void Generate()
    {
        // Fill with exterior rubble
        FillWithRubble();

        // Create the main facility structure
        CreateFacility();

        // Add interior rooms
        AddInteriorRooms();

        // Add tech elements
        AddTechElements();

        // Add damage and decay
        AddDamage();
    }

    private void FillWithRubble()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char rubbleChar = _random.Next(4) switch
                {
                    0 => ASCIIChars.RubbleHeavy,
                    1 => ASCIIChars.Rubble,
                    2 => ASCIIChars.Pebbles,
                    _ => ASCIIChars.Gravel
                };

                SetTile(x, y, TerrainTile.Static(
                    rubbleChar,
                    ASCIIColors.Rubble,
                    ASCIIColors.BgDark
                ));
            }
        }
    }

    private void CreateFacility()
    {
        int facilityX = 5;
        int facilityY = 3;
        int facilityW = Width - 10;
        int facilityH = Height - 6;

        // Outer walls (double-line box drawing)
        // Top wall
        SetTile(facilityX, facilityY, TerrainTile.Static(
            ASCIIChars.BoxTLD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
        SetTile(facilityX + facilityW - 1, facilityY, TerrainTile.Static(
            ASCIIChars.BoxTRD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));

        for (int x = facilityX + 1; x < facilityX + facilityW - 1; x++)
        {
            SetTile(x, facilityY, TerrainTile.Static(
                ASCIIChars.BoxHD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
        }

        // Bottom wall
        SetTile(facilityX, facilityY + facilityH - 1, TerrainTile.Static(
            ASCIIChars.BoxBLD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
        SetTile(facilityX + facilityW - 1, facilityY + facilityH - 1, TerrainTile.Static(
            ASCIIChars.BoxBRD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));

        for (int x = facilityX + 1; x < facilityX + facilityW - 1; x++)
        {
            SetTile(x, facilityY + facilityH - 1, TerrainTile.Static(
                ASCIIChars.BoxHD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
        }

        // Side walls
        for (int y = facilityY + 1; y < facilityY + facilityH - 1; y++)
        {
            SetTile(facilityX, y, TerrainTile.Static(
                ASCIIChars.BoxVD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
            SetTile(facilityX + facilityW - 1, y, TerrainTile.Static(
                ASCIIChars.BoxVD, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
        }

        // Interior floor
        for (int y = facilityY + 1; y < facilityY + facilityH - 1; y++)
        {
            for (int x = facilityX + 1; x < facilityX + facilityW - 1; x++)
            {
                SetTile(x, y, TerrainTile.Static(
                    ASCIIChars.ShadeFull,
                    ASCIIColors.TechFloor,
                    ASCIIColors.BgDark
                ));
            }
        }

        // Main entrance
        int entranceX = facilityX + facilityW / 2;
        SetTile(entranceX, facilityY + facilityH - 1, TerrainTile.Static(
            ASCIIChars.DoorOpen, ASCIIColors.TechWallDark, ASCIIColors.TechFloor));
        SetTile(entranceX - 1, facilityY + facilityH - 1, TerrainTile.Static(
            ASCIIChars.BoxTeeBd, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
        SetTile(entranceX + 1, facilityY + facilityH - 1, TerrainTile.Static(
            ASCIIChars.BoxTeeBd, ASCIIColors.TechWall, ASCIIColors.BgDark, true, true));
    }

    private void AddInteriorRooms()
    {
        int facilityX = 5;
        int facilityY = 3;

        // Control room (top-left)
        AddRoom(facilityX + 2, facilityY + 2, 12, 6, "Control Room");

        // Generator room (bottom-left)
        AddRoom(facilityX + 2, facilityY + 10, 10, 8, "Generator");

        // Storage room (right side)
        AddRoom(facilityX + 20, facilityY + 2, 14, 10, "Storage");
    }

    private void AddRoom(int x, int y, int width, int height, string roomType)
    {
        // Room walls (single-line)
        // Top
        SetTile(x, y, TerrainTile.Static(
            ASCIIChars.BoxTL, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
        SetTile(x + width - 1, y, TerrainTile.Static(
            ASCIIChars.BoxTR, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
        for (int wx = x + 1; wx < x + width - 1; wx++)
        {
            SetTile(wx, y, TerrainTile.Static(
                ASCIIChars.BoxH, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
        }

        // Bottom
        SetTile(x, y + height - 1, TerrainTile.Static(
            ASCIIChars.BoxBL, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
        SetTile(x + width - 1, y + height - 1, TerrainTile.Static(
            ASCIIChars.BoxBR, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
        for (int wx = x + 1; wx < x + width - 1; wx++)
        {
            SetTile(wx, y + height - 1, TerrainTile.Static(
                ASCIIChars.BoxH, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
        }

        // Sides
        for (int wy = y + 1; wy < y + height - 1; wy++)
        {
            SetTile(x, wy, TerrainTile.Static(
                ASCIIChars.BoxV, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
            SetTile(x + width - 1, wy, TerrainTile.Static(
                ASCIIChars.BoxV, ASCIIColors.TechWallDark, ASCIIColors.TechFloor, true));
        }

        // Door
        int doorX = x + width / 2;
        SetTile(doorX, y + height - 1, TerrainTile.Static(
            ASCIIChars.DoorOpen, ASCIIColors.TechWallDark, ASCIIColors.TechFloor));
    }

    private void AddTechElements()
    {
        int facilityX = 5;
        int facilityY = 3;

        // Control room terminals (row of screens)
        for (int i = 0; i < 8; i++)
        {
            int tx = facilityX + 3 + i;
            int ty = facilityY + 3;

            // Some terminals still work, some are dead
            if (_random.Next(3) == 0)
            {
                // Dead terminal
                SetTile(tx, ty, TerrainTile.Static(
                    ASCIIChars.Terminal,
                    ASCIIColors.TechWallDark,
                    ASCIIColors.TechFloor
                ));
            }
            else
            {
                // Working terminal with flicker
                SetTile(tx, ty, TerrainTile.CreateTerminal());
            }
        }

        // Generators in generator room
        SetTile(facilityX + 4, facilityY + 12, TerrainTile.Dancing(
            ASCIIChars.Generator,
            DancingColor.TechFlicker(ASCIIColors.TechGenerator),
            ASCIIColors.TechFloor
        ));
        SetTile(facilityX + 7, facilityY + 12, TerrainTile.Dancing(
            ASCIIChars.Generator,
            DancingColor.TechFlicker(ASCIIColors.TechGenerator),
            ASCIIColors.TechFloor
        ));
        SetTile(facilityX + 4, facilityY + 15, TerrainTile.Static(
            ASCIIChars.Generator,
            ASCIIColors.TechWallDark, // Dead generator
            ASCIIColors.TechFloor
        ));

        // Lights in storage room
        SetTile(facilityX + 25, facilityY + 4, TerrainTile.CreateLight());
        SetTile(facilityX + 30, facilityY + 4, TerrainTile.CreateLight());
        SetTile(facilityX + 25, facilityY + 8, TerrainTile.Static(
            ASCIIChars.LightSource,
            ASCIIColors.TechWallDark, // Broken light
            ASCIIColors.TechFloor
        ));

        // Antenna on exterior
        SetTile(facilityX - 2, facilityY + 5, TerrainTile.Dancing(
            ASCIIChars.Antenna,
            DancingColor.Pulse(ASCIIColors.TechTerminal),
            ASCIIColors.BgDark
        ));
    }

    private void AddDamage()
    {
        int facilityX = 5;
        int facilityY = 3;
        int facilityW = Width - 10;
        int facilityH = Height - 6;

        // Breach in the wall (top-right area)
        int breachX = facilityX + facilityW - 5;
        int breachY = facilityY;

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = 0; dy <= 2; dy++)
            {
                int x = breachX + dx;
                int y = breachY + dy;

                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    if (_random.Next(3) != 0)
                    {
                        SetTile(x, y, TerrainTile.Static(
                            ASCIIChars.RubbleHeavy,
                            ASCIIColors.Rubble,
                            ASCIIColors.BgDark
                        ));
                    }
                }
            }
        }

        // Scattered rubble inside
        for (int i = 0; i < 20; i++)
        {
            int x = facilityX + 1 + _random.Next(facilityW - 2);
            int y = facilityY + 1 + _random.Next(facilityH - 2);

            var tile = GetTile(x, y);
            if (tile.Character == ASCIIChars.ShadeFull)
            {
                SetTile(x, y, TerrainTile.Static(
                    ASCIIChars.Rubble,
                    ASCIIColors.Rubble,
                    ASCIIColors.TechFloor
                ));
            }
        }
    }
}
