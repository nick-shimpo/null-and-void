using Godot;

namespace NullAndVoid.Rendering;

/// <summary>
/// Represents a single terrain tile with animated colors and optional character swapping.
/// Used for open-world terrain rendering with Brogue-style dancing colors.
/// </summary>
public struct TerrainTile
{
    public char Character;
    public char AltCharacter;           // For animation swap (e.g., ~ and ≈ for water)
    public DancingColor Foreground;
    public DancingColor Background;
    public bool BlocksMovement;
    public bool BlocksSight;
    public float CharSwapPeriod;        // Seconds between character swaps (0 = no swap)
    public float CharSwapTimer;
    public bool UseAltChar;             // Currently showing alt character

    /// <summary>
    /// Create a static terrain tile with no animation.
    /// </summary>
    public static TerrainTile Static(char character, Color foreground, Color background,
        bool blocksMovement = false, bool blocksSight = false)
    {
        return new TerrainTile
        {
            Character = character,
            AltCharacter = character,
            Foreground = DancingColor.Static(foreground),
            Background = DancingColor.Static(background),
            BlocksMovement = blocksMovement,
            BlocksSight = blocksSight,
            CharSwapPeriod = 0,
            CharSwapTimer = 0,
            UseAltChar = false
        };
    }

    /// <summary>
    /// Create a terrain tile with dancing foreground color.
    /// </summary>
    public static TerrainTile Dancing(char character, DancingColor foreground, Color background,
        bool blocksMovement = false, bool blocksSight = false)
    {
        return new TerrainTile
        {
            Character = character,
            AltCharacter = character,
            Foreground = foreground,
            Background = DancingColor.Static(background),
            BlocksMovement = blocksMovement,
            BlocksSight = blocksSight,
            CharSwapPeriod = 0,
            CharSwapTimer = 0,
            UseAltChar = false
        };
    }

    /// <summary>
    /// Create a terrain tile with character swapping animation (e.g., water).
    /// </summary>
    public static TerrainTile Animated(char character, char altCharacter,
        DancingColor foreground, Color background, float swapPeriod,
        bool blocksMovement = false, bool blocksSight = false)
    {
        var random = new System.Random();
        return new TerrainTile
        {
            Character = character,
            AltCharacter = altCharacter,
            Foreground = foreground,
            Background = DancingColor.Static(background),
            BlocksMovement = blocksMovement,
            BlocksSight = blocksSight,
            CharSwapPeriod = swapPeriod,
            CharSwapTimer = (float)random.NextDouble() * swapPeriod, // Randomize start
            UseAltChar = random.Next(2) == 0
        };
    }

    /// <summary>
    /// Update the tile's animations.
    /// </summary>
    public void Update(float delta)
    {
        Foreground.Update(delta);
        Background.Update(delta);

        if (CharSwapPeriod > 0)
        {
            CharSwapTimer -= delta;
            if (CharSwapTimer <= 0)
            {
                CharSwapTimer = CharSwapPeriod;
                UseAltChar = !UseAltChar;
            }
        }
    }

    /// <summary>
    /// Get the current character to display.
    /// </summary>
    public readonly char CurrentChar => UseAltChar ? AltCharacter : Character;

    /// <summary>
    /// Get the current foreground color.
    /// </summary>
    public readonly Color CurrentForeground => Foreground.CurrentColor;

    /// <summary>
    /// Get the current background color.
    /// </summary>
    public readonly Color CurrentBackground => Background.CurrentColor;

    // Factory methods for common terrain types

    /// <summary>
    /// Create a grass tile with subtle color animation.
    /// </summary>
    public static TerrainTile CreateGrass(Color baseColor)
    {
        return Dancing(
            ASCIIChars.Grass,
            DancingColor.GrassWave(baseColor),
            ASCIIColors.BgDark
        );
    }

    /// <summary>
    /// Create a tree tile with canopy sway animation.
    /// </summary>
    public static TerrainTile CreateTree(bool evergreen = false)
    {
        char treeChar = evergreen ? ASCIIChars.TreeEvergreen : ASCIIChars.TreeDeciduous;
        Color treeColor = evergreen ? ASCIIColors.TreePine : ASCIIColors.TreeCanopy;

        return new TerrainTile
        {
            Character = treeChar,
            AltCharacter = treeChar,
            Foreground = DancingColor.ForestCanopy(treeColor),
            Background = DancingColor.Static(ASCIIColors.BgDark),
            BlocksMovement = true,
            BlocksSight = true,
            CharSwapPeriod = 0,
            CharSwapTimer = 0,
            UseAltChar = false
        };
    }

    /// <summary>
    /// Create a water tile with ripple animation and character swapping.
    /// </summary>
    public static TerrainTile CreateWater(bool deep = false)
    {
        Color waterColor = deep ? ASCIIColors.DeepWater : ASCIIColors.WaterFg;
        Color bgColor = deep ? ASCIIColors.WaterDeepOcean : ASCIIColors.Water;

        return Animated(
            ASCIIChars.Water,
            ASCIIChars.DeepWater,
            DancingColor.Water(waterColor),
            bgColor,
            2.0f + (float)new System.Random().NextDouble() * 1.0f,
            blocksMovement: deep,
            blocksSight: false
        );
    }

    /// <summary>
    /// Create a mountain tile with heat haze effect.
    /// </summary>
    public static TerrainTile CreateMountain(bool peak = false)
    {
        char mountainChar = peak ? ASCIIChars.Mountain : ASCIIChars.Hill;
        Color mountainColor = peak ? ASCIIColors.MountainPeak : ASCIIColors.MountainSlope;

        return Dancing(
            mountainChar,
            DancingColor.HeatHaze(mountainColor),
            ASCIIColors.BgDark,
            blocksMovement: true,
            blocksSight: peak
        );
    }

    /// <summary>
    /// Create a marsh tile with murky animation.
    /// </summary>
    public static TerrainTile CreateMarsh()
    {
        return Dancing(
            ASCIIChars.Marsh,
            DancingColor.Marsh(ASCIIColors.MarshWater),
            ASCIIColors.WaterMurky
        );
    }

    /// <summary>
    /// Create a tech terminal with flicker effect.
    /// </summary>
    public static TerrainTile CreateTerminal()
    {
        return Dancing(
            ASCIIChars.Terminal,
            DancingColor.TechFlicker(ASCIIColors.TechTerminal),
            ASCIIColors.TechFloor
        );
    }

    /// <summary>
    /// Create a light source with pulse effect.
    /// </summary>
    public static TerrainTile CreateLight()
    {
        return Dancing(
            ASCIIChars.LightSource,
            DancingColor.Pulse(ASCIIColors.TechLight),
            ASCIIColors.BgDark
        );
    }
}
