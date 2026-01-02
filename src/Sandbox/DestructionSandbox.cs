using System;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Sandbox environment for testing destruction and fire mechanics.
/// Features multiple zones with different materials and destructible elements.
/// </summary>
public class DestructionSandbox
{
    public const int Width = 60;
    public const int Height = 35;

    public DestructibleTile[,] Tiles { get; private set; }
    public FireSimulation FireSim { get; private set; }
    public ExplosionSystem ExplosionSys { get; private set; }

    private readonly Random _random = new();

    public DestructionSandbox()
    {
        Tiles = new DestructibleTile[Width, Height];
        FireSim = new FireSimulation(Tiles, Width, Height);
        ExplosionSys = new ExplosionSystem(Tiles, Width, Height, FireSim);
    }

    public void Generate()
    {
        // Fill with base floor
        FillWithFloor();

        // Create distinct zones
        CreateForestZone(0, 0, 20, 15);
        CreateBuildingZone(20, 0, 20, 15);
        CreateTechZone(40, 0, 20, 15);

        CreateWaterZone(0, 15, 15, 8);
        CreateMixedZone(15, 15, 20, 8);
        CreateFuelDepotZone(35, 15, 25, 8);

        CreateWallTestZone(0, 23, 30, 12);
        CreateChainReactionZone(30, 23, 30, 12);
    }

