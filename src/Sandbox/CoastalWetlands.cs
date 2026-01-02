using System;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Coastal Wetlands sandbox: Demonstrates water/land transitions, marsh terrain,
/// tidal zones, and varied vegetation with multiple water animation types.
/// </summary>
public class CoastalWetlands : SandboxEnvironment
{
    public override string Name => "Coastal Wetlands";
    public override string Description => "A coastal marsh with tidal pools and reed beds. Demonstrates gradual water-to-land transitions, multiple water depths, marsh vegetation, and varied animation speeds for different water types.";
    public override int Width => 50;
    public override int Height => 30;

    private readonly Random _random = new();

    protected override void Generate()
    {
        // Fill with deep ocean on the left
        FillWithOcean();

        // Create the coastline gradient
        CreateCoastline();

        // Add tidal pools
        AddTidalPools();

        // Add reed beds and vegetation
        AddVegetation();

        // Add dry land features
        AddDryLandFeatures();
    }

    private void FillWithOcean()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // Default to deep ocean
                SetTile(x, y, TerrainTile.Animated(
                    ASCIIChars.DeepWater,
                    ASCIIChars.Water,
                    DancingColor.Water(ASCIIColors.WaterDeepOcean),
                    ASCIIColors.WaterDeepOcean,
                    3.0f + (float)_random.NextDouble() * 2.0f,
                    blocksMovement: true
                ));
            }
        }
    }

    private void CreateCoastline()
    {
        for (int y = 0; y < Height; y++)
        {
            // Coastline varies with noise
            float baseCoast = Width * 0.3f;
            float coastNoise = (float)(Math.Sin(y * 0.2) * 5 + Math.Sin(y * 0.5) * 3);
            float coastX = baseCoast + coastNoise;

            for (int x = 0; x < Width; x++)
            {
                float distFromCoast = x - coastX;

                if (distFromCoast < -8)
                {
                    // Deep ocean - already filled
                }
                else if (distFromCoast < -4)
                {
                    // Shallow ocean
                    SetTile(x, y, TerrainTile.Animated(
                        ASCIIChars.Water,
                        ASCIIChars.DeepWater,
                        DancingColor.Water(ASCIIColors.WaterShallow),
                        ASCIIColors.Water,
                        2.0f + (float)_random.NextDouble()
                    ));
                }
                else if (distFromCoast < 0)
                {
                    // Surf zone / tidal
                    SetTile(x, y, TerrainTile.Animated(
                        ASCIIChars.Water,
                        ASCIIChars.Marsh,
                        DancingColor.Water(ASCIIColors.WaterFg),
                        ASCIIColors.WaterShallow,
                        1.5f + (float)_random.NextDouble()
                    ));
                }
                else if (distFromCoast < 3)
                {
                    // Wet marsh
                    SetTile(x, y, TerrainTile.CreateMarsh());
                }
                else if (distFromCoast < 8)
                {
                    // Tall grass / reeds zone
                    SetTile(x, y, TerrainTile.Dancing(
                        ASCIIChars.TallGrass,
                        DancingColor.GrassWave(ASCIIColors.Reeds),
                        ASCIIColors.BgDark
                    ));
                }
                else
                {
                    // Dry grassland
                    char grassChar = _random.Next(3) == 0 ? ASCIIChars.Grass : ASCIIChars.Floor;
                    SetTile(x, y, TerrainTile.Dancing(
                        grassChar,
                        DancingColor.GrassWave(ASCIIColors.GrassDry),
                        ASCIIColors.BgDark
                    ));
                }
            }
        }
    }

    private void AddTidalPools()
    {
        // Add several tidal pools in the marsh/grass zone
        for (int i = 0; i < 5; i++)
        {
            int poolX = Width / 2 + _random.Next(Width / 3);
            int poolY = _random.Next(Height);
            int poolRadius = 2 + _random.Next(3);

            for (int dy = -poolRadius; dy <= poolRadius; dy++)
            {
                for (int dx = -poolRadius; dx <= poolRadius; dx++)
                {
                    int x = poolX + dx;
                    int y = poolY + dy;

                    if (x >= 0 && x < Width && y >= 0 && y < Height)
                    {
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        float noise = (float)_random.NextDouble() * 0.5f;

                        if (dist < poolRadius - 0.5f + noise)
                        {
                            var current = GetTile(x, y);
                            // Only create pool in land areas
                            if (!current.BlocksMovement && current.Character != ASCIIChars.Water)
                            {
                                SetTile(x, y, TerrainTile.Animated(
                                    ASCIIChars.Water,
                                    ASCIIChars.Marsh,
                                    DancingColor.Marsh(ASCIIColors.MarshWater),
                                    ASCIIColors.WaterMurky,
                                    2.5f + (float)_random.NextDouble()
                                ));
                            }
                        }
                    }
                }
            }
        }
    }

    private void AddVegetation()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = GetTile(x, y);

                // Add occasional reeds in shallow water
                if (tile.Character == ASCIIChars.Water && _random.Next(15) == 0)
                {
                    SetTile(x, y, TerrainTile.Dancing(
                        ASCIIChars.Reeds,
                        DancingColor.GrassWave(ASCIIColors.Reeds),
                        tile.CurrentBackground
                    ));
                }

                // Add scrub in the transition zones
                if (tile.Character == ASCIIChars.TallGrass && _random.Next(8) == 0)
                {
                    SetTile(x, y, TerrainTile.Dancing(
                        ASCIIChars.Scrub,
                        DancingColor.GrassWave(ASCIIColors.Scrub),
                        ASCIIColors.BgDark
                    ));
                }
            }
        }
    }

    private void AddDryLandFeatures()
    {
        // Add some sandy patches
        for (int i = 0; i < 8; i++)
        {
            int x = Width - 10 + _random.Next(8);
            int y = _random.Next(Height);

            SetTile(x, y, TerrainTile.Static(
                ASCIIChars.Sand,
                ASCIIColors.Sand,
                ASCIIColors.BgDark
            ));
        }

        // Add occasional driftwood (boulders)
        for (int i = 0; i < 3; i++)
        {
            int x = (int)(Width * 0.4f) + _random.Next(10);
            int y = _random.Next(Height);

            var tile = GetTile(x, y);
            if (!tile.BlocksMovement)
            {
                SetTile(x, y, TerrainTile.Static(
                    ASCIIChars.Boulder,
                    ASCIIColors.TreeDead,
                    ASCIIColors.BgDark,
                    blocksMovement: true
                ));
            }
        }
    }
}
