using System;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Mountain Pass sandbox: Demonstrates mountain rendering, elevation changes,
/// rock formations, and mountain trails with heat haze effects.
/// </summary>
public class MountainPass : SandboxEnvironment
{
    public override string Name => "Mountain Pass";
    public override string Description => "A high mountain pass between two peaks. Demonstrates mountain elevation with snow-capped peaks, rocky terrain, winding trails, and subtle heat haze animation on sun-lit slopes.";
    public override int Width => 50;
    public override int Height => 30;

    private readonly Random _random = new();

    protected override void Generate()
    {
        // Fill with rocky base
        FillWithRock();

        // Create the two mountain peaks
        CreateMountains();

        // Carve the pass between them
        CarvePass();

        // Add the trail
        AddTrail();

        // Add rock features
        AddRockFeatures();
    }

    private void FillWithRock()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                SetTile(x, y, TerrainTile.Static(
                    ASCIIChars.Pebbles,
                    ASCIIColors.Rock,
                    ASCIIColors.BgDark
                ));
            }
        }
    }

    private void CreateMountains()
    {
        // Left mountain (larger)
        CreateMountainPeak(10, 8, 14);

        // Right mountain (slightly smaller)
        CreateMountainPeak(Width - 12, 10, 12);

        // Distant mountains in background (top)
        for (int x = 0; x < Width; x++)
        {
            int peakHeight = 3 + (int)(Math.Sin(x * 0.3) * 2) + (int)(Math.Sin(x * 0.7) * 1);
            for (int y = 0; y < peakHeight; y++)
            {
                Color color = y < 2 ? ASCIIColors.MountainPeak : ASCIIColors.MountainSlope;
                SetTile(x, y, TerrainTile.Dancing(
                    ASCIIChars.MountainRange,
                    DancingColor.HeatHaze(color),
                    ASCIIColors.BgDark,
                    blocksMovement: true,
                    blocksSight: true
                ));
            }
        }
    }

    private void CreateMountainPeak(int centerX, int centerY, int radius)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float dx = x - centerX;
                float dy = (y - centerY) * 1.5f; // Stretch vertically
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist < radius)
                {
                    float normalizedDist = dist / radius;

                    // Determine elevation zone
                    if (normalizedDist < 0.3f)
                    {
                        // Peak - snow capped
                        SetTile(x, y, TerrainTile.Dancing(
                            ASCIIChars.Mountain,
                            DancingColor.Subtle(ASCIIColors.MountainPeak),
                            ASCIIColors.BgDark,
                            blocksMovement: true,
                            blocksSight: true
                        ));
                    }
                    else if (normalizedDist < 0.6f)
                    {
                        // Upper slopes
                        SetTile(x, y, TerrainTile.Dancing(
                            ASCIIChars.Mountain,
                            DancingColor.HeatHaze(ASCIIColors.MountainSlope),
                            ASCIIColors.BgDark,
                            blocksMovement: true,
                            blocksSight: true
                        ));
                    }
                    else
                    {
                        // Lower slopes - hills
                        SetTile(x, y, TerrainTile.Dancing(
                            ASCIIChars.Hill,
                            DancingColor.HeatHaze(ASCIIColors.HillBrown),
                            ASCIIColors.BgDark,
                            blocksMovement: true
                        ));
                    }
                }
            }
        }
    }

    private void CarvePass()
    {
        // Carve a pass through the center
        int passY = Height / 2 + 2;
        int passWidth = 8;

        for (int y = passY - passWidth / 2; y <= passY + passWidth / 2; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // Vary the pass width
                int localWidth = passWidth / 2 + (int)(Math.Sin(x * 0.2) * 2);
                if (Math.Abs(y - passY) <= localWidth)
                {
                    var current = GetTile(x, y);
                    if (current.BlocksMovement)
                    {
                        // Clear to rocky floor
                        SetTile(x, y, TerrainTile.Static(
                            ASCIIChars.Pebbles,
                            ASCIIColors.Rock,
                            ASCIIColors.BgDark
                        ));
                    }
                }
            }
        }
    }

    private void AddTrail()
    {
        // Winding trail through the pass
        int passY = Height / 2 + 2;

        for (int x = 0; x < Width; x++)
        {
            int trailY = passY + (int)(Math.Sin(x * 0.15) * 2);

            SetTile(x, trailY, TerrainTile.Static(
                ASCIIChars.Trail,
                ASCIIColors.Trail,
                ASCIIColors.BgDark
            ));

            // Occasional wider sections
            if (_random.Next(5) == 0)
            {
                if (trailY > 0)
                    SetTile(x, trailY - 1, TerrainTile.Static(
                        ASCIIChars.Gravel,
                        ASCIIColors.Trail,
                        ASCIIColors.BgDark
                    ));
                if (trailY < Height - 1)
                    SetTile(x, trailY + 1, TerrainTile.Static(
                        ASCIIChars.Gravel,
                        ASCIIColors.Trail,
                        ASCIIColors.BgDark
                    ));
            }
        }
    }

    private void AddRockFeatures()
    {
        // Add boulders scattered around the pass
        for (int i = 0; i < 15; i++)
        {
            int x = _random.Next(Width);
            int y = _random.Next(Height);

            var tile = GetTile(x, y);
            if (!tile.BlocksMovement && tile.Character != ASCIIChars.Trail)
            {
                bool darkRock = _random.Next(3) == 0;
                SetTile(x, y, TerrainTile.Static(
                    darkRock ? ASCIIChars.DarkRock : ASCIIChars.Boulder,
                    darkRock ? ASCIIColors.RockDark : ASCIIColors.Rock,
                    ASCIIColors.BgDark,
                    blocksMovement: true
                ));
            }
        }
    }
}