    private void FillWithFloor()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Tiles[x, y] = DestructibleTile.GrassTile();
            }
        }
    }

    private void CreateForestZone(int startX, int startY, int width, int height)
    {
        // Dense forest for fire spread testing
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                if (_random.Next(100) < 60) // 60% tree coverage
                {
                    bool evergreen = _random.Next(3) == 0;
                    Tiles[x, y] = DestructibleTile.Tree(evergreen);
                }
                else
                {
                    // Grass (flammable)
                    Tiles[x, y] = DestructibleTile.GrassTile();
                }
            }
        }

        // Add zone label
        WriteLabel(startX + 1, startY, "FOREST", ASCIIColors.TreeCanopy);
    }

    private void CreateBuildingZone(int startX, int startY, int width, int height)
    {
        // Building with wood and stone walls
        int buildX = startX + 2;
        int buildY = startY + 2;
        int buildW = width - 4;
        int buildH = height - 4;

        // Outer walls (stone)
        for (int x = buildX; x < buildX + buildW; x++)
        {
            Tiles[x, buildY] = DestructibleTile.StoneWall();
            Tiles[x, buildY + buildH - 1] = DestructibleTile.StoneWall();
        }
        for (int y = buildY; y < buildY + buildH; y++)
        {
            Tiles[buildX, y] = DestructibleTile.StoneWall();
            Tiles[buildX + buildW - 1, y] = DestructibleTile.StoneWall();
        }

        // Inner walls (wood)
        int midX = buildX + buildW / 2;
        for (int y = buildY + 1; y < buildY + buildH - 2; y++)
        {
            Tiles[midX, y] = DestructibleTile.WoodWall();
        }

        // Door opening
        Tiles[midX, buildY + buildH - 2] = DestructibleTile.GrassTile();
        Tiles[buildX + buildW / 4, buildY] = DestructibleTile.GrassTile();

        // Interior floor
        for (int y = buildY + 1; y < buildY + buildH - 1; y++)
        {
            for (int x = buildX + 1; x < buildX + buildW - 1; x++)
            {
                if (Tiles[x, y].Material.Name == "Grass")
                {
                    Tiles[x, y] = DestructibleTile.Create(
                        ASCIIChars.Floor,
                        ASCIIColors.Floor,
                        MaterialProperties.Stone,
                        blocksMovement: false,
                        blocksSight: false
                    );
                }
            }
        }

        WriteLabel(startX + 1, startY, "BUILDING", ASCIIColors.Wall);
    }

    private void CreateTechZone(int startX, int startY, int width, int height)
    {
        // Tech facility with metal walls and terminals
        int facX = startX + 2;
        int facY = startY + 2;
        int facW = width - 4;
        int facH = height - 4;

        // Metal walls
        for (int x = facX; x < facX + facW; x++)
        {
            Tiles[x, facY] = DestructibleTile.MetalWall();
            Tiles[x, facY + facH - 1] = DestructibleTile.MetalWall();
        }
        for (int y = facY; y < facY + facH; y++)
        {
            Tiles[facX, y] = DestructibleTile.MetalWall();
            Tiles[facX + facW - 1, y] = DestructibleTile.MetalWall();
        }

        // Tech floor
        for (int y = facY + 1; y < facY + facH - 1; y++)
        {
            for (int x = facX + 1; x < facX + facW - 1; x++)
            {
                Tiles[x, y] = DestructibleTile.Create(
                    ASCIIChars.Floor,
                    ASCIIColors.TechFloor,
                    MaterialProperties.Metal,
                    blocksMovement: false,
                    blocksSight: false
                );
            }
        }

        // Terminals
        Tiles[facX + 2, facY + 1] = DestructibleTile.TechTerminal();
        Tiles[facX + 4, facY + 1] = DestructibleTile.TechTerminal();
        Tiles[facX + 6, facY + 1] = DestructibleTile.TechTerminal();

        // Entrance
        Tiles[facX + facW / 2, facY + facH - 1] = DestructibleTile.GrassTile();

        WriteLabel(startX + 1, startY, "TECH FAC", ASCIIColors.TechTerminal);
    }

    private void CreateWaterZone(int startX, int startY, int width, int height)
    {
        // Water area (extinguishes fire)
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                Tiles[x, y] = DestructibleTile.Create(
                    ASCIIChars.Water,
                    ASCIIColors.WaterFg,
                    MaterialProperties.Water,
                    blocksMovement: false,
                    blocksSight: false
                );
            }
        }

        WriteLabel(startX + 1, startY, "WATER", ASCIIColors.WaterFg);
    }

    private void CreateMixedZone(int startX, int startY, int width, int height)
    {
        // Mixed materials for testing
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                int pattern = (x + y) % 4;
                Tiles[x, y] = pattern switch
                {
                    0 => DestructibleTile.WoodWall(),
                    1 => DestructibleTile.Tree(),
                    2 => DestructibleTile.GrassTile(),
                    _ => DestructibleTile.StoneWall()
                };
            }
        }

        WriteLabel(startX + 1, startY, "MIXED", ASCIIColors.AlertWarning);
    }

    private void CreateFuelDepotZone(int startX, int startY, int width, int height)
    {
        // Fuel tanks for chain reaction testing
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                Tiles[x, y] = DestructibleTile.GrassTile();
            }
        }

        // Scattered fuel tanks
        for (int i = 0; i < 8; i++)
        {
            int fx = startX + 2 + _random.Next(width - 4);
            int fy = startY + 2 + _random.Next(height - 4);
            Tiles[fx, fy] = DestructibleTile.FuelTank();
        }

        WriteLabel(startX + 1, startY, "FUEL DEPOT", ASCIIColors.AlertDanger);
    }

    private void CreateWallTestZone(int startX, int startY, int width, int height)
    {
        // Different wall materials in a row for damage testing
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                Tiles[x, y] = DestructibleTile.GrassTile();
            }
        }

        // Wood wall row
        for (int x = startX + 2; x < startX + 8; x++)
        {
            Tiles[x, startY + 3] = DestructibleTile.WoodWall();
        }

        // Stone wall row
        for (int x = startX + 10; x < startX + 16; x++)
        {
            Tiles[x, startY + 3] = DestructibleTile.StoneWall();
        }

        // Metal wall row
        for (int x = startX + 18; x < startX + 24; x++)
        {
            Tiles[x, startY + 3] = DestructibleTile.MetalWall();
        }

        // Glass wall row
        for (int x = startX + 2; x < startX + 8; x++)
        {
            Tiles[x, startY + 7] = DestructibleTile.Create(
                ASCIIChars.ShadeLight,
                ASCIIColors.TextWhite,
                MaterialProperties.Glass,
                blocksMovement: true,
                blocksSight: false,
                damagedChar: '/',
                destroyedChar: '·'
            );
        }

        WriteLabel(startX + 1, startY, "WALL TEST", ASCIIColors.Wall);
        WriteLabel(startX + 2, startY + 2, "Wood", ASCIIColors.Door);
        WriteLabel(startX + 10, startY + 2, "Stone", ASCIIColors.Wall);
        WriteLabel(startX + 18, startY + 2, "Metal", ASCIIColors.TechWall);
        WriteLabel(startX + 2, startY + 6, "Glass", ASCIIColors.TextWhite);
    }

    private void CreateChainReactionZone(int startX, int startY, int width, int height)
    {
        // Clustered fuel tanks for dramatic chain reactions
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                Tiles[x, y] = DestructibleTile.GrassTile();
            }
        }

        // Create clusters of fuel tanks
        int[][] clusters = {
            new[] { startX + 5, startY + 3 },
            new[] { startX + 8, startY + 4 },
            new[] { startX + 6, startY + 6 },
            new[] { startX + 15, startY + 5 },
            new[] { startX + 18, startY + 4 },
            new[] { startX + 17, startY + 7 },
            new[] { startX + 20, startY + 6 }
        };

        foreach (var cluster in clusters)
        {
            int cx = cluster[0];
            int cy = cluster[1];

            // Place tank and some surrounding
            Tiles[cx, cy] = DestructibleTile.FuelTank();

            if (_random.Next(2) == 0 && IsInBounds(cx + 1, cy))
                Tiles[cx + 1, cy] = DestructibleTile.FuelTank();
            if (_random.Next(2) == 0 && IsInBounds(cx, cy + 1))
                Tiles[cx, cy + 1] = DestructibleTile.FuelTank();
        }

        WriteLabel(startX + 1, startY, "CHAIN REACT", ASCIIColors.AlertDanger);
    }

    private void WriteLabel(int x, int y, string text, Color color)
    {
        // Labels are just informational, don't affect tiles
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// Update all tile animations.
    /// </summary>
    public void Update(float delta)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Tiles[x, y].Update(delta);
            }
        }

        ExplosionSys.UpdateVisuals(delta);
    }

    /// <summary>
    /// Process one turn of fire simulation.
    /// </summary>
    public void ProcessFireTurn()
    {
        FireSim.ProcessTurn();
    }

    /// <summary>
    /// Trigger an explosion at position.
    /// </summary>
    public void TriggerExplosion(int x, int y, ExplosionData explosion)
    {
        ExplosionSys.Explode(x, y, explosion);
    }

    /// <summary>
    /// Ignite a tile.
    /// </summary>
    public void IgniteTile(int x, int y)
    {
        FireSim.Ignite(x, y, FireIntensity.Flame);
    }

    /// <summary>
    /// Reset the sandbox to initial state.
    /// </summary>
    public void Reset()
    {
        ExplosionSys.Clear();
        Generate();
    }
}
