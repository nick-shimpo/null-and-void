using System;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Forest Clearing sandbox: Demonstrates tree rendering, vegetation layers,
/// and forest canopy animation effects.
/// </summary>
public class ForestClearing : SandboxEnvironment
{
    public override string Name => "Forest Clearing";
    public override string Description => "A peaceful clearing in a dense forest. Demonstrates tree density variation, vegetation layers from dense forest to open grass, and subtle canopy sway animation.";
    public override int Width => 50;
    public override int Height => 30;

    private readonly Random _random = new();

    protected override void Generate()
    {
        // Fill with dense forest first
        FillWithForest();

        // Carve out the clearing in the center
        CarveClearing();

        // Add transition zones (scrub, tall grass)
        AddVegetationLayers();

        // Add some features in the clearing
        AddClearingFeatures();
    }

    private void FillWithForest()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // Mix of deciduous and evergreen trees
                bool evergreen = _random.Next(3) == 0;
                SetTile(x, y, TerrainTile.CreateTree(evergreen));
            }
        }
    }

    private void CarveClearing()
    {
        int centerX = Width / 2;
        int centerY = Height / 2;
        int radiusX = 12;
        int radiusY = 8;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float dx = (x - centerX) / (float)radiusX;
                float dy = (y - centerY) / (float)radiusY;
                float dist = dx * dx + dy * dy;

                // Add some noise to make it organic
                float noise = (float)(_random.NextDouble() * 0.3);

                if (dist < 1.0f + noise)
                {
                    // Inside clearing - grass
                    var grassTile = TerrainTile.Dancing(
                        ASCIIChars.Floor,
                        DancingColor.GrassWave(ASCIIColors.Grass),
                        ASCIIColors.BgDark
                    );
                    SetTile(x, y, grassTile);
                }
            }
        }
    }

    private void AddVegetationLayers()
    {
        int centerX = Width / 2;
        int centerY = Height / 2;
        int radiusX = 12;
        int radiusY = 8;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float dx = (x - centerX) / (float)radiusX;
                float dy = (y - centerY) / (float)radiusY;
                float dist = dx * dx + dy * dy;
                float noise = (float)(_random.NextDouble() * 0.4);

                // Tall grass ring (just outside clearing)
                if (dist >= 0.8f + noise && dist < 1.1f + noise)
                {
                    var tile = TerrainTile.Dancing(
                        ASCIIChars.TallGrass,
                        DancingColor.GrassWave(ASCIIColors.GrassLush),
                        ASCIIColors.BgDark
                    );
                    SetTile(x, y, tile);
                }
                // Scrub ring (between tall grass and forest)
                else if (dist >= 1.1f + noise && dist < 1.4f + noise)
                {
                    var tile = TerrainTile.Dancing(
                        ASCIIChars.Scrub,
                        DancingColor.Subtle(ASCIIColors.Scrub),
                        ASCIIColors.BgDark
                    );
                    SetTile(x, y, tile);
                }
            }
        }
    }

    private void AddClearingFeatures()
    {
        int centerX = Width / 2;
        int centerY = Height / 2;

        // Add a boulder
        SetTile(centerX - 5, centerY + 2, TerrainTile.Static(
            ASCIIChars.Boulder,
            ASCIIColors.Rock,
            ASCIIColors.BgDark,
            blocksMovement: true
        ));

        // Add some pebbles near the boulder
        SetTile(centerX - 6, centerY + 2, TerrainTile.Static(
            ASCIIChars.Pebbles,
            ASCIIColors.Rock,
            ASCIIColors.BgDark
        ));
        SetTile(centerX - 5, centerY + 3, TerrainTile.Static(
            ASCIIChars.Pebbles,
            ASCIIColors.Rock,
            ASCIIColors.BgDark
        ));

        // Add a lone sapling
        SetTile(centerX + 4, centerY - 2, TerrainTile.Dancing(
            ASCIIChars.Sapling,
            DancingColor.ForestCanopy(ASCIIColors.GrassLush),
            ASCIIColors.BgDark
        ));

        // Add some flowers/grass variation
        for (int i = 0; i < 8; i++)
        {
            int fx = centerX + _random.Next(-8, 9);
            int fy = centerY + _random.Next(-5, 6);

            var tile = GetTile(fx, fy);
            if (tile.Character == ASCIIChars.Floor)
            {
                // Small grass tufts with slightly different color
                Color grassColor = _random.Next(2) == 0 ? ASCIIColors.GrassLush : ASCIIColors.GrassDry;
                SetTile(fx, fy, TerrainTile.Dancing(
                    ASCIIChars.Grass,
                    DancingColor.GrassWave(grassColor),
                    ASCIIColors.BgDark
                ));
            }
        }
    }
}
