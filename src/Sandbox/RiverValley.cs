using System;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// River Valley sandbox: Demonstrates water flow, riverbanks, depth transitions,
/// and water animation effects with character swapping.
/// </summary>
public class RiverValley : SandboxEnvironment
{
    public override string Name => "River Valley";
    public override string Description => "A winding river cutting through hilly terrain with a bridge crossing. Demonstrates water depth gradients, riverbank transitions, and animated water ripples with character swapping.";
    public override int Width => 50;
    public override int Height => 30;

    private readonly Random _random = new();

    protected override void Generate()
    {
        // Fill with grass base
        FillWithGrass();

        // Add hills on the sides
        AddHills();

        // Carve the river
        CarveRiver();

        // Add a bridge
        AddBridge();

        // Add riverbank vegetation
        AddRiverbankDetails();
    }

    private void FillWithGrass()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                SetTile(x, y, TerrainTile.Dancing(
                    ASCIIChars.Grass,
                    DancingColor.GrassWave(ASCIIColors.Grass),
                    ASCIIColors.BgDark
                ));
            }
        }
    }

    private void AddHills()
    {
        // Hills on the left side
        for (int y = 0; y < Height; y++)
        {
            int hillWidth = 6 + (int)(Math.Sin(y * 0.3) * 2);
            for (int x = 0; x < hillWidth; x++)
            {
                float intensity = 1.0f - (x / (float)hillWidth);
                Color hillColor = ASCIIColors.HillBrown.Lerp(ASCIIColors.Grass, 1.0f - intensity);

                SetTile(x, y, TerrainTile.Dancing(
                    ASCIIChars.Hill,
                    DancingColor.HeatHaze(hillColor),
                    ASCIIColors.BgDark,
                    blocksMovement: true
                ));
            }
        }

        // Hills on the right side
        for (int y = 0; y < Height; y++)
        {
            int hillWidth = 5 + (int)(Math.Cos(y * 0.25) * 3);
            for (int x = 0; x < hillWidth; x++)
            {
                int tx = Width - 1 - x;
                float intensity = 1.0f - (x / (float)hillWidth);
                Color hillColor = ASCIIColors.HillBrown.Lerp(ASCIIColors.Grass, 1.0f - intensity);

                SetTile(tx, y, TerrainTile.Dancing(
                    ASCIIChars.Hill,
                    DancingColor.HeatHaze(hillColor),
                    ASCIIColors.BgDark,
                    blocksMovement: true
                ));
            }
        }
    }

    private void CarveRiver()
    {
        // River flows from top to bottom with a slight curve
        float riverCenter = Width / 2.0f;

        for (int y = 0; y < Height; y++)
        {
            // Meandering river
            riverCenter = (Width / 2.0f) + (float)Math.Sin(y * 0.15) * 6;

            int riverWidth = 6 + (int)(Math.Sin(y * 0.2) * 2);
            int halfWidth = riverWidth / 2;

            for (int x = 0; x < Width; x++)
            {
                float distFromCenter = Math.Abs(x - riverCenter);

                if (distFromCenter < halfWidth - 1)
                {
                    // Deep water (center)
                    SetTile(x, y, TerrainTile.CreateWater(deep: true));
                }
                else if (distFromCenter < halfWidth + 1)
                {
                    // Shallow water (edges)
                    SetTile(x, y, TerrainTile.CreateWater(deep: false));
                }
                else if (distFromCenter < halfWidth + 2)
                {
                    // Wet sand/mud at bank
                    SetTile(x, y, TerrainTile.Static(
                        ASCIIChars.Sand,
                        ASCIIColors.Dirt,
                        ASCIIColors.BgDark
                    ));
                }
            }
        }
    }

    private void AddBridge()
    {
        // Place a bridge across the river at the middle
        int bridgeY = Height / 2;
        float riverCenter = (Width / 2.0f) + (float)Math.Sin(bridgeY * 0.15) * 6;
        int riverWidth = 6 + (int)(Math.Sin(bridgeY * 0.2) * 2);

        int bridgeStart = (int)riverCenter - riverWidth / 2 - 2;
        int bridgeEnd = (int)riverCenter + riverWidth / 2 + 2;

        // Bridge deck
        for (int x = bridgeStart; x <= bridgeEnd; x++)
        {
            SetTile(x, bridgeY, TerrainTile.Static(
                ASCIIChars.Bridge,
                ASCIIColors.Bridge,
                ASCIIColors.Water
            ));
        }

        // Bridge railings
        SetTile(bridgeStart, bridgeY - 1, TerrainTile.Static(
            ASCIIChars.BoxTL, ASCIIColors.Bridge, ASCIIColors.BgDark));
        SetTile(bridgeEnd, bridgeY - 1, TerrainTile.Static(
            ASCIIChars.BoxTR, ASCIIColors.Bridge, ASCIIColors.BgDark));
        SetTile(bridgeStart, bridgeY + 1, TerrainTile.Static(
            ASCIIChars.BoxBL, ASCIIColors.Bridge, ASCIIColors.BgDark));
        SetTile(bridgeEnd, bridgeY + 1, TerrainTile.Static(
            ASCIIChars.BoxBR, ASCIIColors.Bridge, ASCIIColors.BgDark));
    }

    private void AddRiverbankDetails()
    {
        // Add reeds and tall grass along the riverbank
        for (int y = 0; y < Height; y++)
        {
            float riverCenter = (Width / 2.0f) + (float)Math.Sin(y * 0.15) * 6;
            int riverWidth = 6 + (int)(Math.Sin(y * 0.2) * 2);

            // Check positions near the river
            for (int dx = -2; dx <= 2; dx++)
            {
                int x = (int)riverCenter + riverWidth / 2 + 2 + dx;
                if (x >= 0 && x < Width && _random.Next(4) == 0)
                {
                    var tile = GetTile(x, y);
                    if (tile.Character == ASCIIChars.Grass || tile.Character == ASCIIChars.Sand)
                    {
                        SetTile(x, y, TerrainTile.Dancing(
                            ASCIIChars.TallGrass,
                            DancingColor.GrassWave(ASCIIColors.Reeds),
                            ASCIIColors.BgDark
                        ));
                    }
                }

                x = (int)riverCenter - riverWidth / 2 - 2 - dx;
                if (x >= 0 && x < Width && _random.Next(4) == 0)
                {
                    var tile = GetTile(x, y);
                    if (tile.Character == ASCIIChars.Grass || tile.Character == ASCIIChars.Sand)
                    {
                        SetTile(x, y, TerrainTile.Dancing(
                            ASCIIChars.TallGrass,
                            DancingColor.GrassWave(ASCIIColors.Reeds),
                            ASCIIColors.BgDark
                        ));
                    }
                }
            }
        }
    }
}
