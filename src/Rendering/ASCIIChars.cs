namespace NullAndVoid.Rendering;

/// <summary>
/// ASCII character vocabulary for consistent visual language.
/// Following Brogue conventions: letters for creatures, symbols for terrain/items.
/// </summary>
public static class ASCIIChars
{
    // Terrain - Basic
    public const char Floor = '.';
    public const char Wall = '#';
    public const char DoorClosed = '+';
    public const char DoorOpen = '/';
    public const char StairsDown = '>';
    public const char StairsUp = '<';
    public const char Water = '~';
    public const char DeepWater = '≈';
    public const char Trap = '^';
    public const char Fog = '░';
    public const char Void = ' ';

    // Terrain - Vegetation
    public const char Grass = ',';
    public const char TallGrass = '\'';
    public const char Scrub = ';';
    public const char TreeDeciduous = '♠';
    public const char TreeEvergreen = '♣';
    public const char TreeExotic = 'τ';
    public const char TreeDead = 'Ψ';
    public const char Sapling = '¥';

    // Terrain - Water & Marsh
    public const char Marsh = '░';
    public const char Reeds = '|';

    // Terrain - Elevation
    public const char Hill = '^';
    public const char Mountain = '▲';
    public const char MountainRange = '∩';
    public const char Valley = '_';

    // Terrain - Rocks & Earth
    public const char Boulder = '○';
    public const char Pebbles = '°';
    public const char DarkRock = '●';
    public const char Gravel = '∙';
    public const char Sand = ':';

    // Terrain - Paths & Roads
    public const char Road = '≡';
    public const char Trail = '·';
    public const char Bridge = '÷';

    // Structures - Ruins
    public const char Pillar = 'π';
    public const char Archway = 'Π';
    public const char BrokenColumn = '╥';
    public const char Rubble = '▒';
    public const char RubbleHeavy = '▓';
    public const char WallDamaged = '▓';
    public const char WallCrumbling = '▒';

    // Structures - Tech
    public const char Generator = 'Ω';
    public const char Terminal = '¤';
    public const char LightSource = '☼';
    public const char Antenna = '¶';

    // Creatures (letters only - Brogue convention)
    public const char Player = '@';
    public const char Drone = 'd';
    public const char DroneHeavy = 'D';
    public const char Sentry = 's';
    public const char SentryHeavy = 'S';
    public const char Guard = 'g';
    public const char GuardHeavy = 'G';
    public const char Boss = 'B';
    public const char Turret = 't';

    // Items (symbols - Brogue convention)
    public const char ItemModule = '*';
    public const char ItemConsumable = '!';
    public const char ItemDataChip = '?';
    public const char ItemCredits = '$';
    public const char ItemKey = '%';
    public const char ItemCorpse = '%';

    // Box Drawing - Single Line
    public const char BoxH = '─';
    public const char BoxV = '│';
    public const char BoxTL = '┌';
    public const char BoxTR = '┐';
    public const char BoxBL = '└';
    public const char BoxBR = '┘';
    public const char BoxTeeL = '├';
    public const char BoxTeeR = '┤';
    public const char BoxTeeT = '┬';
    public const char BoxTeeB = '┴';
    public const char BoxCross = '┼';

    // Box Drawing - Double Line
    public const char BoxHD = '═';
    public const char BoxVD = '║';
    public const char BoxTLD = '╔';
    public const char BoxTRD = '╗';
    public const char BoxBLD = '╚';
    public const char BoxBRD = '╝';
    public const char BoxTeeLd = '╠';
    public const char BoxTeeRd = '╣';
    public const char BoxTeeTd = '╦';
    public const char BoxTeeBd = '╩';
    public const char BoxCrossD = '╬';

    // Shading Blocks
    public const char ShadeLight = '░';
    public const char ShadeMedium = '▒';
    public const char ShadeDark = '▓';
    public const char ShadeFull = '█';

    // Bullets and Markers
    public const char BulletFilled = '●';
    public const char BulletEmpty = '○';
    public const char DiamondFilled = '◆';
    public const char DiamondEmpty = '◇';

    // Arrows
    public const char ArrowRight = '►';
    public const char ArrowLeft = '◄';
    public const char ArrowUp = '▲';
    public const char ArrowDown = '▼';
    public const char ArrowPointRight = '→';
    public const char ArrowPointLeft = '←';
    public const char ArrowPointUp = '↑';
    public const char ArrowPointDown = '↓';

    // Progress/Bars
    public const char BarFull = '█';
    public const char BarHalf = '▌';
    public const char BarEmpty = '░';

    // UI Elements
    public const char Cursor = '>';
    public const char Empty = '-';
    public const char Separator = '─';

    /// <summary>
    /// Get character for entity type.
    /// </summary>
    public static char GetEntityChar(string entityType)
    {
        return entityType.ToLower() switch
        {
            "player" => Player,
            "drone" => Drone,
            "heavy drone" or "heavydrone" => DroneHeavy,
            "sentry" => Sentry,
            "heavy sentry" or "heavysentry" => SentryHeavy,
            "guard" => Guard,
            "heavy guard" or "heavyguard" => GuardHeavy,
            "boss" => Boss,
            "turret" => Turret,
            _ => '?'
        };
    }

    /// <summary>
    /// Get character for terrain type.
    /// </summary>
    public static char GetTerrainChar(int terrainType)
    {
        return terrainType switch
        {
            0 => Void,    // Empty/void
            1 => Floor,   // Walkable floor
            2 => Wall,    // Wall
            3 => DoorClosed,
            4 => DoorOpen,
            5 => StairsDown,
            6 => StairsUp,
            7 => Water,
            8 => DeepWater,
            9 => Trap,
            _ => Fog
        };
    }

    /// <summary>
    /// Draw a horizontal line into a string builder.
    /// </summary>
    public static string HorizontalLine(int width, char left = BoxTL, char middle = BoxH, char right = BoxTR)
    {
        if (width < 2)
            return "";
        return $"{left}{new string(middle, width - 2)}{right}";
    }

    /// <summary>
    /// Create a progress bar string.
    /// </summary>
    public static string ProgressBar(float percent, int width)
    {
        int filled = (int)(percent * width);
        int empty = width - filled;
        return new string(BarFull, filled) + new string(BarEmpty, empty);
    }

    /// <summary>
    /// Format a slot display string.
    /// </summary>
    public static string FormatSlot(string label, string? itemName)
    {
        return itemName != null ? $"{label}: {itemName}" : $"{label}: [{Empty}]";
    }

    /// <summary>
    /// Get a water character that can alternate for animation.
    /// </summary>
    public static char GetWaterChar(bool alternate)
    {
        return alternate ? DeepWater : Water;
    }
}
